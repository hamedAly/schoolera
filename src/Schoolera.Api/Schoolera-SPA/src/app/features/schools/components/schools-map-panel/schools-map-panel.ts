import {
  Component,
  DestroyRef,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Subject, switchMap } from 'rxjs';

import {
  PublicMapConfigurationDto,
  PublicSchoolMapPinDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { SchoolsApi } from '../../data-access/schools.api';
import {
  MapPinMarker,
  MapViewportBounds,
  SchoolsMapLeafletAdapter,
} from '../../data-access/schools-map-leaflet.adapter';
import { SchoolsSearchQuery } from '../../data-access/schools-search-query';

@Component({
  selector: 'se-schools-map-panel',
  imports: [RouterLink, TranslocoPipe],
  templateUrl: './schools-map-panel.html',
  styleUrl: './schools-map-panel.scss',
})
export class SchoolsMapPanel implements OnInit, OnDestroy {
  private readonly api = inject(SchoolsApi);
  private readonly adapter = inject(SchoolsMapLeafletAdapter);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<SchoolsSearchQuery>();

  readonly query = input.required<SchoolsSearchQuery>();
  readonly searchThisArea = output<MapViewportBounds>();
  readonly selectBranch = output<string | undefined>();

  @ViewChild('mapHost', { static: true }) private mapHost!: ElementRef<HTMLDivElement>;

  protected readonly mapLoading = signal(true);
  protected readonly pinsLoading = signal(false);
  protected readonly mapError = signal<string | null>(null);
  protected readonly statusMessage = signal<string | null>(null);
  protected readonly isTruncated = signal(false);
  protected readonly pins = signal<PublicSchoolMapPinDto[]>([]);
  protected readonly selectedPin = signal<PublicSchoolMapPinDto | null>(null);
  protected readonly config = signal<PublicMapConfigurationDto | null>(null);
  private lastBounds: MapViewportBounds | null = null;
  private mounted = false;
  private lastQueryKey = '';

  constructor() {
    effect(() => {
      const query = this.query();
      const key = JSON.stringify({
        view: query.view,
        northLatitude: query.northLatitude,
        southLatitude: query.southLatitude,
        eastLongitude: query.eastLongitude,
        westLongitude: query.westLongitude,
        latitude: query.latitude,
        longitude: query.longitude,
        search: query.search,
        countryId: query.countryId,
        governorateId: query.governorateId,
        cityId: query.cityId,
        districtId: query.districtId,
        stageId: query.stageId,
        gradeId: query.gradeId,
        schoolType: query.schoolType,
        genderType: query.genderType,
        curriculumIds: query.curriculumIds,
        facilityIds: query.facilityIds,
        minimumTuition: query.minimumTuition,
        maximumTuition: query.maximumTuition,
        admissionOpen: query.admissionOpen,
        selectedBranchId: query.selectedBranchId,
      });
      if (!this.mounted || key === this.lastQueryKey) {
        return;
      }
      this.lastQueryKey = key;
      this.reload$.next(query);
    });
  }

  ngOnInit(): void {
    this.reload$
      .pipe(
        switchMap((query) => {
          this.pinsLoading.set(true);
          this.mapError.set(null);
          return this.api.getMapPins(this.toMapPinQuery(query));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: async (result) => {
          this.pinsLoading.set(false);
          if (!result.succeeded || !result.data) {
            this.mapError.set(
              this.transloco.translate('schools.search.map.pinsError'),
            );
            this.statusMessage.set(this.transloco.translate('schools.search.map.pinsError'));
            return;
          }

          const pins = result.data.pins ?? [];
          this.pins.set(pins);
          this.isTruncated.set(!!result.data.isTruncated);
          this.statusMessage.set(
            result.data.isTruncated
              ? this.transloco.translate('schools.search.map.zoomIn')
              : this.transloco.translate('schools.search.map.pinsLoaded', {
                  count: pins.length,
                }),
          );

          const markers: MapPinMarker[] = pins
            .filter((pin) => pin.branchId && pin.latitude != null && pin.longitude != null)
            .map((pin) => ({
              branchId: pin.branchId!,
              schoolId: pin.schoolId!,
              schoolSlug: pin.schoolSlug ?? '',
              schoolName: pin.schoolName ?? '',
              branchName: pin.branchName ?? '',
              latitude: pin.latitude!,
              longitude: pin.longitude!,
              locationSummary: pin.locationSummary,
              isAdmissionOpen: pin.isAdmissionOpen,
              distanceKm: pin.distanceKm,
            }));

          if (this.mounted) {
            this.adapter.setPins(markers, (marker) => this.onPinSelected(marker.branchId));
          }

          const selectedId = this.query().selectedBranchId;
          if (selectedId) {
            const selected = pins.find((pin) => pin.branchId === selectedId) ?? null;
            this.selectedPin.set(selected);
          }
        },
        error: () => {
          this.pinsLoading.set(false);
          this.mapError.set(this.transloco.translate('schools.search.map.pinsError'));
        },
      });

    void this.bootstrapMap();
  }

  ngOnDestroy(): void {
    void this.adapter.destroy();
  }

  /** Called by parent when URL query for map search changes. */
  refreshFromQuery(query: SchoolsSearchQuery): void {
    this.reload$.next(query);
  }

  protected onSearchThisArea(): void {
    const bounds = this.adapter.getBounds() ?? this.lastBounds;
    if (!bounds) {
      return;
    }
    this.searchThisArea.emit(bounds);
  }

  protected closePreview(): void {
    this.selectedPin.set(null);
    this.selectBranch.emit(undefined);
  }

  private async bootstrapMap(): Promise<void> {
    this.mapLoading.set(true);
    this.mapError.set(null);

    this.api
      .getMapConfiguration()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: async (result) => {
          this.mapLoading.set(false);
          if (!result.succeeded || !result.data?.isAvailable || !result.data.tileUrlTemplate) {
            this.config.set(result.data ?? null);
            this.mapError.set(this.transloco.translate('schools.search.map.unavailable'));
            this.statusMessage.set(this.transloco.translate('schools.search.map.unavailable'));
            return;
          }

          this.config.set(result.data);
          try {
            await this.adapter.mount({
              container: this.mapHost.nativeElement,
              tileUrlTemplate: result.data.tileUrlTemplate,
              attributionText: result.data.attributionText ?? '',
              center: [
                result.data.defaultLatitude ?? 30.0444,
                result.data.defaultLongitude ?? 31.2357,
              ],
              zoom: result.data.defaultZoom ?? 11,
              minZoom: result.data.minZoom ?? 5,
              maxZoom: result.data.maxZoom ?? 18,
              onBoundsIdle: (bounds) => {
                this.lastBounds = bounds;
              },
            });
            this.mounted = true;
            this.reload$.next(this.query());
          } catch {
            this.mapError.set(this.transloco.translate('schools.search.map.providerFailed'));
            this.statusMessage.set(this.transloco.translate('schools.search.map.providerFailed'));
          }
        },
        error: () => {
          this.mapLoading.set(false);
          this.mapError.set(this.transloco.translate('schools.search.map.providerFailed'));
        },
      });
  }

  private onPinSelected(branchId: string): void {
    const pin = this.pins().find((item) => item.branchId === branchId) ?? null;
    this.selectedPin.set(pin);
    this.selectBranch.emit(branchId);
    if (pin) {
      this.statusMessage.set(
        this.transloco.translate('schools.search.map.selected', {
          school: pin.schoolName,
          branch: pin.branchName,
        }),
      );
    }
  }

  private toMapPinQuery(query: SchoolsSearchQuery) {
    const hasBounds =
      query.northLatitude != null &&
      query.southLatitude != null &&
      query.eastLongitude != null &&
      query.westLongitude != null;

    if (hasBounds) {
      return {
        ...query,
        northLatitude: query.northLatitude,
        southLatitude: query.southLatitude,
        eastLongitude: query.eastLongitude,
        westLongitude: query.westLongitude,
        centerLatitude: undefined,
        centerLongitude: undefined,
        radiusKm: undefined,
      };
    }

    if (query.latitude != null && query.longitude != null) {
      return {
        ...query,
        northLatitude: undefined,
        southLatitude: undefined,
        eastLongitude: undefined,
        westLongitude: undefined,
        centerLatitude: query.latitude,
        centerLongitude: query.longitude,
        radiusKm: 15,
      };
    }

    // Default Cairo viewport until the user searches this area.
    return {
      ...query,
      northLatitude: 30.15,
      southLatitude: 29.95,
      eastLongitude: 31.4,
      westLongitude: 31.1,
      centerLatitude: undefined,
      centerLongitude: undefined,
      radiusKm: undefined,
    };
  }
}

import { Injectable } from '@angular/core';

export interface MapViewportBounds {
  northLatitude: number;
  southLatitude: number;
  eastLongitude: number;
  westLongitude: number;
}

export interface MapPinMarker {
  branchId: string;
  schoolId: string;
  schoolSlug: string;
  schoolName: string;
  branchName: string;
  latitude: number;
  longitude: number;
  locationSummary?: string | null;
  isAdmissionOpen?: boolean;
  distanceKm?: number | null;
}

export interface LeafletMapAdapterOptions {
  container: HTMLElement;
  tileUrlTemplate: string;
  attributionText: string;
  center: [number, number];
  zoom: number;
  minZoom: number;
  maxZoom: number;
  onBoundsIdle?: (bounds: MapViewportBounds) => void;
  onPinSelect?: (pin: MapPinMarker) => void;
}

/**
 * Isolates Leaflet so tests can mock the adapter without live tile servers.
 */
@Injectable({ providedIn: 'root' })
export class SchoolsMapLeafletAdapter {
  private map: import('leaflet').Map | null = null;
  private cluster: import('leaflet').MarkerClusterGroup | null = null;
  private L: typeof import('leaflet') | null = null;
  private pinByBranchId = new Map<string, MapPinMarker>();

  async mount(options: LeafletMapAdapterOptions): Promise<void> {
    await this.destroy();

    const leaflet = await import('leaflet');
    await import('leaflet.markercluster');
    this.L = leaflet;

    // Fix default icon paths when bundling with Vite/Angular.
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    delete (leaflet.Icon.Default.prototype as any)._getIconUrl;
    leaflet.Icon.Default.mergeOptions({
      iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
      iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
      shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
    });

    this.map = leaflet.map(options.container, {
      center: options.center,
      zoom: options.zoom,
      minZoom: options.minZoom,
      maxZoom: options.maxZoom,
      scrollWheelZoom: true,
      attributionControl: true,
    });

    leaflet
      .tileLayer(options.tileUrlTemplate, {
        attribution: options.attributionText,
        maxZoom: options.maxZoom,
      })
      .addTo(this.map);

    this.cluster = leaflet.markerClusterGroup({
      showCoverageOnHover: false,
      maxClusterRadius: 48,
    });
    this.map.addLayer(this.cluster);

    if (options.onBoundsIdle) {
      const emit = () => {
        const bounds = this.getBounds();
        if (bounds) {
          options.onBoundsIdle?.(bounds);
        }
      };
      this.map.on('moveend', emit);
      this.map.on('zoomend', emit);
    }
  }

  setPins(pins: readonly MapPinMarker[], onSelect?: (pin: MapPinMarker) => void): void {
    if (!this.L || !this.cluster) {
      return;
    }

    this.cluster.clearLayers();
    this.pinByBranchId.clear();

    for (const pin of pins) {
      this.pinByBranchId.set(pin.branchId, pin);
      const marker = this.L.marker([pin.latitude, pin.longitude], {
        title: `${pin.schoolName} — ${pin.branchName}`,
        alt: `${pin.schoolName} ${pin.branchName}`,
      });
      marker.on('click', () => onSelect?.(pin));
      this.cluster.addLayer(marker);
    }
  }

  getBounds(): MapViewportBounds | null {
    if (!this.map) {
      return null;
    }
    const bounds = this.map.getBounds();
    return {
      northLatitude: roundCoord(bounds.getNorth()),
      southLatitude: roundCoord(bounds.getSouth()),
      eastLongitude: roundCoord(bounds.getEast()),
      westLongitude: roundCoord(bounds.getWest()),
    };
  }

  fitToPins(pins: readonly MapPinMarker[]): void {
    if (!this.map || !this.L || pins.length === 0) {
      return;
    }
    const latLngs = pins.map((pin) => this.L!.latLng(pin.latitude, pin.longitude));
    this.map.fitBounds(this.L.latLngBounds(latLngs), { padding: [40, 40], maxZoom: 14 });
  }

  async destroy(): Promise<void> {
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
    this.cluster = null;
    this.pinByBranchId.clear();
  }
}

function roundCoord(value: number): number {
  return Math.round(value * 1e5) / 1e5;
}

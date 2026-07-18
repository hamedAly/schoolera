import 'leaflet';

declare module 'leaflet' {
  interface MarkerClusterGroupOptions {
    showCoverageOnHover?: boolean;
    maxClusterRadius?: number;
  }

  class MarkerClusterGroup extends FeatureGroup {
    clearLayers(): this;
    addLayer(layer: Layer): this;
  }

  function markerClusterGroup(options?: MarkerClusterGroupOptions): MarkerClusterGroup;
}

declare module 'leaflet.markercluster';

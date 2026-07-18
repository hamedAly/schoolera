export const environment = {
  production: false,
  /**
   * Empty origin + Angular proxy (`proxy.conf.json`) forwards `/api` to the local API.
   * Do not put localhost into generated NSwag source; keep relative `/api/...` paths.
   */
  apiBaseUrl: '',
  /** Relative canonical paths during local development (empty origin uses browser origin). */
  publicSiteBaseUrl: 'http://localhost:5100',
  features: {
    favoritesEnabled: true,
    admissionsEnabled: true,
  },
} as const;

export const environment = {
  production: true,
  /**
   * Origin prefix for the NSwag-generated client.
   * Generated URLs already include `/api/...`, so production uses same-origin empty base.
   */
  apiBaseUrl: '',
  /**
   * Public site origin for canonical URLs and absolute Open Graph links.
   * Configure per deployment; do not hardcode unapproved production domains in code.
   */
  publicSiteBaseUrl: '',
  features: {
    favoritesEnabled: true,
    admissionsEnabled: true,
  },
} as const;

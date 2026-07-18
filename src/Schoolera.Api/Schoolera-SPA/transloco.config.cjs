/** @type {import('@jsverse/transloco-utils').TranslocoGlobalConfig} */
module.exports = {
  rootTranslationsPath: 'public/i18n/',
  langs: ['ar', 'en'],
  keysManager: {
    // Exclude generated NSwag client (large, no UI keys) to keep find responsive.
    input: [
      'src/app/features',
      'src/app/shared',
      'src/app/core/auth',
      'src/app/core/http',
      'src/app/core/i18n',
      'src/app/core/layout',
      'src/app/core/seo',
      'src/app/core/config',
      'src/app/testing',
    ],
    output: 'public/i18n',
  },
  scopePathMap: {
    auth: 'public/i18n/auth',
    schools: 'public/i18n/schools',
    admin: 'public/i18n/admin',
    onboarding: 'public/i18n/onboarding',
    portal: 'public/i18n/portal',
    parent: 'public/i18n/parent',
  },
};

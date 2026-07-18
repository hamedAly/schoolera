import { TranslocoService } from '@jsverse/transloco';

const PORTAL_ERROR_PREFIX = 'portal.errors.';

/**
 * Known schoolPortal.* codes mapped via {@link translatePortalErrorCodes}.
 * Includes Prompt 8 codes: accessDenied, branchScopeDenied, lastOwnerProtected,
 * invalidTeamRole, invalidBranchScope, ownershipTransferInvalid.
 */
export const PORTAL_ERROR_CODE_SUFFIXES = [
  'accessDenied',
  'branchScopeDenied',
  'lastOwnerProtected',
  'invalidTeamRole',
  'invalidBranchScope',
  'ownershipTransferInvalid',
] as const;

/** Maps stable API error codes to Transloco keys. Never parse localized message text. */
export function translatePortalErrorCodes(
  transloco: TranslocoService,
  errorCodes: string[] | null | undefined,
): string {
  if (!errorCodes?.length) {
    return transloco.translate('portal.errors.generic');
  }

  const messages = errorCodes.map((code) => {
    const key = `${PORTAL_ERROR_PREFIX}${code.replace(/^schoolPortal\./, '')}`;
    const translated = transloco.translate(key);
    return translated === key ? transloco.translate('portal.errors.generic') : translated;
  });

  return [...new Set(messages)].join(' ');
}

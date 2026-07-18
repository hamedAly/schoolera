const BLOCKED_PREFIXES = ['/auth/login', '/auth/register', '/unauthorized'] as const;

/**
 * Returns a safe in-app path for post-login redirects.
 * Rejects external URLs, protocol-relative paths, and auth-loop targets.
 */
export function sanitizeReturnUrl(
  url: string | null | undefined,
  fallback = '/',
): string {
  if (!url) {
    return fallback;
  }

  const trimmed = url.trim();
  if (!trimmed.startsWith('/') || trimmed.startsWith('//')) {
    return fallback;
  }

  const normalized = trimmed.split('?')[0] ?? trimmed;
  if (BLOCKED_PREFIXES.some((prefix) => normalized === prefix || normalized.startsWith(`${prefix}/`))) {
    return fallback;
  }

  return trimmed;
}

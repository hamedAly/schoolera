import { CurrentUserDto } from '../api-client/SwaggerClient.service';

export const SchooleraRoles = {
  Parent: 'Parent',
  SchoolOwner: 'SchoolOwner',
  SchoolAdmin: 'SchoolAdmin',
  AdmissionOfficer: 'AdmissionOfficer',
  FinanceOfficer: 'FinanceOfficer',
  ContentModerator: 'ContentModerator',
  PlatformAdmin: 'PlatformAdmin',
  SupportAgent: 'SupportAgent',
} as const;

/** Global Identity roles allowed into the School Portal route gate. */
export const SchoolPortalEntryRoles = [
  SchooleraRoles.SchoolOwner,
  SchooleraRoles.SchoolAdmin,
  SchooleraRoles.AdmissionOfficer,
  SchooleraRoles.FinanceOfficer,
  SchooleraRoles.ContentModerator,
] as const;

export type SchooleraRole = (typeof SchooleraRoles)[keyof typeof SchooleraRoles];

export interface AuthUser {
  id: string;
  displayName: string;
  email: string;
  phoneNumber: string | null;
  roles: readonly string[];
  accountStatus: string;
  preferredLanguage: string;
  postLoginDestination: string;
}

export function mapCurrentUserDto(dto: CurrentUserDto): AuthUser | null {
  if (!dto.id) {
    return null;
  }

  return {
    id: dto.id,
    displayName: dto.displayName ?? '',
    email: dto.email ?? '',
    phoneNumber: dto.phoneNumber ?? null,
    roles: dto.roles ?? [],
    accountStatus: dto.accountStatus ?? '',
    preferredLanguage: dto.preferredLanguage ?? 'ar',
    postLoginDestination: dto.postLoginDestination ?? '/',
  };
}

export function hasAnyRole(user: AuthUser | null, roles: readonly string[]): boolean {
  if (!user?.roles.length || !roles.length) {
    return false;
  }

  const allowed = new Set(roles.map((role) => role.toLowerCase()));
  return user.roles.some((role) => allowed.has(role.toLowerCase()));
}

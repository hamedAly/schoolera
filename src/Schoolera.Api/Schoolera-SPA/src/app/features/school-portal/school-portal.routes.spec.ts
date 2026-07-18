import { schoolPortalRoutes } from './school-portal.routes';

describe('schoolPortalRoutes', () => {
  it('defines applications routes under schoolId layout', () => {
    const schoolRoute = schoolPortalRoutes.find((route) => route.path === ':schoolId');
    const paths = schoolRoute?.children?.map((route) => route.path) ?? [];
    expect(paths).toEqual(
      expect.arrayContaining(['applications', 'applications/:applicationId']),
    );
  });

  it('includes expanded portal entry roles', () => {
    const entry = schoolPortalRoutes.find((route) => route.path === '');
    expect(entry?.data?.['roles']).toEqual(
      expect.arrayContaining([
        'SchoolOwner',
        'SchoolAdmin',
        'AdmissionOfficer',
        'FinanceOfficer',
        'ContentModerator',
      ]),
    );
  });

  it('attaches portal permission data on child routes', () => {
    const schoolRoute = schoolPortalRoutes.find((route) => route.path === ':schoolId');
    const children = schoolRoute?.children ?? [];
    const team = children.find((route) => route.path === 'team');
    const fees = children.find((route) => route.path === 'fees');
    const applications = children.find((route) => route.path === 'applications');

    expect(team?.data?.['portalPermissions']).toEqual(['canViewTeam', 'canManageTeam']);
    expect(fees?.data?.['portalPermissions']).toEqual(['canViewFees', 'canManageFees']);
    expect(applications?.data?.['portalPermission']).toBe('canViewApplications');
  });
});

import { parentRoutes } from './parent.routes';

describe('parentRoutes', () => {
  it('requires auth and Parent role on the layout route', () => {
    const layoutRoute = parentRoutes[0];
    expect(layoutRoute.canActivate).toEqual(
      expect.arrayContaining([expect.any(Function), expect.any(Function)]),
    );
    expect(layoutRoute.data?.['roles']).toEqual(['Parent']);
  });

  it('redirects empty path to dashboard', () => {
    const redirect = parentRoutes[0].children?.find((route) => route.path === '');
    expect(redirect?.redirectTo).toBe('dashboard');
  });

  it('defines parent and applications routes', () => {
    const paths = parentRoutes[0].children?.map((route) => route.path) ?? [];
    expect(paths).toEqual(
      expect.arrayContaining([
        'dashboard',
        'profile',
        'children',
        'children/new',
        'children/:childId/edit',
        'applications',
        'applications/new',
        'applications/:applicationId',
        'applications/:applicationId/edit',
        'applications/:applicationId/success',
        'notifications',
        'notification-preferences',
        'admission-subscriptions',
        'favorites',
        'support-tickets',
        'support-tickets/new',
        'support-tickets/:ticketId',
      ]),
    );
  });

  it('guards application editor routes with unsaved-change deactivation', () => {
    const create = parentRoutes[0].children?.find((route) => route.path === 'applications/new');
    const edit = parentRoutes[0].children?.find(
      (route) => route.path === 'applications/:applicationId/edit',
    );
    expect(create?.canDeactivate?.length).toBeGreaterThan(0);
    expect(edit?.canDeactivate?.length).toBeGreaterThan(0);
  });

  it('guards child create and edit routes with unsaved-change deactivation', () => {
    const create = parentRoutes[0].children?.find((route) => route.path === 'children/new');
    const edit = parentRoutes[0].children?.find(
      (route) => route.path === 'children/:childId/edit',
    );
    expect(create?.canDeactivate?.length).toBeGreaterThan(0);
    expect(edit?.canDeactivate?.length).toBeGreaterThan(0);
  });
});

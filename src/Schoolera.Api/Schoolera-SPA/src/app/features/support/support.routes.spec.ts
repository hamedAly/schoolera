import { supportRoutes } from './support.routes';

describe('supportRoutes', () => {
  it('requires auth and SupportAgent or PlatformAdmin on the layout route', () => {
    const layoutRoute = supportRoutes[0];
    expect(layoutRoute.canActivate).toEqual(
      expect.arrayContaining([expect.any(Function), expect.any(Function)]),
    );
    expect(layoutRoute.data?.['roles']).toEqual(['SupportAgent', 'PlatformAdmin']);
  });

  it('redirects empty path to tickets', () => {
    const redirect = supportRoutes[0].children?.find((route) => route.path === '');
    expect(redirect?.redirectTo).toBe('tickets');
  });

  it('defines ticket queue and detail routes', () => {
    const paths = supportRoutes[0].children?.map((route) => route.path) ?? [];
    expect(paths).toEqual(expect.arrayContaining(['tickets', 'tickets/:ticketId']));
  });
});

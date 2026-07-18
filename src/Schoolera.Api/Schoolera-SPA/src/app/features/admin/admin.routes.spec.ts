import { adminRoutes } from './admin.routes';

describe('adminRoutes', () => {
  it('defines admission applications routes', () => {
    const paths = adminRoutes[0].children?.map((route) => route.path) ?? [];
    expect(paths).toEqual(
      expect.arrayContaining(['applications', 'applications/:applicationId']),
    );
  });

  it('defines CMS and contact-request routes', () => {
    const paths = adminRoutes[0].children?.map((route) => route.path) ?? [];
    expect(paths).toEqual(
      expect.arrayContaining([
        'cms/pages',
        'cms/pages/new',
        'cms/pages/:pageId',
        'cms/faq',
        'cms/home',
        'contact-requests',
        'contact-requests/:requestId',
        'support-tickets',
        'support-tickets/:ticketId',
        'integrations',
        'integrations/new',
        'integrations/:id',
        'notification-templates',
        'notifications-ops',
      ]),
    );
  });
});

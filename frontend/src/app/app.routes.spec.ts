import { authGuard } from './core/guards/auth.guard';
import { routes } from './app.routes';

describe('app routes', () => {
  it('includes capture browsing routes inside the authenticated shell', () => {
    const shellRoute = routes.find(route => route.path === '');

    expect(shellRoute).toBeTruthy();
    expect(shellRoute?.canActivate).toContain(authGuard);
    expect(shellRoute?.children?.some(route => route.path === 'search')).toBe(true);
    expect(shellRoute?.children?.some(route => route.path === 'captures/new')).toBe(true);
    expect(shellRoute?.children?.some(route => route.path === 'captures')).toBe(true);
    expect(shellRoute?.children?.some(route => route.path === 'captures/:id')).toBe(true);
    expect(shellRoute?.children?.some(route => route.path === 'topics/:id')).toBe(true);
    expect(shellRoute?.children?.some(route => route.path === 'labels')).toBe(true);
    expect(shellRoute?.children?.some(route => route.path === 'settings')).toBe(true);
    expect(shellRoute?.children?.some(route => route.path === 'tags')).toBe(true);
    expect(shellRoute?.children?.some(route => route.path === '**')).toBe(true);
  });
});

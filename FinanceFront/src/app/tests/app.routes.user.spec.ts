import { AppRoutes } from "../Routes/app.routes.constants";
import { USER_ROUTES } from "../Routes/app.routes.user";


describe('USER_ROUTES', () => {
  it('should expose the client root protected route', () => {
    expect(USER_ROUTES.length).toBe(1);
    expect(USER_ROUTES[0].path).toBe(AppRoutes.ClientRoot);
    expect(USER_ROUTES[0].canActivate?.length).toBe(2);
  });

  it('should redirect the legacy finance route to watchlist', () => {
    const financeRoute = USER_ROUTES[0].children?.find((route) => route.path === AppRoutes.Finance);

    expect(financeRoute).toEqual(jasmine.objectContaining({
      path: AppRoutes.Finance,
      pathMatch: 'full',
      redirectTo: AppRoutes.Watchlist
    }));
  });

  it('should keep the separated watchlist, portfolio, analysis, history and simulation routes', () => {
    const children = USER_ROUTES[0].children ?? [];
    const childPaths = children.map((route) => route.path);

    expect(childPaths).toContain(AppRoutes.Watchlist);
    expect(childPaths).toContain(AppRoutes.Portfolio);
    expect(childPaths).toContain(AppRoutes.Analysis);
    expect(childPaths).toContain(AppRoutes.History);
    expect(childPaths).toContain(AppRoutes.Simulation);
  });

  it('should keep account profile and security nested under account', () => {
    const accountRoute = USER_ROUTES[0].children?.find((route) => route.path === AppRoutes.Account);
    const accountChildren = accountRoute?.children ?? [];

    expect(accountChildren).toEqual([
      jasmine.objectContaining({ path: '', pathMatch: 'full', redirectTo: AppRoutes.Profile }),
      jasmine.objectContaining({ path: AppRoutes.Profile }),
      jasmine.objectContaining({ path: AppRoutes.Security })
    ]);
  });
});

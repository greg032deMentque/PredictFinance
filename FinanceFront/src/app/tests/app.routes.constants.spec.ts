import { buildRoute, AuthPaths, AppRoutes, UserPaths, AdminPaths } from "../Routes/app.routes.constants";

describe('app.routes.constants', () => {
  it('buildRoute should join non-empty segments', () => {
    expect(buildRoute('client', '', 'watchlist')).toBe('client/watchlist');
  });

  it('AuthPaths should stay aligned with AppRoutes', () => {
    expect(AuthPaths.Login).toBe(AppRoutes.Login);
    expect(AuthPaths.Register).toBe(AppRoutes.Register);
    expect(AuthPaths.ForgotPassword).toBe(AppRoutes.ForgotPassword);
    expect(AuthPaths.ResetPassword).toBe(AppRoutes.ResetPassword);
  });

  it('UserPaths should expose the separated client pages', () => {
    expect(UserPaths.Watchlist).toBe('client/watchlist');
    expect(UserPaths.Portfolio).toBe('client/portfolio');
    expect(UserPaths.AnalysisEntry).toBe('client/analysis');
    expect(UserPaths.History).toBe('client/history');
    expect(UserPaths.Simulation).toBe('client/simulation');
    expect(UserPaths.Profile).toBe('client/account/profile');
    expect(UserPaths.Security).toBe('client/account/security');
  });

  it('UserPaths should preserve the legacy finance path as a distinct compatibility route', () => {
    expect(UserPaths.Finance).toBe('client/finance');
    expect(UserPaths.Finance).not.toBe(UserPaths.Watchlist);
  });

  it('AdminPaths should expose the governance routes', () => {
    expect(AdminPaths.InstrumentRegistry).toBe('admin/instrument-registry');
    expect(AdminPaths.PeaRegistry).toBe('admin/pea-registry');
    expect(AdminPaths.ScoringPolicy).toBe('admin/scoring-policy');
    expect(AdminPaths.ParameterDictionary).toBe('admin/parameter-dictionary');
    expect(AdminPaths.WordingVersions).toBe('admin/wording-versions');
    expect(AdminPaths.SnapshotAudit).toBe('admin/snapshot-audit');
    expect(AdminPaths.DataQuality).toBe('admin/data-quality');
  });
});

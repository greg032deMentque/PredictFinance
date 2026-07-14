import { Routes } from '@angular/router';
import { AuthGuard, ClientGuard } from '../guard';
import { AppRoutes, buildRoute } from './app.routes.constants';

export const USER_ROUTES: Routes = [
  {
    path: AppRoutes.ClientRoot,
    loadComponent: () => import('../components/client/client-layout-component/client-layout-component').then(m => m.ClientLayoutComponent),
    canActivate: [AuthGuard, ClientGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: AppRoutes.Dashboard },
      { path: AppRoutes.Dashboard, loadComponent: () => import('../components/client/client-dashboard/client-dashboard').then(m => m.ClientDashboardComponent) },
      { path: AppRoutes.Finance, pathMatch: 'full', redirectTo: AppRoutes.Watchlist },
      { path: AppRoutes.Notifications, loadComponent: () => import('../components/client/notifications/notifications.component').then(m => m.NotificationsComponent) },
      { path: AppRoutes.Watchlist, loadComponent: () => import('../components/client/pages/watchlist-page/watchlist-page.component').then(m => m.WatchlistPageComponent) },
      { path: AppRoutes.Portfolio, loadComponent: () => import('../components/client/pages/portfolio-page/portfolio-page.component').then(m => m.PortfolioPageComponent) },
      { path: buildRoute(AppRoutes.Portfolio, ':portfolioId'), loadComponent: () => import('../components/client/pages/portfolio-detail-page/portfolio-detail-page.component').then(m => m.PortfolioDetailPageComponent) },
      { path: AppRoutes.Analysis, loadComponent: () => import('../components/client/pages/analysis-entry-page/analysis-entry-page.component').then(m => m.AnalysisEntryPageComponent) },
      { path: buildRoute(AppRoutes.Analysis, ':analysisId'), loadComponent: () => import('../components/client/pages/analysis-detail-page/analysis-detail-page.component').then(m => m.AnalysisDetailPageComponent) },
      { path: buildRoute(AppRoutes.History, AppRoutes.Compare), loadComponent: () => import('../components/client/pages/snapshot-compare-page/snapshot-compare-page').then(m => m.SnapshotComparePageComponent) },
      { path: AppRoutes.History, loadComponent: () => import('../components/client/pages/history-page/history-page.component').then(m => m.HistoryPageComponent) },
      { path: AppRoutes.Simulation, loadComponent: () => import('../components/client/pages/simulation-page/simulation-page.component').then(m => m.SimulationPageComponent) },
      { path: AppRoutes.Patterns, loadComponent: () => import('../components/client/pages/pattern-explorer-page/pattern-explorer-page.component').then(m => m.PatternExplorerPageComponent) },
      { path: buildRoute(AppRoutes.Instruments, ':symbol'), loadComponent: () => import('../components/client/pages/instrument-detail-page/instrument-detail-page.component').then(m => m.InstrumentDetailPageComponent) },
      { path: buildRoute(AppRoutes.Parameters, ':parameterId'), loadComponent: () => import('../components/client/pages/parameter-detail-page/parameter-detail-page').then(m => m.ParameterDetailPageComponent) },
      { path: AppRoutes.Learn, loadComponent: () => import('../components/client/pages/learn-page/learn-page').then(m => m.LearnPageComponent) },
      { path: AppRoutes.Education, loadComponent: () => import('../components/client/pages/education-list-page/education-list-page.component').then(m => m.EducationListPageComponent) },
      { path: `${AppRoutes.Education}/:slug`, loadComponent: () => import('../components/client/pages/education-detail-page/education-detail-page.component').then(m => m.EducationDetailPageComponent) },
      { path: AppRoutes.Glossary, loadComponent: () => import('../components/client/pages/glossary-page/glossary-page.component').then(m => m.GlossaryPageComponent) },
      { path: AppRoutes.Help, loadComponent: () => import('../components/client/pages/help-center-page/help-center-page').then(m => m.HelpCenterPageComponent) },
      { path: AppRoutes.Contact, loadComponent: () => import('../components/client/pages/contact-page/contact-page.component').then(m => m.ContactPageComponent) },
      { path: AppRoutes.Onboarding, loadComponent: () => import('../components/client/pages/onboarding-page/onboarding-page').then(m => m.OnboardingPageComponent) },
      { path: AppRoutes.Legal, loadComponent: () => import('../components/client/legal/legal-center-page/legal-center-page').then(m => m.LegalCenterPageComponent) },
      {
        path: AppRoutes.Account,
        loadComponent: () => import('../components/client/account/shell/client-account-page.component').then(m => m.ClientAccountPageComponent),
        children: [
          { path: '', pathMatch: 'full', redirectTo: AppRoutes.Profile },
          { path: AppRoutes.Profile, loadComponent: () => import('../components/client/account/profile/account-profile.component').then(m => m.AccountProfileComponent) },
          { path: AppRoutes.Security, loadComponent: () => import('../components/client/account/security/account-security.component').then(m => m.AccountSecurityComponent) },
          { path: AppRoutes.Privacy, loadComponent: () => import('../components/client/account/privacy-consent/privacy-consent').then(m => m.PrivacyConsentComponent) },
          { path: AppRoutes.Alerts, loadComponent: () => import('../components/client/account/alert-preferences/alert-preferences').then(m => m.AlertPreferencesComponent) },
          { path: AppRoutes.DataExport, loadComponent: () => import('../components/client/account/data-export/data-export').then(m => m.DataExportComponent) },
          { path: AppRoutes.DeleteAccount, loadComponent: () => import('../components/client/account/delete-account/delete-account').then(m => m.DeleteAccountComponent) }
        ]
      }
    ]
  }
];

import { Routes } from '@angular/router';
import { AdminGuard, AuthGuard } from '../guard';
import { AppRoutes, buildRoute } from './app.routes.constants';

export const ADMIN_ROUTES: Routes = [
  {
    path: AppRoutes.AdminRoot,
    loadComponent: () => import('../components/admin/admin-layout-component/admin-layout-component').then(m => m.AdminLayoutComponent),
    canActivate: [AuthGuard, AdminGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: AppRoutes.Dashboard },
      { path: AppRoutes.Dashboard, loadComponent: () => import('../components/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: AppRoutes.Users, loadComponent: () => import('../components/admin/user/list/admin-users-list.component').then(m => m.AdminUsersListComponent) },
      { path: `${AppRoutes.Users}/${AppRoutes.Add}`, loadComponent: () => import('../components/admin/user/form/admin-user-form.component').then(m => m.AdminUserFormComponent) },
      { path: `${AppRoutes.Users}/${AppRoutes.Edit}/:id`, loadComponent: () => import('../components/admin/user/form/admin-user-form.component').then(m => m.AdminUserFormComponent) },
      { path: AppRoutes.InstrumentRegistry, loadComponent: () => import('../components/admin/governance/admin-instrument-registry/admin-instrument-registry.component').then(m => m.AdminInstrumentRegistryComponent) },
      { path: AppRoutes.PeaRegistry, loadComponent: () => import('../components/admin/governance/admin-pea-registry/admin-pea-registry.component').then(m => m.AdminPeaRegistryComponent) },
      { path: AppRoutes.ScoringPolicy, loadComponent: () => import('../components/admin/governance/admin-scoring-policy/admin-scoring-policy.component').then(m => m.AdminScoringPolicyComponent) },
      { path: AppRoutes.ParameterDictionary, loadComponent: () => import('../components/admin/governance/admin-parameter-dictionary/admin-parameter-dictionary.component').then(m => m.AdminParameterDictionaryComponent) },
      { path: `${AppRoutes.ParameterDictionary}/${AppRoutes.Detail}/:parameterId`, loadComponent: () => import('../components/admin/governance/admin-parameter-dictionary-detail/admin-parameter-dictionary-detail.component').then(m => m.AdminParameterDictionaryDetailComponent) },
      { path: AppRoutes.WordingVersions, loadComponent: () => import('../components/admin/governance/admin-wording-versions/admin-wording-versions.component').then(m => m.AdminWordingVersionsComponent) },
      { path: `${AppRoutes.WordingVersions}/${AppRoutes.Detail}/:wordingVersionId`, loadComponent: () => import('../components/admin/governance/admin-wording-version-detail/admin-wording-version-detail.component').then(m => m.AdminWordingVersionDetailComponent) },
      { path: AppRoutes.SnapshotAudit, loadComponent: () => import('../components/admin/governance/admin-snapshot-audit/admin-snapshot-audit.component').then(m => m.AdminSnapshotAuditComponent) },
      { path: `${AppRoutes.SnapshotAudit}/${AppRoutes.Detail}/:analysisRunId`, loadComponent: () => import('../components/admin/governance/admin-snapshot-audit-detail/admin-snapshot-audit-detail.component').then(m => m.AdminSnapshotAuditDetailComponent) },
      { path: `${AppRoutes.SnapshotAudit}/${AppRoutes.Compare}`, loadComponent: () => import('../components/admin/governance/admin-snapshot-audit-compare/admin-snapshot-audit-compare.component').then(m => m.AdminSnapshotAuditCompareComponent) },
      { path: AppRoutes.DataQuality, loadComponent: () => import('../components/admin/governance/admin-data-quality/admin-data-quality.component').then(m => m.AdminDataQualityComponent) },
      { path: buildRoute(AppRoutes.Kpi, AppRoutes.SignalQuality), loadComponent: () => import('../components/admin/kpi/admin-kpi-signal-quality/admin-kpi-signal-quality').then(m => m.AdminKpiSignalQualityComponent) },
      { path: buildRoute(AppRoutes.Kpi, AppRoutes.Engagement), loadComponent: () => import('../components/admin/kpi/admin-kpi-engagement/admin-kpi-engagement').then(m => m.AdminKpiEngagementComponent) },
      { path: AppRoutes.Education, loadComponent: () => import('../components/admin/education/admin-education-list/admin-education-list.component').then(m => m.AdminEducationListComponent) },
      { path: `${AppRoutes.Education}/${AppRoutes.Add}`, loadComponent: () => import('../components/admin/education/admin-education-form/admin-education-form.component').then(m => m.AdminEducationFormComponent) },
      { path: `${AppRoutes.Education}/${AppRoutes.Edit}/:id`, loadComponent: () => import('../components/admin/education/admin-education-form/admin-education-form.component').then(m => m.AdminEducationFormComponent) },
      { path: `${AppRoutes.Education}/:slug`, loadComponent: () => import('../components/admin/education/admin-education-detail/admin-education-detail.component').then(m => m.AdminEducationDetailComponent) },
      { path: AppRoutes.Glossary, loadComponent: () => import('../components/admin/glossary/admin-glossary/admin-glossary.component').then(m => m.AdminGlossaryComponent) },
      { path: AppRoutes.Faq, loadComponent: () => import('../components/admin/faq/admin-faq/admin-faq.component').then(m => m.AdminFaqComponent) },
      { path: AppRoutes.LegalCards, loadComponent: () => import('../components/admin/legal/admin-legal-cards/admin-legal-cards.component').then(m => m.AdminLegalCardsComponent) },
      { path: AppRoutes.LearnTopics, loadComponent: () => import('../components/admin/learn/admin-learn-topics/admin-learn-topics.component').then(m => m.AdminLearnTopicsComponent) }
    ]
  }
];

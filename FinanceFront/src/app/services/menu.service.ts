import { Injectable } from '@angular/core';
import { AppRoutes } from '../Routes/app.routes.constants';

export interface MenuAction {
  label: string;
  icon?: string;
  commands: readonly (string | number)[];
  exact?: boolean;
}

export interface MenuLink {
  label: string;
  icon?: string;
  commands: readonly (string | number)[];
  exact?: boolean;
  actions?: readonly MenuAction[];
}

export interface MenuBlock {
  label: string;
  icon?: string;
  links: readonly MenuLink[];
}

const absolute = (...segments: readonly string[]) => ['/', ...segments] as const;
const adminTo = (...segments: readonly string[]) => absolute(AppRoutes.AdminRoot, ...segments);
const clientTo = (...segments: readonly string[]) => absolute(AppRoutes.ClientRoot, ...segments);

@Injectable({ providedIn: 'root' })
export class MenuService {
  private link(
    label: string,
    commands: readonly (string | number)[],
    icon?: string,
    exact = true,
    actions?: readonly MenuAction[]
  ): MenuLink {
    return { label, icon, commands, exact, actions };
  }

  private block(label: string, links: readonly MenuLink[]): MenuBlock {
    return { label, links };
  }

  private readonly adminMenu: readonly MenuBlock[] = [
    this.block('Pilotage', [
      this.link('Vue d\'ensemble', adminTo(AppRoutes.Dashboard), 'bi-speedometer2'),
      this.link('Utilisateurs', adminTo(AppRoutes.Users), 'bi-people', true, [
        { label: 'Liste', icon: 'bi-list', commands: adminTo(AppRoutes.Users), exact: true },
        { label: 'Ajouter', icon: 'bi-plus-lg', commands: adminTo(AppRoutes.Users, AppRoutes.Add), exact: true }
      ])
    ]),
    this.block('Gouvernance', [
      this.link('Registre instruments', adminTo(AppRoutes.InstrumentRegistry), 'bi-building-gear'),
      this.link('Registre PEA', adminTo(AppRoutes.PeaRegistry), 'bi-patch-check'),
      this.link('Politique de scoring', adminTo(AppRoutes.ScoringPolicy), 'bi-sliders'),
      this.link('Dictionnaire paramètres', adminTo(AppRoutes.ParameterDictionary), 'bi-journal-text'),
      this.link('Versions wording', adminTo(AppRoutes.WordingVersions), 'bi-chat-left-text'),
      this.link('Audit snapshot', adminTo(AppRoutes.SnapshotAudit), 'bi-clock-history'),
      this.link('Qualité données', adminTo(AppRoutes.DataQuality), 'bi-database-check')
    ]),
    this.block('Contenu', [
      this.link('Éducation', adminTo(AppRoutes.Education), 'bi-book', true, [
        { label: 'Liste', icon: 'bi-list', commands: adminTo(AppRoutes.Education), exact: true },
        { label: 'Ajouter', icon: 'bi-plus-lg', commands: adminTo(AppRoutes.Education, AppRoutes.Add), exact: true }
      ]),
      this.link('Glossaire produits', adminTo(AppRoutes.Glossary), 'bi-card-text'),
      this.link('FAQ', adminTo(AppRoutes.Faq), 'bi-question-circle'),
      this.link('Légal', adminTo(AppRoutes.LegalCards), 'bi-shield-lock'),
      this.link('Sujets pédagogiques', adminTo(AppRoutes.LearnTopics), 'bi-mortarboard'),
      this.link('Contenus d\'analyse', adminTo(AppRoutes.AnalysisContent), 'bi-diagram-3')
    ])
  ];

  private readonly clientMenu: readonly MenuBlock[] = [
    this.block('Mon espace', [
      this.link('Accueil', clientTo(AppRoutes.Dashboard), 'bi-house-door'),
      this.link('Watchlist', clientTo(AppRoutes.Watchlist), 'bi-stars'),
      this.link('Portfolio', clientTo(AppRoutes.Portfolio), 'bi-briefcase'),
      this.link('Fiscalité', clientTo(AppRoutes.Tax), 'bi-percent'),
      this.link('Analyse', clientTo(AppRoutes.Analysis), 'bi-graph-up-arrow'),
      this.link('Patterns', clientTo(AppRoutes.Patterns), 'bi-diagram-3'),
      this.link('Screener', clientTo(AppRoutes.Screener), 'bi-funnel'),
      this.link('Historique', clientTo(AppRoutes.History), 'bi-clock-history'),
      this.link('Simulation', clientTo(AppRoutes.Simulation), 'bi-bar-chart-steps'),
      this.link('Notifications', clientTo(AppRoutes.Notifications), 'bi-bell'),
      this.link('Contact', clientTo(AppRoutes.Contact), 'bi-envelope-paper'),
      this.link('Compte', clientTo(AppRoutes.Account, AppRoutes.Profile), 'bi-person-gear')
    ]),
    this.block('Ressources', [
      this.link('Éducation', clientTo(AppRoutes.Education), 'bi-book'),
      this.link('Glossaire produits', clientTo(AppRoutes.Glossary), 'bi-card-text')
    ])
  ];

  getAdminMenu(): readonly MenuBlock[] {
    return this.adminMenu;
  }

  getClientMenu(): readonly MenuBlock[] {
    return this.clientMenu;
  }
}

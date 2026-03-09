import { Injectable } from '@angular/core';
import { AppRoutes } from '../Routes/app.routes.constants';

export type MenuAction = {
  label: string;
  icon?: string;
  commands: readonly (string | number)[];
  exact?: boolean;
};

export type MenuLink = {
  label: string;
  icon?: string;
  commands: readonly (string | number)[];
  exact?: boolean;
  actions?: readonly MenuAction[];
};

export type MenuBlock = {
  label: string;
  icon?: string;
  links: readonly MenuLink[];
};

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
    this.block('Administration', [
      this.link('Dashboard', adminTo(AppRoutes.Dashboard), 'bi-speedometer2'),
      this.link('Analyse IA', adminTo(AppRoutes.Analysis), 'bi-graph-up'),
      this.link('Utilisateurs', adminTo(AppRoutes.Users), 'bi-people', true, [
        {
          label: 'Liste',
          icon: 'bi-list',
          commands: adminTo(AppRoutes.Users),
          exact: true
        },
        {
          label: 'Ajouter',
          icon: 'bi-plus-lg',
          commands: adminTo(AppRoutes.Users, AppRoutes.Add),
          exact: true
        }
      ])
    ])
  ];

  private readonly clientMenu: readonly MenuBlock[] = [
    this.block('Mon espace', [
      this.link('Dashboard', clientTo(AppRoutes.Dashboard), 'bi-house'),
      this.link('Liste de mes valeurs', clientTo(AppRoutes.Finance), 'bi-graph-up-arrow')
    ])
  ];

  getAdminMenu(): readonly MenuBlock[] {
    return this.adminMenu;
  }

  getClientMenu(): readonly MenuBlock[] {
    return this.clientMenu;
  }
}

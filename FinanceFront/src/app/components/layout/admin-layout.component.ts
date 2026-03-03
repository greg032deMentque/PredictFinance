import { Component } from '@angular/core';
import { AdminPaths } from '../../Routes/app.routes.constants';
import { SharedShellComponent, ShellMenuBlock } from './shared-shell.component';

const ADMIN_MENU: ShellMenuBlock[] = [
  {
    label: 'Administration',
    icon: 'bi-shield-check',
    links: [
      { label: 'Dashboard', path: AdminPaths.Dashboard, icon: 'bi-speedometer2' },
      { label: 'Utilisateurs', path: AdminPaths.UsersList, icon: 'bi-people' },
      { label: 'Analyse IA', path: AdminPaths.Analysis, icon: 'bi-graph-up' }
    ]
  }
];

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [SharedShellComponent],
  template: `<app-shared-shell [menuBlocks]="menuBlocks" title="PredictFinance Admin"></app-shared-shell>`
})
export class AdminLayoutComponent {
  readonly menuBlocks = ADMIN_MENU;
}

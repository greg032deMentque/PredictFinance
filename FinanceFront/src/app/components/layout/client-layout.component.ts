import { Component } from '@angular/core';
import { UserPaths } from '../../Routes/app.routes.constants';
import { SharedShellComponent, ShellMenuBlock } from './shared-shell.component';

const CLIENT_MENU: ShellMenuBlock[] = [
  {
    label: 'Mon espace',
    icon: 'bi-person-circle',
    links: [
      { label: 'Dashboard', path: UserPaths.Dashboard, icon: 'bi-speedometer2' },
      { label: 'Analyse valeur', path: UserPaths.Finance, icon: 'bi-graph-up-arrow' }
    ]
  }
];

@Component({
  selector: 'app-client-layout',
  standalone: true,
  imports: [SharedShellComponent],
  template: `<app-shared-shell [menuBlocks]="menuBlocks" title="PredictFinance Client"></app-shared-shell>`
})
export class ClientLayoutComponent {
  readonly menuBlocks = CLIENT_MENU;
}

import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AppRoutes } from '../../../../Routes/app.routes.constants';

@Component({
  selector: 'app-client-account-page',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './client-account-page.component.html',
  styleUrl: './client-account-page.component.scss'
})
export class ClientAccountPageComponent {
  protected readonly profileRoute = AppRoutes.Profile;
  protected readonly securityRoute = AppRoutes.Security;
  protected readonly privacyRoute = AppRoutes.Privacy;
  protected readonly alertsRoute = AppRoutes.Alerts;
  protected readonly dataExportRoute = AppRoutes.DataExport;
  protected readonly deleteAccountRoute = AppRoutes.DeleteAccount;
}

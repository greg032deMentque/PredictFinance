import { Component, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthStore } from '../../../core/auth.store';
import { AllModule } from '../../../module/allModule.module';
import { AppRoutes } from '../../../Routes/app.routes.constants';
import { MenuService, MenuLink } from '../../../services';


@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, AllModule],
  templateUrl: './admin-layout-component.html',
  styleUrls: ['./admin-layout-component.scss']
})
export class AdminLayoutComponent {
  private readonly menuService = inject(MenuService);
  private readonly auth = inject(AuthStore);
  readonly menuBlocks = this.menuService.getAdminMenu();
  readonly openedLink = signal<string | null>(null);
  private readonly router = inject(Router);

  toggleLink(linkLabel: string) {
    this.openedLink.update(v => (v === linkLabel ? null : linkLabel));
  }

  isLinkOpen(linkLabel: string) {
    return this.openedLink() === linkLabel;
  }

  onLinkClick(evt: MouseEvent, link: MenuLink) {
    if (!link.actions?.length) return;
    evt.preventDefault();
    this.toggleLink(link.label);
  }

  logout() {
    this.auth.clear(true);
    this.router.navigate([AppRoutes.Login]);
  }
}

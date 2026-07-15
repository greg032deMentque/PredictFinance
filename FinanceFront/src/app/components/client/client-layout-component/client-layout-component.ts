import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AllModule } from '../../../module/allModule.module';
import { MenuLink, MenuService } from '../../../services';

const SIDEBAR_COLLAPSED_KEY = 'pf_sidebar_collapsed';

@Component({
  selector: 'app-client-layout',
  standalone: true,
  imports: [RouterOutlet, AllModule],
  templateUrl: './client-layout-component.html',
  styleUrls: ['./client-layout-component.scss']
})
export class ClientLayoutComponent {
  private readonly menuService = inject(MenuService);
  readonly menuBlocks = this.menuService.getClientMenu();
  readonly openedLink = signal<string | null>(null);

  readonly sidebarCollapsed = signal<boolean>(
    localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === 'true'
  );

  toggleSidebar(): void {
    const next = !this.sidebarCollapsed();
    this.sidebarCollapsed.set(next);
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(next));
  }

  toggleLink(linkLabel: string): void {
    this.openedLink.update((v) => (v === linkLabel ? null : linkLabel));
  }

  isLinkOpen(linkLabel: string): boolean {
    return this.openedLink() === linkLabel;
  }

  onLinkClick(evt: MouseEvent, link: MenuLink): void {
    if (!link.actions?.length) {
      return;
    }
    evt.preventDefault();
    this.toggleLink(link.label);
  }
}

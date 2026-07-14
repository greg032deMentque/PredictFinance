import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AllModule } from '../../../module/allModule.module';
import { MenuLink, MenuService } from '../../../services';

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

  toggleLink(linkLabel: string) {
    this.openedLink.update((v) => (v === linkLabel ? null : linkLabel));
  }

  isLinkOpen(linkLabel: string) {
    return this.openedLink() === linkLabel;
  }

  onLinkClick(evt: MouseEvent, link: MenuLink) {
    if (!link.actions?.length) {
      return;
    }

    evt.preventDefault();
    this.toggleLink(link.label);
  }
}

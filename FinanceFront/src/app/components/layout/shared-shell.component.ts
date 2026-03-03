import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/AuthService.service';

export type ShellMenuLink = {
  label: string;
  path: string;
  icon?: string;
};

export type ShellMenuBlock = {
  label: string;
  icon?: string;
  links: ShellMenuLink[];
};

@Component({
  selector: 'app-shared-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shared-shell.component.html',
  styleUrl: './shared-shell.component.scss'
})
export class SharedShellComponent {
  @Input({ required: true }) menuBlocks: ShellMenuBlock[] = [];
  @Input() title = 'PredictFinance';

  constructor(private readonly authService: AuthService) {}

  logout(): void {
    this.authService.logout();
  }
}

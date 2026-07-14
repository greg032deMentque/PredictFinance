import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { AllModule } from './module/allModule.module';
import { toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { filter } from 'rxjs';
import { AuthStore } from './core/auth.store';
import { AuthService } from './services/AuthService.service';
import { AppAreas, AppRoutes } from './Routes/app.routes.constants';
type ProfileLink = { label: string; icon: string; commands: readonly (string | number)[] };

@Component({
  standalone: true,
  selector: 'app-root',
  imports: [RouterOutlet, AllModule],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class App {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthStore);
  private readonly authService = inject(AuthService);
  private readonly title = inject(Title);

  private readonly navEnd = toSignal(
    this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)),
    { initialValue: null }
  );

  public readonly isPublicShell = computed(() => {
    this.navEnd();
    let r: ActivatedRoute | null = this.route;
    while (r?.firstChild) r = r.firstChild;
    return r?.snapshot.data?.['publicShell'] === true;
  });

  readonly showPrivateShell = computed(() => this.auth.isAuthenticated() && !this.isPublicShell());

  readonly headerTitle = computed(() => {
    const area = this.auth.area();
    if (area === AppAreas.Admin) return 'FinInsight - Administration';
    return 'FinInsight';
  });

  constructor() {
    effect(() => {
      const area = this.auth.area();
      if (area === AppAreas.Admin) {
        this.title.setTitle('FinInsight - Administration');
        return;
      }
      else {
        this.title.setTitle('FinInsight');
        return;
      }
    });
  }

  ngOnInit(): void {
    this.auth.syncFromStorage();
  }


  readonly userDisplayName = computed(() => {
    const anyAuth = this.auth as any;
    return (
      anyAuth.user?.()?.FullName ??
      anyAuth.user?.()?.Name ??
      anyAuth.profile?.()?.fullName ??
      anyAuth.profile?.()?.name ??
      'Mon profil'
    );
  });

  readonly userSubtitle = computed(() => {
    const area = this.auth.area();
    if (area === AppAreas.Admin) return 'Espace gouvernance';
    return 'Espace investisseur';
  });

  readonly userInitials = computed(() => {
    const n = (this.userDisplayName() ?? '').trim();
    if (!n) return 'U';
    const parts = n.split(/\s+/).filter(Boolean);
    const a = parts[0]?.[0] ?? 'U';
    const b = parts.length > 1 ? parts[parts.length - 1]?.[0] : '';
    return (a + b).toUpperCase();
  });

  readonly profileLinks = computed<readonly ProfileLink[]>(() => {
    const area = this.auth.area();

    if (area === AppAreas.Admin) {
      return [
        { label: "Vue d'ensemble", icon: 'bi-speedometer2', commands: ['/', AppRoutes.AdminRoot, AppRoutes.Dashboard] }
      ];
    }

    return [
      { label: 'Mon espace', icon: 'bi-house-door', commands: ['/', AppRoutes.ClientRoot, AppRoutes.Dashboard] },
      { label: 'Mon compte', icon: 'bi-person-gear', commands: ['/', AppRoutes.ClientRoot, AppRoutes.Account, AppRoutes.Profile] },
      { label: 'Notifications', icon: 'bi-bell', commands: ['/', AppRoutes.ClientRoot, AppRoutes.Notifications] }
    ];
  });

  logout(): void {
    this.authService.logout();
  }

}

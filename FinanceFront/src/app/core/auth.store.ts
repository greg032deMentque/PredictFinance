import { Injectable, computed, inject, signal } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { StorageService } from '../services/storage.service';
import { AppAreas } from '../Routes/app.routes.constants';

/**
 * Site courant (contexte applicatif global).
 *
 * ReprÃ©sente la "zone" active de lâ€™application :
 * - dÃ©terminÃ©e par le sous-domaine / host
 * - utilisÃ©e par les guards
 * - utilisÃ©e pour choisir le layout (AdminLayout / SiteLayout)
 * - utilisÃ©e pour afficher le bon menu principal
 *
 * Exemple :
 *  - admin.monsite.fr      â†’ 'admin'
 *  - ecole.monsite.fr      â†’ 'ecole'
 *  - asso.monsite.fr       â†’ 'asso'
 */
export type AppArea = (typeof AppAreas)[keyof typeof AppAreas];




/**
 * RÃ‰SUMÃ‰ DES RESPONSABILITÃ‰S
 *
 * | Concept           | Exemple              | ResponsabilitÃ© principale                         |
 * |-------------------|----------------------|--------------------------------------------------|
 * | AppArea           | 'admin'              | Contexte global (layout, guard, menu principal) |
 * | Mode              | 'adminLegalPerson'   | Comportement mÃ©tier / choix endpoint API        |
 * | AdminEntityKind   | 'schools'            | Construction des routes admin dâ€™une entitÃ©      |
 *
 * Ces trois concepts sont indÃ©pendants et complÃ©mentaires :
 *
 * - AppArea â†’ "OÃ¹ suis-je dans lâ€™application ?"
 * - Mode â†’ "Comment ce composant doit-il fonctionner ?"
 * - AdminEntityKind â†’ "Quelle entitÃ© admin est manipulÃ©e ?"
 */



type Session = {
  token: string;
  refreshToken: string;
  tenantId: string;
  tenantCode: string;
  defaultSite: string;
  sites: string[];
  area: AppArea;
  isSuperAdmin: boolean;
  roles: string[];
};

type JwtPayload = {
  tenant_id?: string;
  tenant_code?: string;
  default_site?: string;
  site?: string[] | string;
  is_superadmin?: string | boolean;
  roles?: string[] | string;
  role?: string[] | string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string[] | string;
};

export function normalizeBool(v: unknown): boolean {
  if (v === true) return true;
  if (v === false) return false;
  if (typeof v === 'string') return v.toLowerCase() === 'true';
  return false;
}

export function normalizeStringArray(v: unknown): string[] {
  if (Array.isArray(v)) return v.map(x => String(x ?? '').trim().toLowerCase()).filter(Boolean);
  if (typeof v === 'string') return [v.trim().toLowerCase()].filter(Boolean);
  return [];
}

export function resolveAreaFromHost(hostname: string): AppArea {
  const sub = (hostname ?? '').trim().toLowerCase().split('.')[0] ?? '';
  if (sub === 'admin') return sub;
  return 'client';
}

export function normalizeDefaultArea(defaultSite: unknown, tenantCode: unknown): AppArea {
  const d = (typeof defaultSite === 'string' ? defaultSite : '').trim().toLowerCase();
  if (d === 'admin') return 'admin';
  const t = (typeof tenantCode === 'string' ? tenantCode : '').trim().toLowerCase();
  if (t === 'admin') return t as AppArea;
  return 'client';
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly storage = inject(StorageService);
  private readonly jwt = inject(JwtHelperService);

  private readonly hostArea = signal<AppArea>(resolveAreaFromHost(window.location.hostname));

  readonly token = signal<string>('');
  readonly refreshToken = signal<string>('');

  readonly tenantId = signal<string>('');
  readonly tenantCode = signal<string>('');

  readonly defaultSite = signal<string>('');
  readonly sites = signal<string[]>([]);

  readonly area = signal<AppArea>(this.hostArea());

  readonly isSuperAdmin = signal<boolean>(false);
  readonly roles = signal<string[]>([]);

  private readonly authenticated = computed(() => {
    const t = (this.token() ?? '').trim();
    const rt = (this.refreshToken() ?? '').trim();
    return t.length > 0 && rt.length > 0;
  });

  readonly isTenantAdmin = computed(() => this.roles().includes('tenant_admin') || this.roles().includes('admin'));
  readonly canAccessAdmin = computed(
    () =>
      this.isSuperAdmin() ||
      this.roles().includes('tenant_admin') ||
      this.roles().includes('admin') ||
      this.roles().includes('superadmin')
  );
  readonly canSeeSiteTabs = computed(() => this.sites().length > 0);

  isAuthenticated(): boolean {
    return this.authenticated();
  }

  isTokenExpired(): boolean {
    const t = (this.token() ?? '').trim();
    if (!t) return true;
    return this.jwt.isTokenExpired(t);
  }

  hasSite(site: AppArea): boolean {
    if (site === 'admin') return this.canAccessAdmin();
    return this.sites().includes(site);
  }

  syncFromStorage(): void {
    this.hostArea.set(resolveAreaFromHost(window.location.hostname));
    this.area.set(this.hostArea());

    const token = this.getTokenScoped();
    const refreshToken = this.getRefreshTokenScoped();

    if (!token || !refreshToken) {
      this.clear(false);
      return;
    }

    if (this.jwt.isTokenExpired(token)) {
      this.clear(true);
      return;
    }

    const decoded = this.jwt.decodeToken(token) as JwtPayload | null;
    if (!decoded) {
      this.clear(true);
      return;
    }

    const roles = normalizeStringArray(
      decoded.roles ?? decoded.role ?? decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    );
    const isSuperAdmin = normalizeBool(decoded.is_superadmin) || roles.includes('superadmin');

    const tenantId = (decoded.tenant_id ?? '').trim();
    const tenantCode = (decoded.tenant_code ?? '').trim().toLowerCase();
    const hostArea = resolveAreaFromHost(window.location.hostname);
    if (hostArea !== 'admin' && tenantCode && tenantCode !== hostArea) {
      this.clear(true);
      return;
    }

    const sites = normalizeStringArray(decoded.site);
    const defaultSite = (decoded.default_site ?? '').trim().toLowerCase();
    const area = normalizeDefaultArea(defaultSite, tenantCode);

    this.setSession({
      token,
      refreshToken,
      tenantId,
      tenantCode,
      defaultSite,
      sites,
      area,
      isSuperAdmin,
      roles
    });
  }

  setSession(s: Session): void {
    this.token.set(s.token);
    this.refreshToken.set(s.refreshToken);

    this.tenantId.set(s.tenantId);
    this.tenantCode.set(s.tenantCode);

    this.defaultSite.set(s.defaultSite);
    this.sites.set(s.sites);

    this.area.set(s.area);

    this.isSuperAdmin.set(s.isSuperAdmin);
    this.roles.set(s.roles);

    this.setTokenScoped(s.token);
    this.setRefreshTokenScoped(s.refreshToken);
  }

  clear(clearStorage: boolean): void {
    this.token.set('');
    this.refreshToken.set('');

    this.tenantId.set('');
    this.tenantCode.set('');

    this.defaultSite.set('');
    this.sites.set([]);

    this.isSuperAdmin.set(false);
    this.roles.set([]);

    this.hostArea.set(resolveAreaFromHost(window.location.hostname));
    this.area.set(this.hostArea());

    if (clearStorage) this.clearStorageScoped();
  }

  private getTokenScoped(): string {
    const token = this.storage.GetToken?.();
    return token;
  }

  private getRefreshTokenScoped(): string {
    const refreshToken = this.storage.GetRefreshToken?.();
    return refreshToken;
  }

  private setTokenScoped(token: string): void {
    this.storage.SetToken(token);
  }

  private setRefreshTokenScoped(refreshToken: string): void {
    this.storage.SetRefreshToken(refreshToken);
  }

  private clearStorageScoped(): void {
    this.storage.RemoveRefreshToken();
    this.storage.RemoveToken();
  }
}

import { Injectable, computed, signal } from '@angular/core';

const SESSION_KEY = 'finance_front_session';

interface StoredSession {
  email: string;
  displayName: string;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly session = signal<StoredSession | null>(this.restoreSession());

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly displayName = computed(() => this.session()?.displayName ?? '');

  login(email: string, password: string): boolean {
    const normalizedEmail = email.trim().toLowerCase();
    if (!normalizedEmail || password.trim().length < 6) {
      return false;
    }

    const localPart = normalizedEmail.split('@')[0] ?? 'user';
    const displayName = localPart.charAt(0).toUpperCase() + localPart.slice(1);
    const payload: StoredSession = {
      email: normalizedEmail,
      displayName
    };

    this.session.set(payload);
    localStorage.setItem(SESSION_KEY, JSON.stringify(payload));
    return true;
  }

  logout(): void {
    this.session.set(null);
    localStorage.removeItem(SESSION_KEY);
  }

  private restoreSession(): StoredSession | null {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) {
      return null;
    }

    try {
      const parsed = JSON.parse(raw) as Partial<StoredSession>;
      if (typeof parsed.email !== 'string' || typeof parsed.displayName !== 'string') {
        return null;
      }
      return { email: parsed.email, displayName: parsed.displayName };
    } catch {
      return null;
    }
  }
}

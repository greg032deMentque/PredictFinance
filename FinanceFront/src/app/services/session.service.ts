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
    // Règle minimale locale (longueur >= 6) : ce service ne parle pas au back, donc il ne peut pas s'aligner
    // sur la politique réelle du serveur (voir components/auth/password-policy.ts pour le vrai formulaire
    // d'authentification applicatif). Ne pas durcir cette règle ici sans vérifier où ce service est encore utilisé.
    if (!normalizedEmail || password.trim().length < 6) {
      return false;
    }

    // Nom d'affichage dérivé de la partie locale de l'email (avant le @), capitalisée — heuristique de confort
    // UI uniquement, aucune garantie que ça corresponde au vrai nom de l'utilisateur.
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
      // localStorage peut contenir une valeur corrompue, tronquée ou écrite par une version antérieure du
      // schéma : on vérifie la forme avant de faire confiance au contenu plutôt que de propager un objet partiel.
      if (typeof parsed.email !== 'string' || typeof parsed.displayName !== 'string') {
        return null;
      }
      return { email: parsed.email, displayName: parsed.displayName };
    } catch {
      // JSON invalide : on démarre non authentifié plutôt que de faire planter le constructeur du service.
      return null;
    }
  }
}

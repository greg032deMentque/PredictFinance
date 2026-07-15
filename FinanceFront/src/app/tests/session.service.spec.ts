import { SessionService } from '../services/session.service';

describe('SessionService', () => {
  const SESSION_KEY = 'finance_front_session';

  afterEach(() => {
    localStorage.removeItem(SESSION_KEY);
  });

  describe('login', () => {
    it('should reject a blank email or a password shorter than 6 characters', () => {
      localStorage.removeItem(SESSION_KEY);
      const service = new SessionService();

      expect(service.login('   ', 'longpassword')).toBeFalse();
      expect(service.login('user@example.com', 'ab')).toBeFalse();
      expect(service.isAuthenticated()).toBeFalse();
      expect(localStorage.getItem(SESSION_KEY)).toBeNull();
    });

    it('should normalize the email and derive a capitalized display name from the local part', () => {
      localStorage.removeItem(SESSION_KEY);
      const service = new SessionService();

      const result = service.login('  John.Doe@EXAMPLE.com  ', 'Password1');

      expect(result).toBeTrue();
      expect(service.isAuthenticated()).toBeTrue();
      expect(service.displayName()).toBe('John.doe');
    });

    it('should persist the session to localStorage', () => {
      localStorage.removeItem(SESSION_KEY);
      const service = new SessionService();

      service.login('alice@example.com', 'Password1');

      const stored = JSON.parse(localStorage.getItem(SESSION_KEY) ?? 'null');
      expect(stored).toEqual({ email: 'alice@example.com', displayName: 'Alice' });
    });
  });

  describe('logout', () => {
    it('should clear the in-memory session and the localStorage entry', () => {
      localStorage.removeItem(SESSION_KEY);
      const service = new SessionService();
      service.login('alice@example.com', 'Password1');

      service.logout();

      expect(service.isAuthenticated()).toBeFalse();
      expect(localStorage.getItem(SESSION_KEY)).toBeNull();
    });
  });

  describe('restoreSession (constructor)', () => {
    it('should restore a previously persisted valid session', () => {
      localStorage.setItem(SESSION_KEY, JSON.stringify({ email: 'bob@example.com', displayName: 'Bob' }));

      const service = new SessionService();

      expect(service.isAuthenticated()).toBeTrue();
      expect(service.displayName()).toBe('Bob');
    });

    it('should not throw and should start unauthenticated when the stored value is malformed JSON', () => {
      localStorage.setItem(SESSION_KEY, '{not-json');

      expect(() => new SessionService()).not.toThrow();
      const service = new SessionService();
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('should start unauthenticated when the stored shape is missing required fields', () => {
      localStorage.setItem(SESSION_KEY, JSON.stringify({ email: 'incomplete@example.com' }));

      const service = new SessionService();

      expect(service.isAuthenticated()).toBeFalse();
    });
  });
});

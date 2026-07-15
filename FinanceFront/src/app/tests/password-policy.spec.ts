import { STRONG_PASSWORD_PATTERN } from '../components/auth/password-policy';

describe('STRONG_PASSWORD_PATTERN', () => {
  it('should accept passwords with at least 6 chars, one lowercase, one uppercase and one digit', () => {
    expect(STRONG_PASSWORD_PATTERN.test('Password1')).toBeTrue();
    expect(STRONG_PASSWORD_PATTERN.test('Ab1def')).toBeTrue();
    expect(STRONG_PASSWORD_PATTERN.test('LongerPassword123')).toBeTrue();
  });

  it('should reject passwords missing a required character class', () => {
    expect(STRONG_PASSWORD_PATTERN.test('password1')).toBeFalse();
    expect(STRONG_PASSWORD_PATTERN.test('PASSWORD1')).toBeFalse();
    expect(STRONG_PASSWORD_PATTERN.test('Password')).toBeFalse();
  });

  it('should reject passwords shorter than 6 characters', () => {
    expect(STRONG_PASSWORD_PATTERN.test('Ab1de')).toBeFalse();
  });

  it('should accept extra length and special characters as long as the base rule is met', () => {
    expect(STRONG_PASSWORD_PATTERN.test('Ab1!@#longtail')).toBeTrue();
  });
});

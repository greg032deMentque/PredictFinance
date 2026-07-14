import { normalizeBool, normalizeStringArray, resolveAreaFromHost, normalizeDefaultArea } from "../core/auth.store";

describe('auth.store helpers', () => {
  it('normalizeBool should support booleans and case-insensitive strings', () => {
    expect(normalizeBool(true)).toBeTrue();
    expect(normalizeBool(false)).toBeFalse();
    expect(normalizeBool('true')).toBeTrue();
    expect(normalizeBool('TRUE')).toBeTrue();
    expect(normalizeBool('false')).toBeFalse();
    expect(normalizeBool('other')).toBeFalse();
  });

  it('normalizeStringArray should normalize arrays and strings', () => {
    expect(normalizeStringArray([' Admin ', 'reader', '', null as never])).toEqual(['admin', 'reader']);
    expect(normalizeStringArray(' Admin ')).toEqual(['admin']);
    expect(normalizeStringArray(undefined)).toEqual([]);
  });

  it('resolveAreaFromHost should resolve admin host explicitly and default to client otherwise', () => {
    expect(resolveAreaFromHost('admin.predictfinance.local')).toBe('admin');
    expect(resolveAreaFromHost('client.predictfinance.local')).toBe('client');
    expect(resolveAreaFromHost('')).toBe('client');
  });

  it('normalizeDefaultArea should prefer explicit admin values and default to client otherwise', () => {
    expect(normalizeDefaultArea('admin', '')).toBe('admin');
    expect(normalizeDefaultArea('', 'admin')).toBe('admin');
    expect(normalizeDefaultArea('client', '')).toBe('client');
    expect(normalizeDefaultArea('', 'tenant-a')).toBe('client');
  });
});

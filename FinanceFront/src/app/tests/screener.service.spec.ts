import { HttpClient, HttpParams } from '@angular/common/http';
import { Injector, runInInjectionContext } from '@angular/core';
import { of } from 'rxjs';
import { ScreenerService } from '../services/screener.service';

describe('ScreenerService (buildFilterParams)', () => {
  const createSut = () => {
    const http = { get: jasmine.createSpy().and.returnValue(of({})) };
    const injector = Injector.create({ providers: [{ provide: HttpClient, useValue: http }] });
    return { http, service: runInInjectionContext(injector, () => new ScreenerService()) };
  };

  const paramsFromLastCall = (http: { get: jasmine.Spy }): HttpParams => http.get.calls.mostRecent().args[1].params;

  it('should default SortBy/SortDirection and Page/PageSize when no option is provided', () => {
    const { service, http } = createSut();

    service.getScreener().subscribe();

    const params = paramsFromLastCall(http);
    expect(params.get('SortBy')).toBe('Symbol');
    expect(params.get('SortDirection')).toBe('asc');
    expect(params.get('Page')).toBe('1');
    expect(params.get('PageSize')).toBe('20');
  });

  it('should override the defaults with explicit Page/PageSize/SortBy/SortDirection', () => {
    const { service, http } = createSut();

    service.getScreener({ Page: 3, PageSize: 50, SortBy: 'LastPrice', SortDirection: 'desc' }).subscribe();

    const params = paramsFromLastCall(http);
    expect(params.get('Page')).toBe('3');
    expect(params.get('PageSize')).toBe('50');
    expect(params.get('SortBy')).toBe('LastPrice');
    expect(params.get('SortDirection')).toBe('desc');
  });

  it('should append one param entry per Sector/Country instead of overwriting', () => {
    const { service, http } = createSut();

    service.getScreener({ Sectors: ['Tech', 'Health'], Countries: ['FR'] }).subscribe();

    const params = paramsFromLastCall(http);
    expect(params.getAll('Sectors')).toEqual(['Tech', 'Health']);
    expect(params.getAll('Countries')).toEqual(['FR']);
  });

  it('should only set PeaOnly when explicitly true, never when false or omitted', () => {
    const { service, http } = createSut();

    service.getScreener({ PeaOnly: false }).subscribe();
    expect(paramsFromLastCall(http).has('PeaOnly')).toBeFalse();

    service.getScreener({}).subscribe();
    expect(paramsFromLastCall(http).has('PeaOnly')).toBeFalse();

    service.getScreener({ PeaOnly: true }).subscribe();
    expect(paramsFromLastCall(http).get('PeaOnly')).toBe('true');
  });

  it('should trim Search and omit optional numeric filters when null or undefined', () => {
    const { service, http } = createSut();

    service.getScreener({ Search: '  air  ', MinPE: null, MaxPE: undefined, MinDividendYield: 2, MinMarketCap: 0 }).subscribe();

    const params = paramsFromLastCall(http);
    expect(params.get('Search')).toBe('air');
    expect(params.has('MinPE')).toBeFalse();
    expect(params.has('MaxPE')).toBeFalse();
    expect(params.get('MinDividendYield')).toBe('2');
    expect(params.get('MinMarketCap')).toBe('0');
  });

  it('exportCsv should reuse the same filter params without Page/PageSize', () => {
    const { service, http } = createSut();

    service.exportCsv({ Sectors: ['Tech'] }).subscribe();

    const params = paramsFromLastCall(http);
    expect(params.getAll('Sectors')).toEqual(['Tech']);
    expect(params.has('Page')).toBeFalse();
    expect(params.has('PageSize')).toBeFalse();
  });
});

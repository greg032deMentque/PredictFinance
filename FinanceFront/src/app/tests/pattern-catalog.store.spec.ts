import { Injector, runInInjectionContext } from '@angular/core';
import { Subject, of } from 'rxjs';
import { PatternCatalogStore } from '../services/pattern-catalog.store';
import { ClientFinanceService } from '../services/client-finance.service';
import type { PatternCatalogItem } from '../Models/client-finance-models/client-finance-models';

describe('PatternCatalogStore', () => {
  const item = (overrides: Partial<PatternCatalogItem> = {}): PatternCatalogItem => ({
    Id: 'HeadAndShoulders',
    Label: 'Tête-épaules',
    Family: 'Reversal',
    Description: '',
    Direction: 'Bearish',
    FamilyLabel: '',
    DirectionLabel: '',
    AnalysisNarrative: '',
    Reliability: 0,
    ReliabilityLabel: '',
    ...overrides
  });

  const createSut = (patterns: PatternCatalogItem[]) => {
    const clientFinanceService = {
      getPatternCatalog: jasmine.createSpy().and.returnValue(of(patterns))
    };

    const injector = Injector.create({
      providers: [{ provide: ClientFinanceService, useValue: clientFinanceService }]
    });

    return {
      clientFinanceService,
      store: runInInjectionContext(injector, () => new PatternCatalogStore())
    };
  };

  it('ensureLoaded should discard entries with a blank Id or Label', (done) => {
    const { store } = createSut([
      item(),
      item({ Id: '  ', Label: 'Sans id' }),
      item({ Id: 'DoubleTop', Label: '   ' })
    ]);

    store.ensureLoaded().subscribe((patterns) => {
      expect(patterns.length).toBe(1);
      expect(store.items().length).toBe(1);
      done();
    });
  });

  it('ensureLoaded should not call the API again once the catalog is cached', (done) => {
    const { store, clientFinanceService } = createSut([item()]);

    store.ensureLoaded().subscribe(() => {
      store.ensureLoaded().subscribe((patterns) => {
        expect(patterns.length).toBe(1);
        expect(clientFinanceService.getPatternCatalog).toHaveBeenCalledTimes(1);
        done();
      });
    });
  });

  it('ensureLoaded should share a single in-flight request between concurrent callers', () => {
    const subject = new Subject<PatternCatalogItem[]>();
    const clientFinanceService = {
      getPatternCatalog: jasmine.createSpy().and.returnValue(subject.asObservable())
    };
    const injector = Injector.create({
      providers: [{ provide: ClientFinanceService, useValue: clientFinanceService }]
    });
    const store = runInInjectionContext(injector, () => new PatternCatalogStore());

    let firstResult: readonly PatternCatalogItem[] | undefined;
    let secondResult: readonly PatternCatalogItem[] | undefined;
    store.ensureLoaded().subscribe((patterns) => (firstResult = patterns));
    store.ensureLoaded().subscribe((patterns) => (secondResult = patterns));

    subject.next([item()]);
    subject.complete();

    expect(clientFinanceService.getPatternCatalog).toHaveBeenCalledTimes(1);
    expect(firstResult?.length).toBe(1);
    expect(secondResult?.length).toBe(1);
  });

  describe('labelFor', () => {
    it('should return "Pattern indisponible" for a blank id without calling the API', () => {
      const { store, clientFinanceService } = createSut([item()]);

      expect(store.labelFor('   ')).toBe('Pattern indisponible');
      expect(clientFinanceService.getPatternCatalog).not.toHaveBeenCalled();
    });

    it('should fall back to the raw id when the catalog has not been loaded yet', () => {
      const { store } = createSut([item()]);

      expect(store.labelFor('HeadAndShoulders')).toBe('HeadAndShoulders');
    });

    it('should return the mapped label once the catalog is loaded', (done) => {
      const { store } = createSut([item()]);

      store.ensureLoaded().subscribe(() => {
        expect(store.labelFor('HeadAndShoulders')).toBe('Tête-épaules');
        expect(store.labelFor(' HeadAndShoulders ')).toBe('Tête-épaules');
        done();
      });
    });
  });
});

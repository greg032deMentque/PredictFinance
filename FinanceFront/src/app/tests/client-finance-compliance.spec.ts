import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ClientAnalysisLaunchRequest } from '../Models/client-finance-models/client-finance-models';
import { ClientFinanceMapper } from '../services/client-finance.mapper';
import { ClientFinanceService } from '../services/client-finance.service';

describe('client finance compliance mappings', () => {
  let mapper: ClientFinanceMapper;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ClientFinanceMapper]
    });
    mapper = TestBed.inject(ClientFinanceMapper);
  });

  it('analysis launch request should not send a legacy DOUBLE_TOP fallback', () => {
    const request = new ClientAnalysisLaunchRequest({ Symbol: 'AIR.PA' });

    expect(request.RequestedPatternIds).toEqual([]);
    expect(JSON.stringify(request)).not.toContain('DOUBLE_TOP');
  });

  it('analysis detail should preserve primary pattern and alternatives', () => {
    const detail = mapper.mapAnalysisDetail({
      AnalysisId: 'analysis-1',
      Instrument: { Symbol: 'AIR.PA' },
      MarketReading: {
        PrimaryPatternDisplayName: 'Rectangle continuation',
        Alternatives: [
          {
            PatternId: 'BullFlagContinuation',
            DisplayName: 'Bull flag continuation',
            ConfidenceLabel: 'MEDIUM',
            ProgressStatusLabel: 'Sous surveillance'
          }
        ]
      },
      ConfidenceBreakdown: { Level: 'HIGH', Criteria: [{ Code: 'C1', Label: 'Critere', State: 'met', Source: 'DETECTION' }] },
      ActionPlan: { HoldingStatus: 'Held', PolicyVersion: 'v1', Steps: [{ Kind: 'SET_ALERT', Label: 'Alerte', Source: 'risk', AlertTrigger: 'LEVEL_CROSSED' }] }
    });

    expect(detail.MarketReading.PrimaryPatternDisplayName).toBe('Rectangle continuation');
    expect(detail.MarketReading.Alternatives[0].PatternId).toBe('BullFlagContinuation');
    expect(detail.ConfidenceBreakdown.Criteria.length).toBe(1);
    expect(detail.ActionPlan.Steps[0].AlertTrigger).toBe('LEVEL_CROSSED');
  });

  it('instrument detail should preserve held context, alternatives and confidence summary', () => {
    const detail = mapper.mapInstrumentDetail({
      Symbol: 'AIR.PA',
      InstrumentSummary: { Instrument: { Symbol: 'AIR.PA' }, Freshness: {}, HasPersistedAnalysis: true },
      MarketReading: {
        ConfidenceLabel: 'HIGH',
        ValidationSummary: 'Valide',
        InvalidationLevel: 136,
        Alternatives: [{ PatternId: 'BullFlagContinuation', DisplayName: 'Bull flag continuation' }]
      },
      PersonalSituation: { HoldsInstrument: true, TotalQuantityHeld: 2, Recommendation: {} },
      SupportReading: {},
      NavigationLinks: {}
    });

    expect(detail.PersonalSituation.HoldsInstrument).toBeTrue();
    expect(detail.MarketReading.ConfidenceLabel).toBe('HIGH');
    expect(detail.MarketReading.Alternatives[0].PatternId).toBe('BullFlagContinuation');
  });

});

describe('client finance service compliance payloads', () => {
  let service: ClientFinanceService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ClientFinanceService,
        ClientFinanceMapper,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(ClientFinanceService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('runAnalysis should send RequestedPatternIds empty by default and no RequestedPattern field', () => {
    service.runAnalysis(new ClientAnalysisLaunchRequest({ Symbol: 'AIR.PA', RequestedPatternIds: [] })).subscribe();

    const request = httpTesting.expectOne((req) => req.url.endsWith('ClientFinance/analysis/run'));
    expect(request.request.body).toEqual({ Symbol: 'AIR.PA', RequestedPatternIds: [] });
    expect(request.request.body.RequestedPattern).toBeUndefined();
    request.flush({
      Id: 'analysis-1',
      Symbol: 'AIR.PA',
      Pattern: 'RectangleContinuation',
      RecommendationAction: 'Buy',
      RiskLevel: 'Moderate',
      ModelStatus: 'Go'
    });
  });

});

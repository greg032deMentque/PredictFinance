# PredictFinance V2 — Spec : Modules Simulateurs

> **Statut** : backlog V2 — pas encore implémenté
> **Date** : 2026-06-16
> **Périmètre** : 3 modules de simulation financière, usage famille (≤ 5 utilisateurs)

---

## 1. Vue d'ensemble

Trois modules indépendants de simulation financière, accessibles depuis la zone client.
Aucun ordre passé, aucun accès bancaire. Calculs côté back, affichage côté front.

| Module | Entrées | Sorties clés |
|---|---|---|
| **Crédit — Simulation prêt** | capital, taux, durée | mensualité, coût total, tableau d'amortissement |
| **Crédit — Capacité d'endettement** | revenus, charges, taux, durée | taux endettement, reste-à-vivre, montant max empruntable |
| **ROI Action** | ticker, prix d'achat, date d'achat | CAGR, payback period (plus-value + dividendes) |
| **ROI Immobilier** | prix achat, apport, taux, durée, loyer, charges | cash-flow mensuel, rendement brut/net, effet de levier, seuil de rentabilité |

---

## 2. Module Crédit

### 2.1 Simulation prêt

**Entrées**

| Champ | Type | Contraintes |
|---|---|---|
| `principal` | decimal | > 0 |
| `annualRate` | decimal | 0–100 (%) |
| `durationMonths` | int | 1–480 |

**Sorties**

| Champ | Description |
|---|---|
| `monthlyPayment` | mensualité constante (formule PMT) |
| `totalCost` | coût total = mensualité × durée |
| `totalInterest` | intérêts totaux = coût total − capital |
| `amortizationTable` | liste mois par mois : capital restant, intérêts, principal remboursé |

**Formule** : PMT standard — `M = P × [r(1+r)^n] / [(1+r)^n − 1]` avec `r = taux mensuel`.

---

### 2.2 Capacité d'endettement

**Entrées**

| Champ | Type | Description |
|---|---|---|
| `monthlyNetIncome` | decimal | revenus nets mensuels du foyer |
| `existingMonthlyCharges` | decimal | charges fixes existantes (loyer, crédits en cours…) |
| `annualRate` | decimal | taux du prêt envisagé |
| `durationMonths` | int | durée souhaitée |

**Sorties**

| Champ | Description |
|---|---|
| `debtRatio` | taux d'endettement actuel (charges / revenus) |
| `availableForDebt` | capacité mensuelle disponible pour un nouveau crédit (seuil 33 %) |
| `remainingIncome` | reste-à-vivre après nouveau crédit |
| `maxBorrowable` | capital max empruntable avec la capacité disponible et les paramètres fournis |

**Règle** : seuil d'endettement fixé à 33 % (configurable si besoin futur).

---

## 3. Module ROI Action

**Objectif** : calculer en combien de temps un achat d'action est "récupéré" via la combinaison plus-value latente + dividendes perçus.

**Source de données** : Yahoo Finance (prix historiques + dividendes) — même provider que V1.

**Entrées**

| Champ | Type | Description |
|---|---|---|
| `ticker` | string | symbole Yahoo Finance (ex : `MC.PA`) |
| `purchasePrice` | decimal | prix d'achat unitaire |
| `purchaseDate` | date | date d'achat simulée |

**Sorties**

| Champ | Description |
|---|---|
| `cagr` | taux de croissance annuel composé depuis la date d'achat |
| `averageAnnualDividend` | dividende annuel moyen par action sur la période |
| `paybackYears` | nombre d'années estimées pour récupérer le prix d'achat |
| `paybackBreakdown` | part plus-value / part dividendes dans le payback |
| `projectionCurve` | série annuelle : valeur cumulée (PV + dividendes) vs prix d'achat |

**Algorithme payback**

1. Récupérer prix historiques journaliers + dividendes depuis `purchaseDate` à aujourd'hui.
2. Calculer rendement annuel moyen du cours (CAGR ou moyenne des rendements annuels).
3. Calculer dividende annuel moyen.
4. Projeter année par année : `cumulativeReturn(y) = purchasePrice × (1 + cagr)^y − purchasePrice + dividende × y`.
5. Payback = première année où `cumulativeReturn ≥ purchasePrice`.
6. Si non atteint sur la période historique : extrapoler avec la tendance calculée (mentionner l'extrapolation dans la réponse).

---

## 4. Module ROI Immobilier

**Objectif** : simuler la rentabilité complète d'un investissement locatif avec effet de levier.

**Entrées**

| Champ | Type | Description |
|---|---|---|
| `purchasePrice` | decimal | prix d'achat du bien |
| `downPayment` | decimal | apport personnel |
| `annualRate` | decimal | taux du prêt immobilier |
| `durationMonths` | int | durée du prêt |
| `monthlyRent` | decimal | loyer mensuel attendu |
| `monthlyCharges` | decimal | charges mensuelles (copro, assurance, entretien, taxe foncière / 12…) |

**Sorties**

| Champ | Description |
|---|---|
| `monthlyPayment` | mensualité du prêt |
| `monthlyCashFlow` | cash-flow net = loyer − mensualité − charges |
| `grossYield` | rendement brut = loyer annuel / prix achat (%) |
| `netYield` | rendement net = (loyer − charges) annuel / prix achat (%) |
| `leverageEffect` | ROI sur fonds propres vs ROI sans emprunt |
| `breakEvenYear` | année où le cumul cash-flow positif rembourse l'apport initial |
| `cashFlowCurve` | série mensuelle du cash-flow cumulé sur la durée du prêt |

**Formules**

- `grossYield = (monthlyRent × 12) / purchasePrice`
- `netYield = ((monthlyRent − monthlyCharges) × 12) / purchasePrice`
- `leverageEffect = netYield / (downPayment / purchasePrice)` — ratio amplification
- `breakEvenYear` : premier mois où `Σ cashFlow ≥ downPayment`, converti en années

**Pas de fiscalité** dans V2 (régime simple, charges fixes uniquement).

---

## 5. Architecture technique cible

### Backend

```
BackPredictFinance.Services/
  Simulators/
    ICreditSimulatorService.cs
    CreditSimulatorService.cs
    IStockRoiService.cs
    StockRoiService.cs          -- réutilise le provider Yahoo existant
    IRealEstateSimulatorService.cs
    RealEstateSimulatorService.cs

BackPredictFinance.ViewModels/
  SimulatorViewModels/
    Credit/
      LoanSimulationRequest.cs
      LoanSimulationResult.cs   -- inclut List<AmortizationRow>
      DebtCapacityRequest.cs
      DebtCapacityResult.cs
    StockRoi/
      StockRoiRequest.cs
      StockRoiResult.cs
    RealEstate/
      RealEstateRequest.cs
      RealEstateResult.cs

BackPredictFinance.API/
  Controllers/
    SimulatorsController.cs     -- POST api/simulators/{loan|debt-capacity|stock-roi|real-estate}
```

Pas de migration EF — aucune persistance (calculs à la volée, pas d'historique sauvegardé en V2).

### Frontend

```
FinanceFront/src/app/components/simulators/
  loan-simulator/
  debt-capacity/
  stock-roi/
  real-estate-simulator/
```

Nouvelles routes sous `/simulators/*`, entrée dans la navigation client.

---

## 6. Hors scope V2

- Fiscalité immobilière (micro-foncier, régime réel, LMNP) → V3 si besoin
- Sauvegarde des simulations en base → V3
- Comparaison multi-scénarios côte à côte → V3
- Données immobilières externes (prix marché, rendements par zone) → non prévu

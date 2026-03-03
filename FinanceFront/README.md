# FinanceFront

Front Angular (Bootstrap) pour le MVP PredictFinance.

## Ecrans inclus

- `/login`: ecran de connexion local (prototype front)
- `/app/dashboard`: dashboard prediction avec menu lateral

## Fonctionnement

1. L'utilisateur se connecte (session locale navigateur).
2. Il choisit un ticker (ex: `AAPL`) dans le dashboard.
3. Le front appelle l'API .NET:
   - `GET /api/trading/predict/{symbol}`
4. Le front affiche:
   - probabilite du pattern (`last/mean/max`)
   - action conseillee (`buy`, `hold`, `sell`)
   - raison de la recommandation

## Lancer en local

```bash
npm install
npm start
```

Application disponible sur `http://localhost:4200`.

## Build

```bash
npm run build
```

Build de production dans `dist/FinanceFront`.

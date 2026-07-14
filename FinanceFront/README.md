# FinanceFront

Front Angular du MVP PredictFinance.

## Role du bloc

`FinanceFront` gere l'experience utilisateur:

- connexion,
- affichage dashboard,
- selection du ticker,
- lancement de l'analyse,
- affichage prediction + recommandation.

## Flux utilisateur

1. L'utilisateur se connecte sur `/login`.
2. Il est redirige selon son role:
   - admin -> `/admin/dashboard`
   - user -> `/client/dashboard`
3. Il selectionne une valeur (ticker) et lance l'analyse.
4. Le front appelle `GET /api/Trading/predict/{symbol}`.
5. Le resultat IA est affiche (probabilites + action conseillee).

## Architecture front

- `components/login`: ecran de connexion.
- `components/layout`: shell partage + layouts `admin` et `client` (menus differents).
- `components/dashboard`: formulaire ticker + rendu prediction (composant partage).
- `components/admin`: ecran dashboard admin.
- `Routes/app.routes.auth.ts`: routes publiques auth.
- `Routes/app.routes.admin.ts`: routes zone admin.
- `Routes/app.routes.user.ts`: routes zone client.
- `services/AuthService.service.ts`: login/logout/refresh token.
- `interceptor/TokenInterceptor.ts`: ajoute le JWT et gere le refresh.
- `interceptor/ApiErrorInterceptor.ts`: gestion centralisee des erreurs API.
- `guard/*`: guards auth + role (`admin`/`client`) + guest.
- `core/models/prediction.model.ts`: classes de resultat prediction.

## Auth / refresh token

- Le token est stocke en `sessionStorage`.
- Si le token est expire, `auth.guard` tente d'abord un refresh pour conserver la session.
- Le `TokenInterceptor` gere aussi le refresh pendant les appels API.
- En cas d'echec refresh, la session est nettoyee et l'utilisateur est renvoye au login.


## Lancer en local

```bash
npm install
npm start
```

Application: `http://localhost:4200`

## Build

```bash
npm run build
```

Build production valide dans `dist/FinanceFront`.

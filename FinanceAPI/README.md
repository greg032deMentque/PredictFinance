# FinanceAPI

API .NET de PredictFinance.

## Role du bloc

`FinanceAPI` fait le lien entre le front et le moteur IA:

- authentification utilisateur (JWT access token + refresh token rotatif),
- autorisation des routes protegees,
- orchestration des appels de prediction vers `FinanceIA`,
- exposition d'endpoints simples pour le front.

## Parcours supporte

1. `POST /api/Account/Login` -> renvoie `Token` + `RefreshToken`.
2. Front accede au dashboard (route protegee).
3. Front appelle `GET /api/Trading/predict/{symbol}`.
4. L'API execute la CLI Python et retourne prediction + action.
5. Si access token expire, front appelle `POST /api/Account/Refresh`.

## Endpoints principaux

- `POST /api/Account/Login`
- `POST /api/Account/LoginAdmin`
- `POST /api/Account/Refresh`
- `POST /api/Account/Logout`
- `GET /api/Trading/predict/{symbol}`
- `POST /api/Trading/predict`
- `POST /api/Trading/recommend`

## Refresh token (strategie)

- Token de refresh stocke en base sous forme de hash (`RefreshTokens`).
- Rotation a chaque refresh (nouveau refresh token emis).
- Detection de replay: si un refresh deja remplace est reutilise, la chaine est revoquee.
- Access token court (configurable, par defaut 60 min en dev).

Classes cle:

- `AccountService`: login, refresh, password workflows.
- `JwtGeneratorService`: emission JWT + hash/rotation refresh.
- `AccountController`: endpoints auth.

## Configuration

Fichiers:

- `appsettings.Development.json`
- `appsettings.json`

Sections importantes:

- `ConnectionStrings:DefaultConnection`
- `JWTToken`
- `ServerSalt`
- `Frontend:BaseUrl`
- `PythonCli`

## Lancer en local

```powershell
cd FinanceAPI
dotnet restore
dotnet build
dotnet run --project BackPredictFinance.API
```

Swagger par defaut: `http://localhost:5298/swagger`
HTTPS (optionnel): `dotnet run --project BackPredictFinance.API --launch-profile https` puis `https://localhost:7187/swagger`

## Qualite

- Build solution: OK.
- Design orientee classes (services/controllers/models).
- Separation claire API / Services / Datas / ViewModels.

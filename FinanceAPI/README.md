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
6. L'API applique un quality gate modele (`GO/NO_GO`) avant recommandation finale.
7. Pour la simulation client, l'API appelle maintenant la CLI Python `simulate` et ne calcule plus la simulation localement.

## Endpoints principaux

- `POST /api/Account/Login`
- `POST /api/Account/LoginAdmin`
- `POST /api/Account/Refresh`
- `POST /api/Account/Logout`
- `GET /api/Trading/predict/{symbol}`
- `POST /api/Trading/predict`
- `POST /api/Trading/recommend`
- `POST /api/ClientFinance/simulation/run`
- `GET /IA/Health` (etat global IA)
- `GET /IA/Status` (details complets, admin)

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

Champs `PythonCli` utiles pour le quality gate:

- `MinPrecision`
- `MinF1`
- `MinRocAuc`
- `MinPositives`

## Contrat IA (enum + classes)

Le contrat de reponse de prediction expose:

- `modelStatus` (enum): `Go | NoGo`
- `modelChecks` (liste typée) avec:
  - `check` (enum): `Precision | F1 | RocAuc | MinimumPositives`
  - `status` (enum): `Pass | Fail | NotApplicable`
  - `value`, `threshold`, `detail`
- `modelMessage` (texte court lisible front)

Classes cle:

- `PythonPredictRequest`, `PythonPredictPayload` (transport API -> IA -> API)
- `PythonSimulationRequest`, `PythonSimulationPayload` (transport simulation API -> IA -> API)
- `ModelQualityGate`, `ModelCheckResult` (quality gate metier)
- `PredictOut`, `SimulationOut` (reponses finales API)

Controller / service de supervision IA:

- `IAController`:
  - `GET /IA/Health`: etat global simple (`Up/Degraded/Down`)
  - `GET /IA/Status`: details runtime + artefacts + quality gate
- `IAStatusService`: centralise les checks de sante IA.

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

- Design orientee classes (services/controllers/models).
- Separation claire API / Services / Datas / ViewModels.
- Quality gate modele explicite (enum + seuils config).

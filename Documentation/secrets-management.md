# Secrets management

## Principle

A secret **never** lives in the repository — not in `appsettings.*.json`, not in pipeline YAML, not in git history. Committed config files hold only structure and non-secret values (public URLs, durations, placeholders). Real secret values are injected per environment and merged by .NET configuration in this precedence order (last wins):

```
appsettings.json  →  appsettings.{Env}.json  →  user-secrets (dev)  →  environment variables (staging/prod)  →  [optional] Key Vault
```

## What counts as a secret here

| Key | Secret? | Where it lives |
|---|---|---|
| `JWTToken:Secret` | yes | user-secrets (dev) / env var (staging, prod) |
| `ServerSalt` | yes | user-secrets / env var |
| `AutomapperLicense` | yes | user-secrets / env var |
| `EmailConfig` (SMTP user/password) | yes | user-secrets / env var |
| `adminEmail` / `adminPwd` / `userEmail` / `userPwd` (seed accounts) | yes (dev only) | user-secrets |
| `ConnectionStrings:DefaultConnection` | **depends** | file if LocalDB/integrated-auth (no password); env var if it carries a password |
| `Frontend:BaseUrl`, `JWTToken:Issuer`, `JWTToken:Audience` | no | committed config file |

The dev database uses LocalDB with `Trusted_Connection=True` (no password), so the dev connection string is **not** a secret and stays in the local, gitignored `appsettings.Development.json`. A production connection string that contains a password **is** a secret and must come from an environment variable.

## Per environment

### Development — .NET user-secrets

Secrets live in your Windows user profile, outside the repo, and override the config file automatically in the `Development` environment (the API project has a `UserSecretsId`). See [local-setup.md](local-setup.md) for the exact `dotnet user-secrets set` commands.

### Staging & Production — host environment variables

Set secrets as **Application Settings / environment variables** on the host (e.g. App Service), never as deployed files. .NET maps nesting with a double underscore `__`:

```
ConnectionStrings__DefaultConnection = <prod-db-connection-string>
JWTToken__Secret                     = <prod-signing-key>
ServerSalt                           = <prod-salt>
AutomapperLicense                    = <license>
EmailConfig__Password                = <smtp-password>
```

This removes the legacy "deploy `appsettings.*.json` manually on each server" practice: no secret on disk, each environment isolated, values distinct per environment.

### Pipelines — Azure DevOps secret variables

The build/test pipelines need **no** secrets today (tests use a committed, placeholder-only `appsettings.Testing.json`). When a deploy step needs a secret (e.g. a connection string to run migrations), use an Azure DevOps **secret variable / variable group** (optionally linked to Key Vault) — never inline in YAML.

### Optional — Azure Key Vault

For centralised storage, rotation, and audit: secrets live in Key Vault, the host references them (`@Microsoft.KeyVault(...)`), and .NET reads them as ordinary configuration. This is more than a single-user app needs; environment variables are sufficient. Adopt Key Vault only if central rotation/audit becomes a requirement.

## JWT specifics

- Use a **distinct** signing secret per environment; never reuse the dev secret in prod.
- Make access/refresh token lifetimes explicit in config (`JWTToken:ValidityMinutesAcessToken` / `ValidityMinutesRefreshToken`) rather than relying on code fallbacks.

## Mandatory remediation: rotate leaked secrets

Secrets were historically committed to git and are recoverable from history — they are **compromised**. Regardless of the storage change above, they must be:

1. **Rotated** — generate new values (JWT secret, SMTP password, `ServerSalt`, any DB password, seed passwords). Old values are burned.
2. **Purged from history** (optional but recommended) — e.g. `git filter-repo` to remove the old files from all commits, then force-update the remote.

Changing the storage mechanism does **not** protect values already in history; rotation is the only real fix.

## Quick rules

- Adding a new secret? Add the **key with a placeholder** to the committed config, then set the real value in user-secrets (dev) and as an env var (staging/prod). Never commit the value.
- Reviewing a diff? If it introduces a real credential in a tracked file, stop — it belongs in user-secrets / env vars.

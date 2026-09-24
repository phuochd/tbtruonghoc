# tbtruonghoc — Piranha multi-site platform

One Piranha CMS 12.0.0 (.NET 8) instance serving two sites from a single
database:

- **Site A** — `tbtruonghoc` (default site)
- **Site B** — `trongdoitam.net`

The two `Site` records are created idempotently at application startup (see
`src/TbTruongHoc.Web/Data/SiteSeed.cs`) — re-running the app never creates
duplicates.

## Prerequisites

- .NET 8 SDK (pinned via `global.json`; this machine may also have newer
  SDKs installed, but `global.json` forces `dotnet` commands in this repo
  onto 8.0.406)
- Docker Desktop (for the local MariaDB container only — the app itself
  runs via `dotnet run`, not a container)

## One-time local setup: hosts-file aliases

Local dev resolves the two sites by hostname, the same way production does.
Add these two entries to your hosts file so your browser can reach each site
by name (this mapping lives only in `appsettings.Development.json` — the
production hostnames in `appsettings.json` are untouched):

```
127.0.0.1   tbtruonghoc.local
127.0.0.1   trongdoitam.local
```

- Windows: `C:\Windows\System32\drivers\etc\hosts` (edit as Administrator)
- macOS/Linux: `/etc/hosts` (edit with `sudo`)

## One-time local setup: `.env`

`docker compose up` reads MariaDB's credentials from a `.env` file at the
repo root (never committed - see `.gitignore`). Create one before your first
run:

```
cp .env.example .env
```

The defaults in `.env.example` work for local dev as-is; change them if you
want different credentials.

## Running the app

Docker is only for MariaDB — the app itself always runs via `dotnet run`,
no container build needed.

`appsettings.json` deliberately ships with an empty `ConnectionStrings:piranha`
value (no credentials are hardcoded in tracked files). Store it once per
machine in .NET user-secrets instead, using the same values as your `.env`:

1. Start the database: `docker compose up -d mariadb`
2. One-time: save the connection string to user-secrets (kept outside the
   repo, under `%APPDATA%\Microsoft\UserSecrets\`):
   ```
   dotnet user-secrets set "ConnectionStrings:piranha" "server=localhost;port=3307;database=piranha;uid=piranha;password=<value from .env>" --project src/TbTruongHoc.Web
   ```
3. `dotnet run --project src/TbTruongHoc.Web` (talks to the Compose-exposed
   `mariadb` port on `localhost:3307` — mapped off the default `3306` in
   case another local MySQL/MariaDB is already using it)

`Properties/launchSettings.json` makes `dotnet run` start in the
`Development` environment on `http://localhost:5236`. That matters twice:
user-secrets are only loaded in `Development`, and so is the `.local`
hostname mapping above (`appsettings.Development.json`). If you launch the
app some other way (e.g. `dotnet run --no-launch-profile`), set
`ASPNETCORE_ENVIRONMENT=Development` yourself — without it the connection
string is empty and startup fails, and the app falls back to
`appsettings.json`'s production hostnames (`tbtruonghoc.com` /
`trongdoitam.net`).

A `ConnectionStrings__piranha` environment variable still works too, and
overrides user-secrets.

Then visit:

- `http://tbtruonghoc.local:<port>/` — Site A (default)
- `http://trongdoitam.local:<port>/` — Site B
- Any other/unmapped hostname falls back to the default site (Site A).

The Piranha Manager is at `/manager` (credentials are seeded by Piranha's
default identity seed on first run — see the console output on first
startup for the generated admin login).

## Running tests

`tests/TbTruongHoc.Web.Tests` boots the real app in-process against the real
MariaDB (no mocked/in-memory provider), so the database must already be
running and reachable the same way as "Running the app" above:

1. `docker compose up -d mariadb`
2. Make sure the connection string is in user-secrets (step 2 above) — the
   test host runs as `Development`, so it reads the same secret
3. `dotnet test`

## Notes

- Never a second Piranha instance/deployment for Site B — one instance, one
  database, one Manager login, two `Site` records.
- A third `Site` (trongngocanh.com) is explicitly out of scope for this
  phase — do not add it here.

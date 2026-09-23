# tbtruonghoc — Piranha multi-site platform

One Piranha CMS 12.2.0 (.NET 8) instance serving two sites from a single
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
- Docker Desktop (for the local MariaDB + app Compose stack)

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

## Running with Docker Compose (recommended)

```
docker compose up -d
docker compose ps
```

- `mariadb` runs pinned to `mariadb:10.11` (never `:latest`), with a named
  volume (`mariadb-data`) for the database files.
- `piranha-app` builds from `src/TbTruongHoc.Web/Dockerfile`, waits for
  `mariadb`'s healthcheck before starting, and applies Piranha's EF Core
  migrations automatically on first startup.
- Uploaded media is bind-mounted at `./media` on the host so it persists
  across container restarts/rebuilds.

Once containers are up, visit:

- `http://tbtruonghoc.local:8091/` — Site A (default)
- `http://trongdoitam.local:8091/` — Site B
- Any other/unmapped hostname falls back to the default site (Site A).

The Piranha Manager is at `/manager` (credentials are seeded by Piranha's
default identity seed on first run — see the console output on first
startup for the generated admin login).

## Running locally without Docker

`appsettings.json` deliberately ships with an empty `ConnectionStrings:piranha`
value (no credentials are hardcoded in tracked files). Supply it via an
environment variable, using the same values as your `.env`:

1. Start only the database: `docker compose up -d mariadb`
2. Set the connection string for your shell session, e.g. in PowerShell:
   ```
   $env:ConnectionStrings__piranha = "server=localhost;port=3307;database=piranha;uid=piranha;password=<value from .env>"
   ```
   (or the bash equivalent: `export ConnectionStrings__piranha="..."`)
3. `dotnet run --project src/TbTruongHoc.Web` (talks to the Compose-exposed
   `mariadb` port on `localhost:3307` — mapped off the default `3306` in
   case another local MySQL/MariaDB is already using it)

## Running tests

`tests/TbTruongHoc.Web.Tests` boots the real app in-process against the real
MariaDB (no mocked/in-memory provider), so the database must already be
running and reachable the same way as "Running locally without Docker" above:

1. `docker compose up -d mariadb`
2. Set `ConnectionStrings__piranha` in your shell session (same as step 2
   above)
3. `dotnet test`

## Notes

- Never a second Piranha instance/deployment for Site B — one instance, one
  database, one Manager login, two `Site` records.
- A third `Site` (trongngocanh.com) is explicitly out of scope for this
  phase — do not add it here.

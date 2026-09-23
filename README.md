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
value (no credentials are hardcoded in tracked files), so supply it via an
environment variable using the same values as your `.env`:

1. Start the database: `docker compose up -d mariadb`
2. Set the connection string and environment for your shell session, e.g. in
   PowerShell:
   ```
   $env:ConnectionStrings__piranha = "server=localhost;port=3307;database=piranha;uid=piranha;password=<value from .env>"
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   ```
   (or the bash equivalent: `export ConnectionStrings__piranha="..."` /
   `export ASPNETCORE_ENVIRONMENT="Development"`)
3. `dotnet run --project src/TbTruongHoc.Web` (talks to the Compose-exposed
   `mariadb` port on `localhost:3307` — mapped off the default `3306` in
   case another local MySQL/MariaDB is already using it)

`ASPNETCORE_ENVIRONMENT=Development` is required for the `.local` hostname
mapping above (`appsettings.Development.json`) to apply — without it, the
app falls back to `appsettings.json`'s production hostnames
(`tbtruonghoc.com` / `trongdoitam.net`) and won't match your hosts-file
entries.

By default `dotnet run` listens on its own Kestrel port (printed to the
console, e.g. `http://localhost:5236`) — visit:

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
2. Set `ConnectionStrings__piranha` in your shell session (same as step 2
   above)
3. `dotnet test`

## Notes

- Never a second Piranha instance/deployment for Site B — one instance, one
  database, one Manager login, two `Site` records.
- A third `Site` (trongngocanh.com) is explicitly out of scope for this
  phase — do not add it here.

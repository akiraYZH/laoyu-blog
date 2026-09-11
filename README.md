# Laoyu Blog

A production-minded full-stack blog built with ASP.NET Core, Vue, PostgreSQL, and Docker Compose. The project demonstrates API design, authentication and authorization, relational data modelling, persistent image uploads, automated tests, health checks, and separate development and production container workflows.

The implementation is accompanied by a [step-by-step Chinese engineering series](./note/README.md) that explains how the project was built and why each architectural decision was made.

## Architecture

```text
Browser
  |
  +-- Development: Vite (:5173)
  |       |
  |       +-- /api and /uploads proxy
  |
  +-- Production: Nginx (:80 inside the container)
          |
          +-- ASP.NET Core API (:8080, internal only)
                    |
                    +-- PostgreSQL (:5432, internal only)

Persistent data:
  postgres_data -> PostgreSQL data
  uploads_data  -> uploaded blog images
```

Only the frontend port is published by the production Compose stack. The API and PostgreSQL services remain on the internal Compose network.

## Features

- Blog post CRUD with stable pagination and slug-based routes
- Many-to-many blog categories with category filtering
- Markdown editing and single-image upload
- ASP.NET Core Identity administrator account
- JWT Bearer authentication and role-based write protection
- Draft, publish, and unpublish workflow
- DTO validation and RFC 7807-style error responses
- EF Core migrations with PostgreSQL
- API and database health checks
- Request-scoped structured logging with trace and user identifiers
- xUnit unit and integration tests
- Vue 3, Pinia, Vue Router, Ant Design Vue, and Tailwind CSS frontend
- Multi-stage development and production Docker images

## Prerequisites

The container workflow requires:

- Docker Desktop, or Docker Engine with Docker Compose
- GNU Make

Running the applications directly also requires the .NET 10 SDK and a Node.js version supported by [`frontend/package.json`](./frontend/package.json).

## Configuration

Create a local environment file from the committed template:

```bash
cp .env.example .env.development
```

Replace every `replace_with_...` placeholder. The administrator password must contain uppercase and lowercase letters, a number, and a non-alphanumeric character.

Environment files containing real credentials are ignored by Git. Never commit `.env.development` or `.env.production`.

## Development

Start PostgreSQL, the ASP.NET Core development container, and the Vite development server with Compose Watch:

```bash
make dev
```

Development endpoints:

- Frontend: <http://localhost:5173>
- API: <http://localhost:8080>
- Health check: <http://localhost:8080/health>

Stop the development stack:

```bash
make down
```

## Tests

Run the backend test project:

```bash
dotnet test tests/LaoyuBlog.Api.Tests/LaoyuBlog.Api.Tests.csproj
```

Run the frontend unit tests:

```bash
cd frontend
npm run test:unit -- --run
```

The initial frontend test suite covers blog-list loading, API failure state, cached detail loading, and article-card rendering.

Run the frontend type check and production build:

```bash
cd frontend
npm run build
```

## Local Production-Like Run

The repository includes a separate production Compose file. It builds the ASP.NET Core runtime image and serves the compiled Vue application through Nginx.

Create a production environment file:

```bash
cp .env.example .env.production
```

Use new, strong values for `POSTGRES_PASSWORD`, `ADMIN_PASSWORD`, and `JWT_KEY`. Set `HTTP_PORT=8081` when testing locally to avoid conflicting with another service on port 80.

Start the stack in the background:

```bash
make prod-up
```

Inspect service health and logs:

```bash
make prod-ps
make prod-logs
```

Open the configured frontend port, for example <http://localhost:8081>. On first startup, the API applies pending EF Core migrations before seeding the administrator.

Stop the stack without deleting persistent data:

```bash
make prod-down
```

The named PostgreSQL and upload volumes survive `down` and subsequent `up` operations. Running `docker compose down -v` deletes those volumes and their data, so it should only be used for an intentional reset.

## API Overview

| Method | Route | Access | Purpose |
|---|---|---|---|
| `GET` | `/api/blogs` | Public | List published posts; administrators can also read drafts |
| `GET` | `/api/blogs/by-slug/{slug}` | Public | Read one published post by slug |
| `POST` | `/api/auth/login` | Public | Authenticate the administrator and issue a JWT |
| `POST` | `/api/blogs` | Admin | Create a draft post |
| `PUT` | `/api/blogs/{id}` | Admin | Update a post |
| `POST` | `/api/blogs/{id}/publish` | Admin | Publish a post |
| `POST` | `/api/blogs/{id}/unpublish` | Admin | Return a post to draft status |
| `DELETE` | `/api/blogs/{id}` | Admin | Delete a post |
| `POST` | `/api/images` | Admin | Upload one image |
| `GET` | `/health` | Public | Check API and database readiness |

## Production Boundary

The production Compose stack has been exercised locally, including migrations, administrator login, image persistence, health checks, and draft visibility. It does not by itself provide public TLS certificates, off-site backups, remote object storage, or centralized log retention.

Before exposing the application to the internet, place it behind an HTTPS-capable edge proxy or hosting platform and define backup and secret-management procedures for that environment.

## Engineering Notes

The [Chinese technical note series](./note/README.md) starts from an empty ASP.NET Core Controller API and builds the current system incrementally. Each main-line article includes prerequisites, file locations, implementation reasoning, and a concrete verification step.

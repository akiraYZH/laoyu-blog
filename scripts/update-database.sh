#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$PROJECT_ROOT/.env.development"

if [[ $# -gt 1 ]]; then
    echo "Usage: ./scripts/update-database.sh [TargetMigration]" >&2
    echo "Example: ./scripts/update-database.sh InitialCreate" >&2
    echo "Rollback all migrations: ./scripts/update-database.sh 0" >&2
    exit 64
fi

TARGET_MIGRATION="${1:-}"

if [[ -n "$TARGET_MIGRATION" && ! "$TARGET_MIGRATION" =~ ^([A-Za-z_][A-Za-z0-9_]*|[0-9]+(_[A-Za-z_][A-Za-z0-9_]*)?)$ ]]; then
    echo "Target migration must be a migration name, migration ID, or 0." >&2
    exit 64
fi

if [[ ! -f "$ENV_FILE" ]]; then
    echo "Missing development environment file: $ENV_FILE" >&2
    exit 1
fi

cd "$PROJECT_ROOT"

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

: "${POSTGRES_DB:?POSTGRES_DB is missing from .env.development}"
: "${POSTGRES_USER:?POSTGRES_USER is missing from .env.development}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is missing from .env.development}"

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
export ConnectionStrings__LaoyuBlog="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

if ! dotnet ef --version >/dev/null 2>&1; then
    echo "The project-local dotnet-ef tool is not available." >&2
    echo "Run 'dotnet tool restore' and try again." >&2
    exit 1
fi

if [[ -n "$TARGET_MIGRATION" ]]; then
    echo "Updating AppDbContext database to '$TARGET_MIGRATION'..."
    dotnet ef database update "$TARGET_MIGRATION" --context AppDbContext
else
    echo "Updating AppDbContext database to the latest migration..."
    dotnet ef database update --context AppDbContext
fi

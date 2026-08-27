#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$PROJECT_ROOT/.env.development"

if [[ $# -ne 1 ]]; then
    echo "Usage: ./scripts/add-migration.sh <MigrationName>" >&2
    echo "Example: ./scripts/add-migration.sh InitialCreate" >&2
    exit 64
fi

MIGRATION_NAME="$1"

if [[ ! "$MIGRATION_NAME" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
    echo "Migration name must be a valid C# identifier." >&2
    echo "Example: InitialCreate or AddPublishedAt" >&2
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

echo "Creating migration '$MIGRATION_NAME' for AppDbContext..."

dotnet ef migrations add "$MIGRATION_NAME" \
    --context AppDbContext \
    --output-dir Data/Migrations

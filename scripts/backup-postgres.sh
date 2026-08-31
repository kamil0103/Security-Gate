#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
BACKUP_DIR="${PROJECT_DIR}/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/security-gateway-${TIMESTAMP}.sql.gz"

mkdir -p "${BACKUP_DIR}"

POSTGRES_CONTAINER="security-gateway-postgres"
POSTGRES_DB="${POSTGRES_DB:-securitygateway}"
POSTGRES_USER="${POSTGRES_USER:-securitygateway}"

if ! docker ps --format '{{.Names}}' | grep -q "^${POSTGRES_CONTAINER}$"; then
  echo "Error: PostgreSQL container '${POSTGRES_CONTAINER}' is not running."
  exit 1
fi

echo "Creating backup: ${BACKUP_FILE}"
docker exec -e PGPASSWORD="${POSTGRES_PASSWORD}" "${POSTGRES_CONTAINER}" \
  pg_dump -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" --clean --if-exists \
  | gzip > "${BACKUP_FILE}"

echo "Backup completed: ${BACKUP_FILE}"

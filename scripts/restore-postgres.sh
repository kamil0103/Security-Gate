#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <backup-file.sql.gz>"
  exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "${BACKUP_FILE}" ]; then
  echo "Error: Backup file not found: ${BACKUP_FILE}"
  exit 1
fi

POSTGRES_CONTAINER="security-gateway-postgres"
POSTGRES_DB="${POSTGRES_DB:-securitygateway}"
POSTGRES_USER="${POSTGRES_USER:-securitygateway}"

if ! docker ps --format '{{.Names}}' | grep -q "^${POSTGRES_CONTAINER}$"; then
  echo "Error: PostgreSQL container '${POSTGRES_CONTAINER}' is not running."
  exit 1
fi

echo "Restoring from backup: ${BACKUP_FILE}"
gunzip -c "${BACKUP_FILE}" | docker exec -i -e PGPASSWORD="${POSTGRES_PASSWORD}" "${POSTGRES_CONTAINER}" \
  psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}"

echo "Restore completed."

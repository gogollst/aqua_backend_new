#!/bin/sh
set -e
mc alias set local http://minio:9000 ${MINIO_ROOT_USER:-aqua-dev} ${MINIO_ROOT_PASSWORD:-aqua-dev-secret}
mc mb -p local/aqua-attachments-acme    || true
mc mb -p local/aqua-attachments-default || true
mc mb -p local/aqua-reports-acme        || true
mc mb -p local/aqua-reports-default     || true
echo "MinIO buckets created."

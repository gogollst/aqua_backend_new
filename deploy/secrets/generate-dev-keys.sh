#!/bin/sh
set -e
mkdir -p deploy/secrets/identity
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out deploy/secrets/identity/private.pem
openssl rsa -pubout -in deploy/secrets/identity/private.pem -out deploy/secrets/identity/public.pem
echo "Dev RSA keys created in deploy/secrets/identity/"

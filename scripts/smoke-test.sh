#!/usr/bin/env bash
set -euo pipefail

base_url="${FLUXORA_BASE_URL:-http://localhost:8080}"
base_url="${base_url%/}"
smoke_email="${FLUXORA_SMOKE_EMAIL:-}"
smoke_password="${FLUXORA_SMOKE_PASSWORD:-}"

probe() {
  local label="$1"
  local path="$2"

  curl --fail-with-body --silent --show-error \
    --connect-timeout 5 --max-time 20 \
    --retry 10 --retry-delay 2 --retry-max-time 90 --retry-all-errors \
    "${base_url}${path}" >/dev/null
  printf 'ok - %s\n' "$label"
}

probe "liveness" "/health/live"
probe "PostgreSQL readiness" "/health/ready"

if [[ -z "$smoke_email" && -z "$smoke_password" ]]; then
  printf 'skip - authenticated checks (set FLUXORA_SMOKE_EMAIL and FLUXORA_SMOKE_PASSWORD)\n'
  exit 0
fi

if [[ -z "$smoke_email" || -z "$smoke_password" ]]; then
  printf 'error - both FLUXORA_SMOKE_EMAIL and FLUXORA_SMOKE_PASSWORD are required\n' >&2
  exit 2
fi

if ! command -v jq >/dev/null 2>&1; then
  printf 'error - jq is required for authenticated smoke checks\n' >&2
  exit 2
fi

login_payload="$(jq -n --arg email "$smoke_email" --arg password "$smoke_password" \
  '{email: $email, password: $password}')"
login_response="$(printf '%s' "$login_payload" | curl --fail-with-body --silent --show-error \
  --connect-timeout 5 --max-time 20 \
  --header 'Content-Type: application/json' \
  --data-binary @- \
  "${base_url}/api/auth/login")"
access_token="$(printf '%s' "$login_response" | jq -er '.accessToken | select(length > 0)')"
printf 'ok - login\n'

curl --fail-with-body --silent --show-error \
  --connect-timeout 5 --max-time 20 \
  --header "Authorization: Bearer ${access_token}" \
  "${base_url}/api/auth/me" >/dev/null
printf 'ok - authenticated identity\n'

curl --fail-with-body --silent --show-error \
  --connect-timeout 5 --max-time 20 \
  --header "Authorization: Bearer ${access_token}" \
  "${base_url}/api/reports/dashboard-summary" >/dev/null
printf 'ok - dashboard report\n'

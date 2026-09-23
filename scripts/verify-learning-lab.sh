#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

BASE_URL="${BASE_URL:-http://localhost:${API_PORT:-8080}}"
RUN_ID="${RUN_ID:-$(date -u +%s)-$$}"
KEEP_STACK="${KEEP_STACK:-0}"
TMP_DIR="$(mktemp -d)"
LOG_SINCE="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

if [[ ! "$RUN_ID" =~ ^[A-Za-z0-9-]{1,20}$ ]]; then
    echo "RUN_ID must contain 1-20 letters, digits, or hyphens." >&2
    exit 2
fi

cleanup() {
    local exit_code=$?
    rm -rf "$TMP_DIR"
    if [[ "$KEEP_STACK" != "1" ]]; then
        docker compose down >/dev/null 2>&1 || true
    fi
    exit "$exit_code"
}
trap cleanup EXIT

step() {
    printf '\n==> %s\n' "$1"
}

fail() {
    echo "ERROR: $*" >&2
    exit 1
}

require_command() {
    command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

json_field() {
    local file=$1
    local field=$2
    python3 -c '
import json
import sys
with open(sys.argv[1], encoding="utf-8") as source:
    value = json.load(source)[sys.argv[2]]
print(value)
' "$file" "$field"
}

assert_json_value() {
    local file=$1
    local field=$2
    local expected=$3
    local actual
    actual="$(json_field "$file" "$field")"
    [[ "$actual" == "$expected" ]] || fail \
        "Expected $field=$expected in $file, got $actual. Body: $(cat "$file")"
}

request() {
    local name=$1
    local expected=$2
    local method=$3
    local url=$4
    shift 4

    local status
    status="$(curl \
        --silent \
        --show-error \
        --connect-timeout 5 \
        --max-time 30 \
        --request "$method" \
        --dump-header "$TMP_DIR/$name.headers" \
        --output "$TMP_DIR/$name.body" \
        --write-out '%{http_code}' \
        "$url" \
        "$@")"

    if [[ "$status" != "$expected" ]]; then
        echo "Response headers:" >&2
        tr -d '\r' < "$TMP_DIR/$name.headers" >&2 || true
        echo "Response body:" >&2
        cat "$TMP_DIR/$name.body" >&2 || true
        fail "$method $url returned $status; expected $expected"
    fi

    printf 'PASS %-34s HTTP %s\n' "$name" "$status" >&2
}

wait_for_api() {
    local attempt
    for attempt in $(seq 1 60); do
        if curl \
            --silent \
            --fail \
            --connect-timeout 2 \
            --max-time 3 \
            "$BASE_URL/openapi/v1.json" \
            >/dev/null 2>&1; then
            return 0
        fi
        sleep 2
    done
    docker compose logs --no-color api >&2 || true
    fail "API did not become ready at $BASE_URL within 120 seconds"
}

login() {
    local name=$1
    local user_name=$2
    local password=$3
    request \
        "login-$name" \
        200 \
        POST \
        "$BASE_URL/api/v1/auth/login" \
        --header 'Content-Type: application/json' \
        --data "{\"userName\":\"$user_name\",\"password\":\"$password\"}"
    json_field "$TMP_DIR/login-$name.body" accessToken
}

for command in dotnet docker curl python3 grep git; do
    require_command "$command"
done
docker compose version >/dev/null

step "Static gate"
dotnet tool restore
dotnet restore
dotnet build --no-restore --configuration Debug --warnaserror
docker compose config --quiet

step "Start the Development stack"
echo "Verification run ID: $RUN_ID"
echo "The script creates synthetic rows, restarts the API once, and preserves the PostgreSQL volume."
docker compose up -d --build
wait_for_api

step "Migration and schema gate"
POSTGRES_DB_VALUE="${POSTGRES_DB:-domainlab}"
POSTGRES_USER_VALUE="${POSTGRES_USER:-domainlab}"
POSTGRES_PASSWORD_VALUE="${POSTGRES_PASSWORD:-domainlab_dev_only}"
POSTGRES_PORT_VALUE="${POSTGRES_PORT:-5432}"
ConnectionStrings__DomainDb="Host=localhost;Port=$POSTGRES_PORT_VALUE;Database=$POSTGRES_DB_VALUE;Username=$POSTGRES_USER_VALUE;Password=$POSTGRES_PASSWORD_VALUE;GSS Encryption Mode=Disable" \
    dotnet tool run dotnet-ef migrations list --no-build \
    > "$TMP_DIR/migrations.txt"
for migration in \
    20260919171456_InitialSchemas \
    20260919174514_AddAuthentication \
    20260919175246_AddRefreshTokenConcurrency \
    20260919182752_AddDomainWorkflows; do
    grep -Fq "$migration" "$TMP_DIR/migrations.txt" || fail "Migration not listed: $migration"
done
if grep -qi '(Pending)' "$TMP_DIR/migrations.txt"; then
    cat "$TMP_DIR/migrations.txt" >&2
    fail "At least one migration is pending"
fi

SCHEMA_COUNTS="$(docker compose exec -T postgres \
    psql \
    -U "$POSTGRES_USER_VALUE" \
    -d "$POSTGRES_DB_VALUE" \
    -At \
    -F '|' \
    -c "select table_schema, count(*)
        from information_schema.tables
        where table_schema in ('auth','healthcare','industrial','logistics','banking')
        group by table_schema
        order by table_schema;")"
printf '%s\n' "$SCHEMA_COUNTS"
for schema in auth healthcare industrial logistics banking; do
    grep -Eq "^${schema}\|[1-9][0-9]*$" <<< "$SCHEMA_COUNTS" || \
        fail "Schema $schema has no mapped tables"
done

step "Host and OpenAPI gate"
request openapi 200 GET "$BASE_URL/openapi/v1.json" --header 'Accept: application/json'
request swagger 200 GET "$BASE_URL/swagger/index.html"
python3 - "$TMP_DIR/openapi.body" <<'PY'
import json
import sys

with open(sys.argv[1], encoding="utf-8") as source:
    document = json.load(source)

required_paths = {
    "/api/v1/auth/login",
    "/api/v1/healthcare/patients",
    "/api/v1/industrial/assets",
    "/api/v1/logistics/shipments",
    "/api/v1/banking/transfers",
}
missing_paths = sorted(required_paths - set(document.get("paths", {})))
if missing_paths:
    raise SystemExit(f"OpenAPI paths missing: {', '.join(missing_paths)}")

operation_tags = {
    tag
    for path_item in document.get("paths", {}).values()
    for operation in path_item.values()
    if isinstance(operation, dict)
    for tag in operation.get("tags", [])
}
required_tags = {"Auth", "Healthcare", "Industrial", "Logistics", "Banking"}
missing_tags = sorted(required_tags - operation_tags)
if missing_tags:
    raise SystemExit(f"OpenAPI tags missing: {', '.join(missing_tags)}")
PY

echo "PASS OpenAPI paths and module tags"

step "Authentication and authorization gate"
request unauthenticated-me 401 GET "$BASE_URL/api/v1/auth/me"
HEALTH_TOKEN="$(login healthcare healthcare.demo 'HealthDemo!123')"
request healthcare-policy-allowed 200 GET \
    "$BASE_URL/api/v1/auth/probes/healthcare" \
    --header "Authorization: Bearer $HEALTH_TOKEN"
request healthcare-wrong-role 403 GET \
    "$BASE_URL/api/v1/auth/probes/banking-transfer" \
    --header "Authorization: Bearer $HEALTH_TOKEN"

step "Healthcare validation, domain errors, and persistence gate"
request patient-validation 400 POST \
    "$BASE_URL/api/v1/healthcare/patients" \
    --header "Authorization: Bearer $HEALTH_TOKEN" \
    --header 'Content-Type: application/json' \
    --data '{}'
assert_json_value "$TMP_DIR/patient-validation.body" errorCode validation_failed

MRN="MRN-TOUR-$RUN_ID"
request patient-create 201 POST \
    "$BASE_URL/api/v1/healthcare/patients" \
    --header "Authorization: Bearer $HEALTH_TOKEN" \
    --header 'Content-Type: application/json' \
    --data "{\"medicalRecordNumber\":\"$MRN\",\"displayName\":\"Synthetic Verification Patient\",\"birthDate\":\"1988-05-10\"}"
PATIENT_ID="$(json_field "$TMP_DIR/patient-create.body" id)"

request patient-read 200 GET \
    "$BASE_URL/api/v1/healthcare/patients/$PATIENT_ID" \
    --header "Authorization: Bearer $HEALTH_TOKEN"
assert_json_value "$TMP_DIR/patient-read.body" medicalRecordNumber "$MRN"

request patient-not-found 404 GET \
    "$BASE_URL/api/v1/healthcare/patients/00000000-0000-0000-0000-000000000001" \
    --header "Authorization: Bearer $HEALTH_TOKEN"
assert_json_value "$TMP_DIR/patient-not-found.body" errorCode patient_not_found

request patient-duplicate 409 POST \
    "$BASE_URL/api/v1/healthcare/patients" \
    --header "Authorization: Bearer $HEALTH_TOKEN" \
    --header 'Content-Type: application/json' \
    --data "{\"medicalRecordNumber\":\"$MRN\",\"displayName\":\"Duplicate Synthetic Patient\",\"birthDate\":\"1988-05-10\"}"
assert_json_value "$TMP_DIR/patient-duplicate.body" errorCode patient_already_exists

request appointment-domain-rule 422 POST \
    "$BASE_URL/api/v1/healthcare/patients/$PATIENT_ID/appointments" \
    --header "Authorization: Bearer $HEALTH_TOKEN" \
    --header 'Content-Type: application/json' \
    --data '{"startsAtUtc":"2020-01-01T00:00:00Z","provider":"Synthetic Provider"}'
assert_json_value "$TMP_DIR/appointment-domain-rule.body" errorCode appointment_must_be_future

CORRELATION_ID="verify-$RUN_ID"
request correlation 200 GET \
    "$BASE_URL/api/v1/healthcare/patients/$PATIENT_ID" \
    --header "Authorization: Bearer $HEALTH_TOKEN" \
    --header "X-Correlation-Id: $CORRELATION_ID"
tr -d '\r' < "$TMP_DIR/correlation.headers" | \
    grep -Fqi "X-Correlation-Id: $CORRELATION_ID" || \
    fail "Correlation response header was not preserved"

step "Persistence across API restart"
docker compose restart api >/dev/null
wait_for_api
request patient-after-restart 200 GET \
    "$BASE_URL/api/v1/healthcare/patients/$PATIENT_ID" \
    --header "Authorization: Bearer $HEALTH_TOKEN"
assert_json_value "$TMP_DIR/patient-after-restart.body" id "$PATIENT_ID"

step "Application cache and response-compression gate"
INDUSTRIAL_TOKEN="$(login industrial industrial.demo 'IndustrialDemo!123')"
ASSET_CODE="TOUR-ASSET-$RUN_ID"
request asset-create 201 POST \
    "$BASE_URL/api/v1/industrial/assets" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
    --header 'Content-Type: application/json' \
    --data "{\"assetCode\":\"$ASSET_CODE\",\"displayName\":\"Synthetic Verification Asset\"}"
ASSET_ID="$(json_field "$TMP_DIR/asset-create.body" id)"

request telemetry-create-1 202 POST \
    "$BASE_URL/api/v1/industrial/assets/$ASSET_ID/telemetry" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
    --header 'Content-Type: application/json' \
    --data '{"value":42,"unit":"percent"}'
request telemetry-cache-miss 200 GET \
    "$BASE_URL/api/v1/industrial/assets/$ASSET_ID/telemetry?verification=miss" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN"
request telemetry-cache-hit 200 GET \
    "$BASE_URL/api/v1/industrial/assets/$ASSET_ID/telemetry?verification=hit" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN"

request telemetry-create-2 202 POST \
    "$BASE_URL/api/v1/industrial/assets/$ASSET_ID/telemetry" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
    --header 'Content-Type: application/json' \
    --data '{"value":43,"unit":"percent"}'
request telemetry-after-invalidation 200 GET \
    "$BASE_URL/api/v1/industrial/assets/$ASSET_ID/telemetry?verification=invalidated" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN"

request industrial-compression 200 GET \
    "$BASE_URL/api/v1/industrial/assets/$ASSET_ID/telemetry?verification=compression" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
    --header 'Accept-Encoding: gzip'
tr -d '\r' < "$TMP_DIR/industrial-compression.headers" | \
    grep -Eqi '^Content-Encoding: gzip$' || \
    fail "Industrial telemetry response was not gzip-compressed"

BANKING_TOKEN="$(login banking banking.demo 'BankingDemo!123')"
CUSTOMER_REFERENCE="TOUR-CUSTOMER-$RUN_ID"
request customer-create 201 POST \
    "$BASE_URL/api/v1/banking/customers" \
    --header "Authorization: Bearer $BANKING_TOKEN" \
    --header 'Content-Type: application/json' \
    --data "{\"customerReference\":\"$CUSTOMER_REFERENCE\",\"displayName\":\"Synthetic Verification Customer\"}"
CUSTOMER_ID="$(json_field "$TMP_DIR/customer-create.body" id)"

ACCOUNT_REFERENCE="TOUR-ACCOUNT-$RUN_ID"
request account-create 201 POST \
    "$BASE_URL/api/v1/banking/customers/$CUSTOMER_ID/accounts" \
    --header "Authorization: Bearer $BANKING_TOKEN" \
    --header 'Content-Type: application/json' \
    --data "{\"accountReference\":\"$ACCOUNT_REFERENCE\",\"currency\":\"SIM\",\"initialBalanceMinor\":10000}"
ACCOUNT_ID="$(json_field "$TMP_DIR/account-create.body" id)"

request banking-no-compression 200 GET \
    "$BASE_URL/api/v1/banking/accounts/$ACCOUNT_ID" \
    --header "Authorization: Bearer $BANKING_TOKEN" \
    --header 'Accept-Encoding: gzip'
if tr -d '\r' < "$TMP_DIR/banking-no-compression.headers" | \
    grep -Eqi '^Content-Encoding:'; then
    fail "Banking response unexpectedly used response compression"
fi

step "Isolated rate-limit gate"
for attempt in 1 2 3; do
    request "command-$attempt" 202 POST \
        "$BASE_URL/api/v1/industrial/assets/$ASSET_ID/commands" \
        --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
        --header 'Content-Type: application/json' \
        --data "{\"idempotencyKey\":\"verify-$RUN_ID-$attempt\",\"commandType\":\"start\"}"
done
request command-rate-limited 429 POST \
    "$BASE_URL/api/v1/industrial/assets/$ASSET_ID/commands" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
    --header 'Content-Type: application/json' \
    --data "{\"idempotencyKey\":\"verify-$RUN_ID-4\",\"commandType\":\"start\"}"
tr -d '\r' < "$TMP_DIR/command-rate-limited.headers" | \
    grep -Eqi '^Retry-After: 5$' || \
    fail "Rate-limit response did not include Retry-After: 5"

step "Operational log gate"
docker compose logs --since "$LOG_SINCE" --no-color api > "$TMP_DIR/api.log"
grep -F "Completed GET /api/v1/healthcare/patients/$PATIENT_ID with 200" \
    "$TMP_DIR/api.log" | grep -Fq "for correlation $CORRELATION_ID" || \
    fail "The correlated patient request was not observable in API completion logs"
grep -Fq "Memory cache miss for industrial:telemetry:$ASSET_ID" "$TMP_DIR/api.log" || \
    fail "Industrial telemetry cache miss was not logged"
grep -Fq "Memory cache hit for industrial:telemetry:$ASSET_ID" "$TMP_DIR/api.log" || \
    fail "Industrial telemetry cache hit was not logged"
grep -Fq "Removed industrial:telemetry:$ASSET_ID from memory cache" "$TMP_DIR/api.log" || \
    fail "Industrial telemetry cache invalidation was not logged"
if grep -Eq 'Unhandled failure|Unhandled exception' "$TMP_DIR/api.log"; then
    grep -E 'Unhandled failure|Unhandled exception' "$TMP_DIR/api.log" >&2
    fail "Unexpected unhandled failure appeared in the verification log window"
fi

echo "PASS correlation, cache miss/hit/invalidation, and unexpected-error log checks"

step "Verification complete"
echo "Run ID: $RUN_ID"
echo "Baseline commit: $(git rev-parse HEAD)"
echo "API base URL: $BASE_URL"
echo "Synthetic rows remain in the PostgreSQL volume so persistence can be inspected."
if [[ "$KEEP_STACK" == "1" ]]; then
    echo "KEEP_STACK=1: Compose services remain running."
else
    echo "Compose services will stop; the PostgreSQL volume will be preserved."
fi

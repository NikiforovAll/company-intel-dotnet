#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ARTIFACTS="$REPO_ROOT/artifacts/test-results"
TIMESTAMP=$(date +%Y%m%dT%H%M%S)
LOG_FILE="$ARTIFACTS/$TIMESTAMP.log"

mkdir -p "$ARTIFACTS"

TEST_PROJECT="${1:-tests/CompanyIntel.AppHost.Tests}"
FILTER="${2:-}"

FILTER_ARG=""
if [[ -n "$FILTER" ]]; then
    FILTER_ARG="--filter $FILTER"
fi

echo "=== Test run: $TIMESTAMP ==="
echo "Project: $TEST_PROJECT"
echo "Filter:  ${FILTER:-<all>}"
echo "Log:     $LOG_FILE"
echo ""

dotnet test "$REPO_ROOT/$TEST_PROJECT" \
    --logger "trx;LogFileName=$TIMESTAMP.trx" \
    --logger "html;LogFileName=$TIMESTAMP.html" \
    --logger "console;verbosity=detailed" \
    --results-directory "$ARTIFACTS" \
    $FILTER_ARG \
    2>&1 | tee "$LOG_FILE"

EXIT_CODE=${PIPESTATUS[0]}

echo ""
echo "=== Results ==="
echo "Log:  $LOG_FILE"
echo "TRX:  $ARTIFACTS/$TIMESTAMP.trx"
echo "HTML: $ARTIFACTS/$TIMESTAMP.html"

exit $EXIT_CODE

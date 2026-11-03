#!/usr/bin/env sh
set -eu
cd "$(dirname "$0")/.."
python3 tools/generate-foundation-models.py --check
python3 tools/generate-core.py --check
python3 tools/generate-core-fixtures.py --check
python3 tools/generate-endpoint-invoker.py --check
python3 tools/generate-coverage.py --check
python3 tools/check-readme.py

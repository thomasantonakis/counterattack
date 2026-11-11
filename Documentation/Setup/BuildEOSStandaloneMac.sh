#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
pipeline() { unity command "$@" --project-path "$repo_root" --caller codex --skill unity-cli --format json; }
unity status --project-path "$repo_root" --format json | jq -e '.success and any(.data.instances[]?; .state == "ready")' >/dev/null
unity recompile --project-path "$repo_root" --format json | jq -e '.success and .data.compilationFailed == false' >/dev/null
pipeline editor_status | jq -e '.success and .data.result.playMode == "stopped" and (.data.result.compiling | not)' >/dev/null
pipeline list_open_scenes | jq -e '.success and all(.data.result.scenes[]; .isDirty == false)' >/dev/null
pipeline test_status | jq -e '.success and (.data.result.status == "completed" or .data.result.status == "no_tests")' >/dev/null
printf 'Building only the standalone EOS proof scene through the open Editor...\n'
queued="$(pipeline build --target StandaloneOSX --outputPath Build/EOSProof/Mac/CounterattackEOSProof.app --scenes '["Assets/Scenes/EOSStandaloneProof.unity"]' --confirm true)"
printf '%s\n' "$queued" | jq -e '.success and .data.result.status == "queued"' >/dev/null
build_id="$(printf '%s\n' "$queued" | jq -r '.data.result.buildId')"
for ((i=0; i<600; i++)); do
    status="$(pipeline build_status)"
    printf '%s\n' "$status" | jq -e --arg id "$build_id" '.success and .data.result.buildId == $id' >/dev/null
    if [ "$(printf '%s\n' "$status" | jq -r '.data.result.status')" = completed ]; then
        printf '%s\n' "$status" | jq '.data.result | {status, result, totalErrors, totalWarnings, errors, warnings}'
        printf '%s\n' "$status" | jq -e '.data.result.result == "Succeeded"' >/dev/null
        printf 'Mac proof build ready. Run: bash Documentation/Setup/RunEOSStandaloneMac.sh login\n'
        exit 0
    fi
    sleep 2
done
printf 'Build polling timed out; inspect build_status before doing anything else.\n' >&2
exit 1

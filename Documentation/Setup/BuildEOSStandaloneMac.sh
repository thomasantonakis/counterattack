#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
scene=Assets/Scenes/EOSStandaloneProof.unity
output=Build/EOSProof/Mac/CounterattackEOSProof.app
mode="${1:-packets}"
case "$mode" in
    packets) ;;
    gameplay) scene=Assets/Scenes/OnlineGroundPassProof.unity; output=Build/OnlineGroundPass/Mac/CounterattackOnlineGroundPass.app ;;
    room) scene=Assets/Scenes/OnlineRoomProof.unity; output=Build/OnlineRoom/Mac/CounterattackOnlineRoom.app ;;
    *) printf 'Usage: %s [packets|gameplay|room]\n' "$0" >&2; exit 2 ;;
esac
pipeline() { unity command "$@" --project-path "$repo_root" --caller codex --skill unity-cli --format json; }
unity status --project-path "$repo_root" --format json | jq -e '.success and any(.data.instances[]?; .state == "ready")' >/dev/null
unity recompile --project-path "$repo_root" --format json | jq -e '.success and .data.compilationFailed == false' >/dev/null
ready_deadline=$((SECONDS + 90))
while :; do
    if editor="$(pipeline editor_status)" && printf '%s\n' "$editor" | jq -e '.success and .data.result.playMode == "stopped" and (.data.result.compiling | not) and (.data.result.domainReloadInProgress | not)' >/dev/null; then break; fi
    if [ "$SECONDS" -ge "$ready_deadline" ]; then printf 'Editor did not finish reload; inspect its log/dialog before retrying.\n' >&2; exit 1; fi
    sleep 2
done
pipeline list_open_scenes | jq -e '.success and all(.data.result.scenes[]; .isDirty == false)' >/dev/null
pipeline test_status | jq -e '.success and (.data.result.status == "completed" or .data.result.status == "no_tests")' >/dev/null
printf 'Building only the standalone EOS proof scene through the open Editor...\n'
scenes="$(jq -cn --arg scene "$scene" '[$scene]')"
queued="$(pipeline build --target StandaloneOSX --outputPath "$output" --scenes "$scenes" --confirm true)"
printf '%s\n' "$queued" | jq -e '.success and .data.result.status == "queued"' >/dev/null
build_id="$(printf '%s\n' "$queued" | jq -r '.data.result.buildId')"
for ((i=0; i<600; i++)); do
    status="$(pipeline build_status)"
    printf '%s\n' "$status" | jq -e --arg id "$build_id" '.success and .data.result.buildId == $id' >/dev/null
    if [ "$(printf '%s\n' "$status" | jq -r '.data.result.status')" = completed ]; then
        printf '%s\n' "$status" | jq '.data.result | {status, result, totalErrors, totalWarnings, errors, warnings}'
        printf '%s\n' "$status" | jq -e '.data.result.result == "Succeeded"' >/dev/null
        printf 'Mac proof build ready. Run: bash Documentation/Setup/RunEOSStandaloneMac.sh login %s\n' "$mode"
        exit 0
    fi
    sleep 2
done
printf 'Build polling timed out; inspect build_status before doing anything else.\n' >&2
exit 1

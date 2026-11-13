#!/usr/bin/env bash
set -euo pipefail
role="${1:-guest}"
mode="${2:-packets}"
interaction="${3:-automatic}"
case "$interaction" in automatic) ;; manual) if [ "$role" != guest ] || { [ "$mode" != gameplay ] && [ "$mode" != room ]; }; then printf 'Manual requires guest gameplay or room.\n' >&2; exit 2; fi ;; *) printf 'Expected automatic or manual.\n' >&2; exit 2 ;; esac
case "$role" in guest|login|local) ;; *) printf 'Usage: %s [login|guest|local] [packets|gameplay]\n' "$0" >&2; exit 2 ;; esac
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
app=Build/EOSProof/Mac/CounterattackEOSProof.app
handoff_name=ActiveEOSStandaloneLobby.json
handoff_kind=standalone-relay-v1
case "$mode" in
    packets) if [ "$role" = local ]; then printf 'Local role requires gameplay mode.\n' >&2; exit 2; fi ;;
    gameplay) app=Build/OnlineGroundPass/Mac/CounterattackOnlineGroundPass.app; handoff_name=ActiveEOSGameplayLobby.json; handoff_kind=ground-gameplay-v4 ;;
    room) app=Build/OnlineRoom/Mac/CounterattackOnlineRoom.app; handoff_name=ActiveEOSRoomLobby.json; handoff_kind=room-gameplay-v2 ;;
    *) printf 'Expected packets or gameplay mode.\n' >&2; exit 2 ;;
esac
if [ "$interaction" = manual ]; then
    if [ "$mode" = room ]; then handoff_kind=room-manual-v2; else handoff_kind=ground-manual-v3; fi
fi
if [ ! -d "$app/Contents/MacOS" ]; then printf 'Build the Mac proof first.\n' >&2; exit 1; fi
player="$(find "$app/Contents/MacOS" -maxdepth 1 -type f -perm -111 -print -quit)"
if [ -z "$player" ]; then printf 'No executable in the proof app.\n' >&2; exit 1; fi
run="$repo_root/TestResults/GameplayRegression/Runs/$(date -u +%Y%m%dT%H%M%SZ)-$mode-$role-$$"
mkdir -p "$run"
set -- -ca-eos-role "$role" -ca-eos-output "$run" -logFile "$run/Player.log" -screen-fullscreen 0 -screen-width 640 -screen-height 360
if [ "$interaction" = manual ]; then set -- "$@" -ca-online-manual -screen-width 1280 -screen-height 720; fi
if [ "$role" = guest ]; then
    handoff="$(gh api "repos/thomasantonakis/counterattack/contents/Documentation/Setup/$handoff_name?ref=codex/online-foundation-personal-mac" --jq '.content' | tr -d '\n' | base64 -D)"
    printf '%s' "$handoff" | jq -e --arg kind "$handoff_kind" --argjson now "$(date +%s)" '.kind == $kind and (.lobbyId | test("^[0-9a-f]{32}$")) and (.createdUnix <= $now + 30) and ($now - .createdUnix < 900)' >/dev/null
    printf '%s' "$handoff" | jq -r '.lobbyId' > "$run/lobby-id.txt"
    set -- "$@" -ca-eos-lobby-file "$run/lobby-id.txt"
fi
printf 'Running standalone %s. Artifacts: %s\n' "$role" "$run"
set +e
"$player" "$@" > "$run/stdout.log" 2>&1
player_exit=$?
set -e
if [ ! -f "$run/result.json" ]; then printf 'No report; inspect local Player.log and stdout.log. Exit: %s\n' "$player_exit" >&2; exit 1; fi
cat "$run/result.json"
printf '\nStandalone result: %s/result.json\n' "$run"
jq -e '.status == "passed"' "$run/result.json" >/dev/null
if { [ "$mode" = gameplay ] || [ "$mode" = room ]; } && [ "$role" != login ]; then
    test -f "$run/gameplay-evidence.json"
    if [ "$interaction" = manual ]; then
        jq -e '.role == "guest-manual"' "$run/gameplay-evidence.json" >/dev/null
    else
        jq -e '.sharedPauseRevision == 2 and .rollSequence == 1' "$run/gameplay-evidence.json" >/dev/null
    fi
    if [ "$role" = guest ]; then jq -e '.guestRulesInactive == true' "$run/gameplay-evidence.json" >/dev/null; fi
    cat "$run/gameplay-evidence.json"
    printf '\nGameplay evidence: %s/gameplay-evidence.json\n' "$run"
fi
[ "$player_exit" -eq 0 ]

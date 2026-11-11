#!/usr/bin/env bash
set -euo pipefail
role="${1:-guest}"
case "$role" in guest|login) ;; *) printf 'Usage: %s [login|guest]\n' "$0" >&2; exit 2 ;; esac
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
app=Build/EOSProof/Mac/CounterattackEOSProof.app
if [ ! -d "$app/Contents/MacOS" ]; then printf 'Build the Mac proof first.\n' >&2; exit 1; fi
player="$(find "$app/Contents/MacOS" -maxdepth 1 -type f -perm -111 -print -quit)"
if [ -z "$player" ]; then printf 'No executable in the proof app.\n' >&2; exit 1; fi
run="$repo_root/TestResults/GameplayRegression/Runs/$(date -u +%Y%m%dT%H%M%SZ)-Standalone-$role-$$"
mkdir -p "$run"
set -- -ca-eos-role "$role" -ca-eos-output "$run" -logFile "$run/Player.log" -screen-fullscreen 0 -screen-width 640 -screen-height 360
if [ "$role" = guest ]; then
    handoff="$(gh api 'repos/thomasantonakis/counterattack/contents/Documentation/Setup/ActiveEOSStandaloneLobby.json?ref=codex/online-foundation-personal-mac' --jq '.content' | tr -d '\n' | base64 -D)"
    printf '%s' "$handoff" | jq -e --argjson now "$(date +%s)" '.kind == "standalone-relay-v1" and (.lobbyId | test("^[0-9a-f]{32}$")) and (.createdUnix <= $now + 30) and ($now - .createdUnix < 900)' >/dev/null
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
[ "$player_exit" -eq 0 ]

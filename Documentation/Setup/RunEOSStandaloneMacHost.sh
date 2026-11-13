#!/usr/bin/env bash
set -euo pipefail
mode="${1:-room}"
interaction="${2:-manual}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
case "$mode" in
  room) app=Build/OnlineRoom/Mac/CounterattackOnlineRoom.app; handoff_name=ActiveEOSRoomLobby.json; kind=room-gameplay-v2; manual_kind=room-manual-v2 ;;
  gameplay) app=Build/OnlineGroundPass/Mac/CounterattackOnlineGroundPass.app; handoff_name=ActiveEOSGameplayLobby.json; kind=ground-gameplay-v4; manual_kind=ground-manual-v3 ;;
  *) printf 'Expected room or gameplay.\n' >&2; exit 2 ;;
esac
case "$interaction" in manual) kind="$manual_kind" ;; automatic) ;; *) printf 'Expected manual or automatic.\n' >&2; exit 2 ;; esac
test -d "$app/Contents/MacOS" || { printf 'Build the Mac proof first.\n' >&2; exit 1; }
player="$(find "$app/Contents/MacOS" -maxdepth 1 -type f -perm -111 -print -quit)"
test -n "$player"
branch=codex/online-foundation-personal-mac
uri="repos/thomasantonakis/counterattack/contents/Documentation/Setup/$handoff_name"
gh api "repos/thomasantonakis/counterattack/contents/Documentation/Setup?ref=$branch" | jq -e --arg name "$handoff_name" 'all(.[]; .name != $name)' >/dev/null
run="$repo_root/TestResults/GameplayRegression/Runs/$(date -u +%Y%m%dT%H%M%SZ)-$mode-host-$$"
mkdir -p "$run"
handoff_sha=''
cleanup() {
  if [ -n "$handoff_sha" ]; then
    gh api -X DELETE "$uri" -f 'message=Remove temporary reverse-role EOS lobby' -f "sha=$handoff_sha" -f "branch=$branch" >/dev/null || printf 'Handoff cleanup failed; resolve before another run.\n' >&2
  fi
}
trap cleanup EXIT
set -- -ca-eos-role host -ca-eos-output "$run" -logFile "$run/Player.log" -screen-fullscreen 0 -screen-width 1280 -screen-height 720
if [ "$interaction" = manual ]; then set -- "$@" -ca-online-manual; fi
"$player" "$@" > "$run/stdout.log" 2>&1 &
player_pid=$!
for ((i=0; i<120; i++)); do
  if [ -f "$run/lobby-id.txt" ]; then break; fi
  if [ -f "$run/result.json" ] || ! kill -0 "$player_pid" 2>/dev/null; then printf 'Host did not create a lobby; inspect %s\n' "$run" >&2; exit 1; fi
  sleep 1
done
test -f "$run/lobby-id.txt"
lobby="$(cat "$run/lobby-id.txt")"
printf '%s' "$lobby" | grep -Eq '^[0-9a-f]{32}$'
handoff="$(jq -cn --arg kind "$kind" --arg lobby "$lobby" --argjson now "$(date +%s)" '{kind:$kind,lobbyId:$lobby,createdUnix:$now}')"
encoded="$(printf '%s\n' "$handoff" | base64 | tr -d '\n')"
jq -n --arg content "$encoded" --arg branch "$branch" '{message:"Share temporary reverse-role EOS lobby",content:$content,branch:$branch}' > "$run/handoff-request.json"
handoff_sha="$(gh api -X PUT "$uri" --input "$run/handoff-request.json" --jq '.content.sha')"
printf 'Reverse-role handoff published. On Windows run the guest helper with -Room -Manual (or -Gameplay as selected).\nArtifacts: %s\n' "$run"
set +e
wait "$player_pid"
player_exit=$?
set -e
test -f "$run/result.json"
cat "$run/result.json"
jq -e '.status == "passed"' "$run/result.json" >/dev/null
if [ "$interaction" = manual ]; then jq -e '.role == "host-manual"' "$run/gameplay-evidence.json" >/dev/null; fi
cat "$run/gameplay-evidence.json"
[ "$player_exit" -eq 0 ]

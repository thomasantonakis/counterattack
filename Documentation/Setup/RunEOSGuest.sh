#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

for tool in git gh jq unity; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        printf 'Missing tool: %s\n' "$tool" >&2
        exit 1
    fi
done

if [ "$(git branch --show-current)" != codex/online-foundation-personal-mac ]; then
    printf 'Check out codex/online-foundation-personal-mac first.\n' >&2
    exit 1
fi

status="$(unity status --project-path "$repo_root" --format json)"
printf '%s\n' "$status" | jq -e --arg root "$repo_root" \
    '.success == true and any(.data.instances[]?; .projectPath == $root and .state == "ready")' >/dev/null || {
    printf 'This checkout has no ready Unity Editor connection.\n' >&2
    exit 1
}

unity command recompile --project-path "$repo_root" --caller codex --skill unity-cli --format json >/dev/null
compile_ready=false
for (( attempt = 0; attempt < 60; attempt++ )); do
    compile="$(unity command recompile_status --project-path "$repo_root" --caller codex --skill unity-cli --format json)"
    if printf '%s\n' "$compile" | jq -e \
        '.success == true and .data.result.status == "up_to_date" and .data.result.failed == false' >/dev/null; then
        compile_ready=true
        break
    fi
    if printf '%s\n' "$compile" | jq -e '.data.result.failed == true' >/dev/null; then
        break
    fi
    sleep 2
done
if [ "$compile_ready" != true ]; then
    printf 'Unity compilation is not clean or complete.\n' >&2
    exit 1
fi

editor="$(unity command editor_status --project-path "$repo_root" --caller codex --skill unity-cli --format json)"
printf '%s\n' "$editor" | jq -e \
    '.success == true and .data.result.playMode == "stopped" and .data.result.compiling == false and .data.result.domainReloadInProgress == false' >/dev/null || {
    printf 'The Editor must be stopped and finished compiling.\n' >&2
    exit 1
}

scenes="$(unity command list_open_scenes --project-path "$repo_root" --caller codex --skill unity-cli --format json)"
printf '%s\n' "$scenes" | jq -e \
    '.success == true and .data.result.count > 0 and all(.data.result.scenes[]; .isDirty == false)' >/dev/null || {
    printf 'Save all dirty loaded scenes through Unity before testing.\n' >&2
    exit 1
}

tests="$(unity command test_status --project-path "$repo_root" --caller codex --skill unity-cli --format json)"
printf '%s\n' "$tests" | jq -e \
    '.success == true and .data.result.status == "no_tests"' >/dev/null || {
    printf 'Resolve the existing test run before starting the guest fixture.\n' >&2
    exit 1
}

handoff="$(gh api 'repos/thomasantonakis/counterattack/contents/Documentation/Setup/ActiveEOSLobby.json?ref=codex/online-foundation-personal-mac' --jq '.content | @base64d')"
lobby_id="$(printf '%s\n' "$handoff" | jq -er '.lobbyId | select(test("^[0-9a-f]{32}$"))')"
created_unix="$(printf '%s\n' "$handoff" | jq -er '.createdUnix | numbers')"
now_unix="$(date +%s)"
if (( created_unix > now_unix || now_unix - created_unix > 900 )); then
    printf 'The host lobby handoff is stale; ask for a fresh host run.\n' >&2
    exit 1
fi

mkdir -p Temp
printf '%s' "$lobby_id" > Temp/OnlineEOSJoinId.txt
touch Temp/OnlineEOSTwoMacGuest.run

printf 'Running the one-case EOS guest fixture against the current host lobby.\n'
unity command run_tests --mode editor \
    --filter EOSTwoMacLobbyEditorTests.GuestSearchesAndJoinsHostByLobbyId \
    --filter_type testName --async_tests true \
    --project-path "$repo_root" --caller codex --skill unity-cli --format json

for (( attempt = 0; attempt < 90; attempt++ )); do
    result="$(unity command test_status --project-path "$repo_root" --caller codex --skill unity-cli --format json)"
    state="$(printf '%s\n' "$result" | jq -r '.data.result.status // "unknown"')"
    if [ "$state" != running ]; then
        printf '%s\n' "$result"
        [ "$state" = completed ]
        exit $?
    fi
    sleep 2
done

printf 'The guest fixture did not finish within three minutes. Check test_status.\n' >&2
exit 1

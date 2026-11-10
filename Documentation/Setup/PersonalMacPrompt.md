# Copy this prompt into Codex on the personal Mac

First clone/open branch `codex/online-foundation-personal-mac` of
`thomasantonakis/counterattack`. If Codex itself is not installed, follow steps
1–3 of [PersonalMac.md](PersonalMac.md) manually first. An agent cannot install
itself before it is available. Then paste the text below into a new session with
this checkout as its working directory.

---

Set up this personal Mac for Counterattack development. Read `AGENTS.md` and
`Documentation/Setup/PersonalMac.md`, then execute the setup instructions that
apply, rather than only explaining them. Install missing tools and configure the
environment so Codex can inspect/edit the project, control the open Unity Editor
through Unity CLI/Pipeline, and run focused Unity Test Framework tests.

You are authorized to install the tools described in that guide on this personal
Mac, using official installers and the normal permission prompts. Detect what is
already installed and avoid duplicates. Do not change company network settings,
bypass restrictions, activate paid services or copy credentials from another Mac.

Work through these steps:

1. Determine macOS version, architecture, current checkout root and Git branch.
   Stay on `codex/online-foundation-personal-mac`; preserve existing local edits.
2. Check/install supported command-line tools, Homebrew, Git, ripgrep, jq, Python
   and GitHub CLI. Git LFS is optional unless tracked assets require it. Verify
   GitHub access and my commit identity without inventing identity values.
3. Verify Codex installation/sign-in and project trust. Use the current official
   setup docs where needed; do not copy old authentication files or global config.
4. Check/install Unity Hub and the exact Editor from ProjectVersion.txt
   (currently 6000.5.3f1), with architecture/build support appropriate to this Mac.
   Ask me to complete sign-in/license dialogs and to open Unity manually. Never
   start Unity yourself, use batchmode, kill it or start ordinary gameplay.
5. Install/update Unity CLI through one supported installation route. Ensure
   PATH works in the terminal and the Codex process. Install the Unity agent
   plugin/skills, restarting the agent when required.
6. Verify the already-pinned Pipeline package and live Editor connection for
   THIS checkout. Do not upgrade/reinstall the package just to force a reconnect.
   All Unity commands must follow AGENTS.md, use this Mac's actual absolute
   project root and include caller/skill metadata where required.
7. Verify the portable `.codex/config.toml` entry. Use CLI for Editor control;
   avoid duplicate global Unity MCP registrations. Resolve machine-specific
   executable paths locally without committing usernames to project config.
8. Install CodeGraph and rebuild its ignored local index. Review installer edits
   to agent instructions/config. Missing CodeGraph does not block CLI setup.
9. Confirm Play Mode is stopped and no tests are pending. Recompile through the
   open Editor, inspect active logs, save dirty loaded scenes through the Editor,
   and verify clean state. Run only `OnlineMatchProtocolEditorTests`; inspect its
   actual result and retained native XML. Expected baseline: 27 passes. Do not
   run whole suites or ask me to press Play as automated validation.
10. Read `Documentation/Plans/OnlineMultiplayer.md` and
    `Documentation/Plans/EOSSetup.md`. Guide my EOS development product/client
    setup; I complete portal sign-in/agreements and keep credentials local.
    Do not claim live EOS or Windows/macOS internet connectivity from local tests.

Continue all independent setup work while a manual action is pending. Ask only
for information, permissions or physical actions you actually need. Do not
silently weaken the project's permissions or Editor safety rules to get tools
working. If access is blocked, report the exact blocker and the next manual step.

Finish with a short checklist of installed/verified tools, the live connection,
compilation status, actual test counts/artifact path, and remaining external
requirements. Do not claim setup complete unless required checks actually pass.
Do not commit or push unrelated setup changes without asking me.

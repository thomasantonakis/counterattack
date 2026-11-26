# Starting the next online session without Codex approval prompts

The user requests continuous authorized project work without repeated human
approvals. Select these settings in the client that starts the next session;
a handoff prompt cannot alter currently enforced permissions.

## Desktop / IDE

Use the permissions control beneath the composer and select **Full access**
when available. In the CLI, `/permissions` opens the picker. **Approve for me**
uses automatic review; it can still stop/reject actions and is not full access.

Full access combines `sandbox_mode = "danger-full-access"` and
`approval_policy = "never"`. It grants local filesystem/network access without
the usual sandbox boundary. It does not change Unity workflow rules or the
scope of authorized project work.

## Explicit Windows CLI startup

Open PowerShell and run:

```powershell
codex -C "C:\Users\thomas\Documents\Projects\Personal\counterattack" --sandbox danger-full-access --ask-for-approval never "Read Documentation/Setup/OnlineNextSessionPrompt.md and execute the complete four-step Room handoff."
```

The flags were checked against this machine's `codex --help`. They apply to
that invocation without changing other projects' defaults. Using `never` with
workspace-write alone suppresses prompts but retains the restrictions that
previously blocked Git metadata, Unity CLI or GitHub access.

For persistent defaults, the equivalent **top-level** entries in the user's
`%USERPROFILE%\.codex\config.toml` are:

```toml
approval_policy = "never"
sandbox_mode = "danger-full-access"
```

Edit existing entries rather than duplicating them; do not place them under an
unrelated TOML section. This guide does not change the user config or portable
repository .codex/config.toml. Client-selected profiles and managed policy may
override defaults; check active permissions in the new session.

## Limits

These settings cannot remove OS dialogs (Keychain, firewall/UAC, Unity modals),
authentication/expired credentials, managed policy or tool-specific permission
boundaries. They do not provide remote access to the second Mac, open Unity
automatically, switch physical networks or remove session/usage limits.

Within those limits, the handoff authorizes all four steps, focused tests,
builds and task-related commits/pushes without another "continue?" question.
It cannot promise that every operating-system or external-tool prompt vanishes.

Official documentation checked:
[Agent approvals and security](https://learn.chatgpt.com/docs/agent-approvals-security)
and [Sandbox and permissions](https://learn.chatgpt.com/docs/sandboxing).

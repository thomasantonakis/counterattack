# Contributing to Counterattack

## Work on a feature branch

1. Run `git status --short --branch` and `git fetch origin`. Preserve existing local edits. Use a separate worktree if changing branches would mix those edits into your task.
2. Choose the pull request target before starting. Base work that depends on the online foundation on `origin/codex/online-foundation-personal-mac`. Base independent work on `origin/master`. Use a short branch name that identifies the task, such as `feature/online-save-resume` or `fix/kickoff-selection`.
3. Create the branch with `git switch -c <feature-branch> origin/<target-branch>`. For an isolated checkout, use `git worktree add -b <feature-branch> <worktree-path> origin/<target-branch>`.
4. Keep commits scoped to the task. Follow [AGENTS.md](AGENTS.md) for Unity Editor operations and focused validation. Review the changes you intend to stage and run `git diff --cached --check` before committing.
5. Push the feature branch with `git push -u origin <feature-branch>`. Always open a PR against the chosen target, including for documentation and automation changes. Use a draft PR when work is incomplete. Include what changed, how you verified it, and any remaining checks. Merge through the PR after its checks pass and required reviews are complete. Use this flow for changes to both target branches.

## Declare online compatibility

For a PR targeting `codex/online-foundation-personal-mac`, select exactly one option in the PR template and write a short reason. Review whether the old and new builds could still play together with either one hosting.

| Option | Use it when |
| --- | --- |
| Compatible | The rules and protocol can stay at their current versions. This includes documentation, presentation-only changes, and bundled content changes already detected by the content fingerprint. |
| Rules bump | The builds can exchange messages, but they would disagree about legal actions, turn ownership, gameplay outcomes or how to continue the match. A bug fix can fall into this category. |
| Protocol bump | A change to online messages or their meaning makes the versions unable to communicate correctly. |
| Both | The change breaks both rules and protocol compatibility. |

On the online branch, run `python Tools/online_compatibility.py bump rules`, `python Tools/online_compatibility.py bump protocol`, or `python Tools/online_compatibility.py bump both` with Python 3 from the repo root. Use `python3` where that is the command name. Review and commit the resulting source change with the feature. The command increments the selected compatibility labels. The app's release version and content fingerprint are managed separately.

The **Online compatibility** check compares the PR's merge result with its target and verifies that the declared versions advance and the other versions stay the same. Put the decision and reason in the PR description so edits can rerun the check. Reviewers assess the compatibility decision itself. If the target advances a version while the PR is open, update the feature branch and recheck its declaration against the new target.

## Unity Workflow Rules

- Never start Unity from Codex. Do not launch the Unity app, do not run Unity batchmode, and do not press Play to start a test.
- When Unity is already open and must be stopped, stop the existing editor instance by activating Unity and sending Cmd+P. Do not kill the Unity process.
- After code changes, reload the already-open Unity editor by sending Cmd+R, then inspect `/Users/t.antonakis/Library/Logs/Unity/Editor.log` for compile errors.
- Do not use `dotnet build Assembly-CSharp.csproj` as the Unity validation path for this project. It consumes time/tokens and does not stop or reload the open Unity editor.
- Use static shell checks such as `rg`, `sed`, and `git diff --check` freely. For Unity compilation, rely on the open editor reload and Editor.log.

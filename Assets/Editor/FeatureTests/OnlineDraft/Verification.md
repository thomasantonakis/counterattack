# Authoritative online Draft

- [x] Host-owned catalog attributes, shuffled deck, dealt goalkeeper pairs.
- [x] Team-owned roll-off with ties and alternating four-card batch starters.
- [x] Turn-owned picks, typed slot validation, filled-slot continuation and own roster swaps.
- [x] Detached snapshots, bounded duplicate receipts, rebind invalidation and both-team confirmation.
- [x] Execute focused native tests and retain evidence.
- [x] Connect authenticated frontend messages and authored Draft presentation.
- [ ] Free/international draft variants, opening coin toss and full Room handoff.

Current authority supports Regular draft with dealt goalkeepers; unsupported
variants fail explicitly. It uses the bundled real card catalogs and host-only
random sources. No full Draft scene/network acceptance is claimed yet.
Run only `OnlineDraftAuthorityEditorTests` / `OnlineDraft` in EditMode.

Live Editor compilation completed with zero errors. Native own-feature evidence:
- 40 EditMode authority cases: `20261115T055816884Z`.
- 16 EditMode serialized-channel cases: `20261115T060056922Z`.
- 20 PlayMode actual authored Draft scene cases: `20261115T060647585Z`.
All passed with zero failures/skips, 76 distinct cases. Each has its own deferred
Mac manifest entry; native XML/progress is retained in GameplayRegression/Runs.
Affected authenticated-channel and frontend-recovery fixtures also passed (26
and 16 cases); see the latest native run directories.

The live native frontend enables the Draft lane, and loads the normal Draft
scene when the agreed authoritative Draft state becomes available. The scene
owns 36 inspectable roster slots, four cards and action/leave buttons; no online
visual objects are generated in Play Mode. It disables the existing local Draft
and Draft UI startup paths online, preserves save context, enforces own-team
picks/reordering, projects rolls and attributes and supports both-team confirmation.
Offline managers retain their original panels. Unity serializer whitespace and
prefab serialization changes in Draft were retained; Room was restored clean.

This is Regular/dealt-GK Draft integration, not completed full-match internet
play. Free/international variants, post-draft opening toss, match initialization,
full Room state/roster projection and normal menus remain. Source rules catalog
is now frontend-rules-v2; older native frontend builds are intentionally rejected.
Paired Draft/scene-transition acceptance is deferred. Current exports predate Draft.
Broader user-run checks if desired: `Assets/Editor/FeatureTests/DraftSettingsRules/`
fixture `DraftSettingsRulesEditorTests`, and `Assets/Editor/DraftOrderPresentationEditorTests.cs`
fixture `DraftOrderPresentationEditorTests`, for existing offline settings/order UI.
No full suites were run.

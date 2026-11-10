# EOS setup for Counterattack's free online investigation

Prepared 2026-11-09; portal guidance corrected 2026-11-10. The user has created
an EOS product, found its IDs, and created a desktop client with the predefined
PeerToPeer policy. EOS Unity plugin 6.2.0 is pinned in the project. No credentials
are configured in the project, and no paid service has been activated.

## What this enables

An EOS development product lets us test private Windows/macOS connections without
choosing a game distributor. A player's machine still runs the match. This is
not dedicated server hosting, a store listing or a commitment to sell on Epic.
Epic advertises the online services as free across stores/engines; service limits
and technical compatibility still need checking in the proof.

## Your one-time portal setup

1. Sign in to the [Epic Developer Portal](https://dev.epicgames.com/portal) with
   your developer Epic account. Create/select the organization that will own
   Counterattack. Review any agreements yourself when prompted.
2. Create a product named **Counterattack** for Epic Online Services. A store
   publishing workflow is not required for this connectivity investigation.
3. Under the product's **Product Settings**, find its product, sandbox and
   deployment identifiers. An EOS-only product may have just one **Live** sandbox
   with a **Release** deployment; these can be used for this private development
   proof. A separate Dev sandbox is associated with Epic Games Store publishing
   and is not required here. Both test machines must point at the same
   product/sandbox/deployment.
4. Under **Product Settings → Clients**, create a client such as
   **Counterattack Desktop Dev**, and a client policy such as
   **Counterattack Private PvP Dev**. The documented **Peer2Peer** policy type is
   the starting point for a player-hosted game. Inspect its allowed features;
   our proof needs game-service identity, private room coordination and P2P.
   Do not give desktop clients trusted-server/admin powers.
5. Keep the **Product ID**, **Sandbox ID**, **Deployment ID**, **Client ID** and
   **Client Secret** locally in your password manager for later configuration.
   Do not paste them into chat or commit them to Git. We will agree the actual
   package configuration location after the compatible SDK/transport is selected.
6. Tell Codex only that the development product/client is ready. No screenshots
   or credential values are needed in chat.

## Next local configuration step

In the open Unity Editor, use **EOS Plugin → EOS Configuration**. Enter the
Product Name, Product ID, a nonempty Product Version, and the portal's Live
sandbox/Release deployment and desktop Client ID/Client Secret. Select that
deployment and client for macOS, then choose **Save All Changes**. Use the same
product, sandbox and deployment when Windows is configured later. The plugin
writes configuration under `Assets/StreamingAssets/EOS/`; this checkout ignores
that directory so local values do not enter ordinary Git changes. Do not paste
credential values into chat. The client policy, rather than concealment of a
distributed client secret, controls client permissions.

This configuration alone does not initialize EOS in Room. Native initialization
requires an EOS manager in an appropriate scene or a separate controlled probe;
that remains a later compatibility check. The installed plugin bundles the EOS
SDK, so a separate SDK download is not needed for this plugin path.

Portal labels can change. If a label differs, follow the current
[client-policy guide](https://dev.epicgames.com/docs/epic-online-services/eos-fundamentals/client-and-client-policy/client-policy-guide)
rather than copying Unreal Engine configuration examples into this Unity project.

## Identity decision for the proof

The developer needs an Epic account to configure the product. Game-service users
do not inherently all need Epic accounts: EOS Connect can use supported external
identity providers. Epic social/friends login is a separate feature set.

The technical spike must select and test the sign-in path before asking you to
configure additional services. Store-independent development identity must not be
mistaken for durable account ownership, cross-device purchases or production
authentication. A device-only identity, if used temporarily, does not prove those
features. Development credentials/tools must not become a shipping login path.

If the spike uses Epic test login temporarily, configure the corresponding Epic
Account Services application/linked client and grant both testers development
access as directed by Epic's current guide. This is conditional, not a requirement
to launch through Epic or use Epic social features in the final game.

Client SDK configuration is distributable and cannot protect privileged secrets
by hiding text. The real protection is the correct client policy and verified
identity. Server/admin credentials and storage encryption keys have different
requirements and must not be bundled indiscriminately.

## Plugin compatibility evidence (2026-11-10)

- Pinned `com.playeveryware.eos` to the UPM `v6.2.0` tag in
  `Packages/manifest.json`; the lockfile resolves revision
  `2eeb6d4e25e7c654fcfab057fa47649b29516c95`.
- Inspected the package's MIT license and bundled EOS SDK license. Its declared
  dependencies are Newtonsoft JSON 3.0.2 and Editor Coroutines 1.0.0; Unity
  resolved its existing compatible package versions.
- The live Unity 6000.5.3f1 Editor imported the package and completed script
  compilation with zero errors. Bundled macOS EOS and loader libraries are
  universal x86_64/arm64 binaries. This does not verify native initialization
  or a standalone build.
- After import, `OnlineMatchProtocolEditorTests` passed 27/27 in the connected
  Editor. Native XML: `TestResults/GameplayRegression/Runs/20261110T080134239Z/results.xml`.
  Those local protocol tests do not call EOS.
- The user saved local product, deployment and desktop client configuration.
  A value-presence check confirmed Product Version, IDs, client secret and Mac
  encryption key are populated; the Mac client and deployment selections match
  the product configuration. No values were copied into the repository docs.
- In the stopped Editor, an isolated C# probe loaded the plugin's EOS bindings,
  initialized the native SDK, created and released a platform interface, then
  shut the SDK down. Results: `init=Success`, `create=True`, `shutdown=Success`.
  Room remained clean and the Editor ready. This did not log in or enter Play Mode.
- After reopening Unity to give EOS a fresh process, the focused EditMode
  `EOSConnectLoginEditorTests` case used a disposable local device identity and
  returned a valid EOS Product User ID: 1 passed, 0 failed. Native XML:
  `TestResults/GameplayRegression/Runs/20261110T204345472Z/results.xml`.
  It did not enter Play Mode or expose the ID in test output. The one-use marker
  prevents routine runs from contacting EOS.
- In another fresh Editor process, a focused EditMode case created a two-seat
  invite-only lobby with join-by-ID enabled, received its lobby ID and destroyed
  it successfully: 1 passed, 0 failed. Native XML:
  `TestResults/GameplayRegression/Runs/20261110T205106861Z/results.xml`.
  This confirms this client policy permits private lobby creation, not joining.

## Codex's remaining technical proof

- Select a transport revision and prove standalone packaging on macOS, including
  an Apple Silicon runtime check. Check Windows separately.
  The plugin's platform page targets Unity 2021.3 and its Netcode example cites
  old verified versions; successful import alone does not prove connectivity.
- Authenticate a second distinct test identity. Bind each provider identity to the
  reserved match/team in the protocol; do not trust a sender-claimed team.
- Create/join a private match. A join code must resolve through actual room
  coordination; an EOS account ID alone is not a complete invite-code system.
- Exchange a compatible handshake, a semantic command and an acknowledgement.
- Test direct connections and forced fallback routing, both host OS directions,
  disconnect/rejoin, stale traffic and version rejection.
- Record bytes, connected time, observed route and current applicable quotas.
  Do not claim an internet/cross-platform result from local protocol tests.

## Current evidence and external dependencies

The EOS plugin is installed and the local product/client configuration has passed
native initialization, one EOS Connect Device ID login and private lobby
create/destroy on this Mac. A second identity, private room joining, P2P traffic
and relay tests remain unverified pending transport and identity integration.
The local configuration is Git ignored.
The cross-platform proof also requires an available Windows machine and a macOS
machine on different networks. Existing connected Editor tests cannot supply
that evidence. No separate Unity instance is launched by Codex.

The user has a second Mac and can boot this Mac into Windows. First use both
Macs at once to prove two distinct device identities, private-lobby joining and
P2P traffic; then pair the second Mac with this Mac running Windows to check
cross-platform behavior. The Boot Camp installation cannot run concurrently
with macOS on the same machine. For the second Mac, follow
`Documentation/Setup/PersonalMac.md` from this branch, install Unity 6000.5.3f1,
and enter the same product/sandbox/deployment and desktop client values locally
through the EOS plugin. Sign in to GitHub/Unity on that Mac yourself; do not
copy this Mac's local EOS configuration or device-identity storage. The joining
test harness and standalone build are still to be implemented before asking
the user to launch a second copy or start a match.

The first code batch is provider-independent command admission only. It is not
yet connected to Room input, a network transport, pause UI, dice or persistence.

## Primary references

- [Epic Online Services availability](https://onlineservices.epicgames.com/)
- [EOS Unity plugin prerequisites and supported features](https://github.com/EOS-Contrib/eos_plugin_for_unity)
- [Plugin supported platforms and target versions](https://github.com/EOS-Contrib/eos_plugin_for_unity/blob/stable/com.playeveryware.eos/Documentation~/supported_platforms.md)
- [Plugin Netcode example and its verified versions](https://github.com/EOS-Contrib/eos_plugin_for_unity/blob/stable/com.playeveryware.eos/Documentation~/scene_walkthrough/P2P_netcode_walkthrough.md)
- [Epic client-policy guide](https://dev.epicgames.com/docs/epic-online-services/eos-fundamentals/client-and-client-policy/client-policy-guide)
- [EOS-only products may have one sandbox](https://dev.epicgames.com/documentation/en-us/unreal-engine/online-subsystem-eos-plugin-in-unreal-engine)
- [Epic's Peer2Peer client setup example (portal steps; not its Unreal configuration)](https://dev.epicgames.com/documentation/en-us/unreal-engine/using-lyra-with-epic-online-services-in-unreal-engine)
- [EOS cross-store authentication](https://dev.epicgames.com/docs/epic-online-services/accounts-and-social/crossplay/crossplay-introduction)
- [EOS routing controls](https://dev.epicgames.com/docs/en-US/api-ref/enums/eos-e-relay-control)
- [Plugin configuration security](https://github.com/EOS-Contrib/eos_plugin_for_unity/blob/stable/com.playeveryware.eos/Documentation~/eos_config_security.md)

# EOS setup for Counterattack's free online investigation

Prepared 2026-11-09. No EOS account, product or credentials are configured by this
task. No provider package or paid service has been activated in Counterattack.
The user confirmed there is no existing product and requested these instructions.

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
   deployment identifiers. Use a development sandbox/deployment for this proof;
   if an appropriate development deployment already exists, reuse it. Both test
   machines must point at the same product/sandbox/deployment.
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

## Codex's remaining technical proof

- Select and pin a plugin/transport revision; inspect actual package dependencies
  and licenses before adding it. The EOS Unity plugin lists Windows/macOS, but its
  platform page targets Unity 2021.3, and its Netcode example cites old verified
  Netcode versions. None of that establishes compatibility with Unity 6000.5.2f1.
- Check Unity import, native initialization and packaging on macOS, including
  Apple Silicon and the intended supported architectures. Check Windows separately.
- Authenticate two distinct test identities. Bind the provider identity to the
  reserved match/team in the protocol; do not trust a sender-claimed team.
- Create/join a private match. A join code must resolve through actual room
  coordination; an EOS account ID alone is not a complete invite-code system.
- Exchange a compatible handshake, a semantic command and an acknowledgement.
- Test direct connections and forced fallback routing, both host OS directions,
  disconnect/rejoin, stale traffic and version rejection.
- Record bytes, connected time, observed route and current applicable quotas.
  Do not claim an internet/cross-platform result from local protocol tests.

## Current evidence and external dependencies

The project has no discovered EOS package/configuration. Live EOS login, private
room joining and relay tests are pending the development product/client setup.
The cross-platform proof also requires an available Windows machine and a macOS
machine on different networks. Existing connected Editor tests cannot supply
that evidence. No separate Unity instance is launched by Codex.

The first code batch is provider-independent command admission only. It is not
yet connected to Room input, a network transport, pause UI, dice or persistence.

## Primary references

- [Epic Online Services availability](https://onlineservices.epicgames.com/)
- [EOS Unity plugin prerequisites and supported features](https://github.com/EOS-Contrib/eos_plugin_for_unity)
- [Plugin supported platforms and target versions](https://github.com/EOS-Contrib/eos_plugin_for_unity/blob/stable/com.playeveryware.eos/Documentation~/supported_platforms.md)
- [Plugin Netcode example and its verified versions](https://github.com/EOS-Contrib/eos_plugin_for_unity/blob/stable/com.playeveryware.eos/Documentation~/scene_walkthrough/P2P_netcode_walkthrough.md)
- [Epic client-policy guide](https://dev.epicgames.com/docs/epic-online-services/eos-fundamentals/client-and-client-policy/client-policy-guide)
- [Epic's Peer2Peer client setup example (portal steps; not its Unreal configuration)](https://dev.epicgames.com/documentation/en-us/unreal-engine/using-lyra-with-epic-online-services-in-unreal-engine)
- [EOS cross-store authentication](https://dev.epicgames.com/docs/epic-online-services/accounts-and-social/crossplay/crossplay-introduction)
- [EOS routing controls](https://dev.epicgames.com/docs/en-US/api-ref/enums/eos-e-relay-control)
- [Plugin configuration security](https://github.com/EOS-Contrib/eos_plugin_for_unity/blob/stable/com.playeveryware.eos/Documentation~/eos_config_security.md)

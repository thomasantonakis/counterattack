# Normal-game Mac crash/session collector

- [x] Collect only the latest Room session and allowlisted JSON artifacts.
- [x] Preserve bounded player-log tails and two recent matching crash reports.
- [x] Redact credentials/JWTs; avoid account configuration, live saves and Editor commands.
- [x] Publish a report-only branch using the existing private-index publisher.
- [x] Run seven collector unit cases; report-only publication regression recorded at handoff.
- [ ] Obtain the actual Mac crash report; do not infer its native cause from Windows InvalidAuth.

The Windows host failed EOS receive with InvalidAuth at exactly 60 minutes:
13:32:04.0448638Z through 14:32:04.3255009Z on November 19. The live code lacks
an owned Connect authentication-expiration renewal handler. Treat this as the
priority long-session investigation. The pickup/nutmeg decision and keys must
also be reproduced while authenticated before claiming a separate input defect.
The Mac OS unexpected-close alert establishes a crash, not an intentional quit.

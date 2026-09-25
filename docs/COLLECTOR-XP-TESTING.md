# Collector XP verification — unreleased 1.0.18

Branch: `fix/collector-foraging-xp`  
Worktree: `/tmp/ragnavik-farming-compat`  
Gale profile: `fix-collector-foraging-xp` (ID 8)

The focused local profile contains this compatibility build, published Gameplay 1.0.5, FloraCollector 1.1.4, Foraging 1.0.11, Professions 1.4.7, Jotunn 2.30.2 and BepInEx 5.4.2351. It is a local test fixture, not a complete production client pack. Every declared dependency and plugin file was checked; the compatibility DLL matches this branch's build. Gameplay matches the published 1.0.5 artifact. No UI/direct-server-entry plugin is installed; use the local galetest1 world.

## Completed checks

- Release build against installed Valheim references: zero errors; existing framework-reference conflict warnings remain.
- Compatibility regressions, including collector full/partial/empty/failed/non-owner/repeated-extraction cases.
- Actual FloraCollector and Foraging hook metadata, plus saved client and dedicated-server game API contracts.
- Existing Gameplay tests for selected 100%, unselected 50%, fractional XP and selection changes.
- Package layout and existing creature mappings.
- Gale active profile, enabled dependency versions, single copies of required DLLs and artifact SHA-256 checks.

## In-game verification still required

1. In local galetest1, compare a normal bush pick to collecting one matching item with Foraging selected, then unselected. Compare skill XP, not character-level XP.
2. Extract partial and full collectors. Expect one normal pick award per extracted item, scaled once to 100%/50%.
3. Test empty collectors, held interaction and denied ward access; expect no XP.
4. With matching builds on a test host and client, collect from an object owned by another peer. Only the collecting player should receive XP.
5. Have two players request the same collection; only the successful extraction should reward XP.

No in-game test has been claimed. Nothing from this branch has been pushed, published or deployed. Future coordinated release must bump Shared, Client and Server dependencies and regenerate inventories/anti-cheat policy before publication. The already published Gameplay 1.0.5 is required for the profession rates and remains pending live deployment.

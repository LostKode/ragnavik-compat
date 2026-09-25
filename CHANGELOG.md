# Changelog

| Version | Changes |
| --- | --- |
| 1.0.18 | Award normal Foraging XP per item successfully extracted from full or partial Flora Collectors, using the profession XP multiplier. Prevent rewards for failed or empty interactions. |
| 1.0.17 | Add 60 missing EpicMMO creature mappings, including level-8 Meadows skeletons and level-15 wild Black Forest roots. Match later-biome and sleeping variants to existing progression; summoned companions and offspring grant no kill XP. Preserve the previous 37 mod mappings and validate creature coverage. |
| 1.0.16 | Use the current game successful-placement API so the farming XP bridge activates. |
| 1.0.15 | Award base XP per confirmed harvest without the bulk-harvest cooldown; respect the pickup XP toggle. Award base XP for successful single and PlantEasily bulk crop planting. Skip Farming skill updates for invalid planting previews.  Revalidate spirit recovery for Afterdeath 1.0.11 and bulk planting for PlantEasily 2.2.2. |
| 1.0.14 | Restore all hover prompts and interactions by binding the player argument positionally in the Afterdeath interaction bridge. |
| 1.0.13 | Bind the Afterdeath assigned-bed interaction patch by argument position so current Valheim startup completes without a Harmony parameter-name failure. |
| 1.0.12 | Preserve cooked food, magic reagents, upgrades, and other custom-data items in ItemDrawers. Follow the ItemDrawers package API across compatible updates instead of pinning one version. |
| 1.0.11 | Restore recovered AzuEPI items to their original quick slots after a successful grave Take All. |
| 1.0.10 | Allow Afterdeath spirits to operate permitted doors through Valheim's normal ward and access checks and resurrect at their own assigned bed while keeping other interactions blocked. |
| 1.0.9 | Keep Take All coverage active for every Epic Loot and MagicPlugin item across mod updates while retaining Valheim API signature validation. |
| 1.0.8 | Spawn at the player's valid bed instead of Skathi when the bed is closer to the death point. |
| 1.0.7 | Allow Afterdeath spirits to use ordinary portals and dungeon transitions while preserving normal travel behavior after resurrection. |
| 1.0.6 | Add EpicMMO levels and experience rewards for the current SeaAnimals, OdinBear, OdinHorse, OdinMounts, BottledNeck, BeeQueen, and GoodestBoy creatures. Tamed offspring and companion creatures display levels without granting exploitable experience. |
| 1.0.5 | Send coins collected with Take All directly into CurrencyPocket while leaving other container contents on the normal transfer path. |
| 1.0.4 | Restore the Take All compatibility bridge for MagicPlugin 2.2.1 and the dedicated-server JSON reload guard for WackyEpicMMOSystem 1.9.68. |
| 1.0.3 | Preserve Epic Loot and MagicPlugin item data by routing all Take All contents through Valheim's original-item transfer path. |
| 1.0.2 | Prevent Starvation health drain while a player is an Afterdeath spirit. |
| 1.0.1 | Correct the Hexium installation classification to Client & Server. |
| 1.0.0 | Mark the Compatibility package stable and ship it with the approved Fjord Gate icon. |
| 0.1.0 | Exclude the obsolete MagicRevamp bridge after the pack selected MagicPlugin 2.2.0. Add the dedicated-server EpicMMO JSON reload guard. Disable individual modules when their expected mod version or API shape is absent. |

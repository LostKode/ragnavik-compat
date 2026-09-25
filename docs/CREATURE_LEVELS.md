# Creature level audit

Audited against the installed EpicMMO 1.9.68 embedded Default table and [Jotunn’s generated Valheim 1.0.7 character inventory](https://valheim-modding.github.io/Jotunn/data/prefabs/character-list.html). All 37 previous mod mappings are retained. CreatureManager registrations in the installed mod DLLs were compared with EpicMMO’s embedded mod tables and this supplement; no additional uncovered registrations were found. This registration scan does not prove coverage of creatures created by other mechanisms.

The previous fix covered mod creatures but omitted vanilla 1.0 prefab variants. EpicMMO uses exact, case-sensitive `name + "(Clone)"` keys; a parent mapping does not cover a variant. These are RPG display levels and XP, not Valheim star counts.

Levels and rewards below are proposed balance choices, not upstream game values. Existing EpicMMO biome bands anchor the choices: Meadows 3–10, Black Forest 12–22, Swamp 27–35, Mountains 40–45, Plains 50–55, Mistlands 60–63, Ashlands 70–73, Deep North 78–89, final boss 99. Summoned companions, boss adds and invulnerable transitions grant zero XP. The existing FrozenKing first-phase reward is retained; the final phase receives the same reward band, so a full fight can award both phase rewards. Live combat balance still needs playtesting.

| Prefab | Level | XP | Rationale |
| --- | ---: | ---: | --- |
| `Skeleton_Meadows` | 8 | 30–45 | Meadows encounter; below the level-15 Black Forest skeleton, above basic wildlife. |
| `Skeleton_Meadows_noarcher` | 8 | 30–45 | Meadows encounter; below the level-15 Black Forest skeleton, above basic wildlife. |
| `TentaRoot_wild` | 15 | 40–60 | Black Forest stationary ambush; same level as the Elder root, with a modest reward for a wild encounter. |
| `Skeleton_Swamps` | 28 | 100–150 | Swamp skeleton equipment; below the level-30 draugr. |
| `Skeleton_Swamps_noarcher` | 28 | 100–150 | Swamp skeleton equipment; below the level-30 draugr. |
| `Skeleton_Mountains` | 40 | 200–280 | Mountain skeleton equipment; just below wolves. |
| `Skeleton_Mountains_noarcher` | 40 | 200–280 | Mountain skeleton equipment; just below wolves. |
| `Skeleton_DeepNorth` | 80 | 700–900 | Deep North skeleton equipment; entry tier below the level-81 jotun warriors. |
| `Bat_Swamp` | 27 | 70–100 | Swamp bat; lower reward than the level-35 frost-cave bat. |
| `BlobFrost` | 40 | 200–280 | Frost enemy; mountain entry tier, below wolves and cultists. |
| `Deer_White` | 3 | 10–20 | Wild deer variant; retain deer progression instead of inflating harmless wildlife. |
| `Bjorn_sleeping` | 22 | 300–500 | Variant of Bjorn; preserve the existing parent level and reward. |
| `Troll_sleeping` | 22 | 300–500 | Variant of Troll; preserve the existing parent level and reward. |
| `Draugr_sleeping` | 30 | 100–150 | Variant of Draugr; preserve the existing parent level and reward. |
| `Draugr_Ranged_sleeping` | 31 | 180–220 | Variant of Draugr_Ranged; preserve the existing parent level and reward. |
| `Draugr_Elite_sleeping` | 34 | 300–500 | Variant of Draugr_Elite; preserve the existing parent level and reward. |
| `Ghost_sleeping` | 20 | 80–120 | Variant of Ghost; preserve the existing parent level and reward. |
| `Leech_cave` | 28 | 120–180 | Variant of Leech; preserve the existing parent level and reward. |
| `DvergerMageFire` | 61 | 700–900 | Mistlands dvergr mage specializations; match the existing mage. |
| `DvergerMageIce` | 61 | 700–900 | Mistlands dvergr mage specializations; match the existing mage. |
| `DvergerMageSupport` | 61 | 700–900 | Mistlands dvergr mage specializations; match the existing mage. |
| `DvergerAshlands` | 71 | 1000–1100 | Ashlands dvergr variant; match local humanoid tier. |
| `DvergerDeepNorth` | 83 | 1200–1500 | Deep North imprisoned dvergr; tougher than ordinary northern humanoids. |
| `GoblinDeepNorth` | 80 | 500–700 | Deep North captive fuling; below jotun warriors. |
| `Goblin_Gem` | 51 | 50–100 | Fleeing gem carrier; Plains-level identity with low noncombat reward. |
| `GoblinBrute_Hildir` | 55 | 2000–2500 | Hildir brute variant; match the existing Hildir brute encounter. |
| `ElakingLantern` | 80 | 800–1100 | Lantern elaking variant; match existing Elaking. |
| `Frysling` | 80 | 500–700 | Deep North ranged frost enemy; lower reward than northern melee elites. |
| `FrostWisp` | 80 | 200–300 | Small frost threat; conservative northern entry-tier reward. |
| `Greydwarf_Frozen` | 78 | 300–450 | Frozen greydwarf; below standard Deep North humanoids. |
| `Greydwarf_Shaman_Frozen` | 80 | 400–550 | Frozen shaman support threat; slightly above frozen greydwarf. |
| `Ghost_Void` | 82 | 400–600 | Void ghost with lightning attack; northern threat with modest reward for low durability. |
| `FrozenKing_p2` | 99 | 0–0 | Invulnerable boss transition; display boss level without transition XP. |
| `FrozenKing_p3` | 99 | 10500–17500 | Final boss phase; match existing FrozenKing reward band. |
| `Aspect_Eikthyr` | 95 | 0–0 | Final-boss echo encounter, not the original biome boss; zero XP prevents repeatable summon farming. |
| `Aspect_Elder` | 95 | 0–0 | Final-boss echo encounter, not the original biome boss; zero XP prevents repeatable summon farming. |
| `Aspect_Bonemass` | 95 | 0–0 | Final-boss echo encounter, not the original biome boss; zero XP prevents repeatable summon farming. |
| `Aspect_Moder` | 95 | 0–0 | Final-boss echo encounter, not the original biome boss; zero XP prevents repeatable summon farming. |
| `Aspect_Yagluth` | 95 | 0–0 | Final-boss echo encounter, not the original biome boss; zero XP prevents repeatable summon farming. |
| `Aspect_SeekerQueen` | 95 | 0–0 | Final-boss echo encounter, not the original biome boss; zero XP prevents repeatable summon farming. |
| `Aspect_Fader` | 95 | 0–0 | Final-boss echo encounter, not the original biome boss; zero XP prevents repeatable summon farming. |
| `Aspect_TentaRoot` | 90 | 0–0 | Final-boss adds or invulnerable helpers; zero XP prevents summon farming. |
| `Skeleton_aspect` | 90 | 0–0 | Final-boss adds or invulnerable helpers; zero XP prevents summon farming. |
| `BlobAspect` | 90 | 0–0 | Final-boss adds or invulnerable helpers; zero XP prevents summon farming. |
| `Tendril` | 90 | 0–0 | Final-boss adds or invulnerable helpers; zero XP prevents summon farming. |
| `Tendril_back` | 90 | 0–0 | Final-boss adds or invulnerable helpers; zero XP prevents summon farming. |
| `Bjorn_spiritcaller` | 80 | 0–0 | Player spirit companions; show a northern summon level without kill XP. |
| `Boar_spiritcaller` | 80 | 0–0 | Player spirit companions; show a northern summon level without kill XP. |
| `Moose_spiritcaller` | 80 | 0–0 | Player spirit companions; show a northern summon level without kill XP. |
| `Wolf_spiritcaller` | 80 | 0–0 | Player spirit companions; show a northern summon level without kill XP. |
| `Skeleton_Friendly` | 60 | 0–0 | Player-raised skeleton; no companion kill XP. |
| `Troll_Summoned` | 73 | 0–0 | Player-summoned troll; no companion kill XP. |
| `staff_greenroots_tentaroot` | 80 | 0–0 | Staff-summoned root; no companion kill XP. |
| `Boar_piggy` | 3 | 0–0 | Domestic offspring; no breeding XP farm. |
| `Wolf_cub` | 35 | 0–0 | Domestic offspring; no breeding XP farm. |
| `Lox_Calf` | 45 | 0–0 | Domestic offspring; no breeding XP farm. |
| `Asksvin_hatchling` | 65 | 0–0 | Domestic offspring; no breeding XP farm. |
| `Chicken` | 3 | 0–0 | Domestic chicken variant; no breeding XP farm. |
| `BogWitchKvastur` | 30 | 0–0 | Trader guardian in the Swamp; no reward for farming the trader guardian. |
| `Mistile` | 80 | 0–0 | Timed kamikaze entity; no repeatable projectile XP. |

## Explicit exclusions

- `Player`: Player avatar, not a monster.
- `TrainingDummy`: Practice target, not a progression enemy.
- `piece_TrainingDummy`: Buildable practice target.
- `DvergerTest`: Internal test prefab.
- `Ghost_old`: Legacy ghost prefab; current Ghost and Ghost_sleeping are covered.
- `ShadowPerson`: Noncombat talking apparition; all damage ignored.
- `Hive`: Internal encounter prototype; live encounter spawning not verified.
- `TheHive`: Invulnerable internal encounter prototype; live spawning not verified.

## Validation

Run `python3 scripts/validate_creatures.py` and `python3 -m unittest discover -s tests -p "test_*.py"`. The committed snapshot checks every character against an upstream mapping, a Ragnavik mapping, or a documented exclusion; it catches missing variants instead of requiring a fixed row count. Refresh the snapshot against the installed DLL and generated game inventory when either version changes. Review upstream overlaps before releasing an EpicMMO update because the loader keeps the first mapping for a name.

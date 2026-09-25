# Cart sign compatibility investigation

Supported binaries: CraftyCarts 3.2.3 and BoardersBumperBlurbs 1.0.3.

Both transpilers replace the first generic ZNetView lookup in Sign.Awake. The first replacement removes the generic call, so the other transpiler cannot match. BoardersBumperBlurbs only recognizes its BumperSticker component; CraftyCarts recognizes its BumperSticker parent. The wrong resolver returns null and vanilla Sign.Awake dereferences it. The bridge patches both resolver helpers and fills only missing results for recognized cart signs.

The CraftyCarts prefix also creates TMP on an active object before assigning a generated font. The bridge converts only CraftyCart legacy text, uses the vanilla sign font, and activates the replacement after assigning it. Missing templates retain upstream behavior.

Build with the existing Windows SDK and installed game/BepInEx reference directories. tests/CartSign.Tests uses Mono.Cecil from BepInEx core to validate both installed resolver APIs and the compiled font activation order. Run with paths to CraftyCartsRemake.dll, BoardersBumperBlurbs.dll and the built RagnavikCompat.dll. These are binary contract checks; rendering and editing must also be tested in Unity.

Manual regression: in the isolated Gale profile and local galetest1 world, load a CraftyCart, edit its bumper sign, unload/reload it, and verify persistence. Repeat with the vanilla cart bumper and a placed vanilla sign. Verify no Sign.Awake exception or LiberationSans warning. Both bumper mods must remain installed.

## Validation

Release build, binary contract checks, existing compatibility regressions, and package validation pass. The earlier local test artifact was labelled 1.0.17; the release is 1.0.18 because the creature-level release claimed 1.0.17 in the meantime. In-game loading, editing, and persistence verification remain pending.

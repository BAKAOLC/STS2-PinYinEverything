# Pinyin Everything

A RitsuLib-based Slay the Spire 2 mod that automatically replaces displayed Chinese text with pinyin.

## Coverage

- All text returned by the game's `LocString` localization system.
- Dynamically assigned MegaLabel and MegaRichTextLabel content.
- Generic Godot Button, Label, and RichTextLabel content.
- LineEdit and TextEdit placeholder text without modifying user input.
- Strings drawn directly through CanvasItem.DrawString.
- Scene-authored MegaText content after the control becomes ready.
- A RitsuLib settings page with an enable switch and configurable tone notation.

The conversion runs only while Simplified Chinese (`zhs`) or Traditional Chinese (`zht`) is selected. It preserves BBCode tags and inline image bodies. The embedded lexicon uses longest-phrase matching for common polyphonic words, then falls back to a default per-character reading.

Tone display defaults to tone marks such as `xiǎo`. It can be disabled or changed to tone numbers such as `xiao3`. Settings changes affect newly displayed text immediately; already-created controls may need the current screen to be reopened.

When Exclaim Everything is also installed, shared display patches explicitly run Pinyin Everything first so the converted text can be transformed again by Exclaim Everything.

## Requirements

- Slay the Spire 2 `0.107.1` or newer.
- RitsuLib `0.4.38` or newer.

## Build

```powershell
dotnet build .\STS2-PinyinEverything.csproj
```

This is a DLL-only mod. The build copies the following files into the configured local game mods directory:

- `STS2-PinyinEverything.dll`
- `LICENSE`
- `mod_manifest.json`
- `THIRD-PARTY-NOTICES.md`

The mod has no runtime pinyin package or companion assembly. Its compressed lexicon is embedded in the main DLL. To regenerate the lexicon from the pinned upstream datasets:

```powershell
.\scripts\Generate-PinyinLexicon.ps1
```

## Example

```text
获得3点[gold]敏捷[/gold]。
```

becomes:

```text
huo de 3 dian [gold]min jie[/gold]。
```

## License

This project is licensed under the GNU Affero General Public License v3.0 or later. See [LICENSE](LICENSE).

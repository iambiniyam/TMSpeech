# Fork notes: TMSpeech + offline zh→en translation

This is a fork of [TMSpeech](https://github.com/jxlpzqc/TMSpeech) (MIT) that adds
the thing TMSpeech declares but never implements: **translation**.

## Why this fork exists

TMSpeech already solves the hard Windows problem: WASAPI loopback capture of
system audio, sherpa-onnx recognition, and a caption overlay. It shows Chinese
text, which does not help someone who does not read Chinese.

Reading the source turned up a clean seam:

- `TMSpeech.Core/Plugins/ITranslator.cs` defines `string Translate(string text)`
- `PluginManager` loads translator plugins and exposes `.Translators`
- **nothing ever calls `Translate()`** — no implementation ships and no consumer exists

That is a declared-but-empty interface, which makes this a small, low-risk
contribution rather than an invasive rewrite.

## What changed

| File | Change |
|---|---|
| `Core/Plugins/IRecognizer.cs` | `TextInfo` gains `Translation` + `HasTranslation`. Additive only |
| `Core/ConfigTypes.cs` | new `TranslatorConfigTypes` (section name, selection key, config key) |
| `Core/JobManager.cs` | `JobManager.Translator` property; `OnSentenceDone` attaches the translation before anything downstream reads the event |
| `GUI/App.axaml.cs` | loads the configured translator at startup and attaches it |
| `GUI/ViewModels/ConfigViewModel.cs` | `TranslateSectionConfigViewModel`, mirroring the recognizer section |
| `GUI/Views/ConfigWindow.axaml` | new 翻译 tab with a translator dropdown |
| `GUI/Controls/HistoryView.axaml` | translation line under each history entry, hidden when absent |
| `Plugins/TMSpeech.Translator.Offline/**` | new plugin implementing `ITranslator` |

## Design decisions

**Additive, not breaking.** Every change is optional at runtime. With no
translator configured, `JobManager.Translator` is null, `TryTranslate` returns
null, `TextInfo.Translation` stays null, and captions render byte-identically to
upstream. Upstream users are unaffected.

**Translation never interrupts captions.** `TryTranslate` catches everything and
returns null on failure. A crashy or slow plugin degrades to "no translation"
with a notification, never to a stalled caption.

**Local by construction.** The plugin has two modes and neither touches the
network: an external command whose stdout is the translation, or a built-in
meeting glossary.

**The glossary is deliberately small and honest.** It covers common meeting
nouns and returns input unchanged when nothing matches, instead of emitting
mangled text that looks like a translation but is worse than nothing.

## Not yet verified

**None of this has been compiled.** There is no .NET SDK on the machine where
this fork was written, so the C# is unbuilt and untested. Structure was checked
with a tokenizer-based brace validator, and every interface member was matched
against the real declarations in the repository, but that is not a compile.

First thing to do on Windows:

```
dotnet build TMSpeech.sln
```

Expect to fix small things: `Nullable` warnings, a missing `using`, or an
Avalonia binding name. The plugin's `PluginConfigFormItem` usage was corrected
to the positional-record constructors actually used by the repo.

## Next steps

1. `dotnet build` and fix compiler errors
2. Drop a real translator behind the command hook — for example a call into the
   sherpa-onnx pipeline in the sibling `zhsub` project
3. Consider a bundled ONNX zh→en model, so no external command is needed
4. Upstream the `ITranslator` wiring as a PR if it is accepted; the plugin can
   stay separate

## License

TMSpeech is MIT. This fork keeps the same licence; see `LICENSE`.

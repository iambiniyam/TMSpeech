using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TMSpeech.Core.Plugins;

namespace TMSpeech.Translator.Offline;

public class OfflineTranslatorConfig
{
    /// <summary>External command; {text} is replaced by the sentence.</summary>
    public string Command { get; set; } = "";
}

/// <summary>
/// Offline zh->en translation plugin.
///
/// TMSpeech declares <see cref="ITranslator"/> and loads translator plugins,
/// but ships no implementation and never calls <c>Translate</c>. This plugin
/// supplies one, so the caption stream can show English beneath Chinese.
///
/// Both modes are local: nothing is sent over the network.
/// </summary>
public class OfflineTranslatorPlugin : ITranslator
{
    public string GUID => "7f3c1a52-9b4e-4d21-8a77-2c5e9d1b4f60";
    public string Name => "离线翻译";
    public string Description => "本地中译英，文本不上传。Local zh->en translation; sentences stay on the machine.";
    public string Version => "0.1.0";
    public string SupportVersion => "any";
    public string Author => "zhsub";
    public string Url => "https://github.com/";
    public string License => "MIT License";
    public string Note => "用于辅助阅读，非正式译文。An aid to comprehension, not a certified translation.";

    public bool Available => true;

    private OfflineTranslatorConfig _config = new();
    private readonly Dictionary<string, string> _cache = new(StringComparer.Ordinal);
    private const int CacheLimit = 512;

    public void Init() { }

    public void Destroy() => _cache.Clear();

    public IPluginConfigEditor CreateConfigEditor() => new OfflineTranslatorConfigEditor(_config, SaveConfig);

    private void SaveConfig(OfflineTranslatorConfig config)
    {
        _config = config;
        _cache.Clear();
    }

    public void LoadConfig(string config)
    {
        if (string.IsNullOrWhiteSpace(config)) return;
        try
        {
            _config = JsonSerializer.Deserialize<OfflineTranslatorConfig>(config) ?? new OfflineTranslatorConfig();
        }
        catch (JsonException)
        {
            _config = new OfflineTranslatorConfig();
        }
        _cache.Clear();
    }

    public string Translate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        if (_cache.TryGetValue(text, out var cached)) return cached;

        string result;
        try
        {
            result = string.IsNullOrWhiteSpace(_config.Command)
                ? GlossaryTranslate(text)
                : RunCommand(_config.Command, text);
        }
        catch
        {
            // Translation must never interrupt the caption stream.
            result = GlossaryTranslate(text);
        }

        if (_cache.Count >= CacheLimit) _cache.Clear();
        _cache[text] = result;
        return result;
    }

    private static string RunCommand(string command, string text)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add(text);

        using var proc = Process.Start(psi);
        if (proc == null) return GlossaryTranslate(text);

        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(5000);
        var trimmed = output.Trim();
        return string.IsNullOrEmpty(trimmed) ? GlossaryTranslate(text) : trimmed;
    }

    /// <summary>
    /// A deliberately small meeting vocabulary. It helps the reader follow the
    /// shape of a sentence and returns the input unchanged when nothing
    /// matches, rather than emitting mangled text.
    /// </summary>
    private static readonly (string Zh, string En)[] Glossary =
    {
        ("第一项", "first item"), ("第二项", "second item"), ("第三项", "third item"),
        ("项目", "project"), ("进度", "progress"), ("预算", "budget"),
        ("会议", "meeting"), ("材料", "materials"), ("准备", "prepare"),
        ("讨论", "discuss"), ("问题", "issue"), ("部门", "department"),
        ("财务", "finance"), ("下周", "next week"), ("本周", "this week"),
        ("明天", "tomorrow"), ("今天", "today"), ("大家", "everyone"),
        ("请", "please"), ("我们", "we"), ("完成", "complete"),
        ("截止", "deadline"), ("需求", "requirement"), ("客户", "client"),
        ("测试", "testing"), ("上线", "release"), ("版本", "version"),
        ("负责人", "owner"), ("确认", "confirm"), ("同意", "agree"),
    };

    private static string GlossaryTranslate(string text)
    {
        var result = text;
        var matched = false;
        foreach (var (zh, en) in Glossary)
        {
            if (!result.Contains(zh)) continue;
            result = result.Replace(zh, " " + en + " ");
            matched = true;
        }
        if (!matched) return text;
        return Regex.Replace(result, @"\s{2,}", " ").Trim();
    }
}

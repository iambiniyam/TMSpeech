using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TMSpeech.Core.Plugins;

namespace TMSpeech.Translator.Offline;

public class OfflineTranslatorConfigEditor : IPluginConfigEditor
{
    private readonly OfflineTranslatorConfig _config;
    private readonly Action<OfflineTranslatorConfig> _onSave;
    private readonly Dictionary<string, object> _values = new();
    private readonly List<PluginConfigFormItem> _items = new();

    public OfflineTranslatorConfigEditor(OfflineTranslatorConfig config, Action<OfflineTranslatorConfig> onSave)
    {
        _config = config;
        _onSave = onSave;
        _values[nameof(OfflineTranslatorConfig.Command)] = config.Command;
        BuildItems();
    }

    private void BuildItems()
    {
        _items.Clear();
        _items.Add(new PluginConfigFormItemText(
            nameof(OfflineTranslatorConfig.Command),
            "翻译命令 (留空则使用内置词汇表)",
            "指令的标准输出作为译文。例如: C:\\tools\\zh2en.exe"));
        _items.Add(new PluginConfigFormItemMessage(
            "翻译在本机完成，句子不会发送到网络。\nTranslation runs locally; sentences never leave this machine."));
    }

    public event EventHandler<EventArgs>? FormItemsUpdated;
    public event EventHandler<EventArgs>? ValueUpdated;

    public IReadOnlyList<PluginConfigFormItem> GetFormItems() => _items;

    public IReadOnlyDictionary<string, object> GetAll() => _values;

    public object GetValue(string key) => _values.TryGetValue(key, out var v) ? v : string.Empty;

    public void SetValue(string key, object value)
    {
        _values[key] = value;
        if (key == nameof(OfflineTranslatorConfig.Command))
        {
            _config.Command = value as string ?? string.Empty;
            _onSave(_config);
        }
        ValueUpdated?.Invoke(this, EventArgs.Empty);
    }

    public string GenerateConfig()
    {
        _config.Command = _values.TryGetValue(nameof(OfflineTranslatorConfig.Command), out var c)
            ? c as string ?? string.Empty
            : _config.Command;
        _onSave(_config);
        return JsonSerializer.Serialize(_config);
    }

    public void LoadConfigString(string config)
    {
        if (string.IsNullOrWhiteSpace(config)) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<OfflineTranslatorConfig>(config);
            if (loaded != null) _config.Command = loaded.Command;
        }
        catch (JsonException)
        {
        }
        _values[nameof(OfflineTranslatorConfig.Command)] = _config.Command;
        BuildItems();
        FormItemsUpdated?.Invoke(this, EventArgs.Empty);
    }
}

using Xunit;

namespace TMSpeech.Translator.Offline.Tests;

public class OfflineTranslatorTests
{
    private static OfflineTranslatorPlugin MakePlugin()
    {
        var plugin = new OfflineTranslatorPlugin();
        plugin.Init();
        return plugin;
    }

    [Fact]
    public void GlossaryTranslatesKnownMeetingTerms()
    {
        var plugin = MakePlugin();
        var result = plugin.Translate("我们下周要讨论项目进度。");

        Assert.Contains("next week", result);
        Assert.Contains("discuss", result);
        Assert.Contains("project", result);
        Assert.Contains("progress", result);
    }

    [Fact]
    public void UnknownTextIsReturnedUnchanged()
    {
        var plugin = MakePlugin();
        const string input = "这是一个完全没有匹配词表的句子。";

        // Better to show the original than mangled near-translation.
        Assert.Equal(input, plugin.Translate(input));
    }

    [Fact]
    public void EmptyAndWhitespaceAreHandled()
    {
        var plugin = MakePlugin();
        Assert.Equal("", plugin.Translate(""));
        Assert.Equal("   ", plugin.Translate("   "));
    }

    [Fact]
    public void ResultsAreCachedAndStable()
    {
        var plugin = MakePlugin();
        var a = plugin.Translate("项目进度");
        var b = plugin.Translate("项目进度");
        Assert.Equal(a, b);
    }

    [Fact]
    public void MissingCommandFallsBackToGlossary()
    {
        var plugin = MakePlugin();
        plugin.LoadConfig("{\"Command\":\"\"}");
        var result = plugin.Translate("预算问题");
        Assert.Contains("budget", result);
        Assert.Contains("issue", result);
    }

    [Fact]
    public void CommandThatDoesNotExistFallsBackInsteadOfThrowing()
    {
        var plugin = MakePlugin();
        plugin.LoadConfig("{\"Command\":\"definitely-not-a-real-binary-xyz\"}");

        // A broken translator must never interrupt captions.
        var result = plugin.Translate("项目进度");
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains("project", result);
    }

    [Fact]
    public void WhenNoTermMatchesOriginalIsPreserved()
    {
        var plugin = MakePlugin();
        const string input = "abc xyz";
        Assert.Equal(input, plugin.Translate(input));
    }

    [Fact]
    public void PluginMetadataIsFilledIn()
    {
        var plugin = MakePlugin();
        Assert.False(string.IsNullOrWhiteSpace(plugin.GUID));
        Assert.False(string.IsNullOrWhiteSpace(plugin.Name));
        Assert.False(string.IsNullOrWhiteSpace(plugin.Description));
        Assert.True(plugin.Available);
    }

    [Fact]
    public void ConfigEditorRoundTripsCommand()
    {
        var plugin = MakePlugin();
        var editor = plugin.CreateConfigEditor();
        editor.SetValue("Command", "translator.exe");
        var config = editor.GenerateConfig();
        Assert.Contains("translator.exe", config);

        var editor2 = plugin.CreateConfigEditor();
        editor2.LoadConfigString(config);
        Assert.Equal("translator.exe", editor2.GetValue("Command"));
    }

    [Fact]
    public void MalformedConfigDoesNotThrow()
    {
        var plugin = MakePlugin();
        plugin.LoadConfig("not json at all");
        Assert.False(string.IsNullOrWhiteSpace(plugin.Translate("项目")));
    }
}

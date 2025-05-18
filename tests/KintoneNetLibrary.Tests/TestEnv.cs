using System.Text.Json;

namespace KintoneNetLibrary.Tests;

internal static class TestEnv
{
    internal static readonly Config Settings = Load();

    internal sealed class Config
    {
        public required string Domain   { get; init; }
        public required string ApiToken { get; init; }
        public required int    AppID    { get; init; }
    }

    private static Config Load()
    {
        var json = File.ReadAllText("TestConfig.json");
        return JsonSerializer.Deserialize<Config>(json)
               ?? throw new InvalidOperationException("TestConfig.json 読み込み失敗");
    }
}
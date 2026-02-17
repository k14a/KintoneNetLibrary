using System.Text.Json;

namespace KintoneNetLibrary.Tests;

/// <summary>
/// テスト環境の設定を管理するクラス。テストで使用するKintoneのドメイン、APIトークン、アプリIDなどの設定をTestConfig.jsonから読み込みます。
/// </summary>
internal static class TestEnv {
    internal static readonly Config Settings = Load();

    internal sealed class Config {
        public required string Domain { get; init; }
        public required string ApiToken { get; init; }
        public required int AppID { get; init; }
    }

    /// <summary>
    /// TestConfig.jsonから設定を読み込むメソッド。ファイルが存在しない場合や内容が不正な場合は例外をスローします。
    /// </summary>
    /// <returns>読み込まれた設定オブジェクト</returns>
    /// <exception cref="InvalidOperationException">設定の読み込みに失敗した場合にスローされます</exception>
    private static Config Load() {
        var json = File.ReadAllText("TestConfig.json");
        return JsonSerializer.Deserialize<Config>(json)
               ?? throw new InvalidOperationException("TestConfig.json 読み込み失敗");
    }
}
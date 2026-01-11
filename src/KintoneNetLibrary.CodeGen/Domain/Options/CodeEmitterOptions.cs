namespace KintoneNetLibrary.CodeGen.Domain.Options;

/// <summary>
/// C# コード生成オプション
/// </summary>
public class CodeEmitterOptions {
    // ================================
    // 1. 基本設定
    // ================================

    /// <summary>
    /// 生成するクラスの名前空間
    /// </summary>
    public string Namespace { get; set; } = "KintoneModels";
    /// <summary>
    /// メインモデルのクラス名（App{Id} がデフォルト）
    /// </summary>
    public string? MainClassName { get; set; }

    // ================================
    // 2. 生成スタイル設定
    // ================================

    /// <summary>
    /// record を使用するか（false の場合は class）
    /// </summary>
    public bool UseRecord { get; set; } = false;

    /// <summary>
    /// partial class を生成するか
    /// </summary>
    public bool UsePartial { get; set; } = true;

    /// <summary>
    /// #nullable enable を付与するか
    /// </summary>
    public bool NullableEnabled { get; set; } = true;

    /// <summary>
    /// 出力ディレクトリ
    /// </summary>
    public string OutputDirectory { get; set; } = ".";

    /// <summary>
    /// 既存ファイルを上書きするか
    /// </summary>
    public bool OverwriteExistingFiles { get; set; }

    // ================================
    // 3. モード設定（pure / library）
    // ================================

    /// <summary>
    /// KintoneNetLibrary を使用するか（false の場合 pure モード）
    /// </summary>
    public bool UseKintoneNetLibrary { get; set; } = true;

    // ================================
    // 4. 拡張設定（将来のためのフラグ）
    // ================================

    /// <summary>
    /// 選択肢フィールドを enum として生成するか
    /// </summary>
    public bool GenerateEnums { get; set; } = false;

    /// <summary>
    /// サブテーブルクラスを別ファイルに分割するか
    /// </summary>
    public bool SplitSubTableFiles { get; set; } = true;

    /// <summary>
    /// pure モードの補助クラス（UserInfo など）を別ファイルに分割するか
    /// </summary>
    public bool SplitHelperFiles { get; set; } = true;

    // ================================
    // 補助メソッド（kmodel との連携を強化）
    // ================================

    /// <summary>
    /// OutputDirectory を絶対パスに正規化する
    /// </summary>
    public void NormalizePaths() {
        this.OutputDirectory = Path.GetFullPath(this.OutputDirectory);
    }

    /// <summary>
    /// MainClassName が null の場合にデフォルト値を設定する
    /// </summary>
    public void EnsureDefaults(int appId) {
        this.MainClassName ??= $"App{appId}";
    }
}

namespace KintoneNetLibrary.CodeGen.Domain.Options;

/// <summary>
/// Python コード生成オプション
/// </summary>
public class PythonEmitterOptions : CodeEmitterOptions {
    /// <summary>
    /// 生成するモジュール名
    /// </summary>
    public string ModuleName { get; set; } = "kintone_models";

    /// <summary>
    /// type hint を付与するか
    /// </summary>
    public bool UseTypeHint { get; set; } = true;

    /// <summary>
    /// 出力ファイル拡張子
    /// </summary>
    public override string FileExtension => ".py";

    /// <summary>
    /// 言語ごとのコメント文字列
    /// </summary>
    public override string CommentPrefix => "#";

    /// <summary>
    /// オプションのデフォルト値を設定する
    /// </summary>
    /// <param name="appId">アプリケーションの ID</param>
    public void EnsureDefaults(int appId) {
        base.EnsureDefaults(appId);

        this.ModuleName ??= "kintone_models";
    }
}
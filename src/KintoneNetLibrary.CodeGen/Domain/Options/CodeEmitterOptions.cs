namespace KintoneNetLibrary.CodeGen.Domain.Options;

/// <summary>
/// コード生成オプション(共通)
/// </summary>
public abstract class CodeEmitterOptions {
    /// <summary>
    /// メインモデルのクラス名（App{Id} がデフォルト）
    /// </summary>
    public string? MainClassName { get; set; }

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

    /// <summary>
    /// 生成ファイルの先頭に出力するコメント
    /// </summary>
    public string? HeaderComment { get; set; }

    // 言語固有
    /// <summary>
    /// 出力ファイル拡張子
    /// </summary>
    public abstract string FileExtension { get; }

    /// <summary>
    /// 言語ごとのコメント文字列
    /// </summary>
    public abstract string CommentPrefix { get; }

    /// <summary>
    /// オプションのデフォルト値を設定する
    /// </summary>
    /// <param name="appId">アプリケーションの ID</param>
    public virtual void EnsureDefaults(int appId) {
        // MainClassName が null の場合は App{Id}
        this.MainClassName ??= $"App{appId}";
    }
}

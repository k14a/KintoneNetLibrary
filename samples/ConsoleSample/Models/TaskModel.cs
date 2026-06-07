using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.ConsoleSample.Models;

/// <summary>
/// タスク管理アプリのモデル。
/// </summary>
/// <remarks>
/// KintoneModelBase&lt;TSelf&gt; は自己参照ジェネリックのため、継承クラス自身を型引数に渡す。
/// AppId と Access は必ず override しなければコンパイルエラーになる。
/// </remarks>
public class TaskModel : KintoneModelBase<TaskModel> {
    // AppId は Kintone 管理画面のアプリ URL から確認できる（例: /k/1/）
    public override int AppId { get; init; } = 1;

    // アクセス情報は ApiTokenAccess か UserPasswordAccess を選ぶ。
    // ApiToken は 1 アプリにつき 1 つ発行される点に注意。
    // 複数アプリを 1 プログラムで扱う場合は各モデルで別の Access を設定する。
    public override KintoneAccessBase Access { get; init; }
        = new ApiTokenAccess("your-domain.cybozu.com", "YOUR_API_TOKEN_HERE");

    // fieldCode は Kintone フィールドの「フィールドコード」と一致させること（スペース・大文字小文字も含む）
    [KintoneItem("タイトル", KintoneFieldType.SingleLineText)]
    public string Title { get; set; } = string.Empty;

    // KintoneModelBase に virtual string Status が定義されているため同名は使えない。
    // Kintone の組み込み「ステータス」フィールドを使う場合は base.Status を override すること。
    // ここでは独自のドロップダウンフィールドとして別名で定義する。
    [KintoneItem("進捗状況", KintoneFieldType.DropDown)]
    public string ProgressStatus { get; set; } = string.Empty;

    [KintoneItem("優先度", KintoneFieldType.RadioButton)]
    public string Priority { get; set; } = string.Empty;

    // KintoneDateTime は Kintone の日付・日時フォーマットをラップする型。
    // C# の DateTime/DateOnly と相互変換できる。
    [KintoneItem("期日", KintoneFieldType.Date)]
    public KintoneDateTime DueDate { get; set; } = new();

    [KintoneItem("メモ", KintoneFieldType.MultiLineText)]
    public string Note { get; set; } = string.Empty;
}

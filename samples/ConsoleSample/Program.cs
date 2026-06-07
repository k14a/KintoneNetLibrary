using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Factories;
using KintoneNetLibrary.Infrastructure.Repositories;
using KintoneNetLibrary.Infrastructure.Services;
using KintoneNetLibrary.ConsoleSample.Models;

// ─────────────────────────────────────────────────────────────────────────────
// DI コンテナの構築
// ─────────────────────────────────────────────────────────────────────────────

var services = new ServiceCollection();

// コンソール向けロガー。本番では適切なロガー実装に差し替える。
services.AddLogging(builder => builder
    .AddConsole()
    .SetMinimumLevel(LogLevel.Warning));

// KintoneApi が内部で HttpClient を使用するため必須
services.AddHttpClient();

// ── KintoneNetLibrary の標準 DI 登録 ──────────────────────────────────────
// KintoneApiFactory: アクセス情報からアプリごとの IKintoneApi を生成する
services.AddScoped<IKintoneApiFactory, KintoneApiFactory>();

// KintoneRepository: IKintoneApiFactory を経由して API を呼び出す
services.AddScoped<IKintoneRepository, KintoneRepository>();

// KintoneModelCrudService: KintoneModelBase の static CRUD メソッドが受け取るサービス
// 内部で IKintoneTypedCrudService<T> へ委譲するため、両方の登録が必要
services.AddScoped<IKintoneModelCrudService, KintoneModelCrudService>();

// KintoneTypedCrudService<T>: ジェネリック型でオープンジェネリック登録する
services.AddScoped(typeof(IKintoneTypedCrudService<>), typeof(KintoneTypedCrudService<>));

await using var provider = services.BuildServiceProvider();

// KintoneModelBase の static メソッドに渡す共通サービス
var crud = provider.GetRequiredService<IKintoneModelCrudService>();

Console.WriteLine("=== KintoneNetLibrary コンソールサンプル ===");

// ─────────────────────────────────────────────────────────────────────────────
// 1. レコードの登録
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n--- 1. レコード登録 ---");

var newTask = new TaskModel {
    Title  = "サンプルタスク",
    ProgressStatus = "未着手",
    Priority = "中",
    Note   = "KintoneNetLibrary から登録したタスクです。",
};

// CreateAsync は内部で 100 件ずつ分割して登録するため大量データにも対応できる。
// 戻り値の KintoneWriteResult には Succeeded / Failed の両リストが含まれる。
var createResult = await newTask.CreateAsync(crud);

if (createResult.Succeeded.Count == 0) {
    Console.WriteLine("登録失敗:");
    foreach (var fail in createResult.Failed) {
        Console.WriteLine($"  {fail.ErrorMessage}");
    }
    return;
}

var created = createResult.Succeeded[0];
Console.WriteLine($"登録成功: RecordId={created.RecordId}");

// ─────────────────────────────────────────────────────────────────────────────
// 2. Id 指定でレコードを取得
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n--- 2. Id 指定で取得 ---");

// レコードが存在しない場合は null を返す。
// RecordId は string 型であることに注意（Kintone API の仕様）。
var found = await TaskModel.FindByIdAsync(crud, created.RecordId!);
if (found != null) {
    Console.WriteLine($"  タイトル : {found.Title}");
    Console.WriteLine($"  進捗状況 : {found.ProgressStatus}");
    Console.WriteLine($"  優先度   : {found.Priority}");
}

// ─────────────────────────────────────────────────────────────────────────────
// 3. クエリで複数レコードを取得
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n--- 3. クエリ検索 ---");

// Kintone のクエリ構文をそのまま渡す。
// 内部で cursor を自動運用するため、取得件数の上限（500件）を超えても一括取得できる。
var activeTasks = await TaskModel.FindByQueryAsync(
    crud,
    query: "進捗状況 in (\"未着手\", \"進行中\") order by $id asc");
Console.WriteLine($"  該当件数: {activeTasks.Count} 件");

// ─────────────────────────────────────────────────────────────────────────────
// 4. 型安全なクエリ（IKintoneTypedCrudService）
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n--- 4. 型安全なクエリ ---");

// KintoneQuery<T>.Where() にラムダ式を渡すとフィールドコードのタイポをコンパイル時に検出できる。
// ただし Kintone 固有の演算子（like、in の値リストなど）は文字列クエリで記述する必要がある。
var typed = provider.GetRequiredService<IKintoneTypedCrudService<TaskModel>>();
var query = new KintoneQuery<TaskModel>().Where(x => x.Priority == "高");
var highPriority = await typed.FindAsync(kintoneQuery: query);
Console.WriteLine($"  高優先度タスク: {highPriority.Count()} 件");

// ─────────────────────────────────────────────────────────────────────────────
// 5. レコードの更新
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n--- 5. レコード更新 ---");

if (found != null) {
    found.Status = "進行中";
    found.Note   = "ステータスを更新しました。";

    // UpdateAsync は RecordId がセットされていないと InvalidOperationException をスローする。
    // FindByIdAsync で取得したインスタンスは RecordId が自動セットされているため安全。
    var updateResult = await found.UpdateAsync(crud);
    Console.WriteLine($"  更新成功: {updateResult.Succeeded.Count} 件");
}

// ─────────────────────────────────────────────────────────────────────────────
// 6. レコードの削除
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n--- 6. レコード削除 ---");

if (found != null) {
    // validateExistence: true（デフォルト）は削除前に存在確認を行うため API 呼び出しが 1 回増える。
    // 削除対象の存在が確実な場合は false を指定するとパフォーマンスが向上する。
    var deleteResult = await found.DeleteAsync(crud, validateExistence: false);
    Console.WriteLine($"  削除成功: {deleteResult.Succeeded.Count} 件");
    if (deleteResult.Failed.Count > 0) {
        Console.WriteLine($"  削除失敗: {deleteResult.Failed.Count} 件");
    }
}

Console.WriteLine("\n=== 完了 ===");

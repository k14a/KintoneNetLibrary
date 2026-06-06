# TODO - コードレビュー指摘事項

コードレビュー（2026-06-04）で洗い出した問題点の一覧です。

---

## 凡例

- `[ ]` 未対応
- `[x]` 対応済み

---

## 1. Clean Architecture の依存方向違反

### 1-1. `IKintoneApiFactory`（Domain）が `KintoneApi`（Infrastructure）を直接参照している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Interfaces/IKintoneApiFactory.cs:1`](src/KintoneNetLibrary/Domain/Interfaces/IKintoneApiFactory.cs#L1)
- **問題**: `using KintoneNetLibrary.Infrastructure.Api;` を参照し、戻り値型に具象クラス `KintoneApi` を使用している。Domain が Infrastructure 実装に依存している。
- **対応方針**: `KintoneApi` の代わりに `IKintoneApi`（Application 層）を返すよう変更する。

### 1-2. `IKintoneRepository`（Domain）が `KintoneQuery`（Application）を参照している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs:2`](src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs#L2)
- **問題**: `using KintoneNetLibrary.Application.UseCases;` を参照し、メソッド引数に `KintoneQuery<T>` を使用している。Domain が Application に依存している。
- **対応方針**: `KintoneQuery<T>` をビルドした `string` クエリを引数に取るよう変更するか、`KintoneQuery` を Domain 層に移動する。

### 1-3. `KintoneModelBase`（Domain）が `KintoneServiceLocator`（Infrastructure）を直接呼び出している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:6`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L6), [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:21`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L21)
- **問題**: `using KintoneNetLibrary.Infrastructure.Helpers;` を参照し、Service Locator パターンで Infrastructure のサービスを取得している。Domain が Infrastructure に依存している。
- **対応方針**: 下記「設計問題」の Service Locator 廃止と合わせて対応する（→ 項目 3-1）。

### 1-4. `KintoneModelBase`（Domain）が `NameConvertor`（Infrastructure）を参照している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.cs:3`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.cs#L3)
- **問題**: `using KintoneNetLibrary.Infrastructure.Converters;` を参照している。
- **対応方針**: `NameConvertor` クラスを Domain または共通層に移動するか、Domain 側に抽象化（インターフェース）を置く。

---

## 2. 命名規則の問題

### 2-1. `NameConvertor` のスペルミス
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/NameConverter.cs`](src/KintoneNetLibrary/Domain/Entities/NameConverter.cs)
- **問題**: クラス名が `NameConvertor`（誤り）。他の Converter クラスはすべて `Converter`（正しいスペル）を使用している。
  - 正しい例: `KintoneRecordConverter`, `KintoneValueConverter`, `KintoneContentConverter`, `KintoneErrorConverter`, `TimeOnlyJsonConverter`
- **対応方針**: `NameConvertor` → `NameConverter` にリネームし、ファイル名も合わせて変更する。

### 2-2. `KintoneModelBase.Crud.cs` の変数名スペルミス
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:343`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L343), [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs:387`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs#L387)
- **問題**: ローカル変数名が `keyTYpe`（大文字 Y は誤り）。
- **対応方針**: `keyTYpe` → `keyType` に修正する。

---

## 3. 設計上の問題

### 3-1. Service Locator パターンの使用（アンチパターン）
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Helpers/KintoneServiceLocator.cs`](src/KintoneNetLibrary/Infrastructure/Helpers/KintoneServiceLocator.cs), [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: Domain エンティティ（`KintoneModelBase<TSelf>`）内で `KintoneServiceLocator.Resolve<T>()` を直接呼び出している。依存性が隠蔽され、テスト困難・初期化順序依存が生じる。
- **対応方針**: エンティティから CRUD 操作を分離し、アプリケーションサービス（またはコンストラクタ注入）経由で呼び出す設計に変更する。

#### 設計変更の概要
**問題**: `KintoneModelBase` の CRUD メソッドが `KintoneModelContext`（Domain 層のグローバル静的リゾルバー）経由でサービスを取得しており、依存が隠蔽されている。

**方針**: 全 CRUD メソッドに `IKintoneModelCrudService service` パラメーターを追加し、呼び出し側が明示的にサービスを渡す設計に変更する（Service Locator → Explicit Dependency）。

```csharp
// 変更前
await model.SaveAsync();
await BookModel.FindAllAsync();

// 変更後
await model.SaveAsync(service);
await BookModel.FindAllAsync(service);
```

**変更ファイル**:
- `Domain/Entities/KintoneModelBase.Crud.cs` — 全メソッドに `service` パラメーターを追加、`Service` プロパティ削除
- `Domain/Common/KintoneModelContext.cs` — 削除
- `Infrastructure/Helpers/KintoneServiceLocator.cs` — 削除（`Resolve<T>()` も未使用のため）
- テスト 5 ファイル（75 箇所）— `KintoneServiceLocator.Initialize(...)` の setup を削除し、モックを直接渡す形式に変更

### 3-2. `IKintoneApi` インターフェースが大きすぎる（ISP 違反）
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Application/Interfaces/IKintoneApi.cs`](src/KintoneNetLibrary/Application/Interfaces/IKintoneApi.cs)
- **問題**: 1 つのインターフェースに Find（型付き・Raw・Stream）/CRUD/File/Cursor の全操作が含まれており、インターフェース分離原則に反する。
- **対応方針**: 責任ごとに分割する（例: `IKintoneRecordReadApi`, `IKintoneRecordWriteApi`, `IKintoneFileApi`, `IKintoneCursorApi`）。

### 3-3. `KintoneModelCrudService.cs` のメソッドコメントと実装の不一致
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Services/KintoneModelCrudService.cs:29-34`](src/KintoneNetLibrary/Infrastructure/Services/KintoneModelCrudService.cs#L29)
- **問題**: `DeleteAsync` メソッドのサマリーコメントが「既存のレコードを更新します」になっており、戻り値説明も「削除結果のインデックス情報」と「更新」が混在している。
- **対応方針**: コメントを正しく修正する（「既存のレコードを削除します」）。

### 3-4. `KintoneModelBase` に責任が集中しすぎている
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/`](src/KintoneNetLibrary/Domain/Entities/) （7 ファイルに分割済みだが依然として肥大）
- **問題**: Kintoneアクセス情報の管理・CRUD操作・JSON変換・レコード構築・ユーティリティ機能が 1 つのクラスに集約されており、SRP に反する。
- **対応方針**: 段階的にリファクタリングし、CRUD 操作は外部サービスに委譲する（→ 項目 3-1 と連動）。

#### 設計変更の概要
**現状**: `KintoneModelBase.JsonLoader.cs` および `KintoneSubTableBase.cs` が
`KintoneValueConverter`（Infrastructure）を参照しており、Clean Architecture の依存方向に違反している。

**方針**: `KintoneValueConverter` を `Infrastructure/Converters/` から `Domain/Entities/` へ移動する
（1-4 での `NameConverter` 移動と同様の対応）。

**変更ファイル**:
- `Domain/Entities/KintoneValueConverter.cs` — 新規作成（移動）、namespace を `Domain.Entities` に変更
- `Infrastructure/Converters/KintoneValueConverter.cs` — 削除
- `Domain/Entities/KintoneModelBase.JsonLoader.cs` — `using` を Infrastructure → Domain に変更
- `Domain/Entities/KintoneSubTableBase.cs` — 同上
- `Infrastructure/Converters/KintoneRecordConverter.cs` — 同上

**備考**: 現在の 7 partial ファイル構成（Crud / Conversion / Hooks / JsonLoader / RecordBuilder / Utility）は
SRP の段階的分離として適切であり、これ以上の責任分離は破壊的変更を伴うため今回は対象外とする。

---

## 4. 重複コード

### 4-1. `BulkAsync` / `SingleAsync` の実装パターンが重複している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: `CreateBulkAsync`/`CreateSingleAsync`、`UpdateBulkAsync`/`UpdateSingleAsync`、`DeleteBulkAsync`/`DeleteSingleAsync`、`SaveBulkAsync`/`SaveSingleAsync`、`SaveWithRetryBulkAsync`/`SaveWithRetrySingleAsync` で、「Single 版は Bulk 版をループで呼ぶ」という同じパターンが 5 回繰り返されている。
- **対応方針**: プライベートヘルパー `RunSingleWriteAsync`（書き込み系4種）と `RunSingleDeleteAsync<TItem>`（削除系2種）に集約した。あわせて `KintoneDeleteResult` に `Merge` メソッドを追加。

### 4-2. 各 `BulkAsync` と `SingleAsync` 内での `KintoneServiceLocator.Resolve` の重複呼び出し
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: 全 static メソッドがそれぞれ `KintoneServiceLocator.Resolve<IKintoneModelCrudService>()` を呼び出している（インスタンスメソッドは `this.Service` プロパティを使用）。
- **対応方針**: 項目 3-1 にて `KintoneServiceLocator` を削除し、明示的サービス引数に変更済みのため解消。

---

## 5. その他

### 5-1. `FindByKeyAsync` / `FindByKeysAsync` でリフレクションを多用している
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneModelBase.Crud.cs)
- **問題**: キーフィールドへのアクセスにリフレクションを多用しており、型安全性が低く、パフォーマンス・可読性に懸念がある。
- **対応方針**: `abstract BuildKeyQuery()` の代わりに、呼び出し側が明示的にキーセレクターを渡す型安全な形式（`Expression<Func<TSelf, TKey>> keySelector`）に変更した。`CreateFieldSelector`・`ConvertExpression` ヘルパーおよび `using System.Reflection;` を削除。テストも更新し、`IsKey` 属性バリデーション系のテストを削除した。

### 5-2. `IKintoneRepository` の戻り値が `string` で型安全でない
- [x] **対象ファイル**: [`src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs`](src/KintoneNetLibrary/Domain/Interfaces/IKintoneRepository.cs)
- **問題**: CRUD・Find 系メソッドがすべて `Task<string>` / `Task<string?>` を返しており、JSON 文字列をそのまま戻り値としている。型安全性がなく、呼び出し側でのデシリアライズが必要になる。
- **対応方針**: 設計上の意図的仕様のため**コード変更なし**。`IKintoneRepository` はローレベルAPIとして生JSONを返し、独自JSONパーサーを使いたい利用者向けに提供する。型付き結果が必要な場合は `KintoneTypedCrudService` 等のハイレベルAPIを使用する。

---

## 6. KintoneNetLibrary.Backup プロジェクトのレビュー（2026-06-06）

### 6-A. Clean Architecture 依存方向違反

#### 6-A-1. `BackupService` / `RestoreService` が `KintoneApi`（具象クラス）を直接 `new` している
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:114`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L114), [`src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs:126`](src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs#L126)
- **問題**: `EnsureApiInitialized()` 内で `new KintoneApi(...)` を直接呼び出している。Infrastructure 実装への具体的な依存が生じており、テストが困難。
- **対応方針**: `IKintoneApiFactory` 等のインターフェースを通じてAPIインスタンスを取得するか、コンストラクタで `IKintoneApi` を受け取るよう変更する。

#### 6-A-2. `BackupService` / `RestoreService` が `KintoneAppMetadataApi`（具象クラス）を直接 `new` している
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:651`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L651), [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:699`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L699), [`src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs:602`](src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs#L602)
- **問題**: メソッド内で `new KintoneAppMetadataApi(...)` を直接生成している。`SaveFieldSchemaAsync` / `SaveLayoutSchemaAsync` / `ValidateSchemaAsync` それぞれで重複生成しており、テスト・差し替えが困難。
- **対応方針**: `ISchemaProvider` を拡張するか、`KintoneAppMetadataApi` 用のインターフェースをコンストラクタ注入に変更する。

#### 6-A-3. `RestoreOptions` が `KintoneNetLibrary.Domain.Common.KintoneConstants` を直接参照している
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Application/DTOs/RestoreOptions.cs:3`](src/KintoneNetLibrary.Backup/Application/DTOs/RestoreOptions.cs#L3)
- **問題**: Backup プロジェクトの Application 層 DTO がメインプロジェクトの Domain 層に依存しており、`KintoneConstants.KintoneLimit` を `BatchSize` のデフォルト値として使用している。
- **対応方針**: Backup プロジェクト内に定数を定義するか、デフォルト値をリテラルで記述する（`= 100` 等）。

### 6-B. 設計上の問題

#### 6-B-1. `IBackupService.Options` / `IRestoreService.Options` がインターフェースで `{ get; set; }` として公開されている
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Application/Interfaces/IBackupService.cs:17`](src/KintoneNetLibrary.Backup/Application/Interfaces/IBackupService.cs#L17), [`src/KintoneNetLibrary.Backup/Application/Interfaces/IRestoreService.cs:17`](src/KintoneNetLibrary.Backup/Application/Interfaces/IRestoreService.cs#L17)
- **問題**: オプションが実行後も外部から変更可能で、サービスの状態管理が呼び出し側に依存してしまう。
- **対応方針**: `RunBackupAsync(BackupOptions options)` のようにメソッド引数として渡すか、コンストラクタで受け取る形にする。

#### 6-B-2. `BackupService.BackupRoot` が `public` になっている
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:44`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L44)
- **問題**: `BackupRoot` は `IBackupService` に含まれない内部実装の詳細であるにもかかわらず `public` で公開されている。
- **対応方針**: `private` に変更する。

#### 6-B-3. `BackupService` のインスタンス状態が `RunBackupAsync()` 複数回呼び出しでリセットされない
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs)
- **問題**: `_partIndex`・`_api`・`BackupRoot` 等がインスタンス状態として保持されており、2回目の `RunBackupAsync()` 呼び出し時に `_partIndex` がリセットされず重複ファイル名が生成される可能性がある。
- **対応方針**: `RunBackupAsync()` 冒頭でインスタンス状態を初期化する、またはサービスを都度 new するよう DI を構成する。

#### 6-B-4. `RestoreService._result` がインスタンス変数で複数回呼び出し時に累積する
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs:37`](src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs#L37)
- **問題**: `private readonly RestoreResult _result = new();` がインスタンスフィールドのため、`RunRestoreAsync()` を2回呼ぶと `AddedRecords` 等が前回結果に加算される。
- **対応方針**: `_result` を `RunRestoreAsync()` のメソッドローカル変数にする。

#### 6-B-5. `Options = default!` による null 安全性の偽装
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:43`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L43), [`src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs:51`](src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs#L51)
- **問題**: `Options` が実質 `null` の状態で `EnsureApiInitialized()` 以外の箇所からアクセスされると `NullReferenceException` が発生する。
- **対応方針**: 6-B-1 の対応（コンストラクタ注入 or メソッド引数化）で根本解消する。

#### 6-B-6. `PrepareBackupDirectories()` が `this.Options.OutputPath` を副作用で書き換えている
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:198`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L198)
- **問題**: `this.Options.OutputPath = new DirectoryInfo(backupRoot);` により、呼び出し元が渡した設定オブジェクトを実行中に上書きしている。`BackupOptions.OutputPath` は `{ get; set; }` のため可能だが、外部から見えない副作用になっており予期しにくい。
- **対応方針**: バックアップ先の実際のパスは戻り値として返し、`Options.OutputPath` は書き換えない。

### 6-C. デッドコード（未使用メソッド）

#### 6-C-1. `BackupService` に未使用メソッドが複数存在する
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs)
- **問題**: 以下のメソッドは `RunBackupAsync()` から呼び出されておらず、デッドコードになっている。
  - `FetchRecordsAsStreamAsync`（line 210）
  - `CountRecordsInStreamAsync`（line 228）
  - `SaveSplitJsonFilesFromStreamAsync`（line 274）
  - `DownloadFilesWithResultFromStreamAsync`（line 433）
  - `ExtractFileInfos`（line 461）
  - `DownloadSingleFileAsync`（line 517）
  - `ExtractJsonObject`（line 532）
- **対応方針**: 不要なメソッドを削除する。将来的に必要な場合はその時点で実装する。

### 6-D. バグ・エラーハンドリング

#### 6-D-1. `DownloadFilesAsync` が失敗時に `FileFailedCount` をインクリメントしていない
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:421`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L421)
- **問題**: ダウンロード失敗時にログのみで `result.FileFailedCount++` をしていないため、`BackupResult.FileFailedCount` が常に 0 のまま返される。
- **対応方針**: `catch` ブロック内で `result.FileFailedCount++` を追加する。ただし `result` への参照を `DownloadFilesAsync` に渡す必要がある。

#### 6-D-2. `RunBackupAsync` の `catch` でスタックトレースが記録されていない
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:88`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L88)
- **問題**: `result.Errors.Add(ex.Message)` のみで、スタックトレースがログにも結果にも残らない。障害調査が困難になる。
- **対応方針**: `this._logger?.LogError(ex, "バックアップ中にエラーが発生しました")` を追加する。

#### 6-D-3. `DeleteAllRecordsAsync` が `RawFindAllAsync` で全件一括取得している
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs:657`](src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs#L657)
- **問題**: `RawFindAllAsync(fieldCodes: ["$id"])` で全レコードの ID を一度にメモリに展開している。レコード数が多い場合にメモリを大量消費する。
- **対応方針**: カーソル API（`StreamRecordsAsync` 等）を使ってページ単位で取得・削除する。

### 6-E. コメント・命名の問題

#### 6-E-1. `ValidateSchemaAsync` のコメントに誤字がある
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs:621`](src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs#L621)
- **問題**: `// リストアリストアで扱わなすすｓすｋすスキップ` という誤字・文字化けのようなコメントが残っている。
- **対応方針**: 正しいコメント（例: `// Backup プロジェクトで扱わないフィールドタイプはスキップ`）に修正する。

#### 6-E-2. `GetPartFiles` の `<param>` コメントが引数名と不一致
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs:181`](src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs#L181)
- **問題**: `<param name="parts">` と記述されているが、実際の引数名は `partFiles`。
- **対応方針**: `<param name="partFiles">` に修正する。

#### 6-E-3. `BackupService` コンストラクタ引数の命名が不一致
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs:26`](src/KintoneNetLibrary.Backup/Infrastructure/Services/BackupService.cs#L26)
- **問題**: プライマリコンストラクタの引数 `_accessFactory` だけ `_` プレフィックスが付いており、他の引数（`schemaProvider`、`httpClientFactory` 等）と命名規則が異なる。
- **対応方針**: `accessFactory` に統一する。

### 6-F. その他

#### 6-F-1. `BackupManifest.Options` の型が `object`
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Application/DTOs/BackupManifest.cs:63`](src/KintoneNetLibrary.Backup/Application/DTOs/BackupManifest.cs#L63)
- **問題**: `public object Options { get; set; } = default!;` のため、JSON デシリアライズ時に型情報が失われ `JsonElement` になる。再利用時にキャストが必要になる。
- **対応方針**: 専用の `BackupOptionsSnapshot` DTO 等を定義するか、`JsonElement` 型にする。

#### 6-F-2. `BackupOptions` のプロパティ mutability が不一致
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Application/DTOs/BackupOptions.cs`](src/KintoneNetLibrary.Backup/Application/DTOs/BackupOptions.cs)
- **問題**: 大半のプロパティは `{ get; init; }` だが、`Pretty`・`EscapeUnicode`・`SplitSize`・`OutputPath` だけが `{ get; set; }` になっており、一貫性がない。
- **対応方針**: `OutputPath` は 6-B-6 の対応で書き換え不要にし、`Pretty`・`EscapeUnicode`・`SplitSize` も `init;` に統一する。

#### 6-F-3. `RemoveRecordIdFields` の再帰が SUBTABLE 以外にも適用されている
- [x] **対象ファイル**: [`src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs:462`](src/KintoneNetLibrary.Backup/Infrastructure/Services/RestoreService.cs#L462)
- **問題**: `else` ブランチで SUBTABLE 以外の `JsonObject` フィールドにも再帰的に `RemoveRecordIdFields` を呼び出しているが、通常の Kintone フィールド（`{"type": "...", "value": "..."}` 構造）は `$id` を持たないため無駄な再帰が発生している。
- **対応方針**: 再帰は SUBTABLE のみに限定する（`else` ブランチを削除）。

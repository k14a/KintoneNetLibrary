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

---

## 7. KintoneNetLibrary.CodeGen プロジェクトのレビュー（2026-06-06）

### 7-A. Clean Architecture 依存方向違反

#### 7-A-1. `CSharpCodeEmitter` / `PythonCodeEmitter` が `SystemDateTimeProvider`（Infrastructure）を直接 `new` している
- [x] **対象ファイル**: [`src/KintoneNetLibrary.CodeGen/Application/Emitters/CodeEmitter.CSharp.cs:36`](src/KintoneNetLibrary.CodeGen/Application/Emitters/CodeEmitter.CSharp.cs#L36), [`src/KintoneNetLibrary.CodeGen/Application/Emitters/CodeEmitter.Python.cs:32`](src/KintoneNetLibrary.CodeGen/Application/Emitters/CodeEmitter.Python.cs#L32)
- **問題**: `clock ?? new SystemDateTimeProvider()` と、Application 層のエミッターが `KintoneNetLibrary.Infrastructure.Helpers`（Infrastructure 層）に直接依存している。
- **対応方針**: DI 側で `IDateTimeProvider` のデフォルト実装を登録するか、CodeGen プロジェクト内に実装を移動する。

### 7-B. 設計上の問題

#### 7-B-1. `ISchemaProvider.SetDomain` がインターフェースをステートフルにしており、実装と矛盾している
- [x] **対象ファイル**: [`src/KintoneNetLibrary.CodeGen/Application/Interfaces/ISchemaProvider.cs:23`](src/KintoneNetLibrary.CodeGen/Application/Interfaces/ISchemaProvider.cs#L23), [`src/KintoneNetLibrary.CodeGen/Infrastructure/Services/SchemaProvider.cs:48-55`](src/KintoneNetLibrary.CodeGen/Infrastructure/Services/SchemaProvider.cs#L48)
- **問題**: `GetMetadataAsync(domain, ...)` は引数 `domain` を受け取るのに `this._domain is null` をチェックして例外を投げる。`this._domain` は実際には使っておらず、`SetDomain` を呼ばないと引数 `domain` があっても常に例外になる。
- **対応方針**: `SetDomain` を廃止し、引数 `domain` をそのまま利用する形に統一する。`ISchemaProvider` からも `SetDomain` を除去する。

#### 7-B-2. `ICodeEmitter.SetNameConverter` の設計により、異なる `nameConverter` での再利用が無視される
- [x] **対象ファイル**: [`src/KintoneNetLibrary.CodeGen/Infrastructure/Factories/CodeEmitterFactory.cs:22`](src/KintoneNetLibrary.CodeGen/Infrastructure/Factories/CodeEmitterFactory.cs#L22), [`src/KintoneNetLibrary.CodeGen/Application/Emitters/CodeEmitter.CSharp.cs:44`](src/KintoneNetLibrary.CodeGen/Application/Emitters/CodeEmitter.CSharp.cs#L44)
- **問題**: `SetNameConverter` 内で `this._converter ??= converter` のため、2回目以降の `Create(lang, nameConverter)` 呼び出しで `nameConverter` が無視される。Factory が同一インスタンスを辞書で保持し使い回しているため。
- **対応方針**: Factory の `Create` でエミッターを都度生成するか、コンストラクタで `INameConverter` を受け取る設計に変更する。

### 7-C. バグ・論理エラー

#### 7-C-1. `CSharpNameConverter.Dictionary`（日本語→英語変換辞書）がデッドコード
- [x] **対象ファイル**: [`src/KintoneNetLibrary.CodeGen/Application/Emitters/NameConverter.CSharp.cs:16`](src/KintoneNetLibrary.CodeGen/Application/Emitters/NameConverter.CSharp.cs#L16), [`src/KintoneNetLibrary.CodeGen/Application/Emitters/NameConverter.CSharp.cs:103`](src/KintoneNetLibrary.CodeGen/Application/Emitters/NameConverter.CSharp.cs#L103)
- **問題**: `Convert` 内で `SanitizeFieldCode` により日本語が `_` に置換された後の `codeName`（PascalCase 済み）で辞書を引いているため、日本語キー（"顧客"、"担当者" 等）とマッチする機会がなく、辞書が事実上機能していない。
- **対応方針**: 辞書の参照を `SanitizeFieldCode` 前の元の `label` に対して行うよう修正する。

#### 7-C-2. `CSharpTypeMapper.MapPure` で `OrganizationSelect` と `GroupSelect` の型マッピングが逆
- [x] **対象ファイル**: [`src/KintoneNetLibrary.CodeGen/Application/Emitters/TypeMapper.CSharp.cs:141`](src/KintoneNetLibrary.CodeGen/Application/Emitters/TypeMapper.CSharp.cs#L141)
- **問題**: `OrganizationSelect → List<GroupInfo>` / `GroupSelect → List<OrganizationInfo>` となっており逆（`OrganizationSelect` は組織選択なので `OrganizationInfo`、`GroupSelect` はグループ選択なので `GroupInfo` が正しい）。
- **対応方針**: 型マッピングを正しい順序に修正する。

### 7-D. コード品質・その他

#### 7-D-1. `MetadataConverter` のサブフィールド検索が O(n²)
- [x] **対象ファイル**: [`src/KintoneNetLibrary.CodeGen/Infrastructure/Services/MetadataConverter.cs:71`](src/KintoneNetLibrary.CodeGen/Infrastructure/Services/MetadataConverter.cs#L71)
- **問題**: `subMeta.SubFields!.Any(sf => sf.FieldCode == f.Code)` と `subMeta.SubFields!.First(sf => sf.FieldCode == f.Code)` を各サブフィールドで繰り返し呼び出しており O(n²) の処理になっている。
- **対応方針**: `subMeta.SubFields.ToDictionary(sf => sf.FieldCode)` で事前に辞書化し O(n) に改善する。

#### 7-D-2. `CSharpNameConverter.IsCSharpKeyword` で毎回配列を生成している
- [x] **対象ファイル**: [`src/KintoneNetLibrary.CodeGen/Application/Emitters/NameConverter.CSharp.cs:154`](src/KintoneNetLibrary.CodeGen/Application/Emitters/NameConverter.CSharp.cs#L154)
- **問題**: `new[] { ... }.Contains(name)` で毎回配列を生成している。
- **対応方針**: `private static readonly HashSet<string>` のフィールドに変更する。

#### 7-D-3. `PythonCodeEmitter` のコメントアウトコード残存・命名規則違反・未使用変数
- [x] **対象ファイル**: [`src/KintoneNetLibrary.CodeGen/Application/Emitters/CodeEmitter.Python.cs:23`](src/KintoneNetLibrary.CodeGen/Application/Emitters/CodeEmitter.Python.cs#L23)
- **問題**:
  - コンストラクタ引数のコメントアウトが残存している（`// INameConverterFactory converterFactory,` 等）
  - フィールド `_invalid_field_name` がスネークケースで C# 命名規則に違反（`_invalidFieldName` が正しい）
  - `GenerateHeader` で `appName` を計算しているが実際には使っておらず、旧コードがコメントアウトのまま残存している
- **対応方針**: 未使用コードの削除、命名規則の統一、未使用変数の除去。

---

## 8. KintoneNetLibrary.Tests ユニットテスト修正（2026-06-06）

統合テスト分離作業（`KintoneNetLibrary.IntegrationTests` プロジェクト新設）により、以前は TestConfig.json 不在でアセンブリがクラッシュして隠れていた既存の失敗 52 件が表面化した。以下はその分類と対応方針。

### 8-A. Castle.DynamicProxy: `internal SampleModel` と `Mock<ILogger>` の非互換

- [x] **対象ファイル**:
  - [`tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceDeleteTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceDeleteTests.cs)（全件）
  - [`tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceCreateTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceCreateTests.cs)（`CreateAsyncWhenBulkFailsLogsWarningMessage`）
  - [`tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceUpdateTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceUpdateTests.cs)（ログ検証 2 件）
  - [`tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceFindTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceFindTests.cs)（エラー系 2 件）
  - [`tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceSaveTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceSaveTests.cs)（ログ検証系）
- **問題**: 各テストファイル内の `SampleModel` / `SampleModel2` が `internal class` であるため、Moq（Castle.DynamicProxy）が `ILogger<KintoneTypedCrudService<SampleModel>>` のプロキシを生成できない。`Microsoft.Extensions.Logging.Abstractions` が strong-named アセンブリなため `InternalsVisibleTo` 宣言が必要。
  ```
  Can not create proxy for type ILogger`1[...SampleModel...] because type SampleModel
  is not accessible. Make it public, or internal and mark your assembly with
  [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=...")]
  ```
- **対応方針**: テストプロジェクトに `AssemblyInfo.cs` を追加し、以下を宣言する。
  ```csharp
  [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
  ```

### 8-B-1. `ParseCreatedRecords` の JSON 形式不一致（実装バグ）

- [x] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Helpers/KintoneResponseParser.cs:31`](src/KintoneNetLibrary/Infrastructure/Helpers/KintoneResponseParser.cs#L31), [`src/KintoneNetLibrary/Domain/Entities/KintoneRecordIndexesResponse.cs`](src/KintoneNetLibrary/Domain/Entities/KintoneRecordIndexesResponse.cs)
- **問題**: `ParseCreatedRecords` が `KintoneRecordIndexesResponse`（`{"records":[{"id":"...","revision":"..."},...]}`）でパースしているが、Kintone Create API（`POST /k/v1/records.json`）の実際のレスポンスは `{"ids":[...],"revisions":[...]}` 形式。テストモックは正しい形式を返しているが実装が対応できていない。
  - 影響テスト: `CreateAsyncWithValidRecordsReturnsSucceededResult`（期待 10 件 → 実際 0 件）、`CreateAsyncWhenKintoneExceptionOccursAndRetrySucceedsAddsToSucceeded`、`CreateAsyncWhenBulkFailsAndSingleRetrySucceedsAllRecordsAddedToSucceeded`、`CreateAsyncWhenRevisionIsInvalidSetsDefaultRevision`、`CreateAsyncWhenResponseHasNullOrEmptyIdsSetsEmptyStringToId` 等
- **対応方針**: `ParseCreatedRecords` を `{"ids":[...],"revisions":[...]}` 形式に対応するよう修正する（`KintoneRecordIndexesResponse` ではなく専用パーサーを使う）。

### 8-B-2. UpdateAsync テストのモック JSON 形式不一致（テストバグ）

- [x] **対象ファイル**: [`tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceUpdateTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceUpdateTests.cs), [`tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceSaveTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceSaveTests.cs)
- **問題**: テストのモックが `{"ids":[...],"revisions":[...]}` を返しているが、`ParseUpdatedRecords` は Kintone Update API の実際のレスポンス形式である `{"records":[{"id":"...","revision":"..."},...]}`（`KintoneRecordIndexesResponse`）を期待している。
  - 影響テスト: `UpdateAsyncWithSingleRecordReturnsSucceededResult`、`UpdateAsyncWithMultipleRecordsReturnsAllSucceeded`、`UpdateAsyncWhenBulkFailsAndSingleRetrySucceedsRecordsAddedToSucceeded` 等
- **対応方針**: UpdateAsync・SaveAsync 系テストのモック返却 JSON を `{"records":[{"id":"...","revision":"..."},...]}` 形式に修正する。

### 8-C. DownloadFile / UploadFile: 相対 URL と `BaseAddress` 不整合

- [x] **対象ファイル**: [`src/KintoneNetLibrary/Infrastructure/Api/KintoneApi.File.cs:158`](src/KintoneNetLibrary/Infrastructure/Api/KintoneApi.File.cs#L158), [`src/KintoneNetLibrary/Infrastructure/Api/KintoneApi.File.cs:209`](src/KintoneNetLibrary/Infrastructure/Api/KintoneApi.File.cs#L209)
- **問題**: `DownloadFileAsync` / `DownloadFileStreamAsync` / `UploadFileInternalAsync` が相対 URL（`"file.json?fileKey=..."` 等）で `HttpRequestMessage` を構築するため `HttpClient.BaseAddress` が必須。テストで `MockHttpMessageHandler.ToHttpClient()` を使う際に `BaseAddress` が未設定のため `InvalidOperationException` が発生。
  - 影響テスト: `KintoneApiDownloadFileTests` の大半（15 件）、`KintoneApiFileUploadTests` の一部（4 件）
- **対応方針**: `BuildRequestUri`（絶対 URL）を使うよう実装を修正するか、失敗テストすべてに `httpClient.BaseAddress = new Uri($"https://{DummyDomain}/k/v1/")` を追加する。

### 8-D. `KintoneModelFileService` ロガー呼び出しの検証失敗

- [x] **対象ファイル**: [`tests/KintoneNetLibrary.Tests/Services/KintoneModelFileServiceTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelFileServiceTests.cs)（`DownloadFilesAsyncContinuesOnDownloadError`、`DownloadFilesAsyncSkipsFilesWithEmptyFileKey`）
- **問題**: `LoggerMessage.Define` によるログ呼び出しは `ILogger.IsEnabled()` を先行チェックし、`false` が返ると `Log()` を呼ばない。Moq の `Mock<ILogger>` はデフォルトで `IsEnabled` が `false` を返すため、Moq.Verify で `0 times` になる。
- **対応方針**: テストで `mock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true)` 等を追加し、ログが実際に呼ばれるよう設定する。

### 8-E-1. `ValidateSubTablePropertiesWhenTypeIsValidListDoesNotThrow` — バリデーター強化との不一致

- [x] **対象ファイル**: [`tests/KintoneNetLibrary.Tests/Helpers/KintoneModelValidatorTests.cs`](tests/KintoneNetLibrary.Tests/Helpers/KintoneModelValidatorTests.cs)
- **問題**: テスト内のサブテーブル行モデル `FakeSubRow` の `ID` プロパティに `[KintoneItem]` が付与されていないため、バリデーターの強化（サブテーブル行の全プロパティに `KintoneItemAttribute` を要求）により例外が発生。
  ```
  System.InvalidOperationException : サブテーブル行 'FakeSubRow' のプロパティ 'ID' に KintoneItemAttribute がありません。
  ```
- **対応方針**: `FakeSubRow.ID` に `[KintoneItem(...)]` を追加するか、バリデーターの対象プロパティ条件を見直す。

### 8-E-2. `FindAsyncWithSingleIDReturnsSingleRecord` — `$id` の JSON パース失敗

- [x] **対象ファイル**: [`tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceFindTests.cs`](tests/KintoneNetLibrary.Tests/Services/KintoneModelCrudServiceFindTests.cs)
- **問題**: テストが `JsonSerializerOptions` に `ReferenceHandler.Preserve` を設定しているため、シリアライズした JSON に `$id` メタデータが埋め込まれる。`KintoneResponseParser.ParseRecord<T>` はこれを文字列として読もうとし `JsonException` が発生。
  ```
  The JSON value could not be converted to System.String. Path: $.$id
  ```
- **対応方針**: テストの `JsonSerializerOptions` から `ReferenceHandler.Preserve` を除去する（または `ParseRecord` を参照保持 JSON に対応させる）。

### 8-E-3. `QueryMultiSelectorAny` — クエリビルダーの `in (...)` 生成バグ

- [x] **対象ファイル**: [`tests/KintoneNetLibrary.Tests/Helpers/Queries/KintoneQueryExpressionsTests.cs:225`](tests/KintoneNetLibrary.Tests/Helpers/Queries/KintoneQueryExpressionsTests.cs#L225)（テスト側）および クエリビルダー実装
- **問題**: `MultiSelector in ("選択肢1", "選択肢2")` が生成されるべきところ、`"MultiSelector"選択肢1""選択肢2""` という不正な文字列が生成される。`in` キーワードと括弧が欠落している。
- **対応方針**: クエリビルダーの `in` 演算子（`MultiSelect` 向け）の実装を修正し、`フィールドコード in ("値1", "値2")` の形式で生成されるようにする。

---

## 9. v1.0.0 リリース目標 — NuGet 公開対応

### 9-1. csproj パッケージメタデータの設定
- [ ] 各ライブラリ（KintoneNetLibrary / Backup / CodeGen）の csproj に以下を追加する
  - `<PackageId>` / `<Authors>` / `<Copyright>` / `<Description>`
  - `<PackageTags>` / `<PackageLicenseExpression>`
  - `<PackageProjectUrl>` / `<RepositoryUrl>` / `<RepositoryType>`
  - `<PackageReadmeFile>` で README.md を同梱する

### 9-2. ターゲットフレームワークの方針決定
- [ ] 現在 `net10.0` 専用だが、`net10.0` は非 LTS のためライブラリとして対象が狭い
  - **選択肢 A**: `net8.0` (LTS) を追加して多ターゲット対応（`net8.0;net10.0`）
  - **選択肢 B**: `net8.0` 単一ターゲットにして上位互換とする
  - `LangVersion=preview` や net10.0 固有 API の使用状況を確認してから判断する

### 9-3. public API の整理
- [ ] パッケージ利用者に公開すべきでないクラス・型を `internal` に絞り込む
- [ ] テストプロジェクトから `internal` メンバーを参照できるよう `InternalsVisibleTo` を設定する

### 9-4. シンボルパッケージ（snupkg）の設定
- [ ] 各ライブラリの csproj に以下を追加し、デバッグ体験を向上させる
  ```xml
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  ```

### 9-5. GitHub Actions — NuGet 公開ワークフローの追加
- [ ] `v*` タグ push 時に `dotnet pack` → `nuget push` するジョブを `.github/workflows/` に追加する
- [ ] NuGet API キーを GitHub Secrets に登録する

### 9-6. ライセンスファイルの確認
- [ ] `LICENSE` ファイルの内容と `<PackageLicenseExpression>` の値が一致していることを確認する

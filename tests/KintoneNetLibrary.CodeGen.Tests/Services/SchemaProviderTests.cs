using KintoneNetLibrary.CodeGen.Infrastructure.Services;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using KintoneNetLibrary.CodeGen.Application.Interfaces;

namespace KintoneNetLibrary.CodeGen.Tests.Services;

/// <summary>
/// SchemaProvider の単体テストクラス。SchemaProvider は、kintone アプリのメタデータを取得し、コード生成に必要なスキーマ情報を提供するためのクラスであり、その機能が正しく動作することを確認するためのテストを提供する。
/// </summary>
public class SchemaProviderTests {
    private readonly SchemaProvider _provider;
    private readonly Mock<IKintoneAppMetadataApi> _mockApi;
    private readonly Mock<IMetadataConverter> _mockConverter;

    /// <summary>
    /// SchemaProviderTests クラスのコンストラクタ。テストクラスのインスタンスが生成される際に、必要な依存関係をモックして SchemaProvider のインスタンスを初期化する。これにより、SchemaProvider のメソッドをテストする際に、実際の API 呼び出しを行わずに、モックされたデータを使用してテストを実行できるようになる。
    /// 具体的には、IKintoneAppMetadataApi をモックして、GetAppMetadataAsync メソッドが特定のデータを返すように設定する。これにより、GetSchemaAsync や CompareAsync のテストで、モックされたメタデータを使用して、SchemaProvider の機能が正しく動作することを確認できるようになる。また、ILogger<SchemaProvider> もモックして、必要なロギング機能を提供する。さらに、SetDomain メソッドを呼び出して、テストで使用するドメインを設定する。これにより、SchemaProvider のメソッドがドメイン情報を必要とする場合に、適切なドメインが設定されていることを保証する。
    /// </summary>
    public SchemaProviderTests() {
        var logger = Mock.Of<ILogger<SchemaProvider>>();
        this._mockApi = new Mock<IKintoneAppMetadataApi>();
        this._mockConverter = new Mock<IMetadataConverter>();

        // SchemaProvider が DI で IKintoneAppMetadataApi を受け取る前提
        this._provider = new SchemaProvider(
            logger: logger,
            metadataApi: this._mockApi.Object,
            converter: this._mockConverter.Object
        );

        // デフォルトの最新メタデータ（GetSchemaAsync / CompareAsync 共通）
        this._mockApi.Setup(x => x.GetAppMetadataAsync("example.cybozu.com", "dummy", 1))
            .ReturnsAsync(new KintoneAppMetadata {
                AppId = 1,
                Revision = 3,
                Fields = [
                    new KintoneFieldMetadata {
                        FieldCode = "customer",
                        FieldLabel = "顧客名",
                        FieldType = KintoneFieldType.SingleLineText,
                        Required = true,
                        Options = []
                    }
                ]
            });

        // Convert は取得したメタデータをそのままスキーマに変換する
        this._mockConverter
            .Setup(x => x.Convert(It.IsAny<KintoneAppMetadata>()))
            .Returns<KintoneAppMetadata>(meta => new KintoneNetLibrary.CodeGen.Domain.Schemas.KintoneAppSchema {
                AppId = meta.AppId,
                Revision = meta.Revision,
                Fields = meta.Fields.Select(f => new KintoneNetLibrary.CodeGen.Domain.Schemas.KintoneFieldSchema {
                    FieldCode = f.FieldCode,
                    Label = f.FieldLabel,
                    FieldType = f.FieldType,
                    Required = f.Required,
                    Options = f.Options?.ToList() ?? []
                }).ToList(),
                SubTables = []
            });
    }


    /// <summary>
    /// SchemaProviderTests クラス内で使用する、IKintoneAppMetadataApi のフェイク実装クラス。FakeKintoneAppMetadataApi は、GetAppMetadataAsync メソッドが特定のメタデータを返すように実装されており、GetFieldsJsonAsync と GetLayoutJsonAsync メソッドは空の JSON を返すように実装されている。このクラスは、SchemaProvider のテストで使用されることを想定しており、実際の API 呼び出しを行わずに、モックされたデータを提供するために使用される。
    /// 例えば、GetAppMetadataAsync メソッドは、AppId が 1、Revision が 3、Fields に 1 つのフィールド（FieldCode が "customer"、FieldLabel が "顧客名"、FieldType が SingleLineText、Required が true）を含む KintoneAppMetadata オブジェクトを返すように実装されている。これにより、SchemaProvider の GetSchemaAsync や CompareAsync のテストで、このフェイク API を使用して、特定のメタデータを提供し、SchemaProvider の機能が正しく動作することを確認できるようになる。また、GetFieldsJsonAsync と GetLayoutJsonAsync メソッドは空の JSON を返すように実装されているため、これらのメソッドが呼び出された場合でも、テストが正常に実行されるようになっている。
    /// </summary>
    public class FakeKintoneAppMetadataApi : IKintoneAppMetadataApi {
        /// <summary>
        /// FakeKintoneAppMetadataApi クラスの GetAppMetadataAsync メソッド。指定されたドメイン、API トークン、およびアプリ ID に基づいて、特定の KintoneAppMetadata オブジェクトを返すように実装されている。このメソッドは、SchemaProvider のテストで使用されることを想定しており、実際の API 呼び出しを行わずに、モックされたメタデータを提供するために使用される。
        /// 例えば、ドメインが "example.cybozu.com"、API トークンが "dummy"、アプリ ID が 1 の場合に、AppId が 1、Revision が 3、Fields に 1 つのフィールド（FieldCode が "customer"、FieldLabel が "顧客名"、FieldType が SingleLineText、Required が true）を含む KintoneAppMetadata オブジェクトを返すように実装されている。これにより、SchemaProvider の GetSchemaAsync や CompareAsync のテストで、このフェイク API を使用して、特定のメタデータを提供し、SchemaProvider の機能が正しく動作することを確認できるようになる。
        /// </summary>
        public KintoneAppMetadata Metadata { get; set; } =
            new KintoneAppMetadata {
                AppId = 1,
                Revision = 3,
                Fields = [
                    new KintoneFieldMetadata {
                        FieldCode = "customer",
                        FieldLabel = "顧客名",
                        FieldType = KintoneFieldType.SingleLineText,
                        Required = true,
                        Options = []
                    }
                ]
            };

        /// <summary>
        /// FakeKintoneAppMetadataApi クラスの GetAppMetadataAsync メソッド。指定されたドメイン、API トークン、およびアプリ ID に基づいて、事前に設定された KintoneAppMetadata オブジェクトを返すように実装されている。このメソッドは、SchemaProvider のテストで使用されることを想定しており、実際の API 呼び出しを行わずに、モックされたメタデータを提供するために使用される。これにより、SchemaProvider の GetSchemaAsync や CompareAsync のテストで、このフェイク API を使用して、特定のメタデータを提供し、SchemaProvider の機能が正しく動作することを確認できるようになる。
        /// </summary>
        /// <param name="domain">Kintone のドメイン</param>
        /// <param name="apiToken">API トークン</param>
        /// <param name="appId">アプリ ID</param>
        /// <returns>事前に設定された KintoneAppMetadata オブジェクト</returns>
        public Task<KintoneAppMetadata> GetAppMetadataAsync(string domain, string apiToken, int appId) => Task.FromResult(this.Metadata);

        /// <summary>
        /// FakeKintoneAppMetadataApi クラスの GetFieldsJsonAsync メソッド。指定されたドメイン、API トークン、およびアプリ ID に基づいて、空の JSON オブジェクトを返すように実装されている。このメソッドは、SchemaProvider のテストで使用されることを想定しており、実際の API 呼び出しを行わずに、モックされたデータを提供するために使用される。これにより、SchemaProvider の GetSchemaAsync や CompareAsync のテストで、このフェイク API を使用して、特定のメタデータを提供し、SchemaProvider の機能が正しく動作することを確認できるようになる。
        /// </summary>
        /// <param name="domain">Kintone のドメイン</param>
        /// <param name="apiToken">API トークン</param>
        /// <param name="appId">アプリ ID</param>
        /// <returns>空の JSON オブジェクト</returns>
        public Task<string> GetFieldsJsonAsync(string domain, string apiToken, int appId) => Task.FromResult("{}");

        /// <summary>
        /// FakeKintoneAppMetadataApi クラスの GetLayoutJsonAsync メソッド。指定されたドメイン、API トークン、およびアプリ ID に基づいて、空の JSON オブジェクトを返すように実装されている。このメソッドは、SchemaProvider のテストで使用されることを想定しており、実際の API 呼び出しを行わずに、モックされたデータを提供するために使用される。これにより、SchemaProvider の GetSchemaAsync や CompareAsync のテストで、このフェイク API を使用して、特定のメタデータを提供し、SchemaProvider の機能が正しく動作することを確認できるようになる。
        /// </summary>
        /// <param name="domain">Kintone のドメイン</param>
        /// <param name="apiToken">API トークン</param>
        /// <param name="appId">アプリ ID</param>
        /// <returns>空の JSON オブジェクト</returns>
        public Task<string> GetLayoutJsonAsync(string domain, string apiToken, int appId) => Task.FromResult("{}");
    }

    // -----------------------------
    // GetSchemaAsync のテスト
    // -----------------------------
    /// <summary>
    /// SchemaProviderTests クラスの GetSchemaAsync_ReturnsConvertedSchema テストメソッド。GetSchemaAsync メソッドが、API から取得したメタデータを正しく変換して、コード生成に必要なスキーマ情報を返すことを確認するためのテスト。このテストでは、事前にモックされた IKintoneAppMetadataApi を使用して、特定の KintoneAppMetadata オブジェクトを返すように設定されている。GetSchemaAsync メソッドを呼び出して、返されたスキーマ情報が期待される値と一致することをアサートする。具体的には、AppId が 1、Revision が 3、Fields に 1 つのフィールド（FieldCode が "customer"、FieldLabel が "顧客名"、FieldType が SingleLineText、Required が true）を含むスキーマ情報が返されることを確認する。これにより、GetSchemaAsync メソッドが API から取得したメタデータを正しく変換していることが確認できる。
    /// </summary>
    [Fact]
    public async Task GetSchemaAsync_ReturnsConvertedSchema() {
        var schema = await this._provider.GetSchemaAsync("example.cybozu.com", "dummy", 1);

        Assert.Equal(1, schema.AppId);
        Assert.Equal(3, schema.Revision);
        Assert.Single(schema.Fields);

        var f = schema.Fields[0];
        Assert.Equal("customer", f.FieldCode);
        Assert.Equal("顧客名", f.Label);
        Assert.Equal(KintoneFieldType.SingleLineText, f.FieldType);
        Assert.True(f.Required);
    }

    // -----------------------------
    // CompareAsync のテスト（追加）
    // -----------------------------
    /// <summary>
    /// SchemaProviderTests クラスの CompareAsync_DetectsAddedField テストメソッド。CompareAsync メソッドが、バックアップのメタデータと最新のメタデータを比較して、フィールドの追加を正しく検出することを確認するためのテスト。このテストでは、バックアップのメタデータにフィールドが存在しない状態を設定し、最新のメタデータには 1 つのフィールド（FieldCode が "customer"）が存在するようにモックされた IKintoneAppMetadataApi を使用して設定されている。CompareAsync メソッドを呼び出して、返された差分情報にフィールドの追加が正しく検出されていることをアサートする。具体的には、差分情報に 1 つの差分が含まれており、その差分の DiffType が Added であり、追加されたフィールドの FieldCode が "customer" であることを確認する。これにより、CompareAsync メソッドがバックアップと最新のメタデータを比較して、フィールドの追加を正しく検出していることが確認できる。
    /// </summary>
    [Fact]
    public async Task CompareAsync_DetectsAddedField() {
        // backup: フィールドなし
        var backup = new KintoneAppMetadata {
            AppId = 1,
            Revision = 1,
            Fields = []
        };

        var diffs = await this._provider.CompareAsync(backup, "example.cybozu.com", "dummy", 1);

        Assert.Single(diffs);
        Assert.Equal(KintoneMetadataDiffTypes.Added, diffs[0].DiffType);
        Assert.NotNull(diffs[0].After);
        Assert.Equal("customer", diffs[0].After!.FieldCode);
    }

    /// <summary>
    /// SchemaProviderTests クラスの CompareAsync_DetectsChangedField テストメソッド。CompareAsync メソッドが、バックアップのメタデータと最新のメタデータを比較して、フィールドの変更を正しく検出することを確認するためのテスト。このテストでは、バックアップのメタデータにフィールド（FieldCode が "customer"、FieldLabel が "旧ラベル"）が存在し、最新のメタデータには同じ FieldCode を持つフィールドが存在するが、FieldLabel が "顧客名" に変更されているようにモックされた IKintoneAppMetadataApi を使用して設定されている。CompareAsync メソッドを呼び出して、返された差分情報にフィールドの変更が正しく検出されていることをアサートする。具体的には、差分情報に 1 つの差分が含まれており、その差分の DiffType が Changed であり、変更されたプロパティに FieldLabel が含まれていることを確認する。これにより、CompareAsync メソッドがバックアップと最新のメタデータを比較して、フィールドの変更を正しく検出していることが確認できる。
    /// </summary>
    [Fact]
    public async Task CompareAsync_DetectsChangedField() {
        // backup: 顧客名 → 最新: 顧客名（ラベル変更）
        var backup = new KintoneAppMetadata {
            AppId = 1,
            Revision = 1,
            Fields = [
                new KintoneFieldMetadata {
                    FieldCode = "customer",
                    FieldLabel = "旧ラベル",
                    FieldType = KintoneFieldType.SingleLineText,
                    Required = true,
                    Options = []
                }
            ]
        };

        var diffs = await this._provider.CompareAsync(backup, "example.cybozu.com", "dummy", 1);

        Assert.Single(diffs);
        var diff = diffs[0];

        Assert.Equal(KintoneMetadataDiffTypes.Changed, diff.DiffType);
        Assert.Contains(nameof(KintoneFieldMetadata.FieldLabel), diff.ChangedProperties);
        Assert.Equal(1, diff.BeforeRevision);
        Assert.Equal(3, diff.AfterRevision);
    }

    /// <summary>
    /// SchemaProviderTests クラスの GetSchemaAsync_WithFakeApi_Works テストメソッド。GetSchemaAsync メソッドが、FakeKintoneAppMetadataApi を使用して、API から取得したメタデータを正しく変換して、コード生成に必要なスキーマ情報を返すことを確認するためのテスト。このテストでは、FakeKintoneAppMetadataApi を使用して、特定の KintoneAppMetadata オブジェクトを返すように設定されている。GetSchemaAsync メソッドを呼び出して、返されたスキーマ情報が期待される値と一致することをアサートする。具体的には、AppId が 1、Revision が 3、Fields に 1 つのフィールド（FieldCode が "customer"）を含むスキーマ情報が返されることを確認する。これにより、GetSchemaAsync メソッドが API から取得したメタデータを正しく変換していることが確認できる。
    /// </summary>
    [Fact]
    public async Task GetSchemaAsync_WithFakeApi_Works() {
        var fakeApi = new FakeKintoneAppMetadataApi();

        var provider = new SchemaProvider(
            logger: Mock.Of<ILogger<SchemaProvider>>(),
            metadataApi: fakeApi,
            converter: this._mockConverter.Object
        );

        var schema = await provider.GetSchemaAsync("example.cybozu.com", "dummy", 1);

        Assert.Equal(1, schema.AppId);
        Assert.Equal(3, schema.Revision);
        Assert.Single(schema.Fields);
        Assert.Equal("customer", schema.Fields[0].FieldCode);
    }
}

using KintoneNetLibrary.CodeGen.Infrastructure.Services;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace KintoneNetLibrary.CodeGen.Tests.Services;

public class SchemaProviderTests {
    private readonly SchemaProvider _provider;
    private readonly Mock<IKintoneAppMetadataApi> _mockApi;

    public SchemaProviderTests() {
        var logger = Mock.Of<ILogger<SchemaProvider>>();
        this._mockApi = new Mock<IKintoneAppMetadataApi>();

        // SchemaProvider が DI で IKintoneAppMetadataApi を受け取る前提
        this._provider = new SchemaProvider(
            logger: logger,
            metadataApi: this._mockApi.Object
        );

        this._provider.SetDomain("example");

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
    }


    public class FakeKintoneAppMetadataApi : IKintoneAppMetadataApi {
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

        // public Task<string> GetFieldsJsonAsync(int appId, string apiToken)
        //     => Task.FromResult("{}");

        // public Task<string> GetLayoutJsonAsync(int appId, string apiToken)
        //     => Task.FromResult("{}");

        public Task<KintoneAppMetadata> GetAppMetadataAsync(string domain, string apiToken, int appId) => Task.FromResult(this.Metadata);
        public Task<string> GetFieldsJsonAsync(string domain, string apiToken, int appId) => Task.FromResult("{}");
        public Task<string> GetLayoutJsonAsync(string domain, string apiToken, int appId) => Task.FromResult("{}");
    }

    // -----------------------------
    // GetSchemaAsync のテスト
    // -----------------------------
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
    // SetDomain 未設定時の例外
    // -----------------------------
    [Fact]
    public async Task GetMetadataAsync_Throws_WhenDomainNotSet() {
        var logger = Mock.Of<ILogger<SchemaProvider>>();
        var provider = new SchemaProvider(
            this._mockApi.Object,
            logger
        );

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.GetMetadataAsync("example.cybozu.com", "dummy", 1));
    }

    // -----------------------------
    // CompareAsync のテスト（追加）
    // -----------------------------
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

    [Fact]
    public async Task GetSchemaAsync_WithFakeApi_Works() {
        var fakeApi = new FakeKintoneAppMetadataApi();

        var provider = new SchemaProvider(
            logger: Mock.Of<ILogger<SchemaProvider>>(),
            metadataApi: fakeApi
        );

        provider.SetDomain("example");

        var schema = await provider.GetSchemaAsync("example.cybozu.com", "dummy", 1);

        Assert.Equal(1, schema.AppId);
        Assert.Equal(3, schema.Revision);
        Assert.Single(schema.Fields);
        Assert.Equal("customer", schema.Fields[0].FieldCode);
    }
}

using System.Text.Json;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Backup.Application.DTOs;
using KintoneNetLibrary.Backup.Infrastructure.Services;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Moq;

namespace KintoneNetLibrary.Backup.Tests.Helpers;

/// <summary>
/// BackupService テスト用のフィクスチャ
/// </summary>
internal sealed class BackupFixture : IDisposable {
    internal DirectoryInfo OutputDir { get; }
    internal Mock<IKintoneApi> ApiMock { get; } = new();
    internal Mock<IKintoneApiFactory> ApiFactoryMock { get; } = new();
    internal Mock<IKintoneAppMetadataApi> MetadataApiMock { get; } = new();
    internal Mock<ISchemaProvider> SchemaProviderMock { get; } = new();
    internal Mock<IKintoneAccessFactory> AccessFactoryMock { get; } = new();

    internal BackupFixture() {
        this.OutputDir = Directory.CreateTempSubdirectory("BackupTest_");

        this.ApiFactoryMock
            .Setup(x => x.Create(It.IsAny<KintoneAccessBase>(), It.IsAny<int>(), It.IsAny<JsonSerializerOptions?>()))
            .Returns(this.ApiMock.Object);

        this.AccessFactoryMock
            .Setup(x => x.CreateApiTokenAccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(new ApiTokenAccess("example.cybozu.com", "dummy-token"));

        this.ApiMock
            .Setup(x => x.StreamCursorPagesAsync(It.IsAny<string>(), It.IsAny<IList<string>?>(), It.IsAny<int?>()))
            .Returns(AsyncEnumerableHelper.Empty<Stream>());

        this.ApiMock
            .Setup(x => x.StreamRecordsAsync(It.IsAny<string>(), It.IsAny<IList<string>?>(), It.IsAny<int?>()))
            .Returns(AsyncEnumerableHelper.Empty<JsonElement>());

        this.SchemaProviderMock
            .Setup(x => x.GetMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new KintoneAppMetadata {
                AppId = 1,
                Revision = 1,
                Fields = [],
                Layout = new KintoneLayoutMetadata()
            });

        this.MetadataApiMock
            .Setup(x => x.GetFieldsJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("""{"properties": {}}""");

        this.MetadataApiMock
            .Setup(x => x.GetLayoutJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("""{"layout": []}""");
    }

    internal BackupService CreateService() => new(
        this.SchemaProviderMock.Object,
        this.AccessFactoryMock.Object,
        this.MetadataApiMock.Object,
        this.ApiFactoryMock.Object);

    internal BackupOptions CreateOptions(bool downloadFiles = false, bool overwrite = true) => new() {
        SubDomain = "example",
        AppID = 1,
        ApiToken = "dummy-token",
        OutputPath = this.OutputDir,
        DownloadFiles = downloadFiles,
        Overwrite = overwrite,
    };

    /// <summary>
    /// 指定レコード JSON オブジェクトを含むページストリームを作成します
    /// </summary>
    internal static MemoryStream CreatePageStream(IEnumerable<string> recordJsonObjects) {
        var records = string.Join(",", recordJsonObjects);
        var json = $$"""{"records": [{{records}}]}""";
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
    }

    public void Dispose() {
        if (this.OutputDir.Exists) {
            this.OutputDir.Delete(recursive: true);
        }
    }
}

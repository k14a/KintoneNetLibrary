using System.Text.Json;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Backup.Application.DTOs;
using KintoneNetLibrary.Backup.Domain.Enums;
using KintoneNetLibrary.Backup.Infrastructure.Services;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Moq;

namespace KintoneNetLibrary.Backup.Tests.Helpers;

/// <summary>
/// RestoreService テスト用のフィクスチャ
/// </summary>
internal sealed class RestoreFixture : IDisposable {
    internal DirectoryInfo BackupDir { get; }
    internal Mock<IKintoneApi> ApiMock { get; } = new();
    internal Mock<IKintoneApiFactory> ApiFactoryMock { get; } = new();
    internal Mock<IKintoneAppMetadataApi> MetadataApiMock { get; } = new();
    internal Mock<ISchemaProvider> SchemaProviderMock { get; } = new();
    internal Mock<IKintoneAccessFactory> AccessFactoryMock { get; } = new();

    internal RestoreFixture() {
        this.BackupDir = Directory.CreateTempSubdirectory("RestoreTest_");

        this.ApiFactoryMock
            .Setup(x => x.Create(It.IsAny<KintoneAccessBase>(), It.IsAny<int>(), It.IsAny<JsonSerializerOptions?>()))
            .Returns(this.ApiMock.Object);

        this.AccessFactoryMock
            .Setup(x => x.CreateApiTokenAccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(new ApiTokenAccess("example.cybozu.com", "dummy-token"));

        this.ApiMock
            .Setup(x => x.StreamRecordsAsync(It.IsAny<string>(), It.IsAny<IList<string>?>(), It.IsAny<int?>()))
            .Returns(AsyncEnumerableHelper.Empty<JsonElement>());

        this.ApiMock
            .Setup(x => x.RawDeleteAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        this.ApiMock
            .Setup(x => x.RawCreateAsync(It.IsAny<string>()))
            .ReturnsAsync("""{"ids": [1]}""");
    }

    internal RestoreService CreateService() => new(
        this.SchemaProviderMock.Object,
        this.AccessFactoryMock.Object,
        this.MetadataApiMock.Object,
        this.ApiFactoryMock.Object);

    internal RestoreOptions CreateOptions(
        RestoreMode mode = RestoreMode.FullReplace,
        bool dryRun = false,
        bool force = true) => new() {
        SubDomain = "example",
        AppID = 1,
        ApiToken = "dummy-token",
        BackupRootPath = this.BackupDir,
        Mode = mode,
        DryRun = dryRun,
        Force = force,
    };

    /// <summary>
    /// テスト用バックアップデータを一時ディレクトリに書き込みます
    /// </summary>
    internal void WriteBackupFiles(IEnumerable<string>? partRecords = null) {
        var records = partRecords?.ToList() ?? [
            """{"$id": {"type": "__ID__", "value": "1"}, "Title": {"type": "SINGLE_LINE_TEXT", "value": "テスト"}}"""
        ];

        var manifest = new {
            AppId = 1,
            AppRevision = 1,
            BackupAt = "2026-06-06T00:00:00Z",
            RecordCount = records.Count,
            FileFieldCount = 0,
            FileCount = 0,
            PartFiles = new[] { "part_00001.json" },
            BackupMode = "CursorPages",
            SplitSize = 1000,
            Options = new { }
        };

        File.WriteAllText(
            Path.Combine(this.BackupDir.FullName, "manifest.json"),
            JsonSerializer.Serialize(manifest));

        File.WriteAllText(
            Path.Combine(this.BackupDir.FullName, "fields.json"),
            """{"properties": {}}""");

        var dataDir = Path.Combine(this.BackupDir.FullName, "data");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(Path.Combine(this.BackupDir.FullName, "files"));

        var recordsJson = string.Join(",", records);
        File.WriteAllText(
            Path.Combine(dataDir, "part_00001.json"),
            $$"""{"records": [{{recordsJson}}]}""");
    }

    public void Dispose() {
        if (this.BackupDir.Exists) {
            this.BackupDir.Delete(recursive: true);
        }
    }
}

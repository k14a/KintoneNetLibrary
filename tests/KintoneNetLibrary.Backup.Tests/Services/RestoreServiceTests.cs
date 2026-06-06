using System.Text.Json;
using KintoneNetLibrary.Backup.Domain.Enums;
using KintoneNetLibrary.Backup.Tests.Helpers;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Backup.Tests.Services;

/// <summary>
/// RestoreService のテスト
/// </summary>
public class RestoreServiceTests {
    /// <summary>
    /// DryRun=true の場合、Kintone への書き込みが行われない
    /// </summary>
    [Fact]
    public async Task RunRestoreAsync_WhenDryRun_DoesNotWriteToKintone() {
        using var fixture = new RestoreFixture();
        fixture.WriteBackupFiles();

        var service = fixture.CreateService();
        var result = await service.RunRestoreAsync(
            fixture.CreateOptions(mode: RestoreMode.FullReplace, dryRun: true));

        Assert.True(result.Success);
        fixture.ApiMock.Verify(x => x.RawCreateAsync(It.IsAny<string>()), Times.Never);
        fixture.ApiMock.Verify(x => x.RawDeleteAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// FullReplace モードで正常系: 既存レコード削除 → 新規インサートが実行される
    /// </summary>
    [Fact]
    public async Task RunRestoreAsync_FullReplace_DeletesExistingAndInsertsNew() {
        using var fixture = new RestoreFixture();
        fixture.WriteBackupFiles();

        // 削除対象レコードを 1 件返す
        var existingRecord = JsonDocument.Parse(
            """{"$id": {"type": "__ID__", "value": "10"}}""").RootElement;

        fixture.ApiMock
            .Setup(x => x.StreamRecordsAsync(It.IsAny<string>(), It.IsAny<IList<string>?>(), It.IsAny<int?>()))
            .Returns(AsyncEnumerableHelper.Create([existingRecord]));

        var service = fixture.CreateService();
        var result = await service.RunRestoreAsync(
            fixture.CreateOptions(mode: RestoreMode.FullReplace, force: true));

        Assert.True(result.Success);
        Assert.Equal(1, result.DeletedRecords);
        Assert.Equal(1, result.AddedRecords);
        fixture.ApiMock.Verify(x => x.RawDeleteAsync(It.IsAny<string>()), Times.Once);
        fixture.ApiMock.Verify(x => x.RawCreateAsync(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// manifest.json が存在しない場合、リストアが失敗する
    /// </summary>
    [Fact]
    public async Task RunRestoreAsync_WhenManifestMissing_Fails() {
        using var fixture = new RestoreFixture();
        // バックアップファイルを書き込まない（空ディレクトリ）

        var service = fixture.CreateService();
        var result = await service.RunRestoreAsync(fixture.CreateOptions());

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    /// <summary>
    /// Force=true の場合、スキーマ検証をスキップして成功する
    /// </summary>
    [Fact]
    public async Task RunRestoreAsync_WhenForceTrue_SkipsSchemaValidation() {
        using var fixture = new RestoreFixture();
        fixture.WriteBackupFiles();

        var service = fixture.CreateService();
        var result = await service.RunRestoreAsync(
            fixture.CreateOptions(mode: RestoreMode.FullReplace, force: true));

        Assert.True(result.Success);
        // GetAppMetadataAsync はスキーマ検証でしか呼ばれないため、呼び出し回数は 0
        fixture.MetadataApiMock.Verify(
            x => x.GetAppMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);
    }
}

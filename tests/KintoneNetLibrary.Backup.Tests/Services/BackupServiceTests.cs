using KintoneNetLibrary.Backup.Tests.Helpers;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Backup.Tests.Services;

/// <summary>
/// BackupService のテスト
/// </summary>
public class BackupServiceTests {
    /// <summary>
    /// 正常系: レコードが存在する場合、期待されるファイルが出力される
    /// </summary>
    [Fact]
    public async Task RunBackupAsync_WithRecords_CreatesExpectedFiles() {
        using var fixture = new BackupFixture();

        var pageStream = BackupFixture.CreatePageStream([
            """{"$id": {"type": "__ID__", "value": "1"}}""",
            """{"$id": {"type": "__ID__", "value": "2"}}""",
        ]);

        fixture.ApiMock
            .Setup(x => x.StreamCursorPagesAsync(It.IsAny<string>(), It.IsAny<IList<string>?>(), It.IsAny<int?>()))
            .Returns(AsyncEnumerableHelper.Create<Stream>([pageStream]));

        var service = fixture.CreateService();
        var result = await service.RunBackupAsync(fixture.CreateOptions());

        Assert.True(result.Success);
        Assert.Equal(2, result.RecordCount);
        Assert.Single(result.PartFiles);
        Assert.NotNull(result.BackedUpDirectory);

        var root = result.BackedUpDirectory!.FullName;
        Assert.True(File.Exists(Path.Combine(root, "manifest.json")), "manifest.json が存在しない");
        Assert.True(File.Exists(Path.Combine(root, "fields.json")), "fields.json が存在しない");
        Assert.True(File.Exists(Path.Combine(root, "layout.json")), "layout.json が存在しない");
        Assert.True(File.Exists(Path.Combine(root, "data", "part_00001.json")), "part_00001.json が存在しない");
    }

    /// <summary>
    /// レコードが0件の場合、PartFiles が空になる
    /// </summary>
    [Fact]
    public async Task RunBackupAsync_WithNoRecords_ReturnsEmptyPartFiles() {
        using var fixture = new BackupFixture();
        // デフォルトで StreamCursorPagesAsync は Empty を返す

        var service = fixture.CreateService();
        var result = await service.RunBackupAsync(fixture.CreateOptions());

        Assert.True(result.Success);
        Assert.Equal(0, result.RecordCount);
        Assert.Empty(result.PartFiles);
    }

    /// <summary>
    /// DownloadFiles=false の場合、DownloadFileAsync が呼ばれない
    /// </summary>
    [Fact]
    public async Task RunBackupAsync_WhenDownloadFilesDisabled_DoesNotCallDownloadFileAsync() {
        using var fixture = new BackupFixture();

        var service = fixture.CreateService();
        var result = await service.RunBackupAsync(fixture.CreateOptions(downloadFiles: false));

        Assert.True(result.Success);
        fixture.ApiMock.Verify(x => x.DownloadFileAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Overwrite=false で出力先ディレクトリが既に存在する場合、バックアップが失敗する
    /// </summary>
    [Fact]
    public async Task RunBackupAsync_WhenDirectoryExistsAndOverwriteDisabled_Fails() {
        using var fixture = new BackupFixture();

        // 事前にバックアップ先と同じ AppId/タイムスタンプ構造のディレクトリを作成するため
        // Overwrite=false で初回実行してディレクトリを作成し、同秒に再実行する
        var options = fixture.CreateOptions(overwrite: false);
        var first = await fixture.CreateService().RunBackupAsync(options);
        Assert.True(first.Success);

        // 同じ秒内に再実行すると既存ディレクトリと衝突する
        var second = await fixture.CreateService().RunBackupAsync(
            fixture.CreateOptions(overwrite: false));
        Assert.False(second.Success);
    }
}

using System.IO.Compression;
using System.Text;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Tests.Helpers;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

/// <summary>
/// KintoneApiのファイル関連機能（アップロード・ダウンロード）に関するテストクラス。
/// </summary>
public class KintoneApiFileTests {
    private const string FilePath = "Files/test1.txt";

    /// <summary>
    /// テキストファイルのアップロードとダウンロードが正常に動作することを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadAndDownloadPdfFileWorksCorrectly() {
        var api = KintoneTestHelper.CreateApi();
        var uuid = Guid.NewGuid().ToString();
        var pdfPath = KintoneFileTestHelper.CreateSamplePdfFile();

        using (var stream = File.OpenRead(pdfPath)) {
            var fileKey = await api.UploadFileAsync(stream, Path.GetFileName(pdfPath));
            Assert.False(string.IsNullOrWhiteSpace(fileKey));

            var model = new BookModel {
                Title = "PDFファイルテスト",
                Uuid = uuid,
                Files = [new KintoneFile { FileKey = fileKey }]
            };

            var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
            Assert.NotNull(created);
            var createdRecord = created!.First();

            try {
                var query = $"UUID = \"{uuid}\"";
                var found = await api.FindByQueryAsync<BookModel>(query);
                var results = KintoneResponseParser.ParseRecords<BookModel>(found);
                var match = results.FirstOrDefault(b => b.ID == createdRecord.ID);

                Assert.NotNull(match);
                Assert.Single(match!.Files);

                var uploadedFile = match.Files.First();
                var bytes = await api.DownloadFileAsync(uploadedFile.FileKey);

                // 内容確認（バイナリ比較）
                var originalBytes = await File.ReadAllBytesAsync(pdfPath);
                var originalHash = KintoneFileTestHelper.ComputeSha256Hash(originalBytes);
                var downloadedHash = KintoneFileTestHelper.ComputeSha256Hash(bytes);

                Assert.Equal(originalHash, downloadedHash);
            } finally {
                await KintoneTestHelper.DeleteRecordsInChunksAsync(api, [createdRecord]);
            }
        }
    }

    /// <summary>
    /// CSVファイルのアップロードとダウンロードが正常に動作することを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadAndDownloadCsvFileWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        var filePath = KintoneFileTestHelper.CreateSampleCsvFile();
        var uuid = Guid.NewGuid().ToString();

        using var stream = File.OpenRead(filePath);
        var fileKey = await api.UploadFileAsync(stream, Path.GetFileName(filePath));
        Assert.False(string.IsNullOrWhiteSpace(fileKey));

        var model = new BookModel {
            Title = "CSVファイルテスト",
            Uuid = uuid,
            Files = [new KintoneFile { FileKey = fileKey }]
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        Assert.NotNull(created);
        var createdRecord = created!.First();

        try {
            // Act
            var query = $"UUID = \"{uuid}\"";
            var found = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(found);
            var match = results.FirstOrDefault(b => b.ID == createdRecord.ID);

            // Assert
            Assert.NotNull(match);
            Assert.NotNull(match!.Files);
            Assert.Single(match.Files);

            var uploadedFile = match.Files.First();
            var bytes = await api.DownloadFileAsync(uploadedFile.FileKey);
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            var originalHash = KintoneFileTestHelper.ComputeSha256Hash(File.ReadAllBytes(filePath));
            var downloadedHash = KintoneFileTestHelper.ComputeSha256Hash(bytes);
            Assert.Equal(originalHash, downloadedHash);
        } finally {
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, [createdRecord]);
        }
    }

    /// <summary>
    /// PNGファイルのアップロードとダウンロードが正常に動作することを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadAndDownloadPngFileWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        var filePath = KintoneFileTestHelper.CreateSamplePngFile();
        var uuid = Guid.NewGuid().ToString();

        using var stream = File.OpenRead(filePath);
        var fileKey = await api.UploadFileAsync(stream, Path.GetFileName(filePath));
        Assert.False(string.IsNullOrWhiteSpace(fileKey));

        var model = new BookModel {
            Title = "PNGファイルテスト",
            Uuid = uuid,
            Files = [new KintoneFile { FileKey = fileKey }]
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        Assert.NotNull(created);
        var createdRecord = created!.First();

        try {
            // Act
            var query = $"UUID = \"{uuid}\"";
            var found = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(found);
            var match = results.FirstOrDefault(b => b.ID == createdRecord.ID);

            // Assert
            Assert.NotNull(match);
            Assert.NotNull(match!.Files);
            Assert.Single(match.Files);

            var uploadedFile = match.Files.First();
            var bytes = await api.DownloadFileAsync(uploadedFile.FileKey);
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            var originalHash = KintoneFileTestHelper.ComputeSha256Hash(File.ReadAllBytes(filePath));
            var downloadedHash = KintoneFileTestHelper.ComputeSha256Hash(bytes);
            Assert.Equal(originalHash, downloadedHash);
        } finally {
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, [createdRecord]);
        }
    }

    /// <summary>
    /// ZIPファイルのアップロードとダウンロードが正常に動作することを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadAndDownloadZipFileWithStreamWorksCorrectly() {
        var api = KintoneTestHelper.CreateApi();
        var uuid = Guid.NewGuid().ToString();

        var filePath = KintoneFileTestHelper.CreateSampleTxtFile();

        await using var stream = File.OpenRead(filePath);
        var fileKey = await api.UploadFileAsync(stream, Path.GetFileName(filePath));
        Assert.False(string.IsNullOrWhiteSpace(fileKey));

        var model = new BookModel {
            Title = "ファイルテスト",
            Uuid = uuid,
            Files = [new KintoneFile { FileKey = fileKey }]
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        var createdRecord = created!.First();

        try {
            var query = $"UUID = \"{uuid}\"";
            var found = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(found);
            var match = results.FirstOrDefault(b => b.ID == createdRecord.ID);

            Assert.NotNull(match);
            Assert.NotNull(match!.Files);
            Assert.Single(match.Files);

            var uploadedFile = match.Files.First();
            Assert.False(string.IsNullOrEmpty(uploadedFile.FileKey));

            // ダウンロードして内容を確認
            var bytes = await api.DownloadFileAsync(uploadedFile.FileKey);
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            var originalText = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var downloadedText = Encoding.UTF8.GetString(bytes);
            Assert.Equal(originalText, downloadedText);

            var originalHash = KintoneFileTestHelper.ComputeSha256Hash(originalText);
            var downloadedHash = KintoneFileTestHelper.ComputeSha256Hash(downloadedText);
            Assert.Equal(originalHash, downloadedHash);
        } finally {
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, [createdRecord]);
        }
    }

    /// <summary>
    /// Excelファイルのアップロードとダウンロードが正常に動作することを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadAndDownloadExcelFileWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        var filePath = KintoneFileTestHelper.CreateSampleExcelFile();
        var uuid = Guid.NewGuid().ToString();

        await using var stream = File.OpenRead(filePath);
        var fileKey = await api.UploadFileAsync(stream, Path.GetFileName(filePath));
        Assert.False(string.IsNullOrWhiteSpace(fileKey));

        var model = new BookModel {
            Title = "Excelファイルテスト",
            Uuid = uuid,
            Files = [new KintoneFile { FileKey = fileKey }]
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        Assert.NotNull(created);
        var createdRecord = created!.First();

        try {
            // Act
            var query = $"UUID = \"{uuid}\"";
            var found = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(found);
            var match = results.FirstOrDefault(b => b.ID == createdRecord.ID);

            // Assert
            Assert.NotNull(match);
            Assert.NotNull(match!.Files);
            Assert.Single(match.Files);

            var uploadedFile = match.Files.First();
            var downloadedBytes = await api.DownloadFileAsync(uploadedFile.FileKey);
            Assert.NotNull(downloadedBytes);

            var originalBytes = await File.ReadAllBytesAsync(filePath);
            var originalHash = KintoneFileTestHelper.ComputeSha256Hash(originalBytes);
            var downloadedHash = KintoneFileTestHelper.ComputeSha256Hash(downloadedBytes);
            Assert.Equal(originalHash, downloadedHash);
        } finally {
            // Cleanup
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, [createdRecord]);
        }
    }

    /// <summary>
    /// FileInfoを使用したファイルのアップロードとダウンロードが正常に動作することを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadAndDownloadFileWithFileInfoWorksCorrectly() {
        var api = KintoneTestHelper.CreateApi();
        var uuid = Guid.NewGuid().ToString();

        var originalFile = KintoneFileTestHelper.CreateSampleTxtFile(); // returns FileInfo
        var fileInfo = new FileInfo(originalFile);

        // FileInfoベースのアップロード
        var fileKey = await api.UploadFileAsync(fileInfo);
        Assert.False(string.IsNullOrWhiteSpace(fileKey));

        var model = new BookModel {
            Title = "FileInfoテスト",
            Uuid = uuid,
            Files = [new KintoneFile { FileKey = fileKey }]
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        var createdRecord = created!.First();

        try {
            var query = $"UUID = \"{uuid}\"";
            var found = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(found);
            var match = results.FirstOrDefault(b => b.ID == createdRecord.ID);

            Assert.NotNull(match);
            Assert.NotNull(match!.Files);
            Assert.Single(match.Files);

            var uploadedFile = match.Files.First();
            Assert.False(string.IsNullOrEmpty(uploadedFile.FileKey));

            // FileInfoベースのダウンロード
            var destinationPath = Path.Combine(Path.GetTempPath(), $"downloaded_{uuid}.txt");
            var destinationFile = new FileInfo(destinationPath);

            await api.DownloadFileAsync(uploadedFile.FileKey, destinationFile);
            Assert.True(destinationFile.Exists);

            // 内容比較
            var originalText = await File.ReadAllTextAsync(fileInfo.FullName, Encoding.UTF8);
            var downloadedText = await File.ReadAllTextAsync(destinationFile.FullName, Encoding.UTF8);
            Assert.Equal(originalText, downloadedText);

            var originalHash = KintoneFileTestHelper.ComputeSha256Hash(originalText);
            var downloadedHash = KintoneFileTestHelper.ComputeSha256Hash(downloadedText);
            Assert.Equal(originalHash, downloadedHash);
        } finally {
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, [createdRecord]);
        }
    }

    /// <summary>
    /// ZIPファイルのアップロードとダウンロードが正常に動作することを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadAndDownloadZipFileWorksCorrectly() {
        var api = KintoneTestHelper.CreateApi();
        var uuid = Guid.NewGuid().ToString();

        // ZIPファイルの作成（サンプルファイルを圧縮）
        var sampleTextPath = KintoneFileTestHelper.CreateSampleTxtFile(); // returns string path
        var zipFilePath = Path.Combine(Path.GetTempPath(), $"sample_{uuid}.zip");
        using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create)) {
            zip.CreateEntryFromFile(sampleTextPath, Path.GetFileName(sampleTextPath));
        }

        var zipFileInfo = new FileInfo(zipFilePath);
        Assert.True(zipFileInfo.Exists);

        // アップロード
        var fileKey = await api.UploadFileAsync(zipFileInfo);
        Assert.False(string.IsNullOrWhiteSpace(fileKey));

        var model = new BookModel {
            Title = "ZIPファイルテスト",
            Uuid = uuid,
            Files = [new KintoneFile { FileKey = fileKey }]
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        var createdRecord = created!.First();

        try {
            var query = $"UUID = \"{uuid}\"";
            var found = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(found);
            var match = results.FirstOrDefault(b => b.ID == createdRecord.ID);

            Assert.NotNull(match);
            Assert.NotNull(match!.Files);
            Assert.Single(match.Files);

            var uploadedFile = match.Files.First();
            Assert.False(string.IsNullOrEmpty(uploadedFile.FileKey));

            // ダウンロード
            var destinationPath = Path.Combine(Path.GetTempPath(), $"downloaded_{uuid}.zip");
            var destinationFile = new FileInfo(destinationPath);

            await api.DownloadFileAsync(uploadedFile.FileKey, destinationFile);
            Assert.True(destinationFile.Exists);

            // ハッシュ比較（バイナリ一致）
            var originalBytes = await File.ReadAllBytesAsync(zipFileInfo.FullName);
            var downloadedBytes = await File.ReadAllBytesAsync(destinationFile.FullName);
            Assert.Equal(originalBytes.Length, downloadedBytes.Length);

            var originalHash = KintoneFileTestHelper.ComputeSha256Hash(originalBytes);
            var downloadedHash = KintoneFileTestHelper.ComputeSha256Hash(downloadedBytes);
            Assert.Equal(originalHash, downloadedHash);
        } finally {
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, [createdRecord]);
        }
    }
}

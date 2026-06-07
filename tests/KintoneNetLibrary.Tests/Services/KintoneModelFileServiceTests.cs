using System.Text;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

/// <summary>
/// KintoneModelFileServiceの単体テストクラス。
/// </summary>
public class KintoneModelFileServiceTests {
    private readonly Mock<IKintoneRepository> _mockRepo = new();
    private readonly ILogger<KintoneModelFileService<ValidFileModel>> _logger = Mock.Of<ILogger<KintoneModelFileService<ValidFileModel>>>();
    private readonly ILogger<KintoneModelFileService<TestModel>> _logger2 = Mock.Of<ILogger<KintoneModelFileService<TestModel>>>();
    private readonly Mock<ILogger<KintoneModelFileService<ValidFileModel>>> _mockLogger = new();
    private KintoneModelFileService<ValidFileModel> CreateService() => new(this._mockRepo.Object, this._mockLogger.Object);
    /// <summary>
    /// テスト用の既存ファイルを作成するユーティリティメソッド。
    /// </summary>
    /// <param name="content">ファイルに書き込む内容</param>
    /// <returns>作成されたファイルのFileInfoオブジェクト</returns>
    private static FileInfo CreateExistingFile(string content) {
        var path = Path.Combine(Path.GetTempPath(), $"existing_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }

    #region <<Test methods>>
    #region <<Upload methods>>
    /// <summary>
    /// UploadFileAsyncがFileInfoをアップロードし、KintoneFileをモデルに設定することをテストする。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncSetsKintoneFileOnModel() {
        // Arrange
        var fileName = $"test_{Guid.NewGuid()}.txt";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        await File.WriteAllTextAsync(filePath, "これはテストファイルです。");

        var fileInfo = new FileInfo(filePath);
        var model = new SampleFileModel {
            LocalFile = fileInfo
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.UploadFileAsync(model, fileInfo))
            .ReturnsAsync("mocked-file-key");

        var logger = new Mock<ILogger<KintoneModelFileService<SampleFileModel>>>();
        var service = new KintoneModelFileService<SampleFileModel>(mockRepo.Object, logger.Object);

        // Act
        var result = await service.UploadFileAsync(model);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("mocked-file-key", result.FileKey);
        Assert.Equal(fileInfo.Name, result.Name);
        Assert.Equal(fileInfo.Length, result.Size);
        Assert.Equal("text/plain", result.ContentType); // MIME推定

        Assert.NotNull(model.UploadedFile);
        Assert.Equal(result.FileKey, model.UploadedFile!.FileKey);

        if (fileInfo.Exists) { fileInfo.Delete(); }
    }

    /// <summary>
    /// UploadFilesAsyncが複数のFileInfoをアップロードし、対応するKintoneFileリストをモデルに設定することをテストする。
    /// </summary>
    [Fact]
    public async Task UploadFilesAsyncSetsMultipleKintoneFilesOnModel() {
        // Arrange
        var tempDir = Path.GetTempPath();
        var filePaths = new[] {
            Path.Combine(tempDir, $"file1_{Guid.NewGuid()}.txt"),
            Path.Combine(tempDir, $"file2_{Guid.NewGuid()}.txt")
        };

        foreach (var path in filePaths) {
            await File.WriteAllTextAsync(path, $"内容: {Path.GetFileName(path)}");
        }

        var fileInfos = filePaths.Select(p => new FileInfo(p)).ToList();
        var model = new MultiFileModel {
            LocalFiles = fileInfos
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.UploadFileAsync(model, It.IsAny<FileInfo>()))
            .ReturnsAsync((MultiFileModel _, FileInfo f) => $"key-{f.Name}");

        var logger = new Mock<ILogger<KintoneModelFileService<MultiFileModel>>>();
        var service = new KintoneModelFileService<MultiFileModel>(mockRepo.Object, logger.Object);

        // Act
        var result = await service.UploadFilesAsync(model);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, kf => Assert.StartsWith("key-", kf.FileKey));
        Assert.Equal(result, model.UploadedFiles);

        // Cleanup
        foreach (var fi in fileInfos) {
            if (fi.Exists) { fi.Delete(); }
        }
    }

    /// <summary>
    /// UploadFileAsyncでnullモデルを渡すとArgumentNullExceptionが発行されることをテストする。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsArgumentNullExceptionWhenModelIsNull() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadFileAsync(null!));
    }

    /// <summary>
    /// UploadFileAsyncでFileInfo/KintoneFileプロパティがないモデルを渡すとInvalidOperationExceptionが発行されることをテストする。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsInvalidOperationExceptionWhenModelLacksRequiredProps() {
        var logger = Mock.Of<ILogger<KintoneModelFileService<InvalidFileModelNoProps>>>();
        var service = new KintoneModelFileService<InvalidFileModelNoProps>(this._mockRepo.Object, logger);
        var model = new InvalidFileModelNoProps { Dummy = "test" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadFileAsync(model));
    }

    /// <summary>
    /// UploadFileAsyncで存在しないFileInfoを渡すとFileNotFoundExceptionが発行されることをテストする。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsFileNotFoundExceptionWhenFileDoesNotExist() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel {
            LocalFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"notfound_{Guid.NewGuid()}.txt")),
            UploadedFile = new KintoneFile()
        };

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.UploadFileAsync(model));
    }

    /// <summary>
    /// UploadFileAsyncでnullモデルを渡すとArgumentNullExceptionが発行されることをテストする（ファイル引数付きバージョン）。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsArgumentNullExceptionWhenModelIsNull2() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var dummyFile = new FileInfo("dummy.txt");

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadFileAsync(null!, dummyFile));
    }

    /// <summary>
    /// UploadFileAsyncでnullファイルを渡すとArgumentNullExceptionが発行されることをテストする。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsArgumentNullExceptionWhenFileIsNull() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadFileAsync(model, null!));
    }

    /// <summary>
    /// UploadFileAsyncで存在しないFileInfoを渡すとFileNotFoundExceptionが発行されることをテストする（ファイル引数付きバージョン）。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsFileNotFoundExceptionWhenFileDoesNotExist2() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel();
        var nonexistentFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"nofile_{Guid.NewGuid()}.txt"));

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.UploadFileAsync(model, nonexistentFile));
    }

    /// <summary>
    /// UploadFileAsyncで正常にアップロードされると、KintoneFileが返ることをテストする。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncReturnsKintoneFileWhenSuccessful() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel();
        var tempFilePath = Path.GetTempFileName();
        var fileInfo = new FileInfo(tempFilePath);
        await File.WriteAllTextAsync(tempFilePath, "dummy content");

        this._mockRepo.Setup(r => r.UploadFileAsync(model, fileInfo)).ReturnsAsync("mocked-file-key");

        var result = await service.UploadFileAsync(model, fileInfo);

        Assert.NotNull(result);
        Assert.Equal("mocked-file-key", result.FileKey);
        Assert.Equal(fileInfo.Name, result.Name);
        Assert.Equal(fileInfo.Length, result.Size);
        Assert.Equal(MimeTypes.GetMimeType(fileInfo.Name) ?? "application/octet-stream", result.ContentType);
    }

    /// <summary>
    /// MapUploadedFilesToModelAsyncでmodelがnullならArgumentNullExceptionが発行されることをテストする。
    /// </summary>
    [Fact]
    public async Task MapUploadedFilesToModelAsyncThrowsArgumentNullExceptionWhenModelIsNull() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var files = new List<FileInfo> { new FileInfo("dummy.txt") };

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.MapUploadedFilesToModelAsync(null!, files));
    }

    /// <summary>
    /// MapUploadedFilesToModelAsyncでfilesがnullならArgumentNullExceptionが発行されることをテストする。
    /// </summary>
    [Fact]
    public async Task MapUploadedFilesToModelAsyncThrowsArgumentNullExceptionWhenFilesIsNull() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var model = new TestModel();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.MapUploadedFilesToModelAsync(model, null!));
    }

    /// <summary>
    /// MapUploadedFilesToModelAsyncでKintoneFileプロパティに一致するFileInfoがある場合、FileKeyが設定されることをテストする。
    /// </summary>
    [Fact]
    public async Task MapUploadedFilesToModelAsyncSetsFileKeyForMatchingSingleFile() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var file = new FileInfo("match.txt");
        var model = new TestModel {
            SingleFile = new KintoneFile { Name = "match.txt", FileKey = null! }
        };

        await service.MapUploadedFilesToModelAsync(model, new[] { file });

        Assert.Equal("[UPLOADED]", model.SingleFile?.FileKey);
    }

    /// <summary>
    /// MapUploadedFilesToModelAsyncでKintoneFileリストに一致するFileInfoがある場合、各FileKeyが設定されることをテストする。
    /// </summary>
    [Fact]
    public async Task MapUploadedFilesToModelAsyncSetsFileKeyForMatchingListFiles() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var files = new[] {
            new FileInfo("a.txt"),
            new FileInfo("b.txt")
        };

        var model = new TestModel {
            FileList = [
                new KintoneFile { Name = "a.txt", FileKey = null! },
                new KintoneFile { Name = "b.txt", FileKey = null! },
                new KintoneFile { Name = "c.txt", FileKey = "already-set" }
            ]
        };

        await service.MapUploadedFilesToModelAsync(model, files);

        Assert.Equal("[UPLOADED]", model.FileList?[0].FileKey);
        Assert.Equal("[UPLOADED]", model.FileList?[1].FileKey);
        Assert.Equal("already-set", model.FileList?[2].FileKey); // 変更されない
    }

    /// <summary>
    /// MapUploadedFilesToModelAsyncで一致するFileInfoがない場合、FileKeyは変更されないことをテストする。
    /// </summary>
    [Fact]
    public async Task MapUploadedFilesToModelAsyncDoesNotSetFileKeyWhenNoMatch() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var files = new[] { new FileInfo("x.txt") };
        var model = new TestModel {
            SingleFile = new KintoneFile { Name = "notfound.txt", FileKey = null! },
            FileList = [
                new KintoneFile { Name = "notfound1.txt", FileKey = null! }
            ]
        };

        await service.MapUploadedFilesToModelAsync(model, files);

        Assert.Null(model.SingleFile?.FileKey);
        Assert.Null(model.FileList?[0].FileKey);
    }
    #endregion
    #region <<Download methods>>
    /// <summary>
    /// DownloadFileAsyncでKintoneFileプロパティが存在しないモデルを渡すとInvalidOperationExceptionが発行されることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncThrowsInvalidOperationExceptionWhenNoKintoneFileProp() {
        var service = new KintoneModelFileService<NoFileModel>(Mock.Of<IKintoneRepository>(), Mock.Of<ILogger<KintoneModelFileService<NoFileModel>>>());
        var model = new NoFileModel();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadFileAsync(model));
    }

    /// <summary>
    /// DownloadFileAsyncでKintoneFileがnullまたはFileKeyが空ならArgumentExceptionが発行されることをテストする。
    /// </summary>
    /// <param name="fileKey">KintoneFileのFileKey</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DownloadFileAsyncThrowsArgumentExceptionWhenKintoneFileIsInvalid(string? fileKey) {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel {
            UploadedFile = fileKey == null ? null : new KintoneFile { FileKey = fileKey, Name = "dummy.txt" }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileAsync(model));
    }

    /// <summary>
    /// DownloadFileAsyncで正常なKintoneFileがある場合、ファイルがダウンロードされて保存されることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncSavesFileSuccessfully() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel {
            UploadedFile = new KintoneFile {
                FileKey = "valid-key",
                Name = $"test_{Guid.NewGuid()}.txt"
            }
        };

        var expectedContent = Encoding.UTF8.GetBytes("これはダウンロードされたファイルです。");
        this._mockRepo
            .Setup(r => r.DownloadFileAsync(model, model.UploadedFile!.Name))
            .ReturnsAsync(expectedContent);

        var targetDir = Path.GetTempPath();
        var resultFile = await service.DownloadFileAsync(model, targetDir);

        Assert.True(resultFile.Exists);
        var actualContent = await File.ReadAllTextAsync(resultFile.FullName, Encoding.UTF8);
        Assert.Equal("これはダウンロードされたファイルです。", actualContent);

        // 後始末
        resultFile.Delete();
    }

    /// <summary>
    /// DownloadFilesAsyncでFileKeyが未設定のファイルはスキップされることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFilesAsyncSkipsFilesWithEmptyFileKey() {
        this._mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var model = new ValidFileModel();
        var files = new[] {
            new KintoneFile { FileKey = null!, Name = "skip1.txt" },
            new KintoneFile { FileKey = "", Name = "skip2.txt" }
        };

        var service = this.CreateService();
        var result = await service.DownloadFilesAsync(model, files);

        Assert.Empty(result);
        this._mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("skip1.txt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
        this._mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("skip2.txt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
    }

    /// <summary>
    /// DownloadFilesAsyncで正常なファイルはすべて保存されることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFilesAsyncSavesValidFiles() {
        var model = new ValidFileModel();
        var file1 = new KintoneFile { FileKey = "key1", Name = $"file1_{Guid.NewGuid()}.txt" };
        var file2 = new KintoneFile { FileKey = "key2", Name = $"file2_{Guid.NewGuid()}.txt" };
        var files = new[] { file1, file2 };

        var content = Encoding.UTF8.GetBytes("test content");
        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file1.Name)).ReturnsAsync(content);
        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file2.Name)).ReturnsAsync(content);

        var service = this.CreateService();
        var result = (await service.DownloadFilesAsync(model, files)).ToList();

        Assert.Equal(2, result.Count);
        foreach (var file in result) {
            Assert.True(file.Exists);
            Assert.Equal("test content", await File.ReadAllTextAsync(file.FullName));
            file.Delete(); // 後始末
        }
    }

    /// <summary>
    /// DownloadFilesAsyncで一部ファイルのダウンロードに失敗しても他は保存されることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFilesAsyncContinuesOnDownloadError() {
        this._mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var model = new ValidFileModel();
        var file1 = new KintoneFile { FileKey = "key1", Name = $"ok_{Guid.NewGuid()}.txt" };
        var file2 = new KintoneFile { FileKey = "key2", Name = $"fail_{Guid.NewGuid()}.txt" };
        var files = new[] { file1, file2 };

        var content = Encoding.UTF8.GetBytes("ok content");
        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file1.Name)).ReturnsAsync(content);
        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file2.Name)).ThrowsAsync(new IOException("Download failed"));

        var service = this.CreateService();
        var result = (await service.DownloadFilesAsync(model, files)).ToList();

        Assert.Single(result);
        Assert.Contains("ok_", result[0].Name);
        Assert.True(result[0].Exists);
        result[0].Delete();

        this._mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(file2.Name)),
                It.IsAny<IOException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
    }

    /// <summary>
    /// DownloadFileAsyncでKintoneFileがnullまたはFileKeyが空ならArgumentExceptionが発行されることをテストする。
    /// </summary>
    /// <param name="fileKey">テスト対象のファイルキー</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DownloadFileAsyncThrowsArgumentExceptionWhenFileKeyIsInvalid(string? fileKey) {
        var model = new ValidFileModel();
        var file = new KintoneFile { FileKey = fileKey!, Name = "invalid.txt" };

        var service = this.CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileAsync(model, file));

        Assert.Equal("file", ex.ParamName);
        Assert.Contains("FileKeyが未設定", ex.Message);
    }

    /// <summary>
    /// DownloadFileAsyncで正常なKintoneFileがある場合、ファイルがダウンロードされて保存されることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncSavesValidFileSuccessfully() {
        var model = new ValidFileModel();
        var file = new KintoneFile {
            FileKey = "valid-key",
            Name = $"valid_{Guid.NewGuid()}.txt"
        };

        var expectedContent = Encoding.UTF8.GetBytes("これは正常なファイルです。");
        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file.Name)).ReturnsAsync(expectedContent);

        var service = this.CreateService();
        var result = await service.DownloadFileAsync(model, file);

        Assert.True(result.Exists);
        var actualContent = await File.ReadAllTextAsync(result.FullName);
        Assert.Equal("これは正常なファイルです。", actualContent);

        result.Delete(); // 後始末
    }

    /// <summary>
    /// DownloadFileToPathAsyncでFileKeyが未設定のファイルを渡すとArgumentExceptionが発行されることをテストする。
    /// </summary>
    /// <param name="fileKey">テスト対象のファイルキー</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DownloadFileToPathAsyncThrowsArgumentExceptionWhenFileKeyIsInvalid(string? fileKey) {
        var model = new ValidFileModel();
        var file = new KintoneFile { FileKey = fileKey!, Name = "invalid.txt" };
        var savePath = Path.Combine(Path.GetTempPath(), "dummy.txt");

        var service = this.CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileToPathAsync(model, file, savePath));

        Assert.Equal("file", ex.ParamName);
        Assert.Contains("FileKeyが未設定", ex.Message);
    }

    /// <summary>
    /// DownloadFileToPathAsyncで正常なKintoneFileがある場合、ファイルがダウンロードされて指定されたパスに保存されることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFileToPathAsyncSavesFileToSpecifiedPath() {
        var model = new ValidFileModel();
        var file = new KintoneFile {
            FileKey = "valid-key",
            Name = "test.txt"
        };

        var content = Encoding.UTF8.GetBytes("これは保存されるファイルです。");
        var tempPath = Path.Combine(Path.GetTempPath(), $"saved_{Guid.NewGuid()}.txt");

        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file.Name)).ReturnsAsync(content);

        var service = this.CreateService();
        var result = await service.DownloadFileToPathAsync(model, file, tempPath);

        Assert.Equal(tempPath, result.FullName);
        Assert.True(result.Exists);
        var actual = await File.ReadAllTextAsync(result.FullName);
        Assert.Equal("これは保存されるファイルです。", actual);

        result.Delete(); // 後始末
    }

    /// <summary>
    /// DownloadFileToPathAsyncでoverwrite=false, throwIfExists=trueのとき、保存先に既にファイルが存在する場合はIOExceptionが発生することをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFileToPathAsyncThrowsWhenFileExistsAndThrowIfExistsTrue() {
        var model = new ValidFileModel();
        var file = new KintoneFile { FileKey = "key", Name = "conflict.txt" };
        var existing = CreateExistingFile("既存の内容");

        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file.Name))
                 .ReturnsAsync(Encoding.UTF8.GetBytes("新しい内容"));

        var service = this.CreateService();

        var ex = await Assert.ThrowsAsync<IOException>(() =>
            service.DownloadFileToPathAsync(model, file, existing.FullName, overwrite: false, throwIfExists: true));

        Assert.Contains("保存先に既にファイルが存在します", ex.Message);
        existing.Delete();
    }

    /// <summary>
    /// DownloadFileToPathAsyncでoverwrite=false, throwIfExists=falseのとき、保存先に既にファイルが存在する場合は既存ファイルが保持され、新しいファイルが別名で保存されることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFileToPathAsyncSavesWithNewNameWhenFileExistsAndFlagsAreFalse() {
        var model = new ValidFileModel();
        var file = new KintoneFile { FileKey = "key", Name = "skip.txt" };
        var existing = CreateExistingFile("保持される内容");

        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file.Name))
                 .ReturnsAsync(Encoding.UTF8.GetBytes("新しい内容"));

        var service = this.CreateService();
        var result = await service.DownloadFileToPathAsync(model, file, existing.FullName, overwrite: false, throwIfExists: false);

        // 既存ファイルは保持されている
        var originalContent = await File.ReadAllTextAsync(existing.FullName);
        Assert.Equal("保持される内容", originalContent);

        // 新しいファイルは別名で保存されている
        Assert.NotEqual(existing.FullName, result.FullName);
        var newContent = await File.ReadAllTextAsync(result.FullName);
        Assert.Equal("新しい内容", newContent);

        // 後始末
        existing.Delete();
        File.Delete(result.FullName);
    }

    /// <summary>
    /// DownloadFileToPathAsyncでoverwrite=trueのとき、保存先に既にファイルが存在する場合は既存ファイルが上書きされることをテストする。
    /// </summary>
    [Fact]
    public async Task DownloadFileToPathAsyncOverwritesFileWhenOverwriteIsTrue() {
        var model = new ValidFileModel();
        var file = new KintoneFile { FileKey = "key", Name = "overwrite.txt" };
        var existing = CreateExistingFile("古い内容");

        this._mockRepo.Setup(r => r.DownloadFileAsync(model, file.Name))
                 .ReturnsAsync(Encoding.UTF8.GetBytes("新しい内容"));

        var service = this.CreateService();
        var result = await service.DownloadFileToPathAsync(model, file, existing.FullName, overwrite: true, throwIfExists: false);

        var actual = await File.ReadAllTextAsync(result.FullName);
        Assert.Equal("新しい内容", actual);
        existing.Delete();
    }
    #endregion
    #endregion
}

/// <summary>
/// テスト用のモデルクラス。FileInfoとKintoneFileの両方を持ち、アップロード後にKintoneFileが設定されることを想定している。
/// </summary>
public class SampleFileModel : KintoneModelBase<SampleFileModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID { get; init; } = 6666;

    public FileInfo? LocalFile { get; set; }
    public KintoneFile? UploadedFile { get; set; }
}

/// <summary>
/// テスト用のモデルクラス。複数のFileInfoとKintoneFileリストを持ち、アップロード後にKintoneFileリストが設定されることを想定している。
/// </summary>
public class MultiFileModel : KintoneModelBase<MultiFileModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID { get; init; } = 6667;

    public List<FileInfo>? LocalFiles { get; set; }
    public List<KintoneFile>? UploadedFiles { get; set; }
}

/// <summary>
/// テスト用のモデルクラス。FileInfoとKintoneFileの両方を持ち、アップロード後にKintoneFileが設定されることを想定している。
/// </summary>
public class ValidFileModel : KintoneModelBase<ValidFileModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID { get; init; } = 6668;

    public FileInfo? LocalFile { get; set; }
    public KintoneFile? UploadedFile { get; set; }
}

/// <summary>
/// テスト用のモデルクラス。FileInfoやKintoneFileのプロパティがなく、これらが必要な操作を行うと例外が発生することを想定している。
/// </summary>
public class InvalidFileModelNoProps : KintoneModelBase<InvalidFileModelNoProps> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID { get; init; } = 6669;

    public string? Dummy { get; set; }
}

/// <summary>
/// テスト用のモデルクラス。KintoneFileプロパティが1つとリストが1つあるパターンで、MapUploadedFilesToModelAsyncのマッピングロジックをテストするために使用する。
/// </summary>
public class TestModel : KintoneModelBase<TestModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID { get; init; } = 6670;

    public KintoneFile? SingleFile { get; set; }
    public List<KintoneFile>? FileList { get; set; }
}

/// <summary>
/// テスト用のモデルクラス。KintoneFileプロパティがないパターンで、DownloadFileAsyncで例外が発生することをテストするために使用する。
/// </summary>
public class NoFileModel : KintoneModelBase<NoFileModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID { get; init; } = 6671;

    public string? Dummy { get; set; }
}

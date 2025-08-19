using System.Text;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

public class KintoneModelFileServiceTests {
    private readonly Mock<IKintoneRepository> _mockRepo = new();
    private readonly ILogger<KintoneModelFileService<ValidFileModel>> _logger = Mock.Of<ILogger<KintoneModelFileService<ValidFileModel>>>();
    private readonly ILogger<KintoneModelFileService<TestModel>> _logger2 = Mock.Of<ILogger<KintoneModelFileService<TestModel>>>();
    private readonly Mock<ILogger<KintoneModelFileService<ValidFileModel>>> _mockLogger = new();
    private KintoneModelFileService<ValidFileModel> CreateService() => new(this._mockRepo.Object, this._mockLogger.Object);
    private static FileInfo CreateExistingFile(string content) {
        var path = Path.Combine(Path.GetTempPath(), $"existing_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }

    #region <<Test methods>>
    #region <<Upload methods>>
    [Fact(DisplayName = "UploadFileAsyncがFileInfoをアップロードし、KintoneFileをモデルに設定すること")]
    public async Task UploadFileAsync_SetsKintoneFileOnModel() {
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
    [Fact(DisplayName = "UploadFilesAsyncが複数FileInfoをアップロードし、KintoneFileリストをモデルに設定する")]
    public async Task UploadFilesAsync_SetsMultipleKintoneFilesOnModel() {
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
    [Fact(DisplayName = "UploadFileAsyncでnullモデルを渡すとArgumentNullExceptionが発行される")]
    public async Task UploadFileAsync_ThrowsArgumentNullException_WhenModelIsNull() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadFileAsync(null!));
    }
    [Fact(DisplayName = "UploadFileAsyncでFileInfo/KintoneFileプロパティがないモデルを渡すとInvalidOperationExceptionが発行される")]
    public async Task UploadFileAsync_ThrowsInvalidOperationException_WhenModelLacksRequiredProps() {
        var logger = Mock.Of<ILogger<KintoneModelFileService<InvalidFileModel_NoProps>>>();
        var service = new KintoneModelFileService<InvalidFileModel_NoProps>(this._mockRepo.Object, logger);
        var model = new InvalidFileModel_NoProps { Dummy = "test" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadFileAsync(model));
    }
    [Fact(DisplayName = "UploadFileAsyncで存在しないFileInfoを渡すとFileNotFoundExceptionが発行される")]
    public async Task UploadFileAsync_ThrowsFileNotFoundException_WhenFileDoesNotExist() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel {
            LocalFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"notfound_{Guid.NewGuid()}.txt")),
            UploadedFile = new KintoneFile()
        };

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.UploadFileAsync(model));
    }
    [Fact(DisplayName = "UploadFileAsync(model, file)でmodelがnullならArgumentNullException")]
    public async Task UploadFileAsync_ThrowsArgumentNullException_WhenModelIsNull2() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var dummyFile = new FileInfo("dummy.txt");

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadFileAsync(null!, dummyFile));
    }
    [Fact(DisplayName = "UploadFileAsync(model, file)でfileがnullならArgumentNullException")]
    public async Task UploadFileAsync_ThrowsArgumentNullException_WhenFileIsNull() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadFileAsync(model, null!));
    }
    [Fact(DisplayName = "UploadFileAsync(model, file)でfileが存在しないならFileNotFoundException")]
    public async Task UploadFileAsync_ThrowsFileNotFoundException_WhenFileDoesNotExist2() {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel();
        var nonexistentFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"nofile_{Guid.NewGuid()}.txt"));

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.UploadFileAsync(model, nonexistentFile));
    }
    [Fact(DisplayName = "UploadFileAsync(model, file)で正常にアップロードされるとKintoneFileが返る")]
    public async Task UploadFileAsync_ReturnsKintoneFile_WhenSuccessful() {
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
    [Fact(DisplayName = "MapUploadedFilesToModelAsyncでmodelがnullならArgumentNullException")]
    public async Task MapUploadedFilesToModelAsync_ThrowsArgumentNullException_WhenModelIsNull() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var files = new List<FileInfo> { new FileInfo("dummy.txt") };

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.MapUploadedFilesToModelAsync(null!, files));
    }
    [Fact(DisplayName = "MapUploadedFilesToModelAsyncでfilesがnullならArgumentNullException")]
    public async Task MapUploadedFilesToModelAsync_ThrowsArgumentNullException_WhenFilesIsNull() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var model = new TestModel();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.MapUploadedFilesToModelAsync(model, null!));
    }
    [Fact(DisplayName = "KintoneFileプロパティに一致するFileInfoがある場合、FileKeyが設定される")]
    public async Task MapUploadedFilesToModelAsync_SetsFileKey_ForMatchingSingleFile() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var file = new FileInfo("match.txt");
        var model = new TestModel {
            SingleFile = new KintoneFile { Name = "match.txt", FileKey = null }
        };

        await service.MapUploadedFilesToModelAsync(model, new[] { file });

        Assert.Equal("[UPLOADED]", model.SingleFile?.FileKey);
    }
    [Fact(DisplayName = "KintoneFileリストに一致するFileInfoがある場合、各FileKeyが設定される")]
    public async Task MapUploadedFilesToModelAsync_SetsFileKey_ForMatchingListFiles() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var files = new[] {
            new FileInfo("a.txt"),
            new FileInfo("b.txt")
        };

        var model = new TestModel {
            FileList = [
                new KintoneFile { Name = "a.txt", FileKey = null },
                new KintoneFile { Name = "b.txt", FileKey = null },
                new KintoneFile { Name = "c.txt", FileKey = "already-set" }
            ]
        };

        await service.MapUploadedFilesToModelAsync(model, files);

        Assert.Equal("[UPLOADED]", model.FileList?[0].FileKey);
        Assert.Equal("[UPLOADED]", model.FileList?[1].FileKey);
        Assert.Equal("already-set", model.FileList?[2].FileKey); // 変更されない
    }
    [Fact(DisplayName = "一致するファイルがない場合、FileKeyは変更されない")]
    public async Task MapUploadedFilesToModelAsync_DoesNotSetFileKey_WhenNoMatch() {
        var service = new KintoneModelFileService<TestModel>(Mock.Of<IKintoneRepository>(), this._logger2);
        var files = new[] { new FileInfo("x.txt") };
        var model = new TestModel {
            SingleFile = new KintoneFile { Name = "notfound.txt", FileKey = null },
            FileList = [
                new KintoneFile { Name = "notfound1.txt", FileKey = null }
            ]
        };

        await service.MapUploadedFilesToModelAsync(model, files);

        Assert.Null(model.SingleFile?.FileKey);
        Assert.Null(model.FileList?[0].FileKey);
    }
    #endregion
    #region <<Download methods>>
    [Fact(DisplayName = "DownloadFileAsyncでKintoneFileプロパティが存在しない場合はInvalidOperationException")]
    public async Task DownloadFileAsync_ThrowsInvalidOperationException_WhenNoKintoneFileProp() {
        var service = new KintoneModelFileService<NoFileModel>(Mock.Of<IKintoneRepository>(), Mock.Of<ILogger<KintoneModelFileService<NoFileModel>>>());
        var model = new NoFileModel();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadFileAsync(model));
    }
    [Theory(DisplayName = "DownloadFileAsyncでKintoneFileがnullまたはFileKeyが空ならArgumentException")]
    [InlineData(null)]
    [InlineData("")]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenKintoneFileIsInvalid(string? fileKey) {
        var service = new KintoneModelFileService<ValidFileModel>(this._mockRepo.Object, this._logger);
        var model = new ValidFileModel {
            UploadedFile = fileKey == null ? null : new KintoneFile { FileKey = fileKey, Name = "dummy.txt" }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileAsync(model));
    }
    [Fact(DisplayName = "DownloadFileAsyncで正常にファイルが保存される")]
    public async Task DownloadFileAsync_SavesFileSuccessfully() {
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
    [Fact(DisplayName = "DownloadFilesAsyncでFileKeyが未設定のファイルはスキップされる")]
    public async Task DownloadFilesAsync_SkipsFilesWithEmptyFileKey() {
        var model = new ValidFileModel();
        var files = new[] {
            new KintoneFile { FileKey = null, Name = "skip1.txt" },
            new KintoneFile { FileKey = "", Name = "skip2.txt" }
        };

        var service = this.CreateService();
        var result = await service.DownloadFilesAsync(model, files);

        Assert.Empty(result);
        this._mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("skip1.txt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
        this._mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("skip2.txt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
    }
    [Fact(DisplayName = "DownloadFilesAsyncで正常なファイルはすべて保存される")]
    public async Task DownloadFilesAsync_SavesValidFiles() {
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
    [Fact(DisplayName = "DownloadFilesAsyncで一部ファイルのダウンロードに失敗しても他は保存される")]
    public async Task DownloadFilesAsync_ContinuesOnDownloadError() {
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
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(file2.Name)),
                It.IsAny<IOException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
    }
    [Theory(DisplayName = "DownloadFileAsyncでFileKeyが未設定ならArgumentException")]
    [InlineData(null)]
    [InlineData("")]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenFileKeyIsInvalid(string? fileKey) {
        var model = new ValidFileModel();
        var file = new KintoneFile { FileKey = fileKey, Name = "invalid.txt" };

        var service = this.CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileAsync(model, file));

        Assert.Equal("file", ex.ParamName);
        Assert.Contains("FileKeyが未設定", ex.Message);
    }

    [Fact(DisplayName = "DownloadFileAsyncで正常なファイルは保存される")]
    public async Task DownloadFileAsync_SavesValidFileSuccessfully() {
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
    [Theory(DisplayName = "DownloadFileToPathAsyncでFileKeyが未設定ならArgumentException")]
    [InlineData(null)]
    [InlineData("")]
    public async Task DownloadFileToPathAsync_ThrowsArgumentException_WhenFileKeyIsInvalid(string? fileKey) {
        var model = new ValidFileModel();
        var file = new KintoneFile { FileKey = fileKey, Name = "invalid.txt" };
        var savePath = Path.Combine(Path.GetTempPath(), "dummy.txt");

        var service = this.CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileToPathAsync(model, file, savePath));

        Assert.Equal("file", ex.ParamName);
        Assert.Contains("FileKeyが未設定", ex.Message);
    }
    [Fact(DisplayName = "DownloadFileToPathAsyncで正常なファイルは指定パスに保存される")]
    public async Task DownloadFileToPathAsync_SavesFileToSpecifiedPath() {
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
    [Fact(DisplayName = "overwrite=false, throwIfExists=true で既存ファイルがあると例外が発生する")]
    public async Task DownloadFileToPathAsync_Throws_WhenFileExists_AndThrowIfExistsTrue() {
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
    [Fact(DisplayName = "overwrite=false, throwIfExists=false で既存ファイルは保持され、新しいファイルが別名で保存される")]
    public async Task DownloadFileToPathAsync_SavesWithNewName_WhenFileExistsAndFlagsAreFalse() {
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
    [Fact(DisplayName = "overwrite=true で既存ファイルは上書きされる")]
    public async Task DownloadFileToPathAsync_OverwritesFile_WhenOverwriteIsTrue() {
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

public class SampleFileModel : KintoneModelBase<SampleFileModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID => 6666;

    public FileInfo? LocalFile { get; set; }
    public KintoneFile? UploadedFile { get; set; }
}
public class MultiFileModel : KintoneModelBase<MultiFileModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID => 6667;

    public List<FileInfo>? LocalFiles { get; set; }
    public List<KintoneFile>? UploadedFiles { get; set; }
}
public class ValidFileModel : KintoneModelBase<ValidFileModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID => 6668;

    public FileInfo? LocalFile { get; set; }
    public KintoneFile? UploadedFile { get; set; }
}
public class InvalidFileModel_NoProps : KintoneModelBase<InvalidFileModel_NoProps> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID => 6669;

    public string? Dummy { get; set; }
}
public class TestModel : KintoneModelBase<TestModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID => 6670;

    public KintoneFile? SingleFile { get; set; }
    public List<KintoneFile>? FileList { get; set; }
}
public class NoFileModel : KintoneModelBase<NoFileModel> {
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
    public override int AppID => 6671;

    public string? Dummy { get; set; }
}

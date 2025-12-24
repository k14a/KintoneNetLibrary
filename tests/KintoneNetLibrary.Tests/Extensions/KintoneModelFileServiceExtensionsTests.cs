using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Extensions;

public class KintoneModelFileServiceExtensionsTests {
    public class SampleModel : KintoneModelBase<SampleModel> {
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");
        public override int AppID { get; init; } = 11111;

        [KintoneItem(fieldType: KintoneFieldType.File)]
        public List<KintoneFile> Attachments { get; set; }
        [KintoneItem(fieldType: KintoneFieldType.File)]
        public List<KintoneFile> Images { get; set; }
    }

    [Fact(DisplayName = "拡張メソッドUploadFilesAsyncがUploadとMapを正しく呼び出す")]
    public async Task UploadFilesExtensionCallsUploadAndMapCorrectly() {
        // Arrange
        var model = new SampleModel();
        var files = new List<FileInfo> {
            new("file1.txt"),
            new("file2.txt")
        };

        var expectedFiles = new List<KintoneFile> {
            new() { FileKey = "key1", Name = "file1.txt" },
            new() { FileKey = "key2", Name = "file2.txt" }
        };

        var mockService = new Mock<IKintoneModelFileService<SampleModel>>();
        mockService
            .Setup(s => s.UploadFilesAsync(model, files))
            .ReturnsAsync(expectedFiles)
            .Verifiable();

        mockService
            .Setup(s => s.MapUploadedFilesToModelAsync(model, files))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await model.UploadFilesAsync(mockService.Object, files);

        // Assert
        Assert.Equal(expectedFiles, result);
        mockService.Verify(s => s.UploadFilesAsync(model, files), Times.Once);
        mockService.Verify(s => s.MapUploadedFilesToModelAsync(model, files), Times.Once);
    }
    [Fact(DisplayName = "DownloadFilesAsyncはFileKeyありのKintoneFileのみ抽出してDownload呼び出す")]
    public async Task DownloadFilesExtensionFiltersFilesAndCallsDownloadCorrectly() {
        // Arrange
        var model = new SampleModel {
            Attachments = [
                new() { FileKey = "key1", Name = "doc1.pdf" },
                new() { FileKey = null, Name = "doc2.pdf" }, // 無効
                new() { FileKey = "", Name = "doc3.pdf" }    // 無効
            ],
            Images = [
                new() { FileKey = "key2", Name = "img1.png" }
            ]
        };

        var expectedFiles = new List<FileInfo> {
            new("downloaded/doc1.pdf"),
            new("downloaded/img1.png")
        };

        var mockService = new Mock<IKintoneModelFileService<SampleModel>>();
        mockService
            .Setup(s => s.DownloadFilesAsync(
                model,
                It.Is<IEnumerable<KintoneFile>>(files =>
                    files.Count() == 2 &&
                    files.Any(f => f.FileKey == "key1") &&
                    files.Any(f => f.FileKey == "key2")),
                "downloaded",
                true,
                false))
            .ReturnsAsync(expectedFiles)
            .Verifiable();

        // Act
        var result = await model.DownloadFilesAsync(mockService.Object, "downloaded", overwrite: true, throwIfExists: false);

        // Assert
        Assert.Equal(expectedFiles, result);
        mockService.Verify();
    }
}


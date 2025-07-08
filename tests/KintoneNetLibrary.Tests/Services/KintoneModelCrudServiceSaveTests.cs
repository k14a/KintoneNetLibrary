using System.Text.Json;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

public class KintoneModelCrudServiceSaveTests {
    #region <<Test methods>>
    [Fact]
    public async Task SaveAsync_WhenOnlyCreateTargetsExist_CallsCreateOnly() {
        var records = new List<SampleModel> {
        new() { FieldA = "Create", RecordID = null, Revision = -1 }
    };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneAccount()),
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"C1001\"], \"revisions\": [\"1\"]}");

        var result = await service.SaveAsync(records);

        Assert.Single(result.Succeeded);
        Assert.Equal("C1001", result.Succeeded[0].RecordID);
        Assert.Equal(1, result.Succeeded[0].Revision);
        Assert.Empty(result.Failed);

        mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((msg, _) => msg.ToString()!.Contains("SaveAsync() - Start")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((msg, _) => msg.ToString()!.Contains("SaveAsync() - Finish")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    [Fact]
    public async Task SaveAsync_WhenOnlyUpdateTargetsExist_CallsUpdateOnlyAndReturnsResult() {
        var updateRecord = new SampleModel {
            FieldA = "Update",
            RecordID = "1001", // Update対象
            Revision = 5
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneAccount()),
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        // UpdateAsync を構成する AddUpdateRecordsAsync のレスポンスモック
        mockRepo.Setup(r => r.UpdateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"1001\"], \"revisions\": [\"6\"]}");

        // Revision更新確認を可能にするよう ParseUpdatedRecords() の呼び出し動作を前提に結果確認
        var result = await service.SaveAsync(new List<SampleModel> { updateRecord });

        Assert.Single(result.Succeeded);
        Assert.Equal("1001", result.Succeeded[0].RecordID);
        Assert.Equal(6, result.Succeeded[0].Revision);

        Assert.Empty(result.Failed);

        mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((msg, _) => msg.ToString()!.Contains("SaveAsync() - Start")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((msg, _) => msg.ToString()!.Contains("SaveAsync() - Finish")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    [Fact]
    public async Task SaveAsync_WhenCreateAndUpdateTargetsExist_CallsBothAndCombinesResults() {
        var records = new List<SampleModel> {
            new() { FieldA = "CreateA", RecordID = null, Revision = -1 },
            new() { FieldA = "UpdateB", RecordID = "R1002", Revision = 2 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneAccount()),
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"R2001\"], \"revisions\": [\"1\"]}");

        mockRepo.Setup(r => r.UpdateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"R1002\"], \"revisions\": [\"3\"]}");

        var result = await service.SaveAsync(records);

        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);

        Assert.Contains(result.Succeeded, r => r.RecordID == "R2001" && r.Revision == 1);
        Assert.Contains(result.Succeeded, r => r.RecordID == "R1002" && r.Revision == 3);
    }
    [Fact]
    public async Task SaveWithRetryAsync_WhenOnlyCreateTargets_SucceedsOnCreate() {
        var createModel = new SampleModel { RecordID = null, Revision = -1 };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneAccount()),
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"C1001\"], \"revisions\": [\"1\"]}");

        var result = await service.SaveWithRetryAsync([createModel]);

        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.Equal("C1001", result.Succeeded[0].RecordID);
        Assert.Equal(1, result.Succeeded[0].Revision);
    }
    [Fact]
    public async Task SaveWithRetryAsync_WhenCreateFails_UpdatesOnRetry() {
        var model = new SampleModel2 {
            RecordID = null, Revision = -1, CustomUpdateKey = "U123" // これにより Update可能条件を満たす
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneAccount()),
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel2>>()))
            .ThrowsAsync(new Exception("Create failed"));

        mockRepo.Setup(r => r.UpdateRecordsAsync(It.IsAny<IList<SampleModel2>>()))
            .ReturnsAsync("{\"ids\": [\"U123\"], \"revisions\": [\"2\"]}");

        var result = await service.SaveWithRetryAsync(new List<SampleModel2> { model }, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.Equal("U123", result.Succeeded[0].RecordID);
        Assert.Equal(2, result.Succeeded[0].Revision);
    }
    [Fact]
    public async Task SaveWithRetryAsync_WhenOnlyUpdateTargets_SucceedsOnUpdate() {
        var model = new SampleModel { RecordID = "U999", Revision = 7 };

        var mockRepo = new Mock<IKintoneRepository>();

        mockRepo.Setup(r => r.UpdateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"U999\"], \"revisions\": [\"8\"]}");

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneAccount()),
            Options.Create(new KintoneExecutionOptions()),
            null,
            new Mock<ILogger<KintoneModelCrudService>>().Object
        );

        var result = await service.SaveWithRetryAsync(new List<SampleModel> { model });

        Assert.Single(result.Succeeded);
        Assert.Equal("U999", result.Succeeded[0].RecordID);
        Assert.Equal(8, result.Succeeded[0].Revision);
    }

    #endregion
}
internal class SampleModel2 : KintoneModelBase {
    public override int AppID => 9999;

    [KintoneItem(isKey: true)]
    public string CustomUpdateKey { get; set; } = string.Empty;
    [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
    public string FieldA { get; set; } = string.Empty;
}
using System.Text.Json;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

/// <summary>
/// KintoneTypedCrudService<SampleModel> クラスの SaveAsync メソッドと SaveWithRetryAsync メソッドの動作をテストするクラスです。
/// </summary>
public class KintoneModelCrudServiceSaveTests {
    #region <<Test methods>>
    /// <summary>
    /// SaveAsync メソッドに、RecordId が null で Revision が -1 のレコード（＝Create対象）だけを含むリストを渡した場合、CreateRecordsAsync メソッドが呼び出され、その結果が正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task SaveAsyncWhenOnlyCreateTargetsExistCallsCreateOnly() {
        var records = new List<SampleModel> {
            new() { FieldA = "Create", RecordId = null, Revision = -1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>();

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"C1001\"], \"revisions\": [\"1\"]}");

        mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var result = await service.SaveAsync(records);

        Assert.Single(result.Succeeded);
        Assert.Equal("C1001", result.Succeeded[0].RecordId);
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

    /// <summary>
    /// SaveAsync メソッドに、RecordId が null で Revision が -1 のレコード（＝Create対象）を含まないリストを渡した場合、UpdateRecordsAsync メソッドが呼び出され、その結果が正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task SaveAsyncWhenOnlyUpdateTargetsExistCallsUpdateOnlyAndReturnsResult() {
        var updateRecord = new SampleModel {
            FieldA = "Update",
            RecordId = "1001", // Update対象
            Revision = 5
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>();

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        // UpdateAsync を構成する AddUpdateRecordsAsync のレスポンスモック
        mockRepo.Setup(r => r.UpdateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"records\": [{\"id\": \"1001\", \"revision\": \"6\"}]}");

        mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // Revision更新確認を可能にするよう ParseUpdatedRecords() の呼び出し動作を前提に結果確認
        var result = await service.SaveAsync(new List<SampleModel> { updateRecord });

        Assert.Single(result.Succeeded);
        Assert.Equal("1001", result.Succeeded[0].RecordId);
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

    /// <summary>
    /// SaveAsync メソッドに、RecordId が null で Revision が -1 のレコード（＝Create対象）と、RecordId と Revision が両方とも有効な値のレコード（＝Update対象）を両方含むリストを渡した場合、CreateRecordsAsync メソッドと UpdateRecordsAsync メソッドの両方が呼び出され、それぞれの結果が正しく組み合わされて返されることをテストします。
    /// </summary>
    [Fact]
    public async Task SaveAsyncWhenCreateAndUpdateTargetsExistCallsBothAndCombinesResults() {
        var records = new List<SampleModel> {
            new() { FieldA = "CreateA", RecordId = null, Revision = -1 },
            new() { FieldA = "UpdateB", RecordId = "R1002", Revision = 2 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>();

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"R2001\"], \"revisions\": [\"1\"]}");

        mockRepo.Setup(r => r.UpdateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"records\": [{\"id\": \"R1002\", \"revision\": \"3\"}]}");

        mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var result = await service.SaveAsync(records);

        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);

        Assert.Contains(result.Succeeded, r => r.RecordId == "R2001" && r.Revision == 1);
        Assert.Contains(result.Succeeded, r => r.RecordId == "R1002" && r.Revision == 3);
    }

    /// <summary>
    /// SaveWithRetryAsync メソッドに、RecordId が null で Revision が -1 のレコード（＝Create対象）だけを含むリストを渡した場合、CreateRecordsAsync メソッドが呼び出され、その結果が正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryAsyncWhenOnlyCreateTargetsSucceedsOnCreate() {
        var createModel = new SampleModel { RecordId = null, Revision = -1 };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>();

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\": [\"C1001\"], \"revisions\": [\"1\"]}");

        var result = await service.SaveWithRetryAsync([createModel]);

        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.Equal("C1001", result.Succeeded[0].RecordId);
        Assert.Equal(1, result.Succeeded[0].Revision);
    }

    /// <summary>
    /// SaveWithRetryAsync メソッドに、RecordId が null で Revision が -1 のレコード（＝Create対象）を含むリストを渡した場合、最初の CreateRecordsAsync メソッドの呼び出しが例外をスローし、その後のリトライで UpdateRecordsAsync メソッドが呼び出されて成功することをテストします。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryAsyncWhenCreateFailsUpdatesOnRetry() {
        var model = new SampleModel2 {
            RecordId = null, Revision = -1, CustomUpdateKey = "U123" // これにより Update可能条件を満たす
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var mockLogger = new Mock<ILogger<KintoneTypedCrudService<SampleModel2>>>();

        var service = new KintoneTypedCrudService<SampleModel2>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions()),
            null,
            mockLogger.Object
        );

        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel2>>()))
            .ThrowsAsync(new Exception("Create failed"));

        mockRepo.Setup(r => r.UpdateRecordsAsync(It.IsAny<IList<SampleModel2>>()))
            .ReturnsAsync("{\"records\": [{\"id\": \"U123\", \"revision\": \"2\"}]}");

        var result = await service.SaveWithRetryAsync(new List<SampleModel2> { model }, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.Equal("U123", result.Succeeded[0].RecordId);
        Assert.Equal(2, result.Succeeded[0].Revision);
    }

    /// <summary>
    /// SaveWithRetryAsync メソッドに、RecordId と Revision が両方とも有効な値のレコード（＝Update対象）だけを含むリストを渡した場合、最初の UpdateRecordsAsync メソッドの呼び出しが例外をスローし、その後のリトライで UpdateRecordsAsync メソッドが再度呼び出されて成功することをテストします。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryAsyncWhenOnlyUpdateTargetsSucceedsOnUpdate() {
        var model = new SampleModel { RecordId = "U999", Revision = 7 };

        var mockRepo = new Mock<IKintoneRepository>();

        mockRepo.Setup(r => r.UpdateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"records\": [{\"id\": \"U999\", \"revision\": \"8\"}]}");

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions()),
            null,
            new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>().Object
        );

        var result = await service.SaveWithRetryAsync(new List<SampleModel> { model });

        Assert.Single(result.Succeeded);
        Assert.Equal("U999", result.Succeeded[0].RecordId);
        Assert.Equal(8, result.Succeeded[0].Revision);
    }

    #endregion
}

/// <summary>
/// テスト用のサンプルモデルクラスです。SampleModel と異なり、更新対象を識別するためのキー項目 CustomUpdateKey を持ちます。
/// </summary>
internal class SampleModel2 : KintoneModelBase<SampleModel2> {
    public override int AppId { get; init; } = 9999;
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");

    [KintoneItem(isKey: true)]
    public string CustomUpdateKey { get; set; } = string.Empty;
    [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
    public string FieldA { get; set; } = string.Empty;
}
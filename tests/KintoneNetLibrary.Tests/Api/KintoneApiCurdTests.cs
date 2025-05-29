using Xunit;
using KintoneNetLibrary.Tests.Models;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Api.DTO;
using KintoneNetLibrary.Infrastructure.Helpers;
using System.Text.Json;
using System.Reflection;
using System.IO.Compression;

namespace KintoneNetLibrary.Tests.Api;

public class KintoneApiCrudTests {
    private KintoneApi CreateApi() {
        var cfg = TestEnv.Settings;
        var cli = new HttpClient {
            BaseAddress = new Uri($"https://{cfg.Domain}/k/v1/")
        };
        var account = new KintoneAccount { ApiToken = cfg.ApiToken, Domain = cfg.Domain };
        return new KintoneApi(account, cfg.AppID, cli);
    }

    [Fact]
    public async Task Can_Create_Read_Delete_Record() {
        var api = this.CreateApi();
        // 準備：BookModelのインスタンス
        var book = new BookModel { Title = "Test Book", Price = 1000 };

        // Create
        var json = KintoneRequestBuilder.BuildCreateJson([book]);
        var createResult = await api.CreateRecordsAsync(json);
        var createParsed = KintoneResponseParser.ParseCreatedRecords([book], createResult);
        Assert.Single(createParsed);

        // Read（検索条件は ID）
        var recordId = createParsed.First().ID;
        var found = await api.FindByIDAsync<BookModel>(recordId);
        var record = KintoneResponseParser.ParseRecord<BookModel>(found);
        Assert.NotNull(record);
        Assert.Equal("Test Book", record!.Title);
        Assert.Equal(1000, record.Price);

        // Delete
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson([record]);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task Can_Create_Read_Delete_Multiple_Records() {
        var api = this.CreateApi();

        // 準備：複数の BookModel インスタンス
        var books = new List<BookModel> {
            new() { Title = "Book A", Price = 100 },
            new() { Title = "Book B", Price = 200 },
            new() { Title = "Book C", Price = 300 },
        };

        // Create
        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdBooks = KintoneResponseParser.ParseCreatedRecords(books, createResult);
        Assert.Equal(3, createdBooks.Count);

        // Read（ID指定）
        var idList = createdBooks.Select(b => b.ID).ToList();
        var foundJson = await api.FindByIDsAsync<BookModel>(idList);
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

        Assert.Equal(3, foundRecords.Count);
        Assert.Contains(foundRecords, b => b.Title == "Book A" && b.Price == 100);
        Assert.Contains(foundRecords, b => b.Title == "Book B" && b.Price == 200);
        Assert.Contains(foundRecords, b => b.Title == "Book C" && b.Price == 300);

        // Delete
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(foundRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task Can_Create_Update_Find_Delete_Record() {
        var api = this.CreateApi();

        // Step 1: Create
        var book = new BookModel { Title = "Initial Title", Price = 1000 };
        var createJson = KintoneRequestBuilder.BuildCreateJson([book]);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords([book], createResult);
        var created = createdRecords.First();
        Assert.NotNull(created.ID);

        // Step 2: Update
        created.Title = "Updated Title";
        created.Price = 1500;

        var updateJson = KintoneRequestBuilder.BuildUpdateJson([created]);
        var updateResult = await api.UpdateAsync<BookModel>(updateJson);
        Assert.False(string.IsNullOrWhiteSpace(updateResult));

        // Step 3: Find
        var findJson = await api.FindByIDAsync<BookModel>(created.ID);
        var foundRecord = KintoneResponseParser.ParseRecord<BookModel>(findJson);
        Assert.NotNull(foundRecord);
        Assert.Equal("Updated Title", foundRecord!.Title);
        Assert.Equal(1500, foundRecord.Price);

        // Step 4: Delete
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson([foundRecord]);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task Can_Update_ByKey() {
        var api = this.CreateApi();

        // UUID を生成
        var uuid = Guid.NewGuid().ToString();

        // ① レコード作成（UUID を含む）
        var book = new BookModel {
            Title = "Original Title",
            Price = 1000,
            Uuid = uuid
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson([book]);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords([book], createResult);
        var created = createdRecords.First();
        Assert.False(string.IsNullOrWhiteSpace(created.ID), "Record ID is null or empty after creation.");

        // ② updateKey（UUID）で更新
        created.Title = "Updated Title by UUID";
        created.Price = 2000;

        var updateJson = KintoneRequestBuilder.BuildUpdateJson([created]);
        var updateResult = await api.UpdateAsync<BookModel>(updateJson);
        Assert.False(string.IsNullOrWhiteSpace(updateResult));

        // ③ updateKey（UUID）で再取得
        var found = await api.FindByFieldAsync<BookModel>("UUID", uuid);
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(found);
        Assert.Single(foundRecords);

        var fetched = foundRecords[0];
        Assert.Equal("Updated Title by UUID", fetched.Title);
        Assert.Equal(2000, fetched.Price);

        // ④ 削除（IDを使う必要があるので fetched を使う）
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson([fetched]);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task FindByQueryAsync_ShouldReturnFilteredRecords() {
        var api = this.CreateApi();
        // Arrange
        var books = new List<BookModel> {
            new() { Title = "Book A", Price = 1000, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "Book B", Price = 1500, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "Book C", Price = 2000, Uuid = Guid.NewGuid().ToString() },
        };

        // 1. レコード登録
        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);
        Assert.All(createdRecords, r => Assert.NotNull(r.ID));

        // 2. クエリで検索（Price >= 1500）
        string query = "Price >= 1500 order by Price asc";
        var found = await api.FindByQueryAsync<BookModel>(query);
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(found);

        // 3. 検証
        Assert.NotNull(foundRecords);
        var prices = foundRecords.Select(r => r.Price).ToList();
        Assert.Contains(1500, prices);
        Assert.Contains(2000, prices);
        Assert.DoesNotContain(1000, prices);

        // 4. 後始末：登録したレコード削除
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task FindAllAsync_CursorPaging_WorksCorrectly() {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = 2;

        var books = new List<BookModel> {
            new() { Title = "CursorTest01", Price = 1000, Uuid=Guid.NewGuid().ToString() },
            new() { Title = "CursorTest02", Price = 1100, Uuid=Guid.NewGuid().ToString() },
            new() { Title = "CursorTest03", Price = 1200, Uuid=Guid.NewGuid().ToString() },
            new() { Title = "CursorTest04", Price = 1300, Uuid=Guid.NewGuid().ToString() },
            new() { Title = "CursorTest05", Price = 1400, Uuid=Guid.NewGuid().ToString() },
        };

        // Act - 登録
        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        // Assert - 登録確認
        Assert.Equal(5, createdRecords.Count);
        Assert.All(createdRecords, item => Assert.NotNull(item.ID));

        // Act - 全件取得（カーソルAPIが使用されることを期待）
        var found = await api.FindAllAsync<BookModel>();
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(found);

        // Assert - カーソルで5件取得できているか
        Assert.NotNull(foundRecords);
        var matched = foundRecords!.Where(f => f.Title.StartsWith("CursorTest")).ToList();
        Assert.Equal(5, matched.Count);

        // Cleanup - 削除
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }

}

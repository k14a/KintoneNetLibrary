using Xunit;
using KintoneNetLibrary.Tests.Models;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Api.DTO;
using KintoneNetLibrary.Infrastructure.Helpers;
using System.Text.Json;
using System.Reflection;
using System.IO.Compression;
using KintoneNetLibrary.Tests.Helpers;

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
        var book = new BookModel { Title = "Test Book", Price = 1000, Uuid = Guid.NewGuid().ToString() };

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
            new() { Title = "Book A", Price = 100, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "Book B", Price = 200, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "Book C", Price = 300, Uuid = Guid.NewGuid().ToString() },
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
        var book = new BookModel { Title = "Initial Title", Price = 1000, Uuid = Guid.NewGuid().ToString() };
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
            new() { Title = "CursorTest01", Price = 1000, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "CursorTest02", Price = 1100, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "CursorTest03", Price = 1200, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "CursorTest04", Price = 1300, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "CursorTest05", Price = 1400, Uuid = Guid.NewGuid().ToString() },
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
    [Fact]
    public async Task FindAsync_QueryExceedsPageSize_WorksCorrectly() {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = 2;

        var books = new List<BookModel> {
            new() { Title = "QueryTest01", Price = 1000, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "QueryTest02", Price = 1100, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "QueryTest03", Price = 1200, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "QueryTest04", Price = 1300, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "QueryTest05", Price = 1400, Uuid = Guid.NewGuid().ToString() },
        };

        // Act - 登録
        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        // Assert - 登録確認
        Assert.Equal(5, createdRecords.Count);

        // Act - クエリ指定で取得（複数ページにまたがる）
        var found = await api.FindByQueryAsync<BookModel>("Price > 0");
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(found);

        // Assert - 検索結果に5件すべて含まれているか
        Assert.NotNull(foundRecords);
        var matched = foundRecords!.Where(f => f.Title.StartsWith("QueryTest")).ToList();
        Assert.Equal(5, matched.Count);

        // Cleanup
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)] // カーソル使用
    [InlineData(2, 5)] // カーソル不使用
    public async Task FindAsync_QueryWithOrderBy_WorksCorrectly(int recordCount, int pageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = pageSize;

        var books = new List<BookModel>();
        for (int i = 0; i < recordCount; i++) {
            books.Add(new BookModel {
                Title = $"OrderTest{(char)('A' + recordCount - i)}", // Z, Y, X, ...
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            });
        }

        // Act - 登録
        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);
        Assert.Equal(recordCount, createdRecords.Count);

        // Act - クエリで取得（昇順指定）
        var found = await api.FindByQueryAsync<BookModel>("Price > 0 order by Title asc");
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(found);

        // Assert - 昇順で並んでいるか
        var matched = foundRecords!.Where(f => f.Title.StartsWith("OrderTest")).ToList();
        Assert.Equal(recordCount, matched.Count);

        var ordered = matched.OrderBy(b => b.Title, StringComparer.Ordinal).ToList();
        Assert.True(matched.SequenceEqual(ordered), "取得順が昇順でありません");

        // Cleanup
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)] // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_QueryWithComplexCondition_WorksCorrectly(int recordCount, int pageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"ブック{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }
        ).ToList();
        books.Add(new BookModel { Title = "ぶっく", Price = 1000, Uuid = Guid.NewGuid().ToString() });

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        // Act
        string query = "Title like \"ブック\" and Price < 1300";
        var foundRecords = await KintoneTestHelper.WaitForExpectedRecordCountAsync<BookModel>(api, query, 2);

        // Assert
        Assert.NotNull(foundRecords);
        Assert.Equal(2, foundRecords.Count);

        // Cleanup
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)]   // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)]  // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_QueryWithNoHit_WorksCorrectly(int recordCount, int pageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"NoHitBook{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }
        ).ToList();

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        // Act
        string query = "Price > 999999";
        var foundJson = await api.FindByQueryAsync<BookModel>(query);
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

        // Assert
        Assert.NotNull(foundRecords);
        Assert.Empty(foundRecords!);

        // Cleanup
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)]   // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)]  // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_WithInvalidQuery_ThrowsException(int recordCount, int pageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"InvalidQueryBook{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }).ToList();

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Act
            string invalidQuery = "INVALID_FIELD > 0";
            var _ = await api.FindByQueryAsync<BookModel>(invalidQuery);

            // Assert - 到達してはいけない
            Assert.Fail("例外が発生するはずの不正なクエリで成功しました。");

        } catch (KintoneException ex) {
            // Assert - 例外が正しく発生しているか
            Assert.Contains("INVALID_FIELD", ex.Detail, StringComparison.OrdinalIgnoreCase);
        }

        // Cleanup
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)]   // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)]  // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_RepeatedQuery_ReturnsSameResults(int recordCount, int pageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"RepeatedQueryBook{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }).ToList();

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        string query = "Price >= 100";

        // Act - クエリを2回実行
        var foundJson1 = await api.FindByQueryAsync<BookModel>(query);
        var foundRecords1 = KintoneResponseParser.ParseRecords<BookModel>(foundJson1);

        var foundJson2 = await api.FindByQueryAsync<BookModel>(query);
        var foundRecords2 = KintoneResponseParser.ParseRecords<BookModel>(foundJson2);

        // Assert - レコード数およびUUID一致で再現性を確認
        Assert.NotNull(foundRecords1);
        Assert.NotNull(foundRecords2);
        Assert.Equal(foundRecords1!.Count, foundRecords2!.Count);

        var uuids1 = foundRecords1.Select(r => r.Uuid).OrderBy(u => u).ToList();
        var uuids2 = foundRecords2.Select(r => r.Uuid).OrderBy(u => u).ToList();
        Assert.Equal(uuids1, uuids2);

        // Cleanup
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_EmptyQuery_ReturnsAllRecords(int recordCount, int pageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"ブック{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }).ToList();

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        // Act
        string query = ""; // クエリなしで全件取得
        var foundJson = await api.FindByQueryAsync<BookModel>(query);
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

        // Assert
        Assert.NotNull(foundRecords);

        var matched = foundRecords!.Where(r => books.Any(b => b.Uuid == r.Uuid)).ToList();

        Assert.Equal(recordCount, matched.Count);

        // Cleanup
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteJsonAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(500, 100)]  // カーソル使用（pageSize < recordCount）
    [InlineData(499, 500)]  // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_OverMaxLimit_WorksCorrectly(int recordCount, int pageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"大容量ブック{i:000}",
                Price = 1000 + i,
                Uuid = Guid.NewGuid().ToString()
            }).ToList();

        // Chunk して順次登録
        var createdRecords = new List<BookModel>();
        foreach (var chunk in books.Chunk(100))  // Kintoneは最大100件/リクエスト
        {
            var createJson = KintoneRequestBuilder.BuildCreateJson(chunk);
            var createResult = await api.CreateRecordsAsync(createJson);
            var parsed = KintoneResponseParser.ParseCreatedRecords(chunk.ToList(), createResult);
            createdRecords.AddRange(parsed);
        }

        // Act
        var query = "Price >= 1000"; // 全件ヒットするクエリ
        var foundJson = await api.FindByQueryAsync<BookModel>(query);
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

        // Assert
        Assert.NotNull(foundRecords);

        var matched = foundRecords!.Where(r => books.Any(b => b.Uuid == r.Uuid)).ToList();
        Assert.Equal(recordCount, matched.Count);

        // Cleanup
        foreach (var deleteChunk in createdRecords.Chunk(100)) {
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(deleteChunk);
            var deleteResult = await api.DeleteJsonAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用：recordCount = 4, pageSize = 2 → カーソル使用される
    [InlineData(5, 10)] // カーソル不使用：recordCount = 4, pageSize = 10 → 1ページで済むためカーソル不要
    public async Task FindAsync_TitleInCondition_WorksCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック01", Price = 1000, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック02", Price = 1500, Classification = "雑誌" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック03", Price = 2000, Classification = "SF" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック04", Price = 2500, Classification = "雑誌" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Act
            var condition = "Title in (\"ブック01\", \"ブック03\")";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            // Assert
            var titles = foundRecords.Select(r => r.Title).ToList();
            Assert.Contains("ブック01", titles);
            Assert.Contains("ブック03", titles);
            Assert.DoesNotContain("ブック02", titles);
            Assert.DoesNotContain("ブック04", titles);
            Assert.Equal(2, foundRecords.Count);
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteJsonAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用：レコード数 > pageSize
    [InlineData(5, 10)] // カーソル不使用：pageSize >= レコード数
    public async Task FindAsync_ClassificationInCondition_WorksCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "雑誌A", Price = 1000, Classification = "雑誌" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "技術書B", Price = 1500, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "SF C", Price = 2000, Classification = "SF" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "技術書D", Price = 2500, Classification = "技術書" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Act
            var condition = "Classification in (\"雑誌\", \"SF\")";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            // Assert
            var classifications = foundRecords.Select(r => r.Classification).ToList();
            Assert.Contains("雑誌", classifications);
            Assert.Contains("SF", classifications);
            Assert.DoesNotContain("技術書", classifications);
            Assert.Equal(2, foundRecords.Count);
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteJsonAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用：レコード数 > pageSize
    [InlineData(5, 10)] // カーソル不使用：pageSize >= レコード数
    public async Task FindAsync_PriceNotEqualCondition_WorksCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブックA", Price = 1200, Classification = "雑誌" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブックB", Price = 1000, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブックC", Price = 1500, Classification = "SF" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブックD", Price = 1200, Classification = "雑誌" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Act
            var condition = "Price != 1200";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            // Assert
            Assert.All(foundRecords, r => Assert.NotEqual(1200, r.Price));
            var prices = foundRecords.Select(r => r.Price).ToList();
            Assert.Contains(1000, prices);
            Assert.Contains(1500, prices);
            Assert.DoesNotContain(1200, prices);
            Assert.Equal(2, foundRecords.Count);
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteJsonAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_TitleNotEmptyCondition_WorksCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック01", Price = 1000, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "",         Price = 1500, Classification = "雑誌" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック03", Price = 2000, Classification = "SF" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "",         Price = 2500, Classification = "雑誌" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Act
            var condition = "Title != \"\"";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            // Assert
            Assert.All(foundRecords, r => Assert.False(string.IsNullOrEmpty(r.Title)));
            var titles = foundRecords.Select(r => r.Title).ToList();
            Assert.Contains("ブック01", titles);
            Assert.Contains("ブック03", titles);
            Assert.DoesNotContain("", titles);
            Assert.Equal(2, foundRecords.Count);
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteJsonAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_PriceGreaterThanOrEqualZero_WithNulls_ReturnsEmptyList(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "価格未設定01", Price = null, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "価格未設定02", Price = null, Classification = "雑誌" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Act
            var condition = "Price >= 0";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            // Assert
            Assert.NotNull(foundRecords);
            Assert.Empty(foundRecords); // 検索結果は空のはず
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteJsonAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsync_ReleaseDateCondition_WorksCorrectly(int pageSize, int recordCount) {
        // Arrange
        var api = this.CreateApi();
        api.CursorPageSize = pageSize;

        var books = new List<BookModel> {
            new() {
                Uuid = Guid.NewGuid().ToString(),
                Title = "ブックA",
                Price = 1000,
                Classification = "技術書",
                ReleaseDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc) // 条件外
            },
            new() {
                Uuid = Guid.NewGuid().ToString(),
                Title = "ブックB",
                Price = 1500,
                Classification = "雑誌",
                ReleaseDate = new DateTime(2024, 01, 02, 10, 00, 00, DateTimeKind.Utc) // 条件内
            },
            new() {
                Uuid = Guid.NewGuid().ToString(),
                Title = "ブックC",
                Price = 2000,
                Classification = "SF",
                ReleaseDate = new DateTime(2025, 01, 01, 00, 00, 00, DateTimeKind.Utc) // 条件内
            }
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateRecordsAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Act
            var condition = "ReleaseDate > \"2024-01-01T00:00:00Z\"";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            // Assert
            var titles = foundRecords.Select(r => r.Title).ToList();
            Assert.Contains("ブックB", titles);
            Assert.Contains("ブックC", titles);
            Assert.DoesNotContain("ブックA", titles);
            Assert.Equal(2, foundRecords.Count);

        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteJsonAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData("5")] // 強く勧めたい
    [InlineData("3")] // どちらでもない
    [InlineData("1")] // まったく勧めない
    public async Task CreateAndFind_RadioButtonField_WorksCorrectly(string recommendation) {
        // Arrange
        var api = this.CreateApi();
        var model = new BookModel {
            Title = $"おすすめ度テスト_{recommendation}",
            Price = 1800,
            Uuid = Guid.NewGuid().ToString(),
            Classification = "SF",
            ReleaseDate = DateTime.Today,
            Recommendation = recommendation
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);

        try {
            // Act
            var query = $"UUID = \"{model.Uuid}\"";
            var json = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(json);

            // Assert
            Assert.NotEmpty(results);
            var found = results.Single();
            Assert.Equal(model.Recommendation, found.Recommendation);
        } finally {
            // Cleanup
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, created);
        }
    }
    [Theory]
    [MemberData(nameof(CheckBoxTestData))]
    public async Task CreateAndFind_CheckBoxField_WorksCorrectly(string[] selections) {
        // Arrange
        var api = this.CreateApi();
        var model = new BookModel {
            Title = "チェックボックステスト",
            Price = 2000,
            Uuid = Guid.NewGuid().ToString(),
            Classification = "技術書",
            ReleaseDate = DateTime.Today,
            CheckBoxes = selections
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);

        try {
            // Act
            var query = $"UUID = \"{model.Uuid}\"";
            var json = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(json);

            // Assert
            Assert.Single(results);
            var found = results[0];

            Assert.Equal(model.CheckBoxes.OrderBy(x => x), found.CheckBoxes.OrderBy(x => x));
        } finally {
            // Cleanup
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, created);
        }
    }
    [Theory]
    [MemberData(nameof(LinkFieldTestData))]
    public async Task CreateAndFind_LinkFields_WorksCorrectly(string webAddress, string telephone, string email) {
        // Arrange
        var api = this.CreateApi();
        var model = new BookModel {
            Title = "リンク型フィールドテスト",
            Price = 3000,
            Uuid = Guid.NewGuid().ToString(),
            Classification = "技術書",
            ReleaseDate = DateTime.Today,
            WebAddress = webAddress,
            Telephone = telephone,
            Email = email
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);

        try {
            // Act
            var query = $"UUID = \"{model.Uuid}\"";
            var json = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(json);

            // Assert
            Assert.Single(results);
            var found = results[0];

            Assert.Equal(model.WebAddress, found.WebAddress);
            Assert.Equal(model.Telephone, found.Telephone);
            Assert.Equal(model.Email, found.Email);
        } finally {
            // Cleanup
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, created);
        }
    }

    #region <<Protected method>>
    public static IEnumerable<object[]> CheckBoxTestData => new List<object[]> {
        new object[] { new[] { "チェック1", "チェック3" } },
        new object[] { new[] { "チェック2" } },
        new object[] { Array.Empty<string>() },
    };
    public static IEnumerable<object[]> LinkFieldTestData => new List<object[]> {
        new object[] { "https://example.com/", "+81-90-1234-5678", "test@example.com" },
        new object[] { "http://openai.com/", "03-1234-5678", "contact@openai.com" },
        new object[] { string.Empty, string.Empty, string.Empty } // 空も許容
    };
    #endregion
}

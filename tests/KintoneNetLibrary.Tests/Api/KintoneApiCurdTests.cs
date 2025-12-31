using System.Net;
using System.Text.Json;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Tests.Helpers;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

public class KintoneApiCrudTests {
    [Fact]
    public async Task CanCreateReadDeleteRecord() {
        var api = KintoneTestHelper.CreateApi();
        // 準備：BookModelのインスタンス
        var book = new BookModel {
            Title = "Test Book",
            Price = 1000,
            Uuid = Guid.NewGuid().ToString(),
            Access = new ApiTokenAccess(TestEnv.Settings.Domain, TestEnv.Settings.ApiToken)
        };

        // Create
        var json = KintoneRequestBuilder.BuildCreateJson([book]);
        var createResult = await api.CreateAsync(json);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task CanCreateReadDeleteMultipleRecords() {
        var api = KintoneTestHelper.CreateApi();

        // 準備：複数の BookModel インスタンス
        var books = new List<BookModel> {
            new() { Title = "Book A", Price = 100, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "Book B", Price = 200, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "Book C", Price = 300, Uuid = Guid.NewGuid().ToString() },
        };

        // Create
        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task CanReadWithFieldCodes() {
        var api = KintoneTestHelper.CreateApi();

        // 準備：BookModel の3件（ID確保のため）
        var books = new List<BookModel> {
                new() { Title = "Book D", Price = 400, Uuid = Guid.NewGuid().ToString() },
                new() { Title = "Book E", Price = 500, Uuid = Guid.NewGuid().ToString() },
                new() { Title = "Book F", Price = 600, Uuid = Guid.NewGuid().ToString() },
            };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
        var createdBooks = KintoneResponseParser.ParseCreatedRecords(books, createResult);
        var idList = createdBooks.Select(b => b.ID).ToList();

        // fieldCodes を使って特定のフィールドのみ取得
        var fieldCodes = new[] { "Title" };
        var foundJson = await api.FindByIDsAsync<BookModel>(idList, fieldCodes);
        var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

        Assert.Equal(3, foundRecords.Count);
        Assert.All(foundRecords, r => Assert.NotNull(r.Title));
        Assert.All(foundRecords, r => Assert.Null(r.Price));

        // 後始末：削除
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(foundRecords);
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task CanCreateUpdateFindDeleteRecord() {
        var api = KintoneTestHelper.CreateApi();

        // Step 1: Create
        var book = new BookModel { Title = "Initial Title", Price = 1000, Uuid = Guid.NewGuid().ToString() };
        var createJson = KintoneRequestBuilder.BuildCreateJson([book]);
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task CanUpdateByKey() {
        var api = KintoneTestHelper.CreateApi();

        // UUID を生成
        var uuid = Guid.NewGuid().ToString();

        // ① レコード作成（UUID を含む）
        var book = new BookModel {
            Title = "Original Title",
            Price = 1000,
            Uuid = uuid
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson([book]);
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task FindByQueryAsyncShouldReturnFilteredRecords() {
        var api = KintoneTestHelper.CreateApi();
        // Arrange
        var books = new List<BookModel> {
            new() { Title = "Book A", Price = 1000, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "Book B", Price = 1500, Uuid = Guid.NewGuid().ToString() },
            new() { Title = "Book C", Price = 2000, Uuid = Guid.NewGuid().ToString() },
        };

        // 1. レコード登録
        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task FindAllAsyncCursorPagingWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
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
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Fact]
    public async Task FindAsyncQueryExceedsPageSizeWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
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
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)] // カーソル使用
    [InlineData(2, 5)] // カーソル不使用
    public async Task FindAsyncQueryWithOrderByWorksCorrectly(int recordCount, int pageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
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
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)] // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsyncQueryWithComplexConditionWorksCorrectly(int recordCount, int pageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
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
        var createResult = await api.CreateAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        // Act
        string query = "Title like \"ブック\" and Price < 1300";
        var foundRecords = await KintoneTestHelper.WaitForExpectedRecordCountAsync<BookModel>(api, query, 2);

        // Assert
        Assert.NotNull(foundRecords);
        Assert.Equal(2, foundRecords.Count);

        // Cleanup
        var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)]   // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)]  // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsyncQueryWithNoHitWorksCorrectly(int recordCount, int pageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"NoHitBook{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }
        ).ToList();

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)]   // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)]  // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsyncWithInvalidQueryThrowsException(int recordCount, int pageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"InvalidQueryBook{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }).ToList();

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)]   // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)]  // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsyncRepeatedQueryReturnsSameResults(int recordCount, int pageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"RepeatedQueryBook{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }).ToList();

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsyncEmptyQueryReturnsAllRecords(int recordCount, int pageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = pageSize;

        var books = Enumerable.Range(1, recordCount).Select(i =>
            new BookModel {
                Title = $"ブック{i:00}",
                Price = 1000 + i * 100,
                Uuid = Guid.NewGuid().ToString()
            }).ToList();

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
        var deleteResult = await api.DeleteAsync(deleteJson);
        Assert.NotNull(deleteResult);
    }
    [Theory]
    [InlineData(500, 100)]  // カーソル使用（pageSize < recordCount）
    [InlineData(499, 500)]  // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsyncOverMaxLimitWorksCorrectly(int recordCount, int pageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
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
            var createResult = await api.CreateAsync(createJson);
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
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用：recordCount = 4, pageSize = 2 → カーソル使用される
    [InlineData(5, 10)] // カーソル不使用：recordCount = 4, pageSize = 10 → 1ページで済むためカーソル不要
    public async Task FindAsyncTitleInConditionWorksCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック01", Price = 1000, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック02", Price = 1500, Classification = "雑誌" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック03", Price = 2000, Classification = "SF" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック04", Price = 2500, Classification = "雑誌" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用：レコード数 > pageSize
    [InlineData(5, 10)] // カーソル不使用：pageSize >= レコード数
    public async Task FindAsyncClassificationInConditionWorksCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "雑誌A", Price = 1000, Classification = "雑誌" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "技術書B", Price = 1500, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "SF C", Price = 2000, Classification = "SF" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "技術書D", Price = 2500, Classification = "技術書" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用：レコード数 > pageSize
    [InlineData(5, 10)] // カーソル不使用：pageSize >= レコード数
    public async Task FindAsyncPriceNotEqualConditionWorksCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブックA", Price = 1200, Classification = "雑誌" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブックB", Price = 1000, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブックC", Price = 1500, Classification = "SF" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "ブックD", Price = 1200, Classification = "雑誌" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
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
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task FindAsyncTitleNotEmptyConditionSwapTitlesUpdatesCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel>
        {
        new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック01", Price = 1000, Classification = "技術書" },
        new() { Uuid = Guid.NewGuid().ToString(), Title = "",         Price = 1500, Classification = "雑誌" },
        new() { Uuid = Guid.NewGuid().ToString(), Title = "ブック03", Price = 2000, Classification = "SF" },
        new() { Uuid = Guid.NewGuid().ToString(), Title = "",         Price = 2500, Classification = "雑誌" },
    };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Step 1: 初回検索
            var condition = "Title != \"\"";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            var foundTitlesBefore = foundRecords.Select(r => r.Title).ToList();
            Assert.Contains("ブック01", foundTitlesBefore);
            Assert.Contains("ブック03", foundTitlesBefore);
            Assert.Equal(2, foundRecords.Count);

            // Step 2: Title を入れ替え
            books[0].Title = "";            // ブック01 → 空に
            books[1].Title = "タイトルA";    // 空 → タイトルAに
            books[2].Title = "";            // ブック03 → 空に
            books[3].Title = "タイトルB";    // 空 → タイトルBに

            var updateJson = KintoneRequestBuilder.BuildUpdateJson(books);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Step 3: 再検索
            var foundJsonAfterUpdate = await api.FindByQueryAsync<BookModel>(condition);
            var updatedFoundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJsonAfterUpdate);

            var foundTitlesAfter = updatedFoundRecords.Select(r => r.Title).ToList();
            Assert.Contains("タイトルA", foundTitlesAfter);
            Assert.Contains("タイトルB", foundTitlesAfter);
            Assert.DoesNotContain("ブック01", foundTitlesAfter);
            Assert.DoesNotContain("ブック03", foundTitlesAfter);
            Assert.Equal(2, updatedFoundRecords.Count);
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task CreateUpdateAndFindPriceGreaterThanOrEqualZeroHandlesNullsCorrectly(int cursorPageSize, int dummyPageSize) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = cursorPageSize;

        var books = new List<BookModel> {
            new() { Uuid = Guid.NewGuid().ToString(), Title = "価格未設定01", Price = null, Classification = "技術書" },
            new() { Uuid = Guid.NewGuid().ToString(), Title = "価格未設定02", Price = null, Classification = "雑誌" },
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Step 1: Price >= 0 の検索（未設定なのでヒットしない）
            var condition = "Price >= 0";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            Assert.NotNull(foundRecords);
            Assert.Empty(foundRecords);

            // Step 2: Price を設定して更新（1以上に）
            foreach (var book in books) {
                book.Price = 1200;
            }

            var updateJson = KintoneRequestBuilder.BuildUpdateJson(books);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Step 3: 再検索して、更新が反映されているか確認
            var foundJsonAfterUpdate = await api.FindByQueryAsync<BookModel>(condition);
            var updatedFoundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJsonAfterUpdate);

            var titles = updatedFoundRecords.Select(b => b.Title).ToList();
            Assert.Contains("価格未設定01", titles);
            Assert.Contains("価格未設定02", titles);
            Assert.Equal(2, updatedFoundRecords.Count);
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData(5, 2)]  // カーソル使用（pageSize < recordCount）
    [InlineData(5, 10)] // カーソル不使用（pageSize >= recordCount）
    public async Task CreateUpdateAndFindReleaseDateConditionWorksCorrectly(int pageSize, int recordCount) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        api.CursorPageSize = pageSize;

        var books = new List<BookModel> {
            new() {
                Uuid = Guid.NewGuid().ToString(),
                Title = "ブックA",
                Price = 1000,
                Classification = "技術書",
                ReleaseDate = new KintoneDateTime(new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc)) // 条件外
            },
            new() {
                Uuid = Guid.NewGuid().ToString(),
                Title = "ブックB",
                Price = 1500,
                Classification = "雑誌",
                ReleaseDate = new KintoneDateTime(new DateTime(2024, 01, 02, 10, 00, 00, DateTimeKind.Utc)) // 条件内
            },
            new() {
                Uuid = Guid.NewGuid().ToString(),
                Title = "ブックC",
                Price = 2000,
                Classification = "SF",
                ReleaseDate = new KintoneDateTime(new DateTime(2025, 01, 01, 00, 00, 00, DateTimeKind.Utc)) // 条件内
            }
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Step 1: 条件検索（作成直後）
            var condition = "ReleaseDate > \"2024-01-01T00:00:00Z\"";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            var titles = foundRecords.Select(r => r.Title).ToList();
            Assert.Contains("ブックB", titles);
            Assert.Contains("ブックC", titles);
            Assert.DoesNotContain("ブックA", titles);
            Assert.Equal(2, foundRecords.Count);

            // Step 2: 更新処理（"ブックA"のReleaseDateを条件に合うよう変更）
            var bookA = books.First(b => b.Title == "ブックA");
            bookA.ReleaseDate = new KintoneDateTime(new DateTime(2026, 01, 01, 00, 00, 00, DateTimeKind.Utc)); // 条件内に変更

            var updateJson = KintoneRequestBuilder.BuildUpdateJson([bookA]);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Step 3: 再検索して更新後の結果を確認
            var foundJsonAfterUpdate = await api.FindByQueryAsync<BookModel>(condition);
            var updatedFoundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJsonAfterUpdate);

            var updatedTitles = updatedFoundRecords.Select(r => r.Title).ToList();
            Assert.Contains("ブックA", updatedTitles); // 追加で見つかるようになる
            Assert.Contains("ブックB", updatedTitles);
            Assert.Contains("ブックC", updatedTitles);
            Assert.Equal(3, updatedFoundRecords.Count);

        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Theory]
    [InlineData("5")] // 強く勧めたい
    [InlineData("3")] // どちらでもない
    [InlineData("1")] // まったく勧めない
    public async Task CreateUpdateAndFindRadioButtonFieldWorksCorrectly(string recommendation) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        var model = new BookModel {
            Title = $"おすすめ度テスト_{recommendation}",
            Price = 1800,
            Uuid = Guid.NewGuid().ToString(),
            Classification = "SF",
            ReleaseDate = new KintoneDateTime(DateTime.Today),
            Recommendation = recommendation
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        Assert.NotNull(created);
        var createdModel = created!.First();

        try {
            // Step 1: 検索（登録直後）
            var query = $"UUID = \"{model.Uuid}\"";
            var json = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(json);

            Assert.Single(results);
            var found = results[0];
            Assert.Equal(model.Recommendation, found.Recommendation);

            // Step 2: 更新（U） - Recommendation を別の値に変更
            var updatedValue = recommendation == "5" ? "1" : "5";
            found.Recommendation = updatedValue;

            var updateJson = KintoneRequestBuilder.BuildUpdateJson([found]);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Step 3: 再検索して更新内容を確認
            var jsonAfterUpdate = await api.FindByQueryAsync<BookModel>(query);
            var updatedResults = KintoneResponseParser.ParseRecords<BookModel>(jsonAfterUpdate);
            Assert.Single(updatedResults);
            var updated = updatedResults[0];

            Assert.Equal(updatedValue, updated.Recommendation);
        } finally {
            // Cleanup
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, created);
        }
    }
    [Theory]
    [MemberData(nameof(CheckBoxTestData))]
    public async Task CreateUpdateAndFindCheckBoxFieldWorksCorrectly(string[] selections) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        var model = new BookModel {
            Title = "チェックボックステスト",
            Price = 2000,
            Uuid = Guid.NewGuid().ToString(),
            Classification = "技術書",
            ReleaseDate = new KintoneDateTime(DateTime.Today),
            CheckBoxes = selections
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        Assert.NotNull(created);
        var createdModel = created!.First();

        try {
            // Step 1: 検索（登録直後）
            var query = $"UUID = \"{model.Uuid}\"";
            var json = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(json);

            Assert.Single(results);
            var found = results[0];

            Assert.Equal(model.CheckBoxes.OrderBy(x => x), found.CheckBoxes.OrderBy(x => x));

            // Step 2: 更新（U） - チェックボックスの内容を変更
            var updatedSelections = new[] { "チェック1", "チェック2", "チェック3" };
            found.CheckBoxes = updatedSelections;

            var updateJson = KintoneRequestBuilder.BuildUpdateJson([found]);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Step 3: 再検索して更新内容を確認
            var jsonAfterUpdate = await api.FindByQueryAsync<BookModel>(query);
            var updatedResults = KintoneResponseParser.ParseRecords<BookModel>(jsonAfterUpdate);
            Assert.Single(updatedResults);
            var updated = updatedResults[0];

            Assert.Equal(updatedSelections.OrderBy(x => x), updated.CheckBoxes.OrderBy(x => x));
        } finally {
            // Cleanup
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, created);
        }
    }
    [Theory]
    [MemberData(nameof(LinkFieldTestData))]
    public async Task CreateUpdateAndFindLinkFieldsWorksCorrectly(string webAddress, string telephone, string email) {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        var model = new BookModel {
            Title = "リンク型フィールドテスト",
            Price = 3000,
            Uuid = Guid.NewGuid().ToString(),
            Classification = "技術書",
            ReleaseDate = new KintoneDateTime(DateTime.Today),
            WebAddress = webAddress,
            Telephone = telephone,
            Email = email
        };

        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        Assert.NotNull(created);
        var createdModel = created!.First();

        try {
            // Step 1: 検索（登録直後）
            var query = $"UUID = \"{model.Uuid}\"";
            var json = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(json);
            Assert.Single(results);
            var found = results[0];

            Assert.Equal(model.WebAddress, found.WebAddress);
            Assert.Equal(model.Telephone, found.Telephone);
            Assert.Equal(model.Email, found.Email);

            // Step 2: 更新（U）
            found.WebAddress = "https://example.com/updated";
            found.Telephone = "080-9999-9999";
            found.Email = "updated@example.com";

            var updateJson = KintoneRequestBuilder.BuildUpdateJson([found]);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Step 3: 再検索して更新内容を確認
            var jsonAfterUpdate = await api.FindByQueryAsync<BookModel>(query);
            var updatedResults = KintoneResponseParser.ParseRecords<BookModel>(jsonAfterUpdate);
            Assert.Single(updatedResults);
            var updated = updatedResults[0];

            Assert.Equal("https://example.com/updated", updated.WebAddress);
            Assert.Equal("080-9999-9999", updated.Telephone);
            Assert.Equal("updated@example.com", updated.Email);
        } finally {
            // Cleanup
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, created);
        }
    }
    [Fact]
    public async Task CreateAndFindAsyncWithDateAndTimeFieldsWorksCorrectly() {
        // Arrange
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var time = TimeOnly.FromDateTime(now).TruncateToMinute();

        var model = new BookModel {
            Title = "日時テスト",
            Uuid = Guid.NewGuid().ToString(),
            DateField = new KintoneDateTime(today),
            TimeField = new KintoneTimeOnly(time),
        };

        var api = KintoneTestHelper.CreateApi();

        // Act
        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        Assert.NotNull(created);
        Assert.Equal(today, created!.First().DateField.DateOnly);
        Assert.Equal(time, created.First().TimeField.Value);

        try {
            // Step 1: 検索（登録直後）
            var query = $"DateField = \"{today:yyyy-MM-dd}\" and TimeField = \"{time:HH\\:mm}\"";
            var found = await api.FindByQueryAsync<BookModel>(query);
            var results = KintoneResponseParser.ParseRecords<BookModel>(found);
            var match = results.FirstOrDefault(b => b.ID == created.First().ID);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(today, match!.DateField.DateOnly);
            Assert.Equal(time, match.TimeField.Value);

            // Step 2: 更新（U）
            var updatedDate = today.AddDays(1);
            var updatedTime = time.AddMinutes(30);
            match.DateField = new KintoneDateTime(updatedDate);
            match.TimeField = new KintoneTimeOnly(updatedTime);

            var updateJson = KintoneRequestBuilder.BuildUpdateJson([match]);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Step 3: 再確認（R after U）
            var queryAfterUpdate = $"DateField = \"{updatedDate:yyyy-MM-dd}\" and TimeField = \"{updatedTime:HH\\:mm}\"";
            var foundAfterUpdate = await api.FindByQueryAsync<BookModel>(queryAfterUpdate);
            var updatedResults = KintoneResponseParser.ParseRecords<BookModel>(foundAfterUpdate);
            var updatedMatch = updatedResults.FirstOrDefault(b => b.ID == match.ID);

            Assert.NotNull(updatedMatch);
            Assert.Equal(updatedDate, updatedMatch!.DateField.DateOnly);
            Assert.Equal(updatedTime, updatedMatch.TimeField.Value);

        } finally {
            // Cleanup（D）
            await KintoneTestHelper.DeleteRecordsInChunksAsync(api, created);
        }
    }
    [Fact]
    public async Task CreateAndUpdateAsyncWithMultiSelectWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();

        var book = new BookModel {
            Uuid = Guid.NewGuid().ToString(),
            Title = "複数選択テスト",
            Price = 1200,
            Classification = "技術書",
            MultiSelector = ["選択肢1", "選択肢3"]
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson([book]);
        var createResult = await api.CreateAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords([book], createResult);

        try {
            // 検索して作成結果を検証
            var condition = $"UUID = \"{book.Uuid}\"";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var found = KintoneResponseParser.ParseRecords<BookModel>(foundJson).FirstOrDefault();

            Assert.NotNull(found);
            Assert.Equal("複数選択テスト", found.Title);
            Assert.Contains("選択肢1", found.MultiSelector);
            Assert.Contains("選択肢3", found.MultiSelector);
            Assert.Equal(2, found.MultiSelector.Count());

            // 更新：MultiSelector の内容を変更
            found.MultiSelector = ["選択肢2", "選択肢4", "選択肢5"];

            var updateJson = KintoneRequestBuilder.BuildUpdateJson([found]);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // 再取得して変更確認
            var foundJsonAfterUpdate = await api.FindByQueryAsync<BookModel>(condition);
            var updated = KintoneResponseParser.ParseRecords<BookModel>(foundJsonAfterUpdate).FirstOrDefault();

            Assert.NotNull(updated);
            Assert.DoesNotContain("選択肢1", updated.MultiSelector);
            Assert.Contains("選択肢2", updated.MultiSelector);
            Assert.Contains("選択肢4", updated.MultiSelector);
            Assert.Contains("選択肢5", updated.MultiSelector);
            Assert.Equal(3, updated.MultiSelector.Count());
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Fact]
    public async Task CreateAndUpdateAsyncWithMultiSelectAddAndRemoveWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();

        var bookWithSelector = new BookModel {
            Uuid = Guid.NewGuid().ToString(),
            Title = "複数選択あり",
            Price = 1000,
            Classification = "技術書",
            MultiSelector = ["選択肢1", "選択肢3"]
        };

        var bookWithoutSelector = new BookModel {
            Uuid = Guid.NewGuid().ToString(),
            Title = "複数選択なし",
            Price = 1500,
            Classification = "雑誌",
            MultiSelector = [] // 空
        };

        var books = new[] { bookWithSelector, bookWithoutSelector };

        var createJson = KintoneRequestBuilder.BuildCreateJson(books);
        var createResult = await api.CreateAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords(books, createResult);

        try {
            // Act 1: 登録後に確認
            var condition = $"UUID in (\"{bookWithSelector.Uuid}\", \"{bookWithoutSelector.Uuid}\")";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);

            var foundWithSelector = foundRecords.First(r => r.Uuid == bookWithSelector.Uuid);
            var foundWithoutSelector = foundRecords.First(r => r.Uuid == bookWithoutSelector.Uuid);

            Assert.Equal(2, foundRecords.Count);
            Assert.Equal(2, foundWithSelector.MultiSelector.Count());
            Assert.Empty(foundWithoutSelector.MultiSelector);

            // Act 2: 値を入れ替え（あり→なし、なし→あり）
            foundWithSelector.MultiSelector = []; // 選択肢をすべて削除
            foundWithoutSelector.MultiSelector = ["選択肢2", "選択肢4"]; // 選択肢を追加

            var updateJson = KintoneRequestBuilder.BuildUpdateJson([foundWithSelector, foundWithoutSelector]);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Act 3: 再取得して更新後の値を確認
            var updatedJson = await api.FindByQueryAsync<BookModel>(condition);
            var updatedRecords = KintoneResponseParser.ParseRecords<BookModel>(updatedJson);

            var updatedWithSelector = updatedRecords.First(r => r.Uuid == bookWithSelector.Uuid);
            var updatedWithoutSelector = updatedRecords.First(r => r.Uuid == bookWithoutSelector.Uuid);

            // Assert
            Assert.Empty(updatedWithSelector.MultiSelector); // 選択肢が削除されている
            Assert.Contains("選択肢2", updatedWithoutSelector.MultiSelector);
            Assert.Contains("選択肢4", updatedWithoutSelector.MultiSelector);
            Assert.Equal(2, updatedWithoutSelector.MultiSelector.Count());
        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Fact]
    public async Task CreateAndUpdateAsyncWithSubTableWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();

        var book = new BookModel {
            Uuid = Guid.NewGuid().ToString(),
            Title = "サブテーブルあり",
            Price = 1200,
            Classification = "SF",
            Details = [
                new() {
                    No = 1,
                    StoreName = "秋葉原店",
                    DeliveryDate = new KintoneDateTime(DateTime.Today.AddDays(1)),
                    PackageType = ["梱包する"]
                },
                new() {
                    No = 2,
                    StoreName = "渋谷店",
                    DeliveryDate = new KintoneDateTime(DateTime.Today.AddDays(2)),
                    PackageType = []
                },
            ]
        };

        var createJson = KintoneRequestBuilder.BuildCreateJson([book]);
        var createResult = await api.CreateAsync(createJson);
        var createdRecords = KintoneResponseParser.ParseCreatedRecords([book], createResult);

        try {
            // Act 1: 登録後に検索し、値を確認
            var condition = $"UUID = \"{book.Uuid}\"";
            var foundJson = await api.FindByQueryAsync<BookModel>(condition);
            var foundRecords = KintoneResponseParser.ParseRecords<BookModel>(foundJson);
            var found = foundRecords.Single();

            Assert.Equal(book.Uuid, found.Uuid);
            Assert.NotNull(found.Details);
            Assert.Equal(2, found.Details.Count());

            var firstDetail = found.Details.First();
            Assert.Equal("秋葉原店", firstDetail.StoreName);

            // Act 2: サブテーブルの1件を更新・1件を削除
            var updatedDetails = found.Details.ToList();
            updatedDetails.RemoveAt(1); // 渋谷店を削除
            updatedDetails[0].StoreName = "中野店"; // 秋葉原店→中野店に変更
            updatedDetails.Add(new BookModelDetail {
                No = 3,
                StoreName = "新宿店",
                DeliveryDate = new KintoneDateTime(DateTime.Today.AddDays(3)),
                PackageType = new[] { "梱包する" }
            }); // 新規追加

            found.Details = updatedDetails;

            var updateJson = KintoneRequestBuilder.BuildUpdateJson([found]);
            var updateResult = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResult);

            // Act 3: 再取得して更新結果を確認
            var updatedJson = await api.FindByQueryAsync<BookModel>(condition);
            var updatedRecords = KintoneResponseParser.ParseRecords<BookModel>(updatedJson);
            var updated = updatedRecords.Single();

            Assert.NotNull(updated.Details);
            var detailList = updated.Details.ToList();
            Assert.Equal(2, detailList.Count); // 2件に
            Assert.Contains(detailList, d => d.StoreName == "中野店");
            Assert.Contains(detailList, d => d.StoreName == "新宿店");

        } finally {
            // Clean up
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(createdRecords);
            var deleteResult = await api.DeleteAsync(deleteJson);
            Assert.NotNull(deleteResult);
        }
    }
    [Fact]
    public async Task CreateAndUpdateAsyncWithRichTextFieldWorksCorrectly() {
        // Arrange
        var api = KintoneTestHelper.CreateApi();
        var uuid = Guid.NewGuid().ToString();

        var richTextInitial = "<p><b>初期レビュー</b>：内容は <i>充実</i> していた。</p>";
        var richTextUpdated = "<h3>更新済みレビュー</h3><ul><li>良かった点</li><li>改善点</li></ul><script>alert('XSS');</script>";

        var model = new BookModel {
            Title = "RichTextフィールドテスト",
            Uuid = uuid,
            RichText = richTextInitial
        };

        // Act
        var created = await KintoneTestHelper.CreateRecordsInChunksAsync(api, [model]);
        Assert.NotNull(created);
        Assert.Equal(richTextInitial, created!.First().RichText);

        try {
            // Step 1: 検索直後に一致確認（R）
            var query = $"UUID = \"{uuid}\"";
            var foundJson = await api.FindByQueryAsync<BookModel>(query);
            var found = KintoneResponseParser.ParseRecords<BookModel>(foundJson);
            var match = found.FirstOrDefault(b => b.ID == created.First().ID);

            Assert.NotNull(match);
            var actual = WebUtility.HtmlDecode(match!.RichText);
            Assert.Equal(richTextInitial, actual);

            // Step 2: 更新（U）
            match.RichText = richTextUpdated;
            var updateJson = KintoneRequestBuilder.BuildUpdateJson([match]);
            var updateResultJson = await api.UpdateAsync<BookModel>(updateJson);
            Assert.NotNull(updateResultJson);

            // Step 3: 更新結果の再取得（R after U）
            var afterUpdateJson = await api.FindByQueryAsync<BookModel>(query);
            var updatedRecords = KintoneResponseParser.ParseRecords<BookModel>(afterUpdateJson);
            var updatedMatch = updatedRecords.FirstOrDefault(b => b.ID == match.ID);

            Assert.NotNull(updatedMatch);
            var updatedActual = WebUtility.HtmlDecode(updatedMatch!.RichText);
            // updatedActualはrichTextUpdatedから危険なスクリプトが削除されているため。StartWithの値を入れ替えている
            Assert.StartsWith(updatedActual, richTextUpdated);

            // Optional Step: セキュリティ的に <script> 要素が保持されたか確認
            Assert.DoesNotContain("<script>", updatedMatch.RichText, StringComparison.OrdinalIgnoreCase);

        } finally {
            // Cleanup（D）
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

public static class KintoneFieldTestCases {
    public static readonly Dictionary<string, KintoneFieldType> FieldMap = new() {
        { "UserSelect", KintoneFieldType.UserSelect },
        { "GroupSelect", KintoneFieldType.GroupSelect },
        { "DivisionSelect", KintoneFieldType.OrganizationSelect }
    };
}
public class UserSelectionFieldTests {
    [Theory]
    [InlineData("UserSelect")]
    [InlineData("GroupSelect")]
    [InlineData("DivisionSelect")]
    public void ConvertToCSharpUserSelectionFieldsWorkCorrectly(string fieldName) {
        // Arrange
        var fieldType = KintoneFieldTestCases.FieldMap[fieldName];

        var json = $$"""
        {
          "{{fieldName}}": {
            "value": [
              { "code": "test_code_001", "name": "テスト表示A" },
              { "code": "test_code_002", "name": "テスト表示B" }
            ]
          }
        }
        """;

        var root = JsonDocument.Parse(json).RootElement;
        var valueElement = root.GetProperty(fieldName).GetProperty("value");

        // Act
        var result = KintoneValueConverter.ConvertToCSharp(valueElement, fieldType, typeof(List<KintoneUser>)) as List<KintoneUser>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("test_code_001", result[0].Code);
        Assert.Equal("テスト表示B", result[1].Name);
    }
}

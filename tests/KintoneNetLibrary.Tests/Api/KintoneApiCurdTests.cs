using Xunit;
using KintoneNetLibrary.Api;
using KintoneNetLibrary.Tests.Models;
using KintoneNetLibrary.Model;

namespace KintoneNetLibrary.Tests.Api;

public class KintoneApiCrudTests {
    private KintoneApi CreateApi() {
        var cfg = TestEnv.Settings;
        var cli = new HttpClient {
            BaseAddress = new Uri($"https://{cfg.Domain}/k/v1/")
        };
        return new KintoneApi(cli, cfg.ApiToken, cfg.AppID, cfg.Domain);
    }

    [Fact]
    public async Task Create_Find_Delete_Flow() {
        var api = this.CreateApi();
        var book = new BookModel {
            Title = "xUnit Guide",
            Price = 3000
        };

        /* ---- Create ---- */
        var idx = await api.CreateAsync([book]);
        Assert.NotEmpty(idx.IDs);
        var id = idx.IDs[0];

        /* ---- Find ---- */
        var stored = await api.FindByIDAsync<BookModel>(id);
        Assert.Equal("xUnit Guide", stored?.Title);

        /* ---- Delete ---- */
        var actual = await api.DeleteAsync<BookModel>([id]);
        Assert.Single(actual);
    }
    [Fact]
    public async Task FindTest() {
        var api = this.CreateApi();
        var id = "13";
        var actual = await api.FindByIDAsync<BookModel>(id);
        Assert.Equal("xUnit Guide", actual?.Title);
    }
    [Fact]
    public async Task CreateMultiple_Find_Delete_Flow() {
        var api = this.CreateApi();

        var books = new List<BookModel> {
            new() { Title = "xUnit Guide", Price = 3000 },
            new() { Title = "Clean Code", Price = 4500 },
            new() { Title = "Domain-Driven Design", Price = 6000 },
        };

        /* ---- Create (複数) ---- */
        var idx = await api.CreateAsync(books);
        Assert.Equal(3, idx.IDs.Count);

        /* ---- Find by ID (それぞれ) ---- */
        var storedBooks = new List<BookModel>();
        foreach (var id in idx.IDs) {
            var book = await api.FindByIDAsync<BookModel>(id);
            Assert.NotNull(book);
            storedBooks.Add(book!);
        }

        // タイトルを確認（順番は保証されないので Set で検証）
        var expectedTitles = books.Select(b => b.Title).ToHashSet();
        var actualTitles = storedBooks.Select(b => b.Title).ToHashSet();
        Assert.Equal(expectedTitles, actualTitles);

        /* ---- Delete (まとめて) ---- */
        var deletedIDs = await api.DeleteAsync<BookModel>(idx.IDs);
        Assert.Equal(3, deletedIDs.Count);
    }
    [Fact]
    public async Task CreateMultiple_FindByIDs_Delete_Flow() {
        var api = this.CreateApi();
        var books = new List<BookModel> {
            new() { Title = "C# in Depth", Price = 4500 },
            new() { Title = "Effective C#", Price = 4000 },
            new() { Title = "Pro .NET 9", Price = 5000 },
        };

        /* ---- Create ---- */
        var idx = await api.CreateAsync(books);
        Assert.Equal(3, idx.IDs.Count);
        var ids = idx.IDs;

        /* ---- FindByIDs ---- */
        var stored = await api.FindByIDsAsync<BookModel>(ids);
        Assert.Equal(3, stored.Count);

        // 検証：Titleが一致しているか
        var titles = stored.Select(b => b.Title).ToHashSet();
        Assert.Contains("C# in Depth", titles);
        Assert.Contains("Effective C#", titles);
        Assert.Contains("Pro .NET 9", titles);

        /* ---- Delete ---- */
        var deleted = await api.DeleteAsync<BookModel>(ids);
        Assert.Equal(3, deleted.Count);
    }
    [Fact]
    public async Task UpdateSingleRecord_ShouldModifyRecordCorrectly() {
        var api = this.CreateApi();
        var book = new BookModel {
            Title = "Before Update",
            Price = 1000
        };

        var created = await api.CreateAsync([book]);

        try {
            // Act: 値を変更して更新
            book.RecordID = created.IDs.First();
            book.Title = "After Update";
            book.Price = 2000;

            await api.UpdateAsync([book]); // または UpdateByIdAsync(created.ID, created)

            // Assert: 再取得して変更内容を検証
            var updated = await api.FindByIDAsync<BookModel>(book.RecordID);

            Assert.NotNull(updated);
            Assert.Equal("After Update", updated.Title);
            Assert.Equal(2000, updated.Price);
        } finally {
            // Cleanup: テストデータを削除
            if (!string.IsNullOrEmpty(book.RecordID)) {
                await api.DeleteAsync<BookModel>([book.RecordID]);
            }
        }
    }
    [Fact]
    public async Task UpdateMultipleRecords_ShouldUpdateSuccessfully() {
        // Arrange
        var api = CreateApi();
        var books = new List<BookModel>
        {
            new() { Title = "Batch Book 1", Price = 1000 },
            new() { Title = "Batch Book 2", Price = 2000 },
            new() { Title = "Batch Book 3", Price = 3000 }
        };

        // レコードを作成
        var createdIndexes = await api.CreateAsync(books);
        var ids = createdIndexes.IDs;

        // ID を各 BookModel に割り当て
        for (int i = 0; i < books.Count; i++) {
            books[i].RecordID = ids[i];
            books[i].Title += " (Updated)";
            books[i].Price += 500;
        }

        try {
            // Act: 複数レコードの更新
            var updateResult = await api.UpdateAsync(books);
            Assert.Equal(books.Count, updateResult.IDs.Count);

            // Assert: 更新後のデータを取得し、内容を検証
            var updatedBooks = await api.FindByIDsAsync<BookModel>(ids);

            Assert.Equal(books.Count, updatedBooks.Count);
            for (int i = 0; i < books.Count; i++) {
                Assert.Equal(books[i].RecordID, updatedBooks[i].RecordID);
                Assert.Equal(books[i].Title, updatedBooks[i].Title);
                Assert.Equal(books[i].Price, updatedBooks[i].Price);
            }
        } finally {
            // Cleanup: 作成したレコードを削除
            await api.DeleteAsync<BookModel>(ids);
        }
    }

}

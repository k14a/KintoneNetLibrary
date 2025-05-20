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
        var ok = await api.DeleteAsync<BookModel>([id]);
        Assert.True(ok);
    }
    [Fact]
    public async Task FindTest() {
        var api = this.CreateApi();
        var id = "13";
        var actual = await api.FindByIDAsync<BookModel>(id);
        Assert.Equal("xUnit Guidl", actual?.Title);
    }
}

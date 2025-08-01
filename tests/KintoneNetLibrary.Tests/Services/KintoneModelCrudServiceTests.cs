using System.Net;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Tests.Helpers;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

public class KintoneModelCrudServiceTests {
    #region <<Test methods>>
    [Fact]
    public async Task CreateAndUpdateAsync_WithRichTextField_WorksCorrectly_UsingModelCrudService() {
        var uuid = Guid.NewGuid().ToString();
        var richTextInitial = "<p><b>初期レビュー</b>：内容は <i>充実</i> していた。</p>";
        var richTextUpdated = "<h3>更新済みレビュー</h3><ul><li>良かった点</li><li>改善点</li></ul><script>alert('XSS');</script>";

        var model = new BookModel {
            Title = "RichTextフィールドテスト",
            Uuid = uuid,
            RichText = richTextInitial,
            Access = new ApiTokenAccess(TestEnv.Settings.Domain, TestEnv.Settings.ApiToken)
        };

        var service = KintoneTestHelper.CreateCrudService();

        // --- C: Create ---
        var createResult = await service.CreateAsync([model]);
        var created = createResult.Succeeded.FirstOrDefault();

        Assert.NotNull(created);
        Assert.Equal(richTextInitial, created.RichText);

        try {
            // --- R: Find after create ---
            var query = $"UUID = \"{uuid}\"";
            var foundList = (await service.FindAsync<BookModel>(query: query)).ToList();

            var match = foundList.FirstOrDefault(b => b.RecordID == created.RecordID);
            Assert.NotNull(match);

            var actual = WebUtility.HtmlDecode(match!.RichText);
            Assert.Equal(richTextInitial, actual);

            // --- U: Update ---
            match.RichText = richTextUpdated;
            var updateResult = await service.UpdateAsync([match]);
            Assert.Single(updateResult.Succeeded);

            // --- R: Find after update ---
            var updatedList = (await service.FindAsync<BookModel>(query: query)).ToList();
            var updatedMatch = updatedList.FirstOrDefault(b => b.RecordID == match.RecordID);

            Assert.NotNull(updatedMatch);
            var updatedActual = WebUtility.HtmlDecode(updatedMatch!.RichText);
            Assert.StartsWith(updatedActual, richTextUpdated);
            Assert.DoesNotContain("<script>", updatedMatch.RichText, StringComparison.OrdinalIgnoreCase);

        } finally {
            // --- D: Delete ---
            await service.DeleteAsync([created]);
        }
    }

    #endregion
}
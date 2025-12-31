using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Infrastructure.Internal;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Infrastructure.Api;

public class KintoneAppMetadataApi : BaseKintoneApi, IKintoneAppMetadataApi {
    public KintoneAppMetadataApi(
        KintoneAccessBase access,
        HttpClient httpClient,
        ILogger<KintoneAppMetadataApi>? logger = null)
        : base(access, httpClient, logger) { }

    public async Task<string> GetFieldsJsonAsync(int appId) {
        var uri = this.BuildRequestUri(KintoneApiEndpoints.GetAppFields, $"app={appId}");
        return await this.SendGetAsync(uri);
    }

    public async Task<string> GetLayoutJsonAsync(int appId) {
        var uri = this.BuildRequestUri(KintoneApiEndpoints.GetAppLayout, $"app={appId}");
        return await this.SendGetAsync(uri);
    }
}

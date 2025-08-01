using System.Net.Http.Headers;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Access;

public class ApiTokenAccess : KintoneAccessBase {
    public ApiTokenAccess(string domain, string apiToken, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        Domain = domain;
        ApiToken = apiToken;
        BasicAuthUser = basicAuthUser;
        BasicAuthPassword = basicAuthPassword;
        GuestSpaceId = guestSpaceId;
        AuthType = KintoneAuthType.ApiToken;
    }

    public override KintoneAccount ToKintoneAccount() => new() {
        Domain = Domain,
        ApiToken = ApiToken,
        BasicAuthUser = BasicAuthUser,
        BasicAuthPassword = BasicAuthPassword,
        GuestSpaceId = GuestSpaceId
    };

    public override void ApplyAuthentication(HttpRequestMessage request) {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(this.ApiToken)) {
            request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
        }
        // 他にも必要なヘッダーを設定
    }
}

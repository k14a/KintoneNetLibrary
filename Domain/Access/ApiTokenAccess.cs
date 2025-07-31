using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Access;

public class ApiTokenAccess : KintoneAccessBase {
    public string Domain { get; }
    public string ApiToken { get; }
    public int AppID { get; }
    public string BasicAuthUser { get; }
    public string BasicAuthPassword { get; }
    public int GuestSpaceId { get; }

    public ApiTokenAccess( string domain, string apiToken, int appID, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        Domain = domain;
        ApiToken = apiToken;
        AppID = appID;
        BasicAuthUser = basicAuthUser;
        BasicAuthPassword = basicAuthPassword;
        GuestSpaceId = guestSpaceId;
    }

    public override KintoneAccount ToKintoneAccount() => new KintoneAccount {
        Domain = Domain,
        ApiToken = ApiToken,
        BasicAuthUser = BasicAuthUser,
        BasicAuthPassword = BasicAuthPassword,
        GuestSpaceId = GuestSpaceId
    };

    protected override int ExtractAppID() => AppID;
    protected override int ExtractGuestAppID() => AppID;
}

using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Access;

public class UserPasswordAccess : KintoneAccessBase {
    public string Domain { get; }
    public string LoginName { get; }
    public string Password { get; }
    public int AppID { get; }
    public string BasicAuthUser { get; }
    public string BasicAuthPassword { get; }
    public int GuestSpaceId { get; }

    public UserPasswordAccess( string domain, string loginName, string password, int appID, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        Domain = domain;
        LoginName = loginName;
        Password = password;
        AppID = appID;
        BasicAuthUser = basicAuthUser;
        BasicAuthPassword = basicAuthPassword;
        GuestSpaceId = guestSpaceId;
    }

    public override KintoneAccount ToKintoneAccount() => new KintoneAccount {
        Domain = Domain,
        LoginName = LoginName,
        Password = Password,
        BasicAuthUser = BasicAuthUser,
        BasicAuthPassword = BasicAuthPassword,
        GuestSpaceId = GuestSpaceId
    };

    protected override int ExtractAppID() => AppID;
    protected override int ExtractGuestAppID() => AppID;
}

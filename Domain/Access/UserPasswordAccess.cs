using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Access;

public class UserPasswordAccess : KintoneAccessBase {
    public UserPasswordAccess(string domain, string loginName, string password, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        Domain = domain;
        LoginName = loginName;
        Password = password;
        BasicAuthUser = basicAuthUser;
        BasicAuthPassword = basicAuthPassword;
        GuestSpaceId = guestSpaceId;
        AuthType = KintoneAuthType.Login;
    }

    public override KintoneAccount ToKintoneAccount() => new KintoneAccount {
        Domain = Domain,
        LoginName = LoginName,
        Password = Password,
        BasicAuthUser = BasicAuthUser,
        BasicAuthPassword = BasicAuthPassword,
        GuestSpaceId = GuestSpaceId
    };

    public override void ApplyAuthentication(HttpRequestMessage request) {
        throw new NotImplementedException();
    }
}

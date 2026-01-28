using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;

namespace KintoneNetLibrary.Infrastructure.Factories;

public class KintoneAccessFactory : IKintoneAccessFactory {
    public ApiTokenAccess CreateApiTokenAccess(string domain, string apiToken, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        return new ApiTokenAccess(domain, apiToken, basicAuthUser, basicAuthPassword, guestSpaceId);
    }

    public UserPasswordAccess CreateBasicAuthAccess(string domain, string loginName, string password, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        throw new NotImplementedException();
    }
}
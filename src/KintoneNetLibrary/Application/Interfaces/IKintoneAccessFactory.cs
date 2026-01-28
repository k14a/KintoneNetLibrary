using KintoneNetLibrary.Domain.Access;

namespace KintoneNetLibrary.Application.Interfaces;

public interface IKintoneAccessFactory {
    ApiTokenAccess CreateApiTokenAccess(string domain, string apiToken, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0);
    UserPasswordAccess CreateBasicAuthAccess(string domain, string loginName, string password, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0);
}
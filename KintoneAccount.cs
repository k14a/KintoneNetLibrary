namespace KintoneNetLibrary;

public class KintoneAccount
{
    public string Domain { get; set; } = string.Empty;
    public string LoginName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string BasicAuthUser { get; set; } = string.Empty;
    public string BasicAuthPassword { get; set; } = string.Empty;
    public int GuestSpaceId { get; set; } = 0;

    public string GetLoginUrl()
    {
        var url = $"https://{Domain}/login";
        if (this.GuestSpaceId > 0)
        {
            url += $"/guest/{this.GuestSpaceId}";
        }
        return url;
    }
}

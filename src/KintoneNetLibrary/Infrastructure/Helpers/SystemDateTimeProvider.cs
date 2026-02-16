using KintoneNetLibrary.Infrastructure.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public class SystemDateTimeProvider : IDateTimeProvider {
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
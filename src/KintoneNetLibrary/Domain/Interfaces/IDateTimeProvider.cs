namespace KintoneNetLibrary.Infrastructure.Interfaces;

public interface IDateTimeProvider {
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
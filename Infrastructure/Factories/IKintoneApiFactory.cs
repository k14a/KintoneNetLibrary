using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Factories;

public interface IKintoneApiFactory
{
    KintoneApi CreateFromModel(KintoneModelBase model);
    KintoneApi Create(string domain, string apiToken);
}

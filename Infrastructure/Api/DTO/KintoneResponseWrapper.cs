using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Api.DTO {
    public class KintoneResponseWrapper<T> where T : KintoneModelBase, new() {
        public T Record { get; set; } = new T();
    }

    public class KintoneResponseListWrapper<T> where T : KintoneModelBase, new() {
        public List<T> Records { get; set; } = [];
    }
}
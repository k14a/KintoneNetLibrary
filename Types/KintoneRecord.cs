using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Types;

public class KintoneRecord
{
    [JsonPropertyName("record")]
    public Dictionary<string, KintoneField> Fields { get; set; }

    public KintoneRecord() {
        this.Fields = [];
    }

    public KintoneRecord(Dictionary<string, KintoneField> fields) {
        this.Fields = fields;
    }
}

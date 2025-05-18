using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary;

public class KintoneField
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public object Value { get; set; }

    public KintoneField() { }

    public KintoneField(string type, object value) {
        this.Type = type;
        this.Value = value;
    }
}
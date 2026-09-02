using System.Text.Json.Serialization;

namespace Company.Function;

// The Cosmos DB binding in the isolated worker serializes with System.Text.Json,
// so property names are mapped with [JsonPropertyName], not Newtonsoft's [JsonProperty].
public class Counter
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "index";

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

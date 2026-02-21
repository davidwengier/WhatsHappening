using System.Text.Json.Serialization;

namespace WhatsHappening.Services;

public sealed class FirebaseConfig
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("authDomain")]
    public string AuthDomain { get; set; } = "";

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = "";
}

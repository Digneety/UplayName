using Newtonsoft.Json;

namespace UbisoftName.Models;

public class UbisoftToken
{

    [JsonProperty("ticket")]
    public string? Ticket { get; set; }

    [JsonProperty("expiration")]
    public DateTime Expiration { get; set; }

    [JsonProperty("sessionId")]
    public string? SessionId { get; set; }

    public bool IsExpired => Expiration < DateTime.Now;
}
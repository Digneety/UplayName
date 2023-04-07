using Newtonsoft.Json;

namespace NameChecker.Models;

public class Token
{
    [JsonProperty("ticket")] public string? Ticket { get; set; }

    [JsonProperty("expiration")] public string? Expiration { get; set; }

    public bool IsExpired => DateTime.Parse(Expiration!) < DateTime.Now;
    
}
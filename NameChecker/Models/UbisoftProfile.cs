using Newtonsoft.Json;

namespace NameChecker.Models;

public struct Profile
{
    [JsonProperty("profileId")]
    public string ProfileId { get; set; }

    [JsonProperty("userId")]
    public string UserId { get; set; }

    [JsonProperty("platformType")]
    public string PlatformType { get; set; }

    [JsonProperty("idOnPlatform")]
    public string IdOnPlatform { get; set; }

    [JsonProperty("nameOnPlatform")]
    public string NameOnPlatform { get; set; }
}

public struct UbisoftProfile
{
    [JsonProperty("profiles")]
    public List<Profile> Profiles { get; set; }
}
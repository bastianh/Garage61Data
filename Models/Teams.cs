using Newtonsoft.Json;

namespace Garage61Data.APIClient
{
    public class Teams
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("slug")] public string Slug;
    }
}
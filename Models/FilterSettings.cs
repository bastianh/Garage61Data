using System.Collections.Generic;
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class FilterSettings
    {
        [JsonProperty("enabled")] public bool Enabled;
        [JsonProperty("follower")] public bool IncludeFollower;
        [JsonProperty("team")] public List<string> TeamSlugs ;
    }
}
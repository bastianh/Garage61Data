using System.Collections.Generic;
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class FilterSettings
    {
        [JsonProperty("enabled")] public bool Enabled;
        [JsonProperty("follower")] public bool IncludeFollower;
        [JsonProperty("yourself")] public bool IncludeYourself;
        [JsonProperty("team")] public List<string> TeamSlugs = new List<string>();

        public Dictionary<string, string> GetFilterParameters()
        {
            var parameters = new Dictionary<string, string>();
            if (!Enabled) return parameters;
            if (IncludeFollower && IncludeYourself) parameters.Add("drivers", "me,following");
            else if (IncludeFollower) parameters.Add("drivers", "following");
            else if (IncludeYourself) parameters.Add("drivers", "me");
            if (TeamSlugs.Count > 0) parameters.Add("teams", string.Join(",", TeamSlugs));
            return parameters;
        }
    }
}
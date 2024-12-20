using Garage61Data.APIClient;
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class UserInfo
    {
        [JsonProperty("firstName")] public string FirstName;
        [JsonProperty("id")] public string Id;
        [JsonProperty("lastName")] public string LastName;
        [JsonProperty("nickName")] public string NickName;
        [JsonProperty("slug")] public string Slug;
        [JsonProperty("teams")] public Teams[] Teams;
    }
}
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class PluginSettings
    {
        [JsonProperty("filter")] public FilterSettings FilterSettings = new FilterSettings();
    }
}
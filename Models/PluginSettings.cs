using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class PluginSettings
    {
        [JsonProperty("filter")] public FilterSettings FilterSettings = new FilterSettings();
        [JsonProperty("test_car_id")] public int TestCarId;
        [JsonProperty("test_track_id")] public int TestTrackId;
    }
}
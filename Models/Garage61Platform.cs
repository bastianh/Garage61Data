using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class Garage61PlatformCar
    {
        [JsonProperty("id")] public int Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("platform_id")] public string PlatformId;
    }

    public class Garage61PlatformTrack
    {
        [JsonProperty("id")] public int Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("platform_id")] public string PlatformId;
        [JsonProperty("variant")] public string Variant;
    }

    public class Garage61Platform
    {
        [JsonProperty("cars")] public List<Garage61PlatformCar> Cars;
        [JsonProperty("updated")] public DateTime LastUpdated;
        [JsonProperty("tracks")] public List<Garage61PlatformTrack> Tracks;

        public Garage61PlatformCar GetCarByPlatformId(string platformId)
        {
            return Cars?.Find(car => car.PlatformId == platformId);
        }

        public Garage61PlatformTrack GetTrackByPlatformId(string platformId)
        {
            return Tracks?.Find(track => track.PlatformId == platformId);
        }
    }
}
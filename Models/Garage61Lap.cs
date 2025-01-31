using System;
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class Garage61LapDriver
    {
        [JsonProperty("firstName")] public string FirstName { get; set; }
        [JsonProperty("lastName")] public string LastName { get; set; }
        [JsonIgnore] public string FullName => $"{FirstName} {LastName}";
    }


    public class Garage61Lap
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("driver")] public Garage61LapDriver Driver { get; set; }
        [JsonProperty("car")] public Garage61PlatformCar Car { get; set; }
        [JsonProperty("track")] public Garage61PlatformTrack Track { get; set; }

        [JsonProperty("driverRating")] public int DriverRating { get; set; }
        [JsonProperty("lapTime")] public double LapTimeRaw { get; set; }
        [JsonProperty("startTime")] public DateTime StartTime { get; set; }

        [JsonProperty("canViewTelemetry")] public bool CanViewTelemetry { get; set; }
        [JsonProperty("canViewSetup")] public bool CanViewSetup { get; set; }

        [JsonIgnore] public TimeSpan LapTime => TimeSpan.FromSeconds(LapTimeRaw);
    }
}
using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class Garage61LapDriver
    {
        [JsonProperty("firstName")] public string FirstName { get; set; }
        [JsonProperty("lastName")] public string LastName { get; set; }
        [JsonIgnore] public string FullName  => $"{FirstName} {LastName}";
    }
    public class Garage61Lap
    {
        [JsonProperty("driver")] public Garage61LapDriver Driver { get; set; }
        [JsonProperty("driverRating")] public int DriverRating { get; set; }
        [JsonProperty("lapTime")] public double LapTime { get; set; }
        [JsonProperty("startTime")] public DateTime StartTime  { get; set; }
    }
    
}
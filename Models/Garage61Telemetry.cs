using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class Garage61Telemetry
    {
        [JsonIgnore] public int CurrentRow = 0;
        [JsonIgnore] public double NextRowPercent = 0;
        [JsonProperty("lap")] public Garage61Lap Lap { get; set; }
        [JsonProperty("rows")] public List<Garage61TelemetryRow> Rows { get; set; }

        public void SaveToFile(string filePath)
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                throw new InvalidOperationException("Failed to save object to file.", ex);
            }
        }

#nullable enable
        public static Garage61Telemetry? LoadFromFile(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Garage61Telemetry>(jsonData);
            }
            catch
            {
                return null;
            }
        }
    }
}
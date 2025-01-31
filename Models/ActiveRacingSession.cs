using System.Collections.Generic;

namespace Garage61Data.Models
{
    public class ActiveRacingSession
    {
        public long IrCarId { get; set; }
        public string IrCarScreenName { get; set; }
        public long IrTrackId { get; set; }
        public string IrTrackName { get; set; }

        public Garage61Telemetry Telemetry { get; set; }

        public List<Garage61Lap> Laps { get; set; }

#nullable enable
        public Garage61Lap? GetLap(int lapId)
        {
            return lapId < Laps.Count ? Laps[lapId] : null;
        }
    }
}
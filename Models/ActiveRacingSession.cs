using System.Collections.Generic;

namespace Garage61Data.Models
{
    public class ActiveRacingSession
    {
        public long IrCarId { get; set; }
        public string IrCarScreenName { get; set; }
        public long IrTrackId { get; set; }
        public string IrTrackName { get; set; }

        public List<Garage61Lap> Laps { get; set; }
    }
}
using System.Collections.Generic;

namespace Garage61Data.Models
{
    public class ActiveRacingSession
    {
        public long CarId{ get; set;}
        public string CarScreenName{ get; set;}
        public long TrackId{ get; set;}
        public string TrackName{ get; set;}
        
        public List<Garage61Lap> Laps { get; set;}
    }
}
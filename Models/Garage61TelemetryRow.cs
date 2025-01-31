using System.Diagnostics.CodeAnalysis;

namespace Garage61Data.Models
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Garage61TelemetryRow
    {
        public double Speed { get; set; }
        public double LapDistPct { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Brake { get; set; }
        public double Throttle { get; set; }
        public double RPM { get; set; }
        public double SteeringWheelAngle { get; set; }
        public int Gear { get; set; }
        public double Clutch { get; set; }
        public bool ABSActive { get; set; }
        public bool DRSActive { get; set; }
        public double LatAccel { get; set; }
        public double LongAccel { get; set; }
        public double VertAccel { get; set; }
        public double Yaw { get; set; }
        public int PositionType { get; set; }
    }
}
using Garage61Data.Models;

namespace Garage61Data.UIControls
{
    public class DisplayIntComboBoxItem
    {
        public DisplayIntComboBoxItem(string displayText, int value)
        {
            DisplayText = displayText;
            Value = value;
        }

        public DisplayIntComboBoxItem(Garage61PlatformCar car)
        {
            DisplayText = car.Name;
            Value = car.Id;
        }

        public DisplayIntComboBoxItem(Garage61PlatformTrack track)
        {
            DisplayText = $"{track.Name} ({track.Variant})";
            Value = track.Id;
        }

        public string DisplayText { get; set; }
        public int Value { get; set; }
    }
}
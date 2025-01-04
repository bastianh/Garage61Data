namespace Garage61Data.UIControls
{
    public class DisplayStringComboBoxItem
    {
        public DisplayStringComboBoxItem(string displayText, string value)
        {
            DisplayText = displayText;
            Value = value;
        }

        public string DisplayText { get; set; }
        public string Value { get; set; }
    }
}
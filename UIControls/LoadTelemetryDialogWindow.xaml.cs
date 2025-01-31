using SimHub.Plugins.UI;

namespace Garage61Data.UIControls
{
    public partial class LoadTelemetryDialogWindow : SHDialogContentBase
    {
        public LoadTelemetryDialogWindow()
        {
            InitializeComponent();
            DataContext = this;
            ShowOk = true;
            ShowCancel = true;
        }

        public string LapId { get; set; } = "";
    }
}
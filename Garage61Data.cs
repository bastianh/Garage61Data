using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Garage61Data.Helpers;
using Garage61Data.Models;
using Garage61Data.Properties;
using SimHub;
using SimHub.Plugins;

namespace Garage61Data
{
    [PluginDescription("Garage61 data plugin")]
    [PluginAuthor("Bastian Hoyer")]
    [PluginName("Garage61Data")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class Garage61Data : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private const string GeneralSettingsName = "general";
        private const string PlatformSettingsName = "platform";
        private SettingsControl _settingsUi;
        private ApiClient ApiClient { get; set; }

        public UserInfo UserInfo { get; private set; }

        public PluginSettings Settings { get; private set; }
        public Garage61Platform Garage61Platform { get; private set; }

        #region IPlugin Members

        public PluginManager PluginManager { get; set; }

        public void End(PluginManager pluginManager)
        {
            this.SaveCommonSettings(GeneralSettingsName, Settings);
        }

        public void Init(PluginManager pluginManager)
        {
            Logging.Current.Info("Initializing Garage61Data plugin");
            PluginManager = pluginManager;

            InitializeSettings();
            _ = InitializeDependencies();
            AttachDelegates();
        }

        #endregion

        #region IWPFSettingsV2 Members

        public ImageSource PictureIcon => this.ToIcon(Resources.sdkmenuicon);
        public string LeftMenuTitle => "Garage61Data";

        public Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            _settingsUi = new SettingsControl(this);
            return _settingsUi;
        }

        #endregion

        #region Public Methods

        public async Task<List<Garage61Lap>> SendTestRequest(string trackId, string carId)
        {
            var parameters = new Dictionary<string, string>
            {
                { "group", "driver-car" },
                { "tracks", trackId },
                { "cars", carId },
                { "limit", "10" }
            };

            var laps = await ApiClient.GetLaps(parameters);
            return laps;
        }

        public async Task LoginUser()
        {
            await ApiClient.StartOAuthFlow();
            await UpdateGarage61Data();
        }

        public void LogoutUser()
        {
            ApiClient.Logout();
            UserInfo = null;
            _settingsUi?.UpdateDialog();
        }

        #endregion


        #region Private Methods

        private void InitializeSettings()
        {
            Settings = this.ReadCommonSettings(GeneralSettingsName, () => new PluginSettings());
            Garage61Platform = this.ReadCommonSettings(PlatformSettingsName, () => new Garage61Platform());
        }

        private async Task InitializeDependencies()
        {
            // Initialize ApiClient
            ApiClient = new ApiClient(this);

            try
            {
                await UpdateGarage61Data();
            }
            catch (Exception ex)
            {
                Logging.Current.Error($"Garage61Data: error updating data: {ex.Message}");
            }
        }

        private void AttachDelegates()
        {
            this.AttachDelegate("Garage61Data.LapCount", () => ActiveSession?.Laps?.Count ?? 0);

            for (var i = 0; i < 16; i++)
            {
                var index = i;
                this.AttachDelegate($"Garage61Data.Lap.{index}.FirstName",
                    () => ActiveSession?.Laps?[index]?.Driver.FirstName);
                this.AttachDelegate($"Garage61Data.Lap.{index}.LastName",
                    () => ActiveSession?.Laps?[index]?.Driver.LastName);
                this.AttachDelegate($"Garage61Data.Lap.{index}.DriverRating",
                    () => ActiveSession?.Laps?[index]?.DriverRating);
                this.AttachDelegate($"Garage61Data.Lap.{index}.StartTime",
                    () => ActiveSession?.Laps?[index]?.StartTime);
                this.AttachDelegate($"Garage61Data.Lap.{index}.LapTime", () => ActiveSession?.Laps?[index]?.LapTime);
            }
        }

        private async Task UpdateGarage61Data()
        {
            await UpdateGarage61PlatformData();
            UserInfo = await ApiClient.GetMe();
            _settingsUi?.UpdateDialog();
        }

        private async Task UpdateGarage61PlatformData()
        {
            if (Garage61Platform == null ||
                (DateTime.Now - Garage61Platform.LastUpdated).TotalHours > 24)
            {
                var tracks = await ApiClient.GetTracks();
                var cars = await ApiClient.GetCars();
                Garage61Platform = new Garage61Platform
                {
                    Cars = cars,
                    Tracks = tracks,
                    LastUpdated = DateTime.Now
                };
                this.SaveCommonSettings(PlatformSettingsName, Garage61Platform);
                _settingsUi?.UpdatePlatformData();
            }
        }

        #endregion
    }
}
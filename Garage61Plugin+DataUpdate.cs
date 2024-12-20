using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameReaderCommon;
using Garage61Data.Models;
using IRacingReader;
using SimHub;
using SimHub.Plugins;

namespace Garage61Data
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class Garage61Plugin
    {
        private ActiveRacingSession _activeSession;

        private ActiveRacingSession ActiveSession
        {
            get => _activeSession;
            set
            {
                if (_activeSession == value) return;
                _activeSession = value;
                if (value != null) _ = OnNewRacingSession(value);
            }
        }

        private async Task OnNewRacingSession(ActiveRacingSession value)
        {
            if (Garage61Platform == null)
            {
                Logging.Current.Error("Garage61Data: Garage61Platform is null");
                return;
            }

            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "group", "driver-car" },
                    { "tracks", Garage61Platform.GetTrackByPlatformId(value.TrackId.ToString()).Id.ToString() },
                    { "cars", Garage61Platform.GetCarByPlatformId(value.CarId.ToString()).Id.ToString() },
                    { "limit", "16" }
                };

                value.Laps = await ApiClient.GetLaps(parameters);
            }
            catch (Exception ex)
            {
                Logging.Current.Error($"Garage61Data: error fetching laps: {ex.Message}");
            }
        }


        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            if (!data.GameRunning || data.NewData == null)
            {
                if (ActiveSession != null)
                {
                    ActiveSession = null;
                    Logging.Current.Info("Garage61Data: iRacing session ended");
                }

                return;
            }

            if (ActiveSession != null) return;
            
            if (!(data.NewData.GetRawDataObject() is DataSampleEx dataSample)) return;

            ActiveSession = new ActiveRacingSession
            {
                CarId = dataSample.SessionData.DriverInfo.Drivers[dataSample.SessionData.DriverInfo.DriverCarIdx]
                    .CarID,
                CarScreenName = dataSample.SessionData.DriverInfo
                    .Drivers[dataSample.SessionData.DriverInfo.DriverCarIdx]
                    .CarScreenName,
                TrackId = dataSample.SessionData.WeekendInfo.TrackID,
                TrackName = dataSample.SessionData.WeekendInfo.TrackName
            };
            Logging.Current.Info(
                $"Garage61Data: iRacing session started (Track: {ActiveSession.TrackName} / Car: {ActiveSession.CarScreenName})");
        }
    }
}
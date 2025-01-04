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
    public partial class Garage61Data
    {
        private ActiveRacingSession _activeSession;
        private bool _isFetchingLaps;

        public ActiveRacingSession ActiveSession
        {
            get => _activeSession;
            private set
            {
                if (_activeSession == value) return;
                _activeSession = value;
                RacingSessionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #region IDataPlugin Members

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
                IrCarId = dataSample.SessionData.DriverInfo.Drivers[dataSample.SessionData.DriverInfo.DriverCarIdx]
                    .CarID,
                IrCarScreenName = dataSample.SessionData.DriverInfo
                    .Drivers[dataSample.SessionData.DriverInfo.DriverCarIdx]
                    .CarScreenName,
                IrTrackId = dataSample.SessionData.WeekendInfo.TrackID,
                IrTrackName = dataSample.SessionData.WeekendInfo.TrackName
            };
            _ = UpdateRacingSession();
            Logging.Current.Info(
                $"Garage61Data: iRacing session started (Track: {ActiveSession.IrTrackName} / Car: {ActiveSession.IrCarScreenName})");
        }

        #endregion

        public event EventHandler RacingSessionChanged;

        private async Task UpdateRacingSession()
        {
            if (!(_activeSession is { Laps: null }) || _isFetchingLaps) return;
            if (Garage61Platform == null)
            {
                Logging.Current.Error("Garage61Data: Garage61Platform is null");
                return;
            }

            try
            {
                _isFetchingLaps = true;

                var parameters = new Dictionary<string, string>
                {
                    { "group", "driver-car" },
                    {
                        "tracks",
                        Garage61Platform.GetTrackByPlatformId(_activeSession.IrTrackId.ToString()).Id.ToString()
                    },
                    { "cars", Garage61Platform.GetCarByPlatformId(_activeSession.IrCarId.ToString()).Id.ToString() },
                    { "limit", "16" }
                };

                var filterParameters = Settings.FilterSettings.GetFilterParameters();
                foreach (var filterParam in filterParameters) parameters[filterParam.Key] = filterParam.Value;

                _activeSession.Laps = await ApiClient.GetLaps(parameters);
                RacingSessionChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logging.Current.Error($"Garage61Data: error fetching laps: {ex.Message}");
            }
            finally
            {
                _isFetchingLaps = false;
            }
        }
    }
}
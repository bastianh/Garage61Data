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
            if (!data.GameRunning || data.NewData == null || data.GameName != "IRacing") 
            {
                if (ActiveSession == null) return;
                ActiveSession = null;
                Logging.Current.Info("Garage61Data: iRacing session ended");
                return;
            }
            
            if (!(data.NewData.GetRawDataObject() is DataSampleEx newDataSample)) return;
            if (ActiveSession == null) InitializeActiveSession(newDataSample);
        }

        private ActiveRacingSession InitializeActiveSession(DataSampleEx dataSample)
        {
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
            return ActiveSession;
        }

        #endregion

        public event EventHandler RacingSessionChanged;

        public async Task RefreshLaps()
        {
            _activeSession.Laps = null;
            await UpdateRacingSession();
        }
        
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

                var track = Garage61Platform.GetTrackByPlatformId(_activeSession.IrTrackId.ToString());
                if (track == null)
                {
                    Logging.Current.Warn($"Garage61Data: Could not find Garage61 track ID for iRacing track ID: {_activeSession.IrTrackId}. Skipping lap update.");
                    _isFetchingLaps = false; // Reset flag before returning
                    return;
                }

                var car = Garage61Platform.GetCarByPlatformId(_activeSession.IrCarId.ToString());
                if (car == null)
                {
                    Logging.Current.Warn($"Garage61Data: Could not find Garage61 car ID for iRacing car ID: {_activeSession.IrCarId}. Skipping lap update.");
                    _isFetchingLaps = false; // Reset flag before returning
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { "group", "driver-car" },
                    { "tracks", track.Id.ToString() },
                    { "cars", car.Id.ToString() },
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
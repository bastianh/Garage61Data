using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameReaderCommon;
using Garage61Data.Exceptions;
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
            if (ActiveSession?.Telemetry != null)
                UpdateTelemetryPosition(ActiveSession.Telemetry, data.NewData.TrackPositionPercent);
        }

        #endregion

        private void UpdateTelemetryPosition(Garage61Telemetry telemetry, double newPosition)
        {
            // Console.WriteLine($"Garage61Data: {telemetry.CurrentRow} Updating telemetry position {newPosition}");
            if (newPosition <= telemetry.NextRowPercent &&
                newPosition > telemetry.Rows[telemetry.CurrentRow].LapDistPct) return;
            foreach (var row in telemetry.Rows)
                if (row.LapDistPct > newPosition)
                {
                    var index = telemetry.Rows.IndexOf(row);
                    // Console.WriteLine($"Garage61Data: Lap {row.LapDistPct} exceeds track position percent: {newPosition} (Row Index: {index})");
                    telemetry.CurrentRow = index;
                    if (index < telemetry.Rows.Count - 1)
                        telemetry.NextRowPercent = telemetry.Rows[index + 1].LapDistPct;
                    break;
                }
        }

        private void InitializeActiveSession(DataSampleEx dataSample)
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
        }

        public event EventHandler RacingSessionChanged;

        public async Task RefreshLaps()
        {
            _activeSession.Laps = null;
            await UpdateRacingSession();
        }

        public async Task ListTelemetryLaps()
        {
            var parameters = new Dictionary<string, string>
            {
                { "group", "driver-car" },
                {
                    "tracks",
                    Garage61Platform.GetTrackByPlatformId(_activeSession.IrTrackId.ToString()).Id.ToString()
                },
                { "cars", Garage61Platform.GetCarByPlatformId(_activeSession.IrCarId.ToString()).Id.ToString() },
                { "seeTelemetry", "true" },
                { "limit", "16" }
            };

            var result = await ApiClient.GetLaps(parameters);
            foreach (var lap in result)
                Logging.Current.Info($"Garage61Data: Lap  {lap.Id} {lap.Driver.FullName} - {lap.LapTime}");
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


        public async Task LoadTelemetry(string lapId)
        {
            var fileName = PluginManager.GetCommonStoragePath(GetType().Name + ".lap." + lapId + ".json");

            var garage61Telemetry = Garage61Telemetry.LoadFromFile(fileName);
            if (garage61Telemetry == null)
            {
                garage61Telemetry = new Garage61Telemetry
                {
                    Lap = await ApiClient.GetLap(lapId),
                    Rows = await ApiClient.GetLapTelemetry(lapId)
                };

                garage61Telemetry.SaveToFile(fileName);
            }

            if (ActiveSession.IrTrackId != garage61Telemetry.Lap.Track.PlatformIdInt)
                throw new TrackMismatchException(
                    $"Garage61Data: Track ID mismatch. Active session TrackID: {ActiveSession.IrTrackId}, Telemetry TrackID: {garage61Telemetry.Lap.Track.PlatformId}");

            if (ActiveSession.IrCarId != garage61Telemetry.Lap.Car.PlatformIdInt)
                throw new CarMismatchException(
                    $"Garage61Data: Car ID mismatch. Active session CarID: {ActiveSession.IrTrackId}, Telemetry CarID: {garage61Telemetry.Lap.Car.PlatformId}");

            ActiveSession.Telemetry = garage61Telemetry;
            RacingSessionChanged?.Invoke(this, EventArgs.Empty);
            Logging.Current.Info($"Garage61Data: telemetry loaded for lap {lapId}");
        }
    }
}
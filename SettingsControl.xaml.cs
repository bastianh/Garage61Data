using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using Garage61Data.Exceptions;
using Garage61Data.Helpers;
using Garage61Data.Models;
using Garage61Data.UIControls;
using SimHub;
using SimHub.Plugins.Styles;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace Garage61Data
{
    public partial class SettingsControl : UserControl
    {
        private readonly Garage61Data _plugin;

        private readonly List<DisplayStringComboBoxItem> _teams;
        private List<Garage61Lap> _laps = new List<Garage61Lap>();

        public SettingsControl()
        {
            InitializeComponent();
            _teams = new List<DisplayStringComboBoxItem>();
        }

        public SettingsControl(Garage61Data plugin) : this()
        {
            _plugin = plugin;
            _plugin.RacingSessionChanged += PluginOnRacingSessionChanged;
            ManualFilterSwitch.IsChecked = FilterSettings.Enabled;
            FilterConfiguration.Visibility = FilterSettings.Enabled ? Visibility.Visible : Visibility.Collapsed;
            FollowerFilter.IsChecked = FilterSettings.IncludeFollower;
            YourselfFilter.IsChecked = FilterSettings.IncludeYourself;

            UpdateDialog();
        }

        private FilterSettings FilterSettings => _plugin.Settings.FilterSettings;
        private UserInfo UserInfo => _plugin.UserInfo;
        private ActiveRacingSession ActiveSession => _plugin.ActiveSession;
        private Garage61Platform Platform => _plugin.Garage61Platform;


        private void UpdateActiveSession()
        {
            LapsDataGrid.ItemsSource = null;
            _laps = _plugin.ActiveSession.Laps;
            if (_laps != null) LapsDataGrid.ItemsSource = _laps;
            UpdateCurrentSessionText();
            CurrentSession.Visibility = Visibility.Visible;
        }

        private void HideActiveSession()
        {
            CurrentSession.Visibility = Visibility.Collapsed;
            LapsDataGrid.ItemsSource = null;
        }

        private void PluginOnRacingSessionChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_plugin.ActiveSession != null) UpdateActiveSession();
                else HideActiveSession();
            });
        }

        private void UpdateTeamsList()
        {
            _teams.Clear();
            if (UserInfo?.Teams != null)
                foreach (var team in UserInfo.Teams)
                    _teams.Add(new DisplayStringComboBoxItem(team.Name, team.Slug));

            if (FilterSettings.TeamSlugs != null)
                foreach (var item in _teams)
                    if (FilterSettings.TeamSlugs.Contains(item.Value))
                        TeamsList.SelectedItems.Add(item);

            TeamsList.ItemsSource = null;
            TeamsList.ItemsSource = _teams;
        }

        private void UpdateIntroText()
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("Currently logged in as: "));
            paragraph.Inlines.Add(
                new Run($"{UserInfo.FirstName} {UserInfo.LastName}") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run(" ("));
            paragraph.Inlines.Add(new Run($"{UserInfo.NickName}") { FontStyle = FontStyles.Italic });
            paragraph.Inlines.Add(new Run(")"));

            IntroText.Document.Blocks.Clear();
            IntroText.Document.Blocks.Add(paragraph);
        }

        private void UpdateCurrentSessionText()
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("Current Track: "));
            var trackName = Platform?.GetTrackByPlatformId(ActiveSession?.IrTrackId.ToString())?.Name;
            paragraph.Inlines.Add(
                new Run($"{trackName ?? ""}\n") { FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run("Current Car: "));
            paragraph.Inlines.Add(new Run($"{ActiveSession?.IrCarScreenName}") { FontWeight = FontWeights.Bold });

            if (ActiveSession?.Telemetry != null)
            {
                paragraph.Inlines.Add(new Run("\nTelemetry loaded for lap: "));
                paragraph.Inlines.Add(new Run($"{ActiveSession.Telemetry.Lap.Id}") { FontWeight = FontWeights.Bold });
            }

            CurrentSessionText.Document.Blocks.Clear();
            CurrentSessionText.Document.Blocks.Add(paragraph);
        }

        public void UpdateDialog()
        {
            if (!Dispatcher.CheckAccess())
            {
                // Falls nicht, den Dispatcher verwenden
                Dispatcher.Invoke(UpdateDialog);
                return;
            }

            if (UserInfo != null)
            {
                IntroText.Visibility = Visibility.Visible;
                LoginButton.Visibility = Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Visible;
                UpdateIntroText();
            }
            else
            {
                IntroText.Visibility = Visibility.Collapsed;
                LoginButton.Visibility = Visibility.Visible;
                LogoutButton.Visibility = Visibility.Collapsed;
            }

            UpdateTeamsList();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _plugin.LoginUser();
                Logging.Current.Info($"Garage61Data: user logged in: {UserInfo.Slug}");
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
                MessageBox.Show("There was an error logging in. Please check the SimHub System Log.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _plugin.LogoutUser();
        }


        private void TeamsFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedValues = new List<string>();

            foreach (DisplayStringComboBoxItem item in TeamsList.SelectedItems) selectedValues.Add(item.Value);

            FilterSettings.TeamSlugs = selectedValues;
        }

        private void Filter_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                switch (element.Name)
                {
                    case "ManualFilterSwitch":
                        FilterSettings.Enabled = false;
                        FilterConfiguration.Visibility = Visibility.Collapsed;
                        break;
                    case "FollowerFilter":
                        FilterSettings.IncludeFollower = false;
                        break;
                    case "YourselfFilter":
                        FilterSettings.IncludeYourself = false;
                        break;
                }
        }

        private void Filter_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                switch (element.Name)
                {
                    case "ManualFilterSwitch":
                        FilterSettings.Enabled = true;
                        FilterConfiguration.Visibility = Visibility.Visible;
                        break;
                    case "FollowerFilter":
                        FilterSettings.IncludeFollower = true;
                        break;
                    case "YourselfFilter":
                        FilterSettings.IncludeYourself = true;
                        break;
                }
        }

        private void G61FilterDoc_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://garage61.net/docs/usage/filtering",
                UseShellExecute = true
            });
        }

        private void Refresh_OnClick(object sender, RoutedEventArgs e)
        {
            _ = _plugin.RefreshLaps();
        }

        private void ListTelemetry_OnClick(object sender, RoutedEventArgs e)
        {
            _ = _plugin.ListTelemetryLaps();
        }

        private async void LoadTelemetry_Click(object sender, RoutedEventArgs e)
        {
            var dialogWindow = new LoadTelemetryDialogWindow();

            var res = await dialogWindow.ShowDialogWindowAsync(this);

            if (res == DialogResult.OK)
            {
                var lapId = dialogWindow.LapId;
                Logging.Current.Info($"Garage61Data: Loading telemetry for lap {lapId}");
                try
                {
                    await _plugin.LoadTelemetry(lapId);
                }
                catch (TrackMismatchException)
                {
                    await SHMessageBox.Show("The track of the selected lap does not match the current track.");
                }
                catch (CarMismatchException)
                {
                    await SHMessageBox.Show("The car of the selected lap does not match the current car.");
                }
                catch (ApiClientException)
                {
                    await SHMessageBox.Show("The Lap could not be loaded. Please check the SimHub System Log.");
                    throw;
                }
            }
        }
    }
}
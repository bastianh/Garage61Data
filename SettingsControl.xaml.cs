using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Garage61Data.Models;
using Garage61Data.UIControls;
using SimHub;

namespace Garage61Data
{
    public partial class SettingsControl : UserControl
    {
        private readonly List<DisplayIntComboBoxItem> _cars;
        private readonly Garage61Plugin _plugin;

        private readonly List<DisplayStringComboBoxItem> _teams;
        private readonly List<DisplayIntComboBoxItem> _tracks;
        private List<Garage61Lap> _laps = new List<Garage61Lap>();


        public SettingsControl()
        {
            InitializeComponent();
            _teams = new List<DisplayStringComboBoxItem>();
            _tracks = new List<DisplayIntComboBoxItem>();
            _cars = new List<DisplayIntComboBoxItem>();
        }

        public SettingsControl(Garage61Plugin plugin) : this()
        {
            _plugin = plugin;
            UpdateDialog();
            UpdatePlatformData();
        }

        private FilterSettings FilterSettings => _plugin.Settings.FilterSettings;
        private UserInfo UserInfo => _plugin.UserInfo;
        private Garage61Platform Garage61Platform => _plugin.Garage61Platform;

        private void UpdateTeamsList()
        {
            _teams.Clear();
            if (UserInfo != null)
            {
                if (UserInfo.Teams != null)
                {
                    foreach (var team in UserInfo.Teams)
                        _teams.Add(new DisplayStringComboBoxItem(team.Name, team.Slug));

                    TeamsList.ItemsSource = null;
                    TeamsList.ItemsSource = _teams;
                }
            }

            TeamsList.ItemsSource = null;
            TeamsList.ItemsSource = _teams;
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
                LoginButton.Visibility = Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Visible;
            }
            else
            {
                LoginButton.Visibility = Visibility.Visible;
                LogoutButton.Visibility = Visibility.Collapsed;
            }

            UpdateTeamsList();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _plugin.LoginUser().ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    var userInfo = task.Result;
                    Logging.Current.Info($"Garage61Data: user logged in: {userInfo.Slug}");
                }
                else if (task.IsFaulted)
                {
                    Logging.Current.Error($"Garage61Data: OAuth flow error: {task.Exception?.Message}");
                }
            });
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _plugin.LogoutUser();
        }

        private void TestRequest_Click(object sender, RoutedEventArgs e)
        {
            if (CarComboBox.SelectedValue == null || TrackComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a car and track", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _plugin.SendTestRequest(TrackComboBox.SelectedValue.ToString(), CarComboBox.SelectedValue.ToString())
                .ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        var laps = task.Result;
                        Dispatcher.Invoke(() =>
                        {
                            LapsDataGrid.ItemsSource = null;
                            LapsDataGrid.ItemsSource = laps;
                        });
                    }
                    else if (task.IsFaulted)
                    {
                        Logging.Current.Error($"Garage61Data: error sending test request: {task.Exception?.Message}");
                        MessageBox.Show($"Something went wrong: {task.Exception?.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });
        }

        public void UpdatePlatformData()
        {
            if (!Dispatcher.CheckAccess())
            {
                // Falls nicht, den Dispatcher verwenden
                Dispatcher.Invoke(UpdatePlatformData);
                return;
            }

            if (Garage61Platform != null)
            {
                _cars.Clear();
                foreach (var car in Garage61Platform.Cars)
                {
                    _cars.Add(new DisplayIntComboBoxItem(car));
                }

                _tracks.Clear();
                foreach (var track in Garage61Platform.Tracks)
                {
                    _tracks.Add(new DisplayIntComboBoxItem(track));
                }

                CarComboBox.ItemsSource = null;
                CarComboBox.ItemsSource = _cars;
                CarComboBox.SelectedValue = _plugin.Settings.TestCarId;
                TrackComboBox.ItemsSource = null;
                TrackComboBox.ItemsSource = _tracks;
                TrackComboBox.SelectedValue = _plugin.Settings.TestTrackId;
            }
        }

        public void UpdateFilter()
        {
        }


        private void TeamsFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = TeamsList.SelectedItems;
            /*
            if (TeamsList.SelectedItem is DisplayStringComboBoxItem selectedItem && FilterSettings.TeamSlugs != selectedItem.Value)
            {
                FilterSettings.TeamSlugs = selectedItem.Value;
                FilterUpdated?.Invoke();
            }
            */
        }

        private void TestRequestSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox comboBox) || !(comboBox.SelectedItem is DisplayIntComboBoxItem item)) return;
            if (ReferenceEquals(comboBox, CarComboBox)) _plugin.Settings.TestCarId = item.Value;
            if (ReferenceEquals(comboBox, TrackComboBox)) _plugin.Settings.TestTrackId = item.Value;
        }


        private void ManualFilterSwitch_OnChecked(object sender, RoutedEventArgs e)
        {
        }
    }
}
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Threading.Tasks;

namespace ChannelsNativeTest
{
    public partial class SettingsPage : Page
    {
        private UserSettings _settings;

        public SettingsPage()
        {
            InitializeComponent();
            _settings = SettingsManager.Load();
            
            // Populate the UI with current settings
            ServerIpTextBox.Text = _settings.LastServerAddress;
            AutoSkipCheckBox.IsChecked = _settings.AutoSkipCommercials;
            LightModeCheckBox.IsChecked = _settings.IsLightTheme;

            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ServerIpTextBox.Focus();
            RefreshStreamsList(); // Draw the saved links when the page loads!
        }

        // --- NEW: EXTERNAL STREAM LOGIC ---

        private void RefreshStreamsList()
        {
            SavedStreamsList.Children.Clear();

            if (_settings.ExternalStreams.Count == 0)
            {
                SavedStreamsList.Children.Add(new TextBlock { Text = "No external streams added yet.", Foreground = Brushes.Gray, FontStyle = FontStyles.Italic, Margin = new Thickness(5) });
                return;
            }

            foreach (var stream in _settings.ExternalStreams)
            {
                var border = new Border { Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)), CornerRadius = new CornerRadius(6), Padding = new Thickness(15, 10, 15, 10), Margin = new Thickness(0, 0, 0, 8) };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                textStack.Children.Add(new TextBlock { Text = stream.Title, Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.Bold });
                textStack.Children.Add(new TextBlock { Text = $"{stream.Service}  •  {stream.StreamId}", Foreground = Brushes.Gray, FontSize = 12 });

                var delBtn = new Button { Content = "❌", Background = Brushes.Transparent, Foreground = Brushes.Red, BorderThickness = new Thickness(0), FontSize = 16, Cursor = Cursors.Hand, Tag = stream.Id };
                delBtn.Click += DeleteStream_Click;

                Grid.SetColumn(textStack, 0);
                Grid.SetColumn(delBtn, 1);
                grid.Children.Add(textStack);
                grid.Children.Add(delBtn);
                border.Child = grid;

                SavedStreamsList.Children.Add(border);
            }
        }

        private void AddStream_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StreamTitleBox.Text) || string.IsNullOrWhiteSpace(StreamIdBox.Text))
            {
                MessageBox.Show("Please enter both a Title and a Deep Link ID.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newStream = new ExternalStream
            {
                Title = StreamTitleBox.Text.Trim(),
                Service = ((ComboBoxItem)StreamServiceBox.SelectedItem).Content.ToString() ?? "Custom URI",
                StreamId = StreamIdBox.Text.Trim()
            };

            _settings.ExternalStreams.Add(newStream);
            SettingsManager.Save(_settings); // Save to JSON immediately
            
            // Clear the form
            StreamTitleBox.Text = "";
            StreamIdBox.Text = "";
            
            RefreshStreamsList();
        }

        private void DeleteStream_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var streamToRemove = _settings.ExternalStreams.FirstOrDefault(s => s.Id == id);
                if (streamToRemove != null)
                {
                    _settings.ExternalStreams.Remove(streamToRemove);
                    SettingsManager.Save(_settings);
                    RefreshStreamsList();
                }
            }
        }

        // ----------------------------------

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.LastServerAddress = ServerIpTextBox.Text.Trim();
            _settings.AutoSkipCommercials = AutoSkipCheckBox.IsChecked ?? true;
            _settings.IsLightTheme = LightModeCheckBox.IsChecked ?? false;

            SettingsManager.Save(_settings);
            ApplyTheme(_settings.IsLightTheme);

            SaveStatusText.Visibility = Visibility.Visible;
            SaveButton.IsEnabled = false;
            
            Task.Delay(1500).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() => 
                {
                    SaveStatusText.Visibility = Visibility.Collapsed;
                    SaveButton.IsEnabled = true;
                });
            });
        }

        private void ApplyTheme(bool isLight)
        {
            string themeName = isLight ? "LightTheme.xaml" : "DarkTheme.xaml";
            try 
            {
                var newDictionary = new ResourceDictionary { Source = new Uri($"Themes/{themeName}", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(newDictionary);
            }
            catch { }
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Back || e.Key == Key.BrowserBack)
            {
                NavigationService?.GoBack();
                e.Handled = true;
            }
        }
    }
}
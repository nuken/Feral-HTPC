using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace FeralCode
{
    public partial class DashboardPage : Page
    {
        private string _baseUrl = "";

        public DashboardPage()
        {
            InitializeComponent();
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Load();
            _baseUrl = settings.LastServerAddress;

            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                StatusText.Text = "⚠️ DVR Server IP not set.";
                StatusText.SetResourceReference(TextBlock.ForegroundProperty, "StatusError");
                return;
            }

            await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            var api = new ChannelsApi();
            var recent = await api.GetUnifiedRecordingsAsync(_baseUrl);

            if (recent.Count == 0)
            {
                StatusText.Text = "No recordings found.";
                StatusText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                return;
            }

            StatusText.Visibility = Visibility.Collapsed;
            
            // --- FIXED: Hand the data directly to WPF Binding ---
            DashboardItemsControl.ItemsSource = recent.Take(50).ToList();

            // Auto-focus the first item
            if (DashboardItemsControl.Items.Count > 0)
            {
                var container = DashboardItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as UIElement;
                container?.Focus();
            }
        }

        private async void Item_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UnifiedRecording item)
            {
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/dvr/files/{item.Id}/stream.mpg?format=ts&vcodec=copy&acodec=copy";
                
                // Fetch STRM links if it's an external file
                if (!string.IsNullOrWhiteSpace(item.Path) && 
                   (item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase) || 
                    item.Path.EndsWith(".strmlnk", StringComparison.OrdinalIgnoreCase)))
                {
                    var api = new ChannelsApi();
                    var fileDetails = await api.GetFileDetailsAsync(_baseUrl, item.Id);
                    if (fileDetails != null && fileDetails.StreamLinks != null && fileDetails.StreamLinks.Count > 0)
                    {
                        targetUrl = fileDetails.StreamLinks[0];
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = targetUrl, UseShellExecute = true });
                        return; 
                    }
                    else if (fileDetails != null && !string.IsNullOrWhiteSpace(fileDetails.VideoUrl))
                    {
                        targetUrl = fileDetails.VideoUrl;
                    }
                }

                // Play directly in VLC
                if (Application.Current.MainWindow is MainWindow mainWin)
                {
                    if (mainWin.ActivePlayerWindow != null) mainWin.ActivePlayerWindow.Close();
                    
                    // --- FIXED: Combine the new properties for the player window title ---
                    string windowTitle = string.IsNullOrWhiteSpace(item.SecondaryTitle) 
                        ? item.Title 
                        : $"{item.Title} - {item.SecondaryTitle}";

                    mainWin.ActivePlayerWindow = new PlayerWindow(targetUrl, windowTitle, item.PosterUrl, null);
                    
                    mainWin.ActivePlayerWindow.Closed += (s, args) =>
                    {
                        mainWin.ActivePlayerWindow = null;
                        mainWin.Show(); 
                        Application.Current.Dispatcher.InvokeAsync(() => btn.Focus(), System.Windows.Threading.DispatcherPriority.Input);
                    };
                    mainWin.Hide(); 
                    mainWin.ActivePlayerWindow.Show();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new StartPage());
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Back || e.Key == Key.BrowserBack)
            {
                e.Handled = true; 
                BackButton_Click(null!, null!);
            }
        }
    }
}
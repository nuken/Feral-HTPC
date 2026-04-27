using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Xabe.FFmpeg.Downloader; 

namespace FeralCode
{
    public partial class StartPage : Page
    {
        private static string _lastFocusedButtonName = "";

        public StartPage()
        {
            InitializeComponent();
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await EnsureFfmpegIsInstalledAsync();

            if (!string.IsNullOrEmpty(_lastFocusedButtonName))
            {
                if (this.FindName(_lastFocusedButtonName) as Button is Button targetButton)
                {
                    targetButton.Focus();
                    return; 
                }
            }

            BtnLiveTV.Focus(); 
        }

        private async Task EnsureFfmpegIsInstalledAsync()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string ffmpegFolder = Path.Combine(appData, "FeralHTPC", "ffmpeg");
            
            if (!Directory.Exists(ffmpegFolder)) Directory.CreateDirectory(ffmpegFolder);

            string ffmpegPath = Path.Combine(ffmpegFolder, "ffmpeg.exe");
            
            if (!File.Exists(ffmpegPath))
            {
                try
                {
                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegFolder);
                }
                catch { }
            }
        }

        private void LiveTv_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new GuidePage());
        }

        // --- NEW: Wires up the Recently Added button ---
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new DashboardPage());
        }

        private void Movies_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new MoviesPage());
        }

        private void Shows_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new ShowsPage());
        }
        
        // --- FIXED: Wires up the Videos button ---
        private void Videos_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new VideosPage()); 
        }
        
        private void Apps_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new ExternalStreamsPage());
        }
        
        private void Multiview_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new MultiviewSetupPage());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new SettingsPage());
        }
    }
}
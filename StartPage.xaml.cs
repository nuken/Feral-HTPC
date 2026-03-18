using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace FeralCode
{
    public partial class StartPage : Page
    {
        // This static variable survives even when the page is destroyed and recreated!
        private static string _lastFocusedButtonName = "";

        public StartPage()
        {
            InitializeComponent();
            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // If we have a saved button name in memory, find it and focus it!
            if (!string.IsNullOrEmpty(_lastFocusedButtonName))
            {
                if (this.FindName(_lastFocusedButtonName) as Button is Button targetButton)
                {
                    targetButton.Focus();
                    return; // Stop here so we don't accidentally focus the default button
                }
            }

            // Fallback: If memory is empty (first time launching app), focus Live TV
            BtnLiveTV.Focus(); 
        }

        private void LiveTv_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            NavigationService.Navigate(new GuidePage());
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
        
        private void Apps_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) _lastFocusedButtonName = btn.Name;
            // Uses your correct original page name!
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
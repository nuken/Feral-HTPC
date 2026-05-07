using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace FeralCode
{
    public partial class VideosPage : Page
    {
        private string _baseUrl = "";
        private bool _isViewingGroup = false;

        public VideosPage()
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

            await LoadVideoGroupsAsync();
        }

        private async System.Threading.Tasks.Task LoadVideoGroupsAsync()
        {
            _isViewingGroup = false;
            PageTitle.Text = "📂 PERSONAL MEDIA";
            BackButton.Content = "🏠 Home";
            
            GroupsScroller.Visibility = Visibility.Visible;
            VideosScroller.Visibility = Visibility.Collapsed;
            StatusText.Visibility = Visibility.Visible;
            StatusText.Text = "Loading media libraries...";
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "StatusConnecting");

            var api = new ChannelsApi();
            var groups = await api.GetVideoGroupsAsync(_baseUrl);

            if (groups.Count == 0)
            {
                StatusText.Text = "No personal media folders found on the DVR.";
                StatusText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                return;
            }

            StatusText.Visibility = Visibility.Collapsed;
            GroupsItemsControl.ItemsSource = groups;

            if (GroupsItemsControl.Items.Count > 0)
            {
                var container = GroupsItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as UIElement;
                container?.Focus();
            }
        }

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is VideoGroup group)
            {
                _ = LoadVideosInGroupAsync(group);
            }
        }

        private async System.Threading.Tasks.Task LoadVideosInGroupAsync(VideoGroup group)
        {
            _isViewingGroup = true;
            PageTitle.Text = $"📂 {group.Name.ToUpper()}";
            BackButton.Content = "🔙 Back to Folders";
            
            GroupsScroller.Visibility = Visibility.Collapsed;
            VideosScroller.Visibility = Visibility.Visible;
            StatusText.Visibility = Visibility.Visible;
            StatusText.Text = "Loading files...";

            var api = new ChannelsApi();
            var videos = await api.GetVideosInGroupAsync(_baseUrl, group.Id);

            StatusText.Visibility = Visibility.Collapsed;
            VideosItemsControl.ItemsSource = videos;

            if (VideosItemsControl.Items.Count > 0)
            {
                var container = VideosItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as UIElement;
                container?.Focus();
            }
        }

        private async void Video_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Video video)
            {
                // Disable the button briefly so the user doesn't double-click while fetching details
                btn.IsEnabled = false;

                try
                {
                    // Default behavior for standard local media files
                    string targetUrl = $"{_baseUrl.TrimEnd('/')}/dvr/files/{video.Id}/stream.mpg?format=ts&vcodec=copy&acodec=copy";

                    // Check if the path indicates a Stream Link or Stream File
                    bool isStreamLink = !string.IsNullOrWhiteSpace(video.Path) &&
                                        (video.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase) ||
                                         video.Path.EndsWith(".strmlnk", StringComparison.OrdinalIgnoreCase));

                    if (isStreamLink)
                    {
                        var api = new ChannelsApi();
                        var details = await api.GetFileDetailsAsync(_baseUrl, video.Id);

                        // If Channels DVR provided a direct VideoURL or StreamLinks array, use it instead!
                        if (details != null)
                        {
                            if (!string.IsNullOrWhiteSpace(details.VideoUrl))
                            {
                                targetUrl = details.VideoUrl;
                            }
                            else if (details.StreamLinks != null && details.StreamLinks.Count > 0)
                            {
                                targetUrl = details.StreamLinks[0];
                            }
                        }
                    }

                    if (Application.Current.MainWindow is MainWindow mainWin)
                    {
                        if (mainWin.ActivePlayerWindow != null) mainWin.ActivePlayerWindow.Close();

                        // Pass the targetUrl (which is now either the local file OR the Stream Link) to the Player
                        mainWin.ActivePlayerWindow = new PlayerWindow(targetUrl, video.VideoTitle, video.DisplayImage, null, video.Id, 0, video.Duration);

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
                finally
                {
                    // Re-enable the button
                    btn.IsEnabled = true;
                }
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isViewingGroup)
            {
                await LoadVideoGroupsAsync();
            }
            else
            {
                NavigationService?.Navigate(new StartPage());
            }
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

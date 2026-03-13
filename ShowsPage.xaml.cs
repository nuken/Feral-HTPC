using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ChannelsNativeTest
{
    public partial class ShowsPage : Page
    {
        private List<Episode> _allEpisodes = new List<Episode>();
        private List<TvShow> _allShows = new List<TvShow>(); // <-- NEW!
        private string _baseUrl = "";

        public ShowsPage()
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
                LoadingText.Text = "⚠️ No DVR Server IP configured in Settings.";
                return;
            }

            await LoadShowsAsync();
        }

        private async Task LoadShowsAsync()
        {
            var api = new ChannelsApi();
            _allShows = await api.GetShowsAsync(_baseUrl);
            _allEpisodes = await api.GetEpisodesAsync(_baseUrl);

            if (_allShows.Count == 0)
            {
                LoadingText.Text = "No TV Shows found on the DVR.";
                return;
            }

            // Dynamically build the Genre Dropdown based on what's in your library!
            var genres = _allShows.Where(s => s.Genres != null)
                                  .SelectMany(s => s.Genres!)
                                  .Distinct()
                                  .OrderBy(g => g).ToList();
            
            GenreBox.Items.Clear();
            GenreBox.Items.Add(new ComboBoxItem { Content = "All Genres", IsSelected = true });
            foreach (var g in genres) GenreBox.Items.Add(new ComboBoxItem { Content = g });

            ApplyFilters(); // Draws the grid initially
            LoadingText.Visibility = Visibility.Collapsed;
        }
		
		private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (_allShows.Count > 0) ApplyFilters();
        }
		
		private void ToggleFilters_Click(object sender, RoutedEventArgs e)
        {
            if (FilterBar.Visibility == Visibility.Visible)
            {
                FilterBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                FilterBar.Visibility = Visibility.Visible;
                SearchBox.Focus(); // Automatically puts the cursor in the search box!
            }
        }

        private void ApplyAndClose_Click(object sender, RoutedEventArgs e)
        {
            // The grid is already actively filtering in the background as you change drop-downs, 
            // so all this button has to do is hide the menu and return control to the D-Pad!
            FilterBar.Visibility = Visibility.Collapsed;
            
            if (ShowsWrapPanel.Children.Count > 0)
                ((UIElement)ShowsWrapPanel.Children[0]).Focus();
        }

        private void ApplyFilters()
        {
            if (_allShows == null || _allShows.Count == 0) return;

            var filtered = _allShows.AsEnumerable();

            // 1. Text Search
            string search = SearchBox.Text.ToLower().Trim();
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(s => s.Name.ToLower().Contains(search));

            // 2. Genre Filter
            if (GenreBox.SelectedIndex > 0 && GenreBox.SelectedItem is ComboBoxItem gi)
            {
                string genre = gi.Content.ToString() ?? "";
                filtered = filtered.Where(s => s.Genres != null && s.Genres.Contains(genre));
            }

            // 3. Watched Status
            if (WatchedBox.SelectedIndex == 1) filtered = filtered.Where(s => !s.IsWatched); // Unwatched
            if (WatchedBox.SelectedIndex == 2) filtered = filtered.Where(s => s.IsWatched);  // Watched

            // 4. Sorting
            if (SortBox.SelectedIndex == 0) filtered = filtered.OrderBy(s => s.Name);
            else if (SortBox.SelectedIndex == 1) filtered = filtered.OrderByDescending(s => s.CreatedAt);
            else if (SortBox.SelectedIndex == 2) filtered = filtered.OrderByDescending(s => s.ReleaseYear);

            RenderShowsGrid(filtered.ToList());
        }
		
		private void RenderShowsGrid(List<TvShow> showsToRender)
        {
            ShowsWrapPanel.Children.Clear();

            foreach (var show in showsToRender)
            {
                var btn = new Button { Style = (Style)FindResource("PosterButton"), Tag = show };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(260) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var img = new Image { Stretch = Stretch.UniformToFill };
                try { if (!string.IsNullOrWhiteSpace(show.ImageUrl)) img.Source = new BitmapImage(new Uri(show.ImageUrl)); } catch { }

                var textBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)), Padding = new Thickness(10) };
                var textPanel = new StackPanel();
                textPanel.Children.Add(new TextBlock { Text = show.Name, FontWeight = FontWeights.Bold, FontSize = 16, TextWrapping = TextWrapping.Wrap, MaxHeight = 45, TextTrimming = TextTrimming.CharacterEllipsis });
                textPanel.Children.Add(new TextBlock { Text = $"{show.EpisodeCount} Episodes", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 5, 0, 0) });
                
                textBorder.Child = textPanel;
                Grid.SetRow(img, 0);
                Grid.SetRow(textBorder, 1);
                grid.Children.Add(img);
                grid.Children.Add(textBorder);

                btn.Content = grid;
                btn.Click += Show_Click;
                ShowsWrapPanel.Children.Add(btn);
            }

            if (ShowsWrapPanel.Children.Count > 0)
                ((UIElement)ShowsWrapPanel.Children[0]).Focus();
        }

        private void Show_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TvShow show)
            {
                OpenEpisodesView(show);
            }
        }

        private void OpenEpisodesView(TvShow show)
        {
            ShowsScrollViewer.Visibility = Visibility.Collapsed;
            EpisodesView.Visibility = Visibility.Visible;
            ToggleFiltersButton.Visibility = Visibility.Collapsed;
			FilterBar.Visibility = Visibility.Collapsed;
			
            PageTitle.Text = $"🍿 {show.Name.ToUpper()}";
            BackButton.Content = "🔙 Back to Shows";

            SelectedShowTitle.Text = show.Name;
            SelectedShowCount.Text = $"{show.EpisodeCount} Recorded Episodes";
            try
            {
                if (!string.IsNullOrWhiteSpace(show.ImageUrl))
                    SelectedShowImage.Source = new BitmapImage(new Uri(show.ImageUrl));
            }
            catch { }

            EpisodesStackPanel.Children.Clear();

            // Filter to this show, sort by Newest Season & Episode
            var showEpisodes = _allEpisodes.Where(ep => ep.ShowId == show.Id)
                                           .OrderByDescending(ep => ep.SeasonNumber)
                                           .ThenByDescending(ep => ep.EpisodeNumber).ToList();

            foreach (var ep in showEpisodes)
            {
                var btn = new Button
                {
                    Style = (Style)FindResource("EpisodeButton"),
                    Tag = ep
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = $"Season {ep.SeasonNumber} • Episode {ep.EpisodeNumber}", Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204)), FontWeight = FontWeights.Bold, FontSize = 14 });
                sp.Children.Add(new TextBlock { Text = ep.EpisodeTitle, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 0), TextWrapping = TextWrapping.Wrap });

                btn.Content = sp;
                btn.Click += Episode_Click;
                EpisodesStackPanel.Children.Add(btn);
            }

            if (EpisodesStackPanel.Children.Count > 0)
                ((UIElement)EpisodesStackPanel.Children[0]).Focus();
        }

        private void Episode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Episode ep)
            {
                string streamUrl = $"{_baseUrl.TrimEnd('/')}/dvr/files/{ep.Id}/hls/master.m3u8?vcodec=copy&acodec=copy";
                string displayTitle = $"{ep.Title} - S{ep.SeasonNumber:D2}E{ep.EpisodeNumber:D2} - {ep.EpisodeTitle}";

                // Safely grab the MainWindow reference to manage the player
                if (Application.Current.MainWindow is MainWindow mainWin)
                {
                    if (mainWin.ActivePlayerWindow != null) mainWin.ActivePlayerWindow.Close();

                    // Instantiating with the VOD/Movie constructor to get timeline and commercial skipping!
                    mainWin.ActivePlayerWindow = new PlayerWindow(streamUrl, displayTitle, ep.ImageUrl, ep.Commercials);
                    mainWin.ActivePlayerWindow.Closed += (s, args) => mainWin.ActivePlayerWindow = null;
                    mainWin.ActivePlayerWindow.Show();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (EpisodesView.Visibility == Visibility.Visible)
            {
                // Go back to the Posters grid
                EpisodesView.Visibility = Visibility.Collapsed;
				FilterBar.Visibility = Visibility.Collapsed;
                ShowsScrollViewer.Visibility = Visibility.Visible;
                PageTitle.Text = "🍿 TV SHOWS";
                BackButton.Content = "🏠 Home";
                ToggleFiltersButton.Visibility = Visibility.Visible;
				
                if (ShowsWrapPanel.Children.Count > 0)
                    ((UIElement)ShowsWrapPanel.Children[0]).Focus();
            }
            else
            {
                // Go back to the Start Page
                NavigationService?.GoBack();
            }
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Allow 'Escape' or 'Backspace' on the keyboard/remote to go backward natively
            if (e.Key == Key.Escape || e.Key == Key.Back || e.Key == Key.BrowserBack)
            {
                BackButton_Click(null!, null!);
                e.Handled = true;
            }
        }
    }
}
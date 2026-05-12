using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FeralCode
{
    public partial class App : Application
    {
        public App()
        {
            // 1. Catch unhandled exceptions on the main UI thread
            this.DispatcherUnhandledException += (s, e) =>
            {
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "feral_crash_ui.txt"), e.Exception.ToString());
                e.Handled = true;
            };

            // 2. Catch unhandled exceptions on background threads (like the Web Server task)
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "feral_crash_bg.txt"), ex.ToString());
                }
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var splash = new SplashWindow();
            splash.Show();

            // 2. Do the heavy lifting: Load settings and apply the theme!
            var settings = SettingsManager.Load();
            string themeName = settings.IsLightTheme ? "LightTheme.xaml" : "DarkTheme.xaml";

            try 
            {
                var themeDict = new ResourceDictionary { Source = new Uri($"Themes/{themeName}", UriKind.Relative) };
                this.Resources.MergedDictionaries.Clear();
                this.Resources.MergedDictionaries.Add(themeDict);
            }
            catch 
            {
                // Failsafe: If the theme files are missing, WPF will just use its default grays
            }

            // 3. Keep the splash screen up just a little longer so it looks smooth and deliberate
            await Task.Delay(1500); 

            // 4. Boot up the Main Window
            var mainWindow = new MainWindow();
			Application.Current.MainWindow = mainWindow;
            mainWindow.Show();

            // 5. Close the Splash Screen seamlessly
            splash.Close();
        }
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;

namespace FeralCode
{
    public partial class App : Application
    {
        // A Mutex (Mutually Exclusive Flag) to track the app's global state
        private static Mutex? _mutex;

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

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _mutex = new Mutex(true, "FeralHTPC_Unique_App_ID", out createdNew);

            if (!createdNew)
            {
                // The app is already running in the background! 
                // Ping its local web server to wake it up, then kill this duplicate process.
                WakeUpFirstInstance();
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
            
            // If we made it here, we are the first instance. Proceed with the normal UI boot!
            RunStartupSequence();
        }

        private void WakeUpFirstInstance()
        {
            try
            {
                var settings = SettingsManager.Load();
                int port = settings.WebServerPort > 0 ? settings.WebServerPort : 12345;
                
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    // Fire a silent GET request to the original instance
                    client.GetAsync($"http://127.0.0.1:{port}/api/system/wakeup").Wait();
                }
            }
            catch { } // Silently fail if the server didn't respond
        }

        private async void RunStartupSequence()
        {
            var splash = new SplashWindow();
            splash.Show();

            var settings = SettingsManager.Load();
            string themeName = settings.IsLightTheme ? "LightTheme.xaml" : "DarkTheme.xaml";

            try 
            {
                var themeDict = new ResourceDictionary { Source = new Uri($"Themes/{themeName}", UriKind.Relative) };
                this.Resources.MergedDictionaries.Clear();
                this.Resources.MergedDictionaries.Add(themeDict);
            }
            catch { }

            await Task.Delay(1500); 

            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();

            splash.Close();
        }
    }
}
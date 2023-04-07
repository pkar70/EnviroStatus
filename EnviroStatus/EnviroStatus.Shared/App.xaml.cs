using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Navigation;

using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;

namespace EnviroStatus
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Windows.UI.Xaml.Application // : p.PkApplication // Windows.UI.Xaml.Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if __ANDROID__
            InitializeLogging(); // nowsze Uno
#endif

            this.InitializeComponent();
            //this.Suspending += OnSuspending;
#if __ANDROID__
            // for background triggers work, we have to store two value inside Uno
            Windows.ApplicationModel.Background.BackgroundTaskBuilder.UnoInitTriggers(
                typeof(AndroidJobForUWPTriggers), androidBroadcastReceiver);
#endif

        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Windows.UI.Xaml.Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
                rootFrame.Navigated += OnNavigatedAddBackButton;
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += OnBackButtonPressed;


                //if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                //{
                //    //TODO: Load state from previously suspended application
                //}

                // Place the frame in the current Window
                Windows.UI.Xaml.Window.Current.Content = rootFrame;
                p.k.InitLib(null);
                VBlib.App.CreateSourceList(p.k.IsThisMoje(), Windows.Storage.ApplicationData.Current.LocalFolder.Path);
            }
#if NETFX_CORE
            if (e.PrelaunchActivated == false)
#endif
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Windows.UI.Xaml.Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }


        // PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
        private void OnNavigatedAddBackButton(object sender, NavigationEventArgs e)
        {
            Frame oFrame = sender as Frame;
            if (oFrame == null)
                return;

            Windows.UI.Core.SystemNavigationManager oNavig = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();

            if (oFrame.CanGoBack)
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
            else
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
        }

        private void OnBackButtonPressed(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            try
            {
                if ((Window.Current.Content as Frame).CanGoBack)
                    (Window.Current.Content as Frame).GoBack();
                e.Handled = true;
            }
            catch
            {
            }
        }


        #region "Logging"

#if __ANDROID__
        // nowsze Uno
        /// <summary>
        /// Configures global Uno Platform logging
        /// </summary>
        private static void InitializeLogging()
        {
            var factory = LoggerFactory.Create(builder =>
            {
#if __WASM__
                builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
                builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#elif NETFX_CORE
                builder.AddDebug();
#else
                builder.AddConsole();
#endif

                // Exclude logs below this level
                builder.SetMinimumLevel(LogLevel.Information);

                // Default filters for Uno Platform namespaces
                builder.AddFilter("Uno", LogLevel.Warning);
                builder.AddFilter("Windows", LogLevel.Warning);
                builder.AddFilter("Microsoft", LogLevel.Warning);

                // Generic Xaml events
                // builder.AddFilter("Windows.UI.Xaml", LogLevel.Debug );
                // builder.AddFilter("Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug );
                // builder.AddFilter("Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug );
                // builder.AddFilter("Windows.UI.Xaml.UIElement", LogLevel.Debug );
                // builder.AddFilter("Windows.UI.Xaml.FrameworkElement", LogLevel.Trace );

                // Layouter specific messages
                // builder.AddFilter("Windows.UI.Xaml.Controls", LogLevel.Debug );
                // builder.AddFilter("Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug );
                // builder.AddFilter("Windows.UI.Xaml.Controls.Panel", LogLevel.Debug );

                // builder.AddFilter("Windows.Storage", LogLevel.Debug );

                // Binding related messages
                // builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );
                // builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );

                // Binder memory references tracking
                // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

                // RemoteControl and HotReload related
                // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

                // Debug JS interop
                // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
            });

            global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
        }
#endif
        #endregion 

        protected override void OnActivated(IActivatedEventArgs e)
        {
            Frame rootFrame;
            rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active

            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Navigated += OnNavigatedAddBackButton;
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += OnBackButtonPressed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
                // MakeDebugToast("OnActivated - OPEN NULL")
                rootFrame.Navigate(typeof(MainPage));

            Window.Current.Activate();
        }


#if __ANDROID__

        Android.Content.BroadcastReceiver androidBroadcastReceiver = new AndroidBroadcastReceiverForUWPTriggers();


        #region "handling TimeTrigger"

        // for API 21 (), since 2014
        [Android.App.Service(Permission = "android.permission.BIND_JOB_SERVICE")]
        public class AndroidJobForUWPTriggers : Android.App.Job.JobService
        {
            public override bool OnStartJob(Android.App.Job.JobParameters @params)
            {
                // zrob co masz do zrobienia
                // Work that will take longer that a few milliseconds should be performed on a thread to avoid blocking the application.
                System.Threading.Tasks.Task.Run(() =>
                {
                    var jobParams = @params.Extras;
                    // e.g. jobParams.GetInt("Freshness")

                    var args = new BackgroundActivatedEventArgs(@params);
                    ((App)App.Current).OnBackgroundActivated(args);

                    // Have to tell the JobScheduler the work is done. 
                    JobFinished(@params, false); // zwolnienie resources - ze niby nie trzeba Deferala brac?
                });

                // Return true because of the asynchronous work
                return true;
            }

            public override bool OnStopJob(Android.App.Job.JobParameters @params)
            {
                // Android stops our Job
                return false; // do not reschedule it
            }
        }
        #endregion

        #region "handling SystemTrigger"
        public class AndroidBroadcastReceiverForUWPTriggers : Android.Content.BroadcastReceiver
        {
            public override void OnReceive(Android.Content.Context context, Android.Content.Intent intent)
            {
                var args = new BackgroundActivatedEventArgs(intent);
                if (args.TaskInstance != null)
                {
                    ((App)App.Current).OnBackgroundActivated(args);
                }
            }
        }
        #endregion

#endif


        //public static bool mbComparing = false;

        public static async System.Threading.Tasks.Task<pkar.BasicGeopos> GetCurrentPointAsync()
        {
            // Dim oPoint As Point

            // udajemy GPSa
            if (vb14.GetSettingsBool("simulateGPS"))
                return VBlib.App.moGpsPoint;

            // na pewno ma byc wedle GPS
            VBlib.App.moGpsPoint = pkar.BasicGeopos.GetMyTestGeopos(1);

            Windows.Devices.Geolocation.GeolocationAccessStatus rVal; // = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();

            rVal = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
            if ((int)rVal != (int)Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed)
            {
                // If Not GetSettingsBool("noGPSshown") Then
                await vb14.DialogBoxResAsync("resErrorNoGPSAllowed");
                // SetSettingsBool("noGPSshown", True)
                // End If
                return VBlib.App.moGpsPoint;
            }

            Windows.Devices.Geolocation.Geolocator oDevGPS = new Windows.Devices.Geolocation.Geolocator();

            Windows.Devices.Geolocation.Geoposition oPos;
            TimeSpan oCacheTime = new TimeSpan(0, 2, 0);  // 2 minuty 
            TimeSpan oTimeout = new TimeSpan(0, 0, 10);    // timeout 
            bool bErr = false;
            try
            {
                oPos = await oDevGPS.GetGeopositionAsync(oCacheTime, oTimeout);
                VBlib.App.moGpsPoint = pkar.BasicGeopos.FromObject(oPos.Coordinate.Point.Position); //.ToMyGeopos();
            }
            catch 
            {
                bErr = true;
            }

            if (bErr)
            {
                await vb14.DialogBoxResAsync("resErrorGettingPos");

                VBlib.App.moGpsPoint = pkar.BasicGeopos.GetMyTestGeopos(2);
                //moGpsPoint.Latitude = 50.06; // Kraków wedle Wikipedii
                //moGpsPoint.Longitude = 19.93;
            }

            return VBlib.App.moGpsPoint;
        }


        public static void KoncowkaPokazywaniaDanych()
        {
            vb14.DumpCurrMethod();

            VBlib.App.KoncowkaPokazywaniaDanych();

            App.Cache_Save();
            VBlib.App.moLastPomiar = DateTime.Now;

            if (p.k.GetPlatform("uwp")) VBlib.App.TryDataLog();

            //MakeToast("po datalog")
            UpdateTile();
            //MakeToast("po tile")
            vb14.DumpMessage("app:KoncowkaPokazywaniaDanych() end");
        }

        #region "operacje na Tile"

#if !NETFX_CORE
        public static void UpdateTile()
        {
            // empty dla Android - ale wywolywany. Prosciej zmienic tu, niz w calosci kodu
        }
#else

        private static string GetTileXml(string sPomiar, double dValue)
        {
            // mogłoby być i w VBLIB (jako method w Pomiar), ale to mocno związane z UI jednak jest

            string sTmp;
            sTmp = "<tile><visual>";
            sTmp = sTmp + "<binding template='TileSmall' branding='none' hint-textStacking='center' >";
            sTmp = sTmp + "<text hint-style='title' hint-align='center'>" + dValue.ToString("###0") + "</text>";
            sTmp = sTmp + "<text hint-style='caption' hint-align='center'>" + sPomiar + "</text>";
            sTmp = sTmp + "</binding>";

            sTmp = sTmp + "<binding template='TileMedium' hint-textStacking='center'>";
            sTmp = sTmp + "<text hint-style='title' hint-align='center'>" + dValue.ToString("###0") + "</text>";
            sTmp = sTmp + "<text hint-style='caption' hint-align='center'>" + sPomiar + "</text>";
            sTmp = sTmp + "</binding>";

            sTmp = sTmp + "</visual></tile>";

            return sTmp;
        }

        private static Windows.UI.Notifications.TileNotification GetTileObject(string sPomiar, double dValue)
        {
            Windows.UI.Notifications.TileNotification oTile = null;

            try
            {
                string sXml = GetTileXml(sPomiar, dValue);
                var oXml = new Windows.Data.Xml.Dom.XmlDocument();
                oXml.LoadXml(sXml);
                oTile = new Windows.UI.Notifications.TileNotification(oXml);
                oTile.ExpirationTime = DateTime.Now.AddHours(2);
            }
            catch
            {
            }

            return oTile;
        }

        private static string GetEmptyTileXml(string sPomiar, string sSource)
        {
            // mogłoby być i w VBLIB (jako method w Pomiar), ale to mocno związane z UI jednak jest

            string sTmp;
            sTmp = "<tile><visual>";
            sTmp = sTmp + "<binding template='TileSmall' branding='none' hint-textStacking='center' >";
            sTmp = sTmp + "<text hint-style='title' hint-align='center'>" + sPomiar + "</text>";
            sTmp = sTmp + "<text hint-style='caption' hint-align='center'>" + sSource + "</text>";
            sTmp = sTmp + "</binding>";

            sTmp = sTmp + "<binding template='TileMedium' hint-textStacking='center'>";
            sTmp = sTmp + "<text hint-style='title' hint-align='center'>" + sPomiar + "</text>";
            sTmp = sTmp + "<text hint-style='caption' hint-align='center'>" + sSource + "</text>";
            sTmp = sTmp + "</binding>";

            sTmp = sTmp + "</visual></tile>";

            return sTmp;
        }


        private static Windows.UI.Notifications.ScheduledTileNotification GetEmptyTileObject(string sPomiar, string sSource)
        {
            Windows.UI.Notifications.ScheduledTileNotification oTile = null;
            try
            {
                string sXml = GetEmptyTileXml(sPomiar, sSource);
                var oXml = new Windows.Data.Xml.Dom.XmlDocument();
                oXml.LoadXml(sXml);
                oTile = new Windows.UI.Notifications.ScheduledTileNotification(oXml, DateTime.Now.AddHours(2));
            }
            catch 
            {
            }

            return oTile;
        }

        public static string GetNameForSecTile(VBlib.JedenPomiar oPomiar)
        {
            // mogłoby być i w VBLIB (jako method w Pomiar), ale to mocno związane z UI jednak jest
            // stąd, oraz z mainpage. Nie może być spacji w nazwie!
            string sName;
            sName = oPomiar.sPomiar + "(" + oPomiar.sSource + ")";
            sName = sName.Replace(" ", "_"); // rzeka_cm
            sName = sName.Replace("μ", "u"); // μs/h
            return sName;
        }


        public static void UpdateTile()
        {
            vb14.DumpCurrMethod("UpdateTile() started");

            // *TODO* mozna sie pobawic jeszcze w kolorki:
            // SecondaryTileVisualElements.BackgroundColor

            bool bXbox = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily.ToLower().Contains("xbox");

            try     // jesli sie cos nie uda, to zignoruj robienie Tile
            {
                string sReqPomiar = vb14.GetSettingsString("settingsLiveTile");
                Windows.UI.Notifications.TileNotification oTile;
                Windows.UI.Notifications.ScheduledTileNotification oTileEmpty;


                foreach (VBlib.JedenPomiar oPomiar in VBlib.App.moPomiaryAll)
                {

                    // przygotowanie zawartości Tile
                    oTile = GetTileObject(oPomiar.sPomiar, oPomiar.dCurrValue);
                    if (oTile == null)
                        break;
                    oTileEmpty = GetEmptyTileObject(oPomiar.sPomiar, oPomiar.sSource);

                    Windows.UI.Notifications.TileUpdater oTUPS;
                    // jeśli nie ma empty, a jest oTile, to mozna sprobowac primary empty - dlatego nie exit
                    if (oTileEmpty != null)
                    {
                        string sName = App.GetNameForSecTile(oPomiar);

                        if (!bXbox)  // XBox nie ma secondary tile
                            if (Windows.UI.StartScreen.SecondaryTile.Exists(sName))
                            {
                                try
                                {
                                    oTUPS = Windows.UI.Notifications.TileUpdateManager.CreateTileUpdaterForSecondaryTile(sName);
                                }
                                catch 
                                {
                                    oTUPS = null;
                                }

                                if (oTUPS != null)
                                {
                                    // próba ustawienia secondary Tile
                                    oTUPS.Clear();

                                    oTUPS.Update(oTile);
                                    oTUPS.AddToSchedule(oTileEmpty);
                                }
                            }
                    }

                    // próba ustawienia primary Tile
                    if (sReqPomiar == oPomiar.sPomiar + " (" + oPomiar.sSource + ")")
                    {
                        oTUPS = Windows.UI.Notifications.TileUpdateManager.CreateTileUpdaterForApplication();
                        oTUPS.Update(oTile);
                    }
                }
            }
            catch
            {
            }
        }
#endif
        #endregion

#if NETFX_CORE
        private Windows.ApplicationModel.Background.BackgroundTaskDeferral moTaskDeferal = null;
        private Windows.ApplicationModel.AppService.AppServiceConnection moAppConn;
#endif

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            // tile update / warnings / etc.
            bool bNoComplete = false;
            //string sTttt = vb14.GetLangString("_lang");

#if NETFX_CORE
            // Windows.ApplicationModel.Background.BackgroundTaskDeferral oTimerDeferal;
            moTaskDeferal = args.TaskInstance.GetDeferral();
#endif

            p.k.InitLib(null);
            VBlib.App.CreateSourceList(p.k.IsThisMoje(), Windows.Storage.ApplicationData.Current.LocalFolder.Path);

            switch (args.TaskInstance.Task.Name)
            {
                case "EnviroStat_Timer":
                    {

                        vb14.SetSettingsString("lastTimer", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        if (vb14.GetSettingsBool("testTimera"))
                        {
                            vb14.MakeToast("test run", "OnBackgroundActivated from timer");
                            vb14.SetSettingsBool("testTimera", false);
                        }


                        if (!p.k.NetIsIPavailable(false))
                            return;

                        // nie ma sensu wczytywać danych jak nie będzie toastów
                        string sToastSett = vb14.GetSettingsString("settingsAlerts");
                        if (sToastSett.IndexOf("(!") < 0)
                            return;

                        string sFavName = vb14.GetSettingsString("settingStartPage");
                        sFavName = vb14.GetSettingsString("currentFav", sFavName);

                        if (!string.IsNullOrEmpty(sFavName))
                        {
                            await GetFavDataAsync(sFavName, true);
                            // MakeToast("po GetFavData")
                            KoncowkaPokazywaniaDanych();
                            // MakeToast("po Koncowka")
                            VBlib.App.ZrobToasty(p.k.GetPlatform("android"));
                        }

                        break;
                    }

                case "EnviroStat_UserPresent":
                    {
                        break;
                    }

                default:
                    {
#if NETFX_CORE
                        var oDetails = args.TaskInstance.TriggerDetails as Windows.ApplicationModel.AppService.AppServiceTriggerDetails;
                        if (oDetails != null)
                        {
                            bNoComplete = true;
                            // zrob co trzeba
                            //moTaskDeferal = args.TaskInstance.GetDeferral();
                            args.TaskInstance.Canceled += OnTaskCanceled;
                            moAppConn = oDetails.AppServiceConnection;
                            moAppConn.RequestReceived += OnRequestReceived;
                        }
#endif
                        break;
                    }
            }


#if NETFX_CORE
            if (!bNoComplete) moTaskDeferal.Complete();
#endif
        }

        #region "AppServiceConnection"
#if NETFX_CORE
        private void OnTaskCanceled(Windows.ApplicationModel.Background.IBackgroundTaskInstance sender, Windows.ApplicationModel.Background.BackgroundTaskCancellationReason reason)
        {
            if (moTaskDeferal != null)
            {
                moTaskDeferal.Complete();
                moTaskDeferal = null;
            }
        }


        private string AppServiceGetData(string sMask)
        {
            if (!sMask.StartsWith("get ")) return "";

            if (VBlib.App.moPomiaryAll.Count() < 1) Cache_Load();
            if (VBlib.App.moPomiaryAll.Count() < 1) return "ERROR: empty data?";

            sMask = sMask.Substring(4);   // czyli bez "get "
            string sResult = "";

            foreach (VBlib.JedenPomiar oPomiar in VBlib.App.moPomiaryAll)
            {
                if (oPomiar.sPomiar.ToLower().Contains(sMask))
                {
                    sResult = sResult + "\n" + oPomiar.sPomiar + ": " + oPomiar.sCurrValue;
                    if (!string.IsNullOrEmpty(oPomiar.sAlert))
                        sResult = sResult + " " + oPomiar.sAlert;
                    sResult = sResult + " (" + oPomiar.sSource + " @" + oPomiar.sTimeStamp + ")";
                }
            }

            sResult = sResult.Trim();
            if (sResult == "")
                return "No measurement matching '" + sMask + "'";
            else
                return "Measurements matching '" + sMask + "':\n" + sResult;

        }

        private async void OnRequestReceived(Windows.ApplicationModel.AppService.AppServiceConnection sender, Windows.ApplicationModel.AppService.AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral so we can use an awaitable API to respond to the message 
            var messageDeferral = args.GetDeferral();
            Windows.Foundation.Collections.ValueSet oInputMsg = args.Request.Message;
            Windows.Foundation.Collections.ValueSet oResultMsg = new Windows.Foundation.Collections.ValueSet();

            string sStatus = "ERROR while processing command";
            string sResult = "";
            try
            {
                string sCommand = oInputMsg["command"].ToString().ToLower();

                string sLocalCmds = "apikey\t get DarkSky API key (used in MyCamera)\n" +
                                "alerts\t get all alerts (used in MyCamera) \n" +
                                "datacache \t  dump current data (XML format)\n" +
                                "get MASK\t  dump measurements by MASK (TXT format) \n" +
                                "envirostatus\t show current alert level (!, !!, or !!!)";

                sResult = p.k.AppServiceStdCmd(sCommand, sLocalCmds);
                if (sResult == "") sResult = AppServiceGetData(sCommand); // pobranie danych


                if (sResult == "")
                {
                    // komendy stare
                    switch (sCommand.ToLower())
                    {
                        case "apikey": // <-- used by MyCameras

                            if (!vb14.GetSettingsBool("settingsRemSysAPI"))
                            {
                                sStatus = "ERROR: no permission";
                            }
                            else
                            {
                                sResult = "OK";
                                oResultMsg.Add("key", vb14.GetSettingsString("sourceDarkSky_apikey"));
                            }
                            break;

                        case "datacache":
                            {
                                if (!vb14.GetSettingsBool("settingsRemSysData"))
                                    sStatus = "ERROR: no permission";
                                else
                                {

                                    // zawsze odczyt - bo jesli to tylko posrednik, to w pamieci ma stare dane, a plik moze byc nowszy
                                    Cache_Load();

                                    if (VBlib.App.moPomiaryAll.Count() < 1)
                                        sStatus = "ERROR: empty data?";
                                    else
                                    {
                                        // wyslij z pamieci
                                        string sDumpData = VBlib.App.GetPomiaryAllAsString();
                                        if (sDumpData.Length > 28000)
                                            sStatus = "ERROR: too much data";
                                        else
                                        {
                                            sResult = sDumpData;
                                        }
                                    }
                                }

                                break;
                            }

                        case "envirostatus":
                            {
                                if (!vb14.GetSettingsBool("settingsRemSysData"))
                                    sStatus = "ERROR: no permission";
                                else
                                {

                                    // zawsze odczyt - bo jesli to tylko posrednik, to w pamieci ma stare dane, a plik moze byc nowszy
                                    Cache_Load();

                                    if (VBlib.App.moPomiaryAll.Count() < 1)
                                        sStatus = "ERROR: empty data?";
                                    else
                                    {
                                        foreach (VBlib.JedenPomiar oPomiar in VBlib.App.moPomiaryAll)
                                        {
                                            if (oPomiar.sAlert.Length > sResult.Length)
                                                sResult = oPomiar.sAlert;
                                        }                            // wyslij z pamieci
                                    }
                                }

                                break;
                            }

                        case "alerts":  // <-- used by MyCameras
                            {
                                // odpowiednik msgboxu
                                string sToastMsg = "";
                                string sLevel = "!";
                                try
                                {
                                    sLevel = oInputMsg["level"].ToString();
                                }
                                catch 
                                {
                                }
                                foreach (VBlib.JedenPomiar oItem in VBlib.App.moPomiaryAll)
                                {
                                    if (oItem.sAlert.Length >= sLevel.Length)
                                    {
                                        // fragmenty zabrane z ZrobToast, ale tu mamy (byc moze) inny poziom alertowania
                                        if ((oItem.sSource ?? "") == "NOAAalert")
                                            sToastMsg = sToastMsg + oItem.sAlert + " " + oItem.sCurrValue + " (" + oItem.sSource + ")\n";
                                        else if (oItem.sPomiar.StartsWith("Alert") && (oItem.sSource ?? "") == "DarkSky")
                                            sToastMsg = sToastMsg + oItem.sAlert + " " + oItem.sCurrValue + " (" + oItem.sSource + ")";
                                        else
                                        {
                                            string sOneParam = oItem.sPomiar + " (" + oItem.sSource + ")";    // PM10 (Airly)
                                            string sOneParamAlert = oItem.sAlert + " " + sOneParam;
                                            sToastMsg = sToastMsg + sOneParamAlert + "\n";
                                        }
                                    }
                                }
                                if (string.IsNullOrEmpty(sToastMsg))
                                    sToastMsg = "(empty)";
                                oResultMsg.Add("alerty", sToastMsg);
                                sResult = "OK";
                                break;
                            }

                        default:
                            {
                                sStatus = "ERROR unknown command";
                                break;
                            }
                    }
                }
            }
            catch 
            {
            }

            if (sResult != "") sStatus = "OK";
            // odsylamy cokolwiek - zeby "tamta strona" cos zobaczyla
            oResultMsg.Add("result", sResult);
            oResultMsg.Add("status", sStatus);
            await args.Request.SendResponseAsync(oResultMsg);

            messageDeferral.Complete();
        }
#endif
        #endregion

        public static async System.Threading.Tasks.Task<string> SourcesUsedInTimerAsync()
        {

            string sRet = "";
            string sId;
            int iInd;

#if NETFX_CORE
            foreach (Windows.UI.StartScreen.SecondaryTile oTile in await Windows.UI.StartScreen.SecondaryTile.FindAllAsync())
            {
                sId = oTile.TileId;
                iInd = sId.LastIndexOf("(");
                if (iInd > 0)
                    sRet = sRet + "|" + sId.Substring(iInd + 1);
            }
#endif
            sId = vb14.GetSettingsString("settingsLiveTile");
            iInd = sId.LastIndexOf("(");
            if (iInd > 0)
                sRet = sRet + "|" + sId.Substring(iInd + 1);

            if (vb14.GetSettingsString("settingsAlerts").IndexOf("!") < 0)
                return sRet;
            // skoro maja byc Toasty, to dopiszmy to co jest wykrzyknikowalne

            // sRet = sRet & "|airly|gios|IMGWhyd|DarkSky|NOAAkind" ' hydro: poziom wody!, DarkSky - ostrzezenia
            // czyli bez r@h, IMGWmet, foreca
            foreach (VBlib.Source_Base oZrodlo in VBlib.App.gaSrc)
            {
                if (oZrodlo.SRC_IN_TIMER)
                    sRet = sRet + "|" + oZrodlo.SRC_POMIAR_SOURCE;
            }

            return sRet;
        }

        public static async System.Threading.Tasks.Task GetFavDataAsync(string sFavName, bool bInTimer)
        {
            if (!p.k.NetIsIPavailable(false))
                return;

            string sInTiles = "";
            if (bInTimer)
                sInTiles = await SourcesUsedInTimerAsync();

            await VBlib.App.GetFavDataAsync(sFavName, bInTimer, sInTiles);
        }

        #region cache
        /// <summary>
        /// wymaga Windows.Storage.ApplicationData.Current
        /// </summary>
        public static DateTimeOffset Cache_Load()
        {
            vb14.DumpCurrMethod();

            string sLocal = "";
            string sRoam = "";

            // local file
            if (vb14.GetSettingsBool("settingsFileCache"))
                sLocal = Windows.Storage.ApplicationData.Current.TemporaryFolder.Path;

            // roaming file
            if (vb14.GetSettingsBool("settingsFileCacheRoam"))
                sRoam = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;

            return VBlib.App.Cache_Load(sLocal, sRoam);

        }

        /// <summary>
        /// wymaga Windows.Storage.ApplicationData.Current
        /// </summary>
        public static void Cache_Save()
        {
            vb14.DumpCurrMethod();

            string sLocal = "";
            string sRoam = "";

            // local file
            if (vb14.GetSettingsBool("settingsFileCache"))
                sLocal = Windows.Storage.ApplicationData.Current.TemporaryFolder.Path;

            // roaming file
            if (vb14.GetSettingsBool("settingsFileCacheRoam"))
                sRoam = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;

            VBlib.App.Cache_Save(sLocal, sRoam);
        }

        #endregion 

    }
}

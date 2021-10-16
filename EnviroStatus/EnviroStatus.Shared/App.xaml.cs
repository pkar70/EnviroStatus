using Microsoft.Extensions.Logging;
using System;
//using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Controls.Primitives;
//using Windows.UI.Xaml.Data;
//using Windows.UI.Xaml.Input;
//using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace EnviroStatus
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Windows.UI.Xaml.Application
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




        // public static Windows.Foundation.Point moGpsPoint;
        public static Windows.Devices.Geolocation.BasicGeoposition moGpsPoint;
        public static bool mbComparing = false;

        public static async System.Threading.Tasks.Task<Windows.Devices.Geolocation.BasicGeoposition> GetCurrentPoint()
        {
            // Dim oPoint As Point

            // udajemy GPSa
            if (p.k.GetSettingsBool("simulateGPS"))
                return moGpsPoint;

            // na pewno ma byc wedle GPS
            //moGpsPoint.Latitude = 50.0; // 1985 ' latitude - dane domku, choc mała precyzja
            //moGpsPoint.Longitude = 19.9; // 7872
            moGpsPoint = p.k.GetDomekGeopos(1);

            Windows.Devices.Geolocation.GeolocationAccessStatus rVal; // = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();

            rVal = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
            if ((int)rVal != (int)Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed)
            {
                // If Not GetSettingsBool("noGPSshown") Then
                await p.k.DialogBoxResAsync("resErrorNoGPSAllowed");
                // SetSettingsBool("noGPSshown", True)
                // End If
                return moGpsPoint;
            }

            Windows.Devices.Geolocation.Geolocator oDevGPS = new Windows.Devices.Geolocation.Geolocator();

            Windows.Devices.Geolocation.Geoposition oPos;
            TimeSpan oCacheTime = new TimeSpan(0, 2, 0);  // 2 minuty 
            TimeSpan oTimeout = new TimeSpan(0, 0, 10);    // timeout 
            bool bErr = false;
            try
            {
                oPos = await oDevGPS.GetGeopositionAsync(oCacheTime, oTimeout);
                moGpsPoint= oPos.Coordinate.Point.Position;
            }
            catch (Exception ex)
            {
                bErr = true;
            }

            if (bErr)
            {
                await p.k.DialogBoxResAsync("resErrorGettingPos");

                moGpsPoint = p.k.GetDomekGeopos(2);
                //moGpsPoint.Latitude = 50.06; // Kraków wedle Wikipedii
                //moGpsPoint.Longitude = 19.93;
            }

            return moGpsPoint;
        }



        public static async System.Threading.Tasks.Task<Windows.Storage.StorageFile> GetDataFile(bool bRoam, string sName, bool bCreate)
        {
            Windows.Storage.StorageFolder oFold;
            if (bRoam)
                oFold = Windows.Storage.ApplicationData.Current.RoamingFolder;
            else
                oFold = Windows.Storage.ApplicationData.Current.LocalFolder;

            if (oFold == null)
            {
                await p.k.DialogBoxResAsync("errNoRoamFolder");
                return null;
            }

            bool bErr = false;
            Windows.Storage.StorageFile oFile = null;
            try
            {
                if (bCreate)
                {
#if !NETFX_CORE
                    // usun plik, bo nie mamy tu daty Modified a tylko Create
                    oFile = await oFold.TryGetItemAsync(sName) as Windows.Storage.StorageFile;
                    if (oFile != null) await oFile.DeleteAsync();
#endif
                    oFile = await oFold.CreateFileAsync(sName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                }
                else
                    oFile = (Windows.Storage.StorageFile)await oFold.TryGetItemAsync(sName);
            }
            catch (Exception ex)
            {
                bErr = true;
            }

            if (oFile == null)
                bErr = true;
            if (bErr)
                return null;

            return oFile;
        }

        public static async System.Threading.Tasks.Task TryDataLog()
        {
            p.k.DebugOut("TryDataLog() started");

            if (!p.k.GetSettingsBool("settingsDataLog"))
                return;

            if (!p.k.GetPlatform("uwp")) return;

#if NETFX_CORE
            try
            {
                Windows.Storage.StorageFolder oFolder = await p.k.GetLogFolderMonthAsync();

                if (oFolder == null)
                    return ;

                string sFileName = DateTime.Now.ToString("yyyy.MM.dd.HH.mm") + ".xml";
                Windows.Storage.StorageFile oFile = await oFolder.CreateFileAsync(sFileName, Windows.Storage.CreationCollisionOption.OpenIfExists);

                System.Xml.Serialization.XmlSerializer oSer = new System.Xml.Serialization.XmlSerializer(typeof(Collection<JedenPomiar>));

                Stream oStream = await oFile.OpenStreamForWriteAsync();
                oSer.Serialize(oStream, moPomiaryAll);
                oStream.Dispose();   // == fclose
            }
            catch (Exception ex)
            {
            }
#endif
        }

        private static void UsunPowtorki()
        {
            p.k.DebugOut("UsunPowtorki() started");

            for (int i0 = 0, loopTo = moPomiaryAll.Count() - 1; i0 <= loopTo; i0++)
            {
                JedenPomiar oPomiar = moPomiaryAll[i0];
                for (int i1 = 0, loopTo1 = moPomiaryAll.Count() - 1; i1 <= loopTo1; i1++)
                {
                    if (i0 != i1)
                    {
                        JedenPomiar oPomiar1 = moPomiaryAll[i1];
                        // na pozniej >=, ale na razie trzeba wychwycic czemu rosnie
                        if ((oPomiar1.sSource ?? "") == (oPomiar.sSource ?? "") && (oPomiar1.sPomiar ?? "") == (oPomiar.sPomiar ?? "") && oPomiar1.dOdl > oPomiar.dOdl)
                            oPomiar1.bDel = true;
                    }
                }
            }
        }

        // Private Shared msLastToast As String = ""

        private static void ZrobToasty()
        {
            // a teraz toasty
            string sToastSett = p.k.GetSettingsString("settingsAlerts");
            int iInd;
            iInd = sToastSett.IndexOf("(!");
            if (iInd < 0)
                return;
            sToastSett = sToastSett.Substring(iInd + 1).Replace(")", "");
            // sToastSett = !|!!|!!!
            string sLastToast = p.k.GetSettingsString("lastToast");

            // If sToastSett.IndexOf("!") > 0 Then iToastMode = 1
            // If sToastSett.IndexOf("!!") > 0 Then iToastMode = 2
            // If sToastSett.IndexOf("!!!") > 0 Then iToastMode = 3
            // If iToastMode = 0 Then Exit Function

            string sToastMsg = "";
            string sToastMemory = "";

            p.k.DebugOut("Poprzednie alerty: " + sLastToast);

            //        Dim aLastAlerts As String() = sLastToast.Replace(vbCrLf, vbCr).Trim.Split(vbCr)
            string[] aLastAlerts = sLastToast.Replace("\r\n", "\r").Trim().Split('\r');
            bool bCleanAir = true;

            foreach (JedenPomiar oItem in moPomiaryAll)
            {
                if (oItem.bDel)
                    continue;

                if (!oItem.bCleanAir) bCleanAir = false;    // 2021.01.28

                string sAlertTmp = oItem.sAlert;
                if (sAlertTmp.Length < sToastSett.Length)
                    sAlertTmp = "";             // !!

                string sOneParam = oItem.sPomiar + " (" + oItem.sSource + ")";    // PM10 (Airly)

                if ((oItem.sSource ?? "") == "NOAAalert")
                {
                    // dla DarkSky toast, oraz NOAAalert, ma pokazac pelniejsze info
                    // toastMemory - nie zapisujemy, bo i tak nie odczyta drugi raz tego samego
                    // tylko do wyswietlenia podaje wiecej
                    string sTmp;
                    sTmp = oItem.sAlert + " " + oItem.sCurrValue + " (" + oItem.sSource + ")\n";
                    sToastMemory = sToastMemory + sTmp; // 2021.09.26: jednak włączam ta linię (była zakomentowana)
                    if (!sLastToast.Contains(oItem.sCurrValue))
                        sToastMsg = sToastMsg + sTmp;
                }
                else
                {
                    string sOneParamAlert = sAlertTmp + " " + sOneParam;              // !! PM10 (Airly)
                                                                                      // (a) dokladnie to samo bylo wczesniej

                    p.k.DebugOut(" analiza aktualnego: " + sOneParamAlert);

                    int iPoprzedniStatus = 0;
                    foreach (string sPrevAlert in aLastAlerts)
                    {
                        p.k.DebugOut("  poprzedni wpis: " + sPrevAlert);
                        if ((sPrevAlert.Trim() ?? "") == (sOneParamAlert.Trim() ?? ""))
                        {
                            iPoprzedniStatus = 1;
                            p.k.DebugOut("- byl taki sam");
                            break;
                        }
                        else if (sPrevAlert.Contains(sOneParamAlert))
                        {
                            iPoprzedniStatus = 2;
                            p.k.DebugOut("- byl krotszy");
                            break;
                        }
                        else if (sPrevAlert.Contains(sOneParam))
                        {
                            iPoprzedniStatus = 3;
                            p.k.DebugOut("- byl dluzszy");
                            break;
                        }
                    }

                    switch (iPoprzedniStatus)
                    {
                        case 0:
                        case 3:  // nie bylo, bądź było mniejsze
                                if (!string.IsNullOrEmpty(sAlertTmp))
                                {
                                    if (oItem.sPomiar.StartsWith("Alert") && (oItem.sSource ?? "") == "DarkSky")
                                        sToastMsg = sToastMsg + oItem.sAlert + " " + oItem.sCurrValue + " (" + oItem.sSource + ")\n";
                                    else
                                        sToastMsg = sToastMsg + sOneParamAlert + "\n";
                                    sToastMemory = sToastMemory + sOneParamAlert + "\n";
                                }

                                break;
                        case 1:  // bylo takie samo
                                // If na wypadek gdy błąd
                                if (!string.IsNullOrEmpty(sAlertTmp))
                                    sToastMemory = sToastMemory + sOneParamAlert + "\n";
                                break;
                        case 2:  // bylo wieksze
                                if (!string.IsNullOrEmpty(sAlertTmp))
                                    sToastMemory = sToastMemory + sOneParamAlert + "\n";
                                else
                                    sToastMsg = sToastMsg + "(ok) " + sOneParam + "\n";
                                break;
                    }
                }
            }

            p.k.DebugOut("nowy toastmemory" + sToastMemory);
            p.k.DebugOut("toast string" + sToastMsg);

            p.k.SetSettingsString("lastToast", sToastMemory);
            if (string.IsNullOrEmpty(sToastMemory))
            {
                if (string.IsNullOrEmpty(sLastToast))
                    return;
            }

            if(bCleanAir && (!p.k.GetSettingsBool("cleanAir")))
            {
                if (string.IsNullOrEmpty(sToastMemory))
                    sToastMsg = p.k.GetSettingsString("resAllOk"); // GetLangString("msgAllOk")
                else
                    sToastMsg = sToastMsg + "\n" + p.k.GetSettingsString("resAllOk"); 
            }
            p.k.SetSettingsBool("cleanAir", bCleanAir);

            if (!string.IsNullOrEmpty(sToastMsg))
            {
                var arraj = sToastMsg.Split('\n');
                if(arraj.Count() > 1 && p.k.GetPlatform("android"))
                { // dla Android: trzeba przejsc do rozwijanego toastu, wiec dwa parametry potrzebne są
                    p.k.MakeToast(p.k.GetSettingsString("resAndroidToastTitle"), sToastMsg);
                }
                else
                {   // jednolinijkowy, lub nie Android
                    p.k.MakeToast(sToastMsg);
                }

            }
        }

        public static async System.Threading.Tasks.Task KoncowkaPokazywaniaDanych()
        {
            p.k.DebugOut("app:KoncowkaPokazywaniaDanych() started");

            //return;
            // reszta: odtworzona, bo próbowałem jak nie działało z samym return, najwyraźniej skasowalem potem :(

            App.DodajPrzekroczenia();
            UsunPowtorki();
            //MakeToast("po UsunPowtorki")
            DodajTempOdczuwana();
            //MakeToast("po Tapp")

            await App.Cache_Save();
            moLastPomiar = DateTime.Now;

            await App.TryDataLog();
            //MakeToast("po datalog")
            UpdateTile();
            //MakeToast("po tile")
            p.k.DebugOut("app:KoncowkaPokazywaniaDanych() end");
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
            catch (Exception ex)
            {
            }

            return oTile;
        }

        public static string GetNameForSecTile(JedenPomiar oPomiar)
        {
            // stąd, oraz z mainpage. Nie może być spacji w nazwie!
            string sName;
            sName = oPomiar.sPomiar + "(" + oPomiar.sSource + ")";
            sName = sName.Replace(" ", "_"); // rzeka_cm
            sName = sName.Replace("μ", "u"); // μs/h
            return sName;
        }


        public static void UpdateTile()
        {
            p.k.DebugOut("UpdateTile() started");

            // *TODO* mozna sie pobawic jeszcze w kolorki:
            // SecondaryTileVisualElements.BackgroundColor

            bool bXbox = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily.ToLower().Contains("xbox");

            try     // jesli sie cos nie uda, to zignoruj robienie Tile
            {
                string sReqPomiar = p.k.GetSettingsString("settingsLiveTile");
                Windows.UI.Notifications.TileNotification oTile;
                Windows.UI.Notifications.ScheduledTileNotification oTileEmpty;


                foreach (EnviroStatus.JedenPomiar oPomiar in moPomiaryAll)
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

                        if(!bXbox)  // XBox nie ma secondary tile
                            if (Windows.UI.StartScreen.SecondaryTile.Exists(sName))
                            {
                                try
                                {
                                    oTUPS = Windows.UI.Notifications.TileUpdateManager.CreateTileUpdaterForSecondaryTile(sName);
                                }
                                catch (Exception ex)
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
                    if ((sReqPomiar ?? "") == (oPomiar.sPomiar + " (" + oPomiar.sSource + ")" ?? ""))
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

#if NETFX_CORE
            // Windows.ApplicationModel.Background.BackgroundTaskDeferral oTimerDeferal;
            moTaskDeferal = args.TaskInstance.GetDeferral();
#endif
            switch (args.TaskInstance.Task.Name)
            {
                case "EnviroStat_Timer":
                    {
                        p.k.SetSettingsString("lastTimer", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        if (p.k.GetSettingsBool("testTimera"))
                        {
                            p.k.MakeToast("test run", "OnBackgroundActivated from timer");
                            p.k.SetSettingsBool("testTimera", false);
                        }


                        if (!p.k.NetIsIPavailable(false))
                            return;

                        // nie ma sensu wczytywać danych jak nie będzie toastów
                        string sToastSett = p.k.GetSettingsString("settingsAlerts");
                        if(sToastSett.IndexOf("(!") < 0)
                            return;

                        string sFavName = p.k.GetSettingsString("settingStartPage");
                        sFavName = p.k.GetSettingsString("currentFav", sFavName);

                        if (!string.IsNullOrEmpty(sFavName))
                        {
                            await GetFavData(sFavName, true);
                            // MakeToast("po GetFavData")
                            await KoncowkaPokazywaniaDanych();
                            // MakeToast("po Koncowka")
                            ZrobToasty();
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
            if(!bNoComplete) moTaskDeferal.Complete();
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


        private async System.Threading.Tasks.Task<string> AppServiceGetData(string sMask)
        {
            if (!sMask.StartsWith("get ")) return "";

            if (moPomiaryAll.Count() < 1) await Cache_Load();
            if (moPomiaryAll.Count() < 1) return "ERROR: empty data?";

            sMask = sMask.Substring(4);   // czyli bez "get "
            string sResult = "";

            foreach (JedenPomiar oPomiar in moPomiaryAll)
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
                if (sResult == "") sResult = await AppServiceGetData(sCommand); // pobranie danych


                if (sResult == "")
                {
                    // komendy stare
                    switch (sCommand.ToLower())
                    {
                        case "apikey": // <-- used by MyCameras

                            if (!p.k.GetSettingsBool("settingsRemSysAPI"))
                            {
                                sStatus = "ERROR: no permission";
                            }
                            else
                            {
                                sResult = "OK";
                                oResultMsg.Add("key", p.k.GetSettingsString("sourceDarkSky_apikey"));
                            }
                            break;

                        case "datacache":
                            {
                                if (!p.k.GetSettingsBool("settingsRemSysData"))
                                    sStatus = "ERROR: no permission";
                                else
                                {

                                    // zawsze odczyt - bo jesli to tylko posrednik, to w pamieci ma stare dane, a plik moze byc nowszy
                                    await Cache_Load();

                                    if (moPomiaryAll.Count() < 1)
                                        sStatus = "ERROR: empty data?";
                                    else
                                    {
                                        // wyslij z pamieci
                                        System.Xml.Serialization.XmlSerializer oSer;
                                        oSer = new System.Xml.Serialization.XmlSerializer(typeof(JedenPomiar));
                                        Stream oStream = new MemoryStream();
                                        oSer.Serialize(oStream, moPomiaryAll);
                                        oStream.Flush();
                                        var oRdr = new StreamReader(oStream);
                                        string sTmp = oRdr.ReadToEnd();
                                        if (sTmp.Length > 28000)
                                            sStatus = "ERROR: too much data";
                                        else
                                        {
                                            sResult = sTmp;
                                        }

                                        oRdr.Dispose();
                                    }
                                }

                                break;
                            }

                        case "envirostatus":
                            {
                                if (!p.k.GetSettingsBool("settingsRemSysData"))
                                    sStatus = "ERROR: no permission";
                                else
                                {

                                    // zawsze odczyt - bo jesli to tylko posrednik, to w pamieci ma stare dane, a plik moze byc nowszy
                                    await Cache_Load();

                                    if (moPomiaryAll.Count() < 1)
                                        sStatus = "ERROR: empty data?";
                                    else
                                    {
                                        foreach (JedenPomiar oPomiar in moPomiaryAll)
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
                                catch (Exception ex)
                                {
                                }
                                foreach (JedenPomiar oItem in moPomiaryAll)
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
            catch (Exception ex)
            {
            }

            if (sResult != "" ) sStatus = "OK";
            // odsylamy cokolwiek - zeby "tamta strona" cos zobaczyla
            oResultMsg.Add("result", sResult);
            oResultMsg.Add("status", sStatus);
            await args.Request.SendResponseAsync(oResultMsg);

            messageDeferral.Complete();
        }
#endif
#endregion
        private static string Wykrzyknikuj(double dCurrent, double dJeden, double dDwa, double dTrzy)
        {
            if (dCurrent < dJeden)
                return "";
            if (dCurrent < dDwa)
                return "!";
            if (dCurrent < dTrzy)
                return "!!";
            return "!!!";
        }

        public static string PoziomDopuszczalnyPL(string sPomiar)
        {
            // http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
            switch (sPomiar)
            {
                case "PM₂₅":
                        return "Poziom dopuszalny (średnia roczna): 25 μg/m³ od 2015, 20 μg/m³ od 2020\n";
                case "PM₁₀":
                        return "Poziom dopuszalny (od 2005): średnia roczna 40 μg/m³, dobowa 50 μg/m³\n";
                case "C₆H₆":
                        return "Poziom dopuszalny (średnia roczna): 5 μg/m³, od 2010\n";
                case "NO₂":
                        return "Poziom dopuszalny (od 2010): 40 μg/m³ średnia roczna, 200 μg/m³ dobowa\n";
                case "NOx":
                        return "Poziom dopuszalny (średnia roczna): 30 μg/m³ od 2003\n";
                case "SO₂":
                        return "Poziom dopuszalny: 125 μg/m³ (średnia dobowa), 350 μg/m³ (godzinna), od 2005\n";
                case "Pb":
                        return "Poziom dopuszalny (średnia roczna): 0.5 μg/m³, od 2005\n";
                case "CO":
                        return "Poziom dopuszalny (średnia 8 godzinna): 10 000 μg/m³, od 2005\n";
            }
            return "";
        }

        public static string PoziomDocelowyPL(string sPomiar)
        {
            // http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
            switch (sPomiar)
            {
                case "As":
                        return "Poziom docelowy (do 2013): 6 ng/m³ (średnia roczna)\n";
                case "benzoapiren":
                        return "Poziom docelowy (do 2013): 1 ng/m³ (średnia roczna)\n";
                case "Cd":
                        return "Poziom docelowy (do 2013): 5 ng/m³ (średnia roczna)\n";
                case "Ni":
                        return "Poziom docelowy (do 2013): 20 ng/m³ (średnia roczna)\n";
                case "O₃":
                        return "Poziom docelowy (do 2010): 120 μg/m³ (średnia 8 godzinna), okres wegetacji (1 V - 31 VII): 18 000" + "Poziom długoterminowy (do 2020): 120/6000\n";
                case "PM₂₅":
                        return "Poziom docelowy (do 2010): 25 μg/m³ (średnia roczna)\n";
            }
            return "";
        }

        public static string PoziomAlarmuPL(string sPomiar)
        {
            // http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
            switch (sPomiar)
            {
                case "NO₂":
                        return "Poziom alarmowania: 400 μg/m³ średnia godzinna\n";
                case "SO₂":
                        return "Poziom alarmowania: 500 μg/m³ średnia godzinna\n";
                case "O₃":
                        return "Poziom alarmowania: 240 μg/m³ średnia godzinna\n";
                case "PM₁₀":
                        return "Poziom alarmowania: 400 μg/m³ średnia dobowa\n";
            }
            return "";
        }

        public static string PoziomInformowaniaPL(string sPomiar)
        {
            // http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
            switch (sPomiar)
            {
                case "O₃":
                        return "Poziom informowania: 180 μg/m³ średnia godzinna\n";
                case "PM₁₀":
                        return "Poziom informowania: 200 μg/m³ średnia dobowa\n";
            }
            return "";
        }

        public static string PoziomyWHO(string sPomiar)
        {
            // https://www.who.int/news-room/fact-sheets/detail/ambient-(outdoor)-air-quality-and-health
            switch (sPomiar)
            {
                case "PM₂₅":
                        return "Limit WHO 2005: 10 μg/m³ (średnia roczna), 25 μg/m³ (średnia dobowa)\n" +
                        "Limit WHO 2021: 5 μg/m³ (średnia roczna 2006), 15 μg/m³ (średnia dobowa)\n";
                case "PM₁₀":
                        return "Limit WHO 2005: 20 μg/m³ (średnia roczna), 50 μg/m³ (średnia dobowa)\n" +
                        "Limit WHO 2021: 15 μg/m³ (średnia roczna), 45 μg/m³ (średnia dobowa)\n";
                case "O₃":
                        return "Limit WHO: 100 μg/m³ (średnia 8-godzinna)\n";
                case "NO₂":
                        return "Limit WHO 2005: 40 μg/m³ (średnia roczna), 200 μg/m³ (średnia godzinna)\n" +
                        "Limit WHO 2021: 20 μg/m³ (średnia roczna), 25 μg/m³ (średnia dobowa)\n";
                case "SO₂":
                        return "Limit WHO 2005: 20 μg/m³ (średnia dobowa), 500 μg/m³ (średnia 10-minutowa)\n" +
                        "Limit WHO 2021: 40 μg/m³ (średnia dobowa)\n";
                case "CO":
                    return "Limit WHO 2021: 4 mg/m³ (średnia dobowa)\n";
            }
            return "";
        }

        public static void DodajPrzekroczenia()
        {
            p.k.DebugOut("DodajPrzekroczenia() started");

            // http://ec.europa.eu/environment/air/quality/standards.htm
            foreach (EnviroStatus.JedenPomiar oItem in moPomiaryAll)
            {

                // If oItem.sSource = "IMGWhyd" Then Continue For
                // If oItem.sSource = "DarkSky" AndAlso oItem.sPomiar.StartsWith("Alert") Then Continue For
                // If oItem.sSource = "NOAAkind" Then Continue For

                if ((oItem.sSource ?? "") != "gios" && (oItem.sSource ?? "") != "airly" && (oItem.sSource ?? "") != "EEAair")
                    continue;

                oItem.sLimity = PoziomyWHO(oItem.sPomiar) + PoziomDocelowyPL(oItem.sPomiar) + PoziomDopuszczalnyPL(oItem.sPomiar) + PoziomInformowaniaPL(oItem.sPomiar) + PoziomAlarmuPL(oItem.sPomiar);

                switch (p.k.GetSettingsInt("uiLimitWgCombo", 0))
                { // numery: index w uiLimitWgCombo w Settings
                    case 0: // EU
                        switch (oItem.sPomiar)
                        {
                            case "PM₁":
                                break;
                            case "PM₂₅":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)25, (double)1000, (double)2000);
                                break;
                            case "PM₁₀":
                                // 20 μg/m³ średnia roczna, 50 μg/m³ średnia godzinna
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)40, (double)200, (double)400);
                                break;
                            case "μSv/h":
                                break;
                            case "C₆H₆":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)5, (double)1000, (double)2000);
                                break;
                            case "SO₂":
                                // 20 μg/m³ średnia dobowa, 500 μg/m³ średnia 10 minutowa
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)125, (double)125, (double)500);
                                break;
                            case "NO₂":
                                // 40 μg/m³ średnia roczna, 200 μg/m³ średnia godzinna
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)40, (double)40, (double)400);
                                break;
                            case "O₃":
                                // 100 μg/m³ średnia 8 h
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)120, (double)180, (double)240);
                                break;
                            default:
                                oItem.sAlert = "";
                                break;
                        }
                        break;

                    case 1: // WHO
                        switch (oItem.sPomiar)
                        {
                            case "PM₁":
                                break;
                            case "PM₂₅":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)10, (double)25, (double)50);
                                break;
                            case "PM₁₀":
                                // 20 μg/m³ średnia roczna, 50 μg/m³ średnia godzinna
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)20, (double)50, (double)100);
                                break;
                            case "μSv/h":
                                break;
                            case "C₆H₆": // to jest nie WHO, bo WHO nie ma!
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)5, (double)1000, (double)1000);
                                break;
                            case "SO₂":
                                // 20 μg/m³ średnia dobowa, 500 μg/m³ średnia 10 minutowa
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)20, (double)500, (double)1000);
                                break;
                            case "NO₂":
                                // 40 μg/m³ średnia roczna, 200 μg/m³ średnia godzinna
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)40, (double)200, (double)400);
                                break;
                            case "O₃":
                                // 100 μg/m³ średnia 8 h
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, (double)100, (double)100, (double)200);
                                break;
                            default:
                                oItem.sAlert = "";
                                break;
                        }
                        break;
                    case 2: //WHO 2021
                        switch (oItem.sPomiar)
                        {
                            case "PM₂₅":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 5, 15, 50);
                                break;
                            case "PM₁₀":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 15, 45, 100);
                                break;
                            case "C₆H₆": // to jest nie WHO, bo WHO nie ma!
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 5, 1000, 1000);
                                break;
                            case "SO₂":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 40, 500, 1000);
                                break;
                            case "NO₂":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 10, 25, 400);
                                break;
                            case "O₃":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 100, 100, 200);
                                break;
                            case "CO":
                                oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 4, 40, 100);
                                break;
                            default:
                                oItem.sAlert = "";
                                break;
                        }
                        break;
                }




                if (oItem.sAlert.Contains("!")) oItem.bCleanAir = false;    // 2021.01.28
            }
        }

        public static async System.Threading.Tasks.Task<string> SourcesUsedInTimer()
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
            sId = p.k.GetSettingsString("settingsLiveTile");
            iInd = sId.LastIndexOf("(");
            if (iInd > 0)
                sRet = sRet + "|" + sId.Substring(iInd + 1);

            if (p.k.GetSettingsString("settingsAlerts").IndexOf("!") < 0)
                return sRet;
            // skoro maja byc Toasty, to dopiszmy to co jest wykrzyknikowalne

            // sRet = sRet & "|airly|gios|IMGWhyd|DarkSky|NOAAkind" ' hydro: poziom wody!, DarkSky - ostrzezenia
            // czyli bez r@h, IMGWmet, foreca
            foreach (EnviroStatus.Source_Base oZrodlo in gaSrc)
            {
                if (oZrodlo.SRC_IN_TIMER)
                    sRet = sRet + "|" + oZrodlo.SRC_POMIAR_SOURCE;
            }

            return sRet;
        }

        public static event ZmianaDanychEventHandler ZmianaDanych;

        public delegate void ZmianaDanychEventHandler();

        public static async System.Threading.Tasks.Task GetFavData(string sFavName, bool bInTimer)
        {
            if (!p.k.NetIsIPavailable(false))
                return;

            string sSensors = p.k.GetSettingsString("fav_" + sFavName);
            if (string.IsNullOrEmpty(sSensors))
                return ;

            // to chyba niepotrzebne, bo z load(template) odleglosc jest ustalona
            string sPunkt = p.k.GetSettingsString("favgps_" + sFavName);
            if (string.IsNullOrEmpty(sPunkt))
                return ;
            string[] aPunkt = sPunkt.Split('|');
            double dTmp;
            double.TryParse(aPunkt[0], out dTmp);
            moGpsPoint.Latitude = dTmp;
            double.TryParse(aPunkt[1], out dTmp);
            moGpsPoint.Longitude = dTmp;

            if(!App.mbComparing)    // reset nie dla porównywania!
                moPomiaryAll = new Collection<JedenPomiar>();

            Collection<EnviroStatus.JedenPomiar> oPomiary = null;
            p.k.SetSettingsString("seenUri", "");   // bez zwłoki pomiedzy odwolaniami do tego samego serwera (żaden nie odwiedzony)
            string sInTiles = "";
            if (bInTimer)
                sInTiles = await SourcesUsedInTimer();

            var aSensory = sSensors.Split('|');
            foreach (string sSensor in aSensory)
            {
                var aData = sSensor.Split('#');
                if (bInTimer && sInTiles.IndexOf(aData[0]) < 0)
                    continue;
                oPomiary = null;

                foreach (EnviroStatus.Source_Base oZrodlo in gaSrc)
                {
                    if ((aData[0] ?? "") == (oZrodlo.SRC_POMIAR_SOURCE ?? ""))
                    {
                        // gdy porównuje, wczytuje tylko gdy uzywane w porównaniach
                        if(!App.mbComparing || !oZrodlo.SRC_NO_COMPARE)
                            if ((aData[0] ?? "") == "DarkSky" || (aData[0] ?? "") == "SeismicEU")
                                oPomiary = await oZrodlo.GetDataFromFavSensor(moGpsPoint.Latitude.ToString(), moGpsPoint.Longitude.ToString(), bInTimer);
                            else
                                oPomiary = await oZrodlo.GetDataFromFavSensor(aData[1], aData[2], bInTimer);
                    }
                }

                if (oPomiary != null)
                {
                    foreach (EnviroStatus.JedenPomiar oPomiar in oPomiary)
                    {
                        if(App.mbComparing)
                        {
                            // znajdz oPomiar w moPomiaryAll
                            // zmien sCurrValue, ale nie ruszaj reszty
                            foreach (EnviroStatus.JedenPomiar oOldPomiar in moPomiaryAll)
                            {
                                if(oOldPomiar.sSource == oPomiar.sSource &&
                                    oOldPomiar.sPomiar == oPomiar.sPomiar)
                                {
                                    if(oOldPomiar.dCurrValue == (int)oOldPomiar.dCurrValue)
                                        oOldPomiar.sCurrValue = oOldPomiar.dCurrValue.ToString();
                                    else
                                        oOldPomiar.sCurrValue = oOldPomiar.dCurrValue.ToString("###0.00");


                                    if (oOldPomiar.dCurrValue < oPomiar.dCurrValue)
                                        oOldPomiar.sCurrValue += " < ";
                                    else
                                        if (oOldPomiar.dCurrValue == oPomiar.dCurrValue)
                                            oOldPomiar.sCurrValue += " = ";
                                        else
                                            oOldPomiar.sCurrValue += " > ";

                                    if (oPomiar.dCurrValue == (int)oPomiar.dCurrValue)
                                        oOldPomiar.sCurrValue += oPomiar.dCurrValue.ToString();
                                    else
                                        oOldPomiar.sCurrValue += oPomiar.dCurrValue.ToString("###0.00");

                                    break;
                                }
                            }

                        }
                        else
                            moPomiaryAll.Add(oPomiar);
                    }
                    if (oPomiary.Count() > 0 && !bInTimer)
                        ZmianaDanych?.Invoke();
                }
            }

            // NOAA alert - musi byc wywolane nawet jak nie ma w Fav:Template - ale nie przy porównywaniu
            if(!App.mbComparing)
                if (!sSensors.Contains("NOAAalert"))
                {
                    // znajdz ktora to pozycja tabelki Zrodel
                    foreach (EnviroStatus.Source_Base oZrodlo in gaSrc)
                    {
                        if ((oZrodlo.SRC_POMIAR_SOURCE ?? "") == "NOAAalert")
                        {
                            oPomiary = await oZrodlo.GetDataFromFavSensor("", "", bInTimer);
                            break;
                        }
                    }
                    if (oPomiary != null)
                    {
                        foreach (EnviroStatus.JedenPomiar oPomiar in oPomiary)
                            moPomiaryAll.Add(oPomiar);
                    }
                    if (oPomiary.Count() > 0 && !bInTimer)
                        ZmianaDanych?.Invoke();
                }

        }

        public static void DodajTempOdczuwana()
        {
            p.k.DebugOut("DodajTempOdczuwana() started");

            double dTemp = 1000;
            double dWilg = 1000;

            // MakeToast("before loop in Tapp")
            foreach (EnviroStatus.JedenPomiar oItem in moPomiaryAll)
            {
                // MakeToast("source: " & oItem.sSource & ", pomiar " & oItem.sPomiar)
                if (!oItem.bDel && !string.IsNullOrEmpty(oItem.sPomiar))
                {
                    // MakeToast("value: " & oItem.dCurrValue)
                    if ((oItem.sPomiar.ToLower() ?? "") == "humidity")
                        dWilg = oItem.dCurrValue;
                    if (oItem.sPomiar.ToLower().IndexOf("tempe") == 0)
                        dTemp = oItem.dCurrValue; // airly tak, ale IMGW nie (bo tam jest temp)
                }
            }
            // MakeToast("Tapp, mam dane " & dTemp & ", " & dWilg)
            // jesli ktorejs wartosci nie ma, to sie poddaj
            if (dTemp == 1000)
                return;
            if (dWilg == 1000)
                return;

            var oNew = new JedenPomiar()
            {
                sSource = "me",
                dOdl = 0,
                sPomiar = p.k.GetSettingsString("resTempOdczuwana"),
                sUnit = " °C",
                sTimeStamp = DateTime.Now.ToString(),
                sSensorDescr = p.k.GetSettingsString("resTempOdczuwana"),
                sOdl = ""
            };

            // http://www.bom.gov.au/info/thermal_stress/#apparent
            // czyli Source: Norms of apparent temperature in Australia, Aust. Met. Mag., 1994, Vol 43, 1-16
            double dWP; // water pressure, hPa
            double dWind = 0; // wind speed, na wysok 10 m, w m/s

            dWP = dWilg / 100 * 6.105 * Math.Exp(17.27 * dTemp / (237.7 + dTemp));
            oNew.dCurrValue = Math.Round(dTemp + 0.33 * dWP - 0.7 * dWind - 4, 2);
            // uwaga: dla wersji z naslonecznieniem jest inaczej
            oNew.sCurrValue = oNew.dCurrValue.ToString() + " °C";

            // wersja z naslonecznieniem:
            // oraz kalkulator: https://planetcalc.com/2089/
            // var e = (H/100)*6.105*Math.exp( (17.27*Ta)/(237.7+Ta) );
            // AT.SetValue(Ta + 0.348*e - 0.7*V - 4.25);

            moPomiaryAll.Add(oNew);
        }

        public static string String2SentenceCase(string sInput)
        {
            // założenie: wchodzi UPCASE
            string sOut = "";
            bool bFirst = true;

            for (int i = 0, loopTo = sInput.Length - 1; i <= loopTo; i++)
            {
                string sChar = sInput.ElementAt(i).ToString();
                if ("ABCDEFGHIJKLMNOPQRSTUVWXYZĄĆĘŁŃÓŚŻŹ".IndexOf(sChar) < 0)
                {
                    sOut = sOut + sChar;
                    bFirst = true;
                    continue;
                }

                if (bFirst)
                    bFirst = false;
                else
                    sChar = sChar.ToLower();

                sOut = sOut + sChar;
            }

            return sOut;
        }

        public static string ShortPrevDate(string sCurrDate, string sPrevDate)
        {
            if ((sCurrDate.Substring(0, 10) ?? "") == (sPrevDate.Substring(0, 10) ?? ""))
                return sPrevDate.Substring(11, 5);
            // miesiac/dzien
            if ((sCurrDate.Substring(0, 10) ?? "") == (sPrevDate.Substring(0, 10) ?? ""))
                return sPrevDate.Substring(5, 11);
            // calosc ale bez sekund
            return sPrevDate.Substring(0, 16);
        }

        public static string UnixTimeToTime(long lTime)
        {
            // 1509993360
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(lTime);
            return dtDateTime.ToString();
        }

        public static async System.Threading.Tasks.Task<DateTimeOffset> Cache_Load()
        {
            Windows.Storage.StorageFile oFile;
            Windows.Storage.StorageFile oFile1;

            // najpierw sprobuj plik lokalny odczytac, a jak sie nie uda - roaming
            oFile = await GetDataFile(false, "data_cache.xml", false);
            oFile1 = await GetDataFile(true, "data_cache.xml", false);

            if (oFile != null && oFile1 != null)
            {
//#if NETFX_CORE
                if ((await oFile.GetBasicPropertiesAsync()).DateModified < (await oFile1.GetBasicPropertiesAsync()).DateModified)
//#else
//                if (oFile.DateCreated <  oFile1.DateCreated)
//#endif
                    oFile = oFile1;
            }

            if (oFile == null)
                return DateTime.Now.AddYears(-100);

            var oSer = new System.Xml.Serialization.XmlSerializer(typeof(Collection<JedenPomiar>));
            var oStream = await oFile.OpenStreamForReadAsync();

            try
            {
                moPomiaryAll = oSer.Deserialize(oStream) as Collection<EnviroStatus.JedenPomiar>;
            }
            catch (Exception ex)
            {
            }
            oStream.Dispose();   // == fclose
//#if NETFX_CORE
            var oBP = await oFile.GetBasicPropertiesAsync();
            return oBP.DateModified;
//#else
//            return oFile.DateCreated;
//#endif
        }

        public static async System.Threading.Tasks.Task Cache_Save()
        {
            p.k.DebugOut("Cache_Save() started");

            Windows.Storage.StorageFile oFile;

            // local file
            if (p.k.GetSettingsBool("settingsFileCache"))
            {
                oFile = await GetDataFile(false, "data_cache.xml", true);
                if (oFile != null)
                {
                    var oSer = new System.Xml.Serialization.XmlSerializer(typeof(Collection<JedenPomiar>));
                    var oStream = await oFile.OpenStreamForWriteAsync();
                    oSer.Serialize(oStream, moPomiaryAll);
                    oStream.Dispose();   // == fclose
                }
            }


            // roaming file
            if (p.k.GetSettingsBool("settingsFileCacheRoam"))
            {
                oFile = await GetDataFile(true, "data_cache.xml", true);
                if (oFile != null)
                {
                    var oSer = new System.Xml.Serialization.XmlSerializer(typeof(Collection<JedenPomiar>));
                    var oStream = await oFile.OpenStreamForWriteAsync();
                    oSer.Serialize(oStream, moPomiaryAll);
                    oStream.Dispose();   // == fclose
                }
            }

        }

        public static void ReadResStrings()
        {
            // wczytanie tych stringow, ktore są potrzebne w background - gdy nie dziala GetLangString
            p.k.SetSettingsString("resAllOk", p.k.GetLangString("msgAllOk"));
            p.k.SetSettingsString("resAndroidToastTitle", p.k.GetLangString("msgAndroidToastTitle"));

            foreach (EnviroStatus.Source_Base oZrodlo in gaSrc)
                oZrodlo.ReadResStrings();
        }

        public static EnviroStatus.Source_Base[] gaSrc = new EnviroStatus.Source_Base[] {
            new Source_Foreca(),
            new Source_DarkSky(),
            new Source_NoaaKindex(),
            new Source_NoaaWind(),
            new Source_NoaaAlert(),
            new Source_SeismicPortal(),
            new Source_RadioAtHome(),
            new Source_Airly(),
            new Source_EEAair(),
            new Source_GIOS(),
            new Source_IMGWhydro(),
            new Source_IMGWmeteo()
        };

        public static Collection<EnviroStatus.JedenPomiar> moPomiaryAll = new Collection<JedenPomiar>();
        public static DateTime? moLastPomiar = null;

        public static Windows.Devices.Geolocation.BasicGeoposition? moPoint = null;
    }
}

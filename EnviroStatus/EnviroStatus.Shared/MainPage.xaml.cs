
using Windows.UI.Xaml;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using Windows.UI.Xaml.Controls;
using static p.Extensions;
using vb14 = VBlib.pkarlibmodule14;


namespace EnviroStatus
{
    public sealed partial class MainPage : Page
    {
        

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void uiSetup_Click(object sender, RoutedEventArgs e)
        {
            this.Navigate(typeof(Settings));
        }

        private async System.Threading.Tasks.Task ShowDetailsAsync(VBlib.JedenPomiar oItem)
        {
            string sMsg = VBlib.MainPage.ShowDetails(oItem);

            if (sMsg.Length < 5) return;

            { // kod tutaj - bo trzeci guzik (go map) dla trzęsień ziemi
                Windows.UI.Popups.MessageDialog oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
                string sYes = vb14.GetLangString("msgCopyDetails");
                Windows.UI.Popups.UICommand oClip = new Windows.UI.Popups.UICommand(sYes);
                Windows.UI.Popups.UICommand oOk = new Windows.UI.Popups.UICommand("OK");
                Windows.UI.Popups.UICommand oMap = new Windows.UI.Popups.UICommand("Map");
                uint iDefButt = 1;

                // wersja UWP:Phone ma limit dwu guzików.
                if (p.k.GetPlatform("uwp") && p.k.IsFamilyMobile())
                { }
                else
                    if (oItem.sSource == "SeismicEU" && oItem.sPomiar == "mag") // a nie "magΣ"
                    {
                        oMsg.Commands.Add(oMap);
                        iDefButt = 2;
                    }

                oMsg.Commands.Add(oClip);
                oMsg.Commands.Add(oOk);
                oMsg.DefaultCommandIndex = iDefButt;    // default: No
                oMsg.CancelCommandIndex = iDefButt;
                Windows.UI.Popups.IUICommand oCmd = await oMsg.ShowAsync();
                if (oCmd.Label == sYes) vb14.ClipPut(sMsg);
                if (oCmd.Label == "Map")
                {
                    Uri oUri = new Uri("https://bing.com/maps/default.aspx?lvl=7&cp=" + oItem.dLat.ToString() + "~" + oItem.dLon.ToString());
                    oUri.OpenBrowser();
                }

            }

        }

        private void uiShowDetails_Click(object sender, RoutedEventArgs e)
        {// bo zarowno z TextBlock, jak i z Grid moze przyjsc, i z MenuItem
            FrameworkElement oFE;
            oFE = sender as FrameworkElement;
            if (oFE != null)
                ShowDetailsAsync(oFE.DataContext as VBlib.JedenPomiar);
        }


        private async System.Threading.Tasks.Task WczytajDanePunktuAsync(VBlib.MyBasicGeoposition oPoint)
        {
            // uruchamiamy kazde zrodlo - niech sobie WWW sciaga rownolegle
            vb14.DumpCurrMethod();

            var aoWait = new List<System.Threading.Tasks.Task<Collection<VBlib.JedenPomiar>>>();

            foreach (VBlib.Source_Base oZrodlo in VBlib.App.gaSrc)
                aoWait.Add(oZrodlo.GetNearestAsync(oPoint));

            vb14.DumpMessage("WczytajDanePunktu() got all oWait objects");

            VBlib.App.moPomiaryAll = new Collection<VBlib.JedenPomiar>();

            // zbieramy rezultaty od zrodel

            Collection<VBlib.JedenPomiar> oPomiary;

            this.ProgRingShow(true, false, 0, aoWait.Count);

            vb14.DebugOut("WczytajDanePunktu() rozpoczynam petle");

            foreach (System.Threading.Tasks.Task<Collection<VBlib.JedenPomiar>> oTask in aoWait)
            {
                vb14.DebugOut("WczytajDanePunktu() kolejny await zaczynam");
                // App.moPomiaryAll.Concat(Await oTask)
                oPomiary = await oTask; // App.moSrc_Airly.GetNearest(oPoint, 10)
                vb14.DebugOut("WczytajDanePunktu() kolejny await udany");

                if (oPomiary is null)
                {
                    // await p.k.DialogBoxAsync("Error from task...");
                }
                else
                {
                    // zmiana na Concat: 2021.02.02
                    //App.moPomiaryAll.Concat(oPomiary);

                    // VBlib.App.moPomiaryAll = VBlib.App.moPomiaryAll.Concat(oPomiary)
                    foreach (VBlib.JedenPomiar oPomiar in oPomiary)
                        VBlib.App.moPomiaryAll.Add(oPomiar);

                    // zmiana, jeśli coś nowego było
                    if (oPomiary.Count > 0)
                        uiList.ItemsSource = (from c in VBlib.App.moPomiaryAll
                                              orderby c.sPomiar
                                              where c.bDel == false
                                              select c).ToList();
                }
                this.ProgRingInc();
            }

            await KoncowkaPokazywaniaDanychAsync("", false);

            this.ProgRingShow(false);

            uiRefresh.IsEnabled = true;

            // uiAdd.Visibility = Visibility.Visible
            uiAdd.IsEnabled = true;

            vb14.DebugOut("WczytajDanePunktu() ended");
        }

        private async void uiGPS_Click(object sender, RoutedEventArgs e)
        {
            vb14.DumpCurrMethod ();
            if (!p.k.NetIsIPavailable(false))
            {
                await vb14.DialogBoxResAsync("errNoNet");
                return;
            }

            uiRefresh.IsEnabled = false;
            UsunCompare("#tegoNieMa#"); // tak naprawde: włącz wszystkie
            VBlib.App.mbComparing = false;

            this.ProgRingShow(true);

            VBlib.MyBasicGeoposition oPoint;
            oPoint = await App.GetCurrentPointAsync();
            vb14.DebugOut("uiGPS_Click() got point");
            await WczytajDanePunktuAsync(oPoint);
            vb14.DebugOut("uiGPS_Click() data wczytane");
            this.ProgRingShow(false);

            uiTimestamp.Text = "";
        }

        private void uiInfo_Click(object sender, RoutedEventArgs e)
        {
            // przejscie do podstrony
            Frame.Navigate(typeof(Info));
        }

        private void WypelnMenuFavs()
        {
            // wczytaj ze zmniennej liste miejsc i dopisz do menuflyout
            string sLista = vb14.GetSettingsString("favNames");
            var sFavs = sLista.Split('|');

            foreach (string sName in sFavs)
            {
                if (sName.Length > 2)
                {
                    string sUIname = "uiFav" + sName;

                    // nie dodajemy jak juz jest
                    bool bFound = false;
                    foreach (MenuFlyoutItemBase oItem in uiFavMenu.Items) 
                    {
                        if (oItem.Name == sUIname )
                        {
                            bFound = true;
                            break;
                        }
                    }

                    if (!bFound)
                    {
                        var oMFI = new MenuFlyoutItem();
                        oMFI.Name = sUIname;
                        oMFI.Text = sName;
                        oMFI.Click += uiFav_Click;
                        uiFavMenu.Items.Add(oMFI);

                        oMFI = new MenuFlyoutItem();
                        oMFI.Name = "cmp" + sUIname;
                        oMFI.Text = sName;
                        oMFI.Click += uiCompareFav_Click;
                        uiCompareMenu.Items.Add(oMFI);
                    }
                }
            }
        }


        private async void RegisterTriggerAsync(int iMin)
        {
            if (!await p.k.CanRegisterTriggersAsync()) return;

            p.k.UnregisterTriggers("EnviroStat_");

            var oCondit = new Windows.ApplicationModel.Background.SystemCondition(
                        Windows.ApplicationModel.Background.SystemConditionType.InternetAvailable);

            p.k.RegisterTimerTrigger("EnviroStat_Timer", (uint)iMin, false, oCondit);

        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WypelnMenuFavs();
            this.ProgRingInit(true, true);

            //App.ReadResStrings();    // to, co potrzebne dla BackGround


            if (!(VBlib.App.moPoint is null))
            {
                // czyli jestesmy po wskazaniu na mapie
                uiRefresh.IsEnabled = false;

                UsunCompare("#tegoNieMa#"); // tak naprawde: włącz wszystkie
                VBlib.App.mbComparing = false;

                this.ProgRingShow(true);
                await WczytajDanePunktuAsync(VBlib.App.moPoint);
                VBlib.App.moPoint = null;
                this.ProgRingShow(false);

                uiTimestamp.Text = "";

            }

            if (VBlib.App.moPomiaryAll.Count() < 1)
            {
                string sAutoStart = vb14.GetSettingsString("settingStartPage");
                if (!string.IsNullOrEmpty(sAutoStart) && (sAutoStart.Substring(0, 1) ?? "") != "(")
                    await GetFavDataAsync(sAutoStart);
                else
                {
                    // odczytaj plik zawsze - bo moze odczyta plik roaming ustawiany gdzies indziej?
                    var oDTO = App.Cache_Load();
                    if (oDTO.Year > 2010)
                    {
                        uiTimestamp.Text = "(" + oDTO.ToString("d-MM HH:mm") + ")";
                    }
                }
            }
            else
                // są dane w pamięci, skorzystaj z tego - tylko napisz z kiedy to są dane
                if(VBlib.App.moLastPomiar.AddYears(1) > DateTime.Now)
                    if (VBlib.App.moLastPomiar < DateTime.Now.AddMinutes(-10))
                        uiTimestamp.Text = "(" + VBlib.App.moLastPomiar.ToString("d-MM HH:mm") + ")";

            if (VBlib.App.moPomiaryAll.Count() > 0)
            {
                uiCompare.IsEnabled = !VBlib.App.mbComparing;

                uiList.ItemsSource = from c in VBlib.App.moPomiaryAll
                                        orderby c.sPomiar
                                        where c.bDel == false
                                        select c;
            }
            else
                uiCompare.IsEnabled = false;


            // Settings: On = GPS (60 min), Off = lastposition (30 min)
                int iMin = 30;
            if (vb14.GetSettingsBool("settingsLiveClock"))
                iMin = 60;
            RegisterTriggerAsync(iMin);

            // 2022.08.11, do usunięcia po migracji
            await ConvertTemplatesXMLToJSONAsync();


            if (VBlib.App.moPomiaryAll.Count() < 1 && !vb14.GetSettingsBool("wasSetup"))
            {
                await vb14.DialogBoxResAsync("msgFirstRunGoSetup");
                this.Navigate(typeof(Zrodelka));
            }


        }

        private async void uiStore_Click(object sender, RoutedEventArgs e)
        {
            // uiAdd.Visibility = Visibility.Collapsed
            uiAdd.IsEnabled = false;

            string sTmp = "";
            foreach (VBlib.JedenPomiar oItem in VBlib.App.moPomiaryAll)
            {
                if (!oItem.bDel)
                {
                    // Dim sSensor As String = oItem.sSource & "#" & oItem.sId & "#" & oItem.sAddit & "|"
                    string sSensor = oItem.sSource + "#" + oItem.sId + "#|";
                    if (sTmp.IndexOf(sSensor) < 0)
                        sTmp = sTmp + sSensor;
                }
            }

            if (string.IsNullOrEmpty(sTmp))
            {
                await vb14.DialogBoxAsync("Error: current sensor list is empty");
                return;
            }

            string sName = await vb14.DialogBoxInputResAsync("resNazwa", "", "resSaveFav");
            if (string.IsNullOrEmpty(sName))
                return;
#if __ANDROID__
            if(sName == "pktoasttest")
            {
                vb14.MakeToast("tekst 1", "tekst 2");
                vb14.SetSettingsBool("testTimera", true);
                return;
            }
#endif 

            vb14.SetSettingsString("fav_" + sName, sTmp);
            vb14.SetSettingsString("favgps_" + sName, VBlib.App.moGpsPoint.Latitude + "|" + VBlib.App.moGpsPoint.Longitude);

            foreach (VBlib.Source_Base oZrodlo in VBlib.App.gaSrc)
                oZrodlo.FavTemplateSave();

            vb14.SetSettingsString("currentFav", sName);

            sTmp = vb14.GetSettingsString("favNames");
            if (sTmp.IndexOf(sName + "|") > -1)
                return;

            sTmp = sTmp + sName + "|";
            vb14.SetSettingsString("favNames", sTmp);

            var oMFI = new MenuFlyoutItem();
            oMFI.Name = "uiFav" + sName;
            oMFI.Text = sName;
            oMFI.Click += uiFav_Click;
            uiFavMenu.Items.Add(oMFI);

            oMFI = new MenuFlyoutItem();
            oMFI.Name = "cmpuiFav" + sName;
            oMFI.Text = sName;
            oMFI.IsEnabled = false;
            oMFI.Click += uiCompareFav_Click;
            uiCompareMenu.Items.Add(oMFI);


        }



        private async System.Threading.Tasks.Task KoncowkaPokazywaniaDanychAsync(string sTitle, bool bInProgress)
        {

            vb14.DebugOut("mainpage:KoncowkaPokazywaniaDanych(" + sTitle + "," + bInProgress.ToString() + ") started");

            if (!bInProgress)
                EnviroStatus.App.KoncowkaPokazywaniaDanych();
            // App.UpdateTile() - jest juz w App.Koncowka...

            string sTmp = vb14.GetLangString("manifestAppName");
            if (!string.IsNullOrEmpty(sTitle))
                sTmp = sTmp + " - " + sTitle;
            uiTitle.Text = sTitle;

            if (VBlib.App.moPomiaryAll.Count() < 1)
            {
                if (!bInProgress)
                    await vb14.DialogBoxResAsync("resNoSensorInRange");
            }
            else
            {
                uiList.ItemsSource = from c in VBlib.App.moPomiaryAll
                                     orderby c.sPomiar
                                     where c.bDel == false
                                     select c;

                // jesli cos wczytalismy, to mozemy porownac
                uiCompare.IsEnabled = true;
            }
        }


        private async System.Threading.Tasks.Task GetFavDataAsync(string sFavName)
        {
            vb14.SetSettingsString("currentFav", sFavName);

            if(!VBlib.App.mbComparing) UsunCompare(sFavName);     // nie zmieniaj usuniecia, jesli porownuje

            uiRefresh.IsEnabled = false;
            this.ProgRingShow(true);

            VBlib.App.ZmianaDanych += ZmianaDanychEventAsync;
            await EnviroStatus.App.GetFavDataAsync(sFavName, false);
            VBlib.App.ZmianaDanych -= ZmianaDanychEventAsync;

            uiRefresh.IsEnabled = false;

            await KoncowkaPokazywaniaDanychAsync(sFavName, false);

            uiRefresh.IsEnabled = true;
            this.ProgRingShow(false);

            // uiAdd.Visibility = Visibility.Collapsed
            uiAdd.IsEnabled = false;
            uiTimestamp.Text = "";
            
        }

        private void UsunCompare(string sFavName)
        {// usun z listy Fav to co jest wczytywane (bez porównywania z samym sobą)
            foreach(MenuFlyoutItemBase oItem in uiCompareMenu.Items)
            {
                oItem.IsEnabled = !(oItem.Name.EndsWith(sFavName));
            }
        }

        private async void ZmianaDanychEventAsync()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ZmianaDanychEventUIAsync);
        }

        private async void ZmianaDanychEventUIAsync()
        {
            await KoncowkaPokazywaniaDanychAsync("", true);
        }

        private async void uiFav_Click(object sender, RoutedEventArgs e)
        { // fav do pokazania

            if (!p.k.NetIsIPavailable(false))
            {
                await vb14.DialogBoxResAsync("errNoNet");
                return;
            }

            var oMFI = sender as MenuFlyoutItem;
            string sName = oMFI.Name.Replace("uiFav", "");
            VBlib.App.mbComparing = false;

            await GetFavDataAsync(sName);
        }

        private async void uiCompareFav_Click(object sender, RoutedEventArgs e)
        {// fav do porównania
            if (!p.k.NetIsIPavailable(false))
            {
                await vb14.DialogBoxResAsync("errNoNet");
                return;
            }

            var oMFI = sender as MenuFlyoutItem;
            string sName = oMFI.Name.Replace("cmpuiFav", "");
            VBlib.App.mbComparing = true;

            await GetFavDataAsync(sName);
        }


#if NETFX_CORE

        private async void uiAddSecTile_Click(object sender, RoutedEventArgs e)
        {
            // XBox nie ma secondary tile
            if(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily.ToLower().Contains("xbox"))
            {
                await vb14.DialogBoxResAsync("errXBoxNot");
                return;
            }

            VBlib.JedenPomiar oItem;
            oItem = (sender as MenuFlyoutItem).DataContext as VBlib.JedenPomiar;
            string sName = EnviroStatus.App.GetNameForSecTile(oItem);
            // sName = "alamakota (nawias)"
            var oSTile = new Windows.UI.StartScreen.SecondaryTile(sName, sName, sName, new Uri("ms-appx:///Assets/EmptyTile.png"), Windows.UI.StartScreen.TileSize.Square150x150);
            bool isPinned = await oSTile.RequestCreateAsync();

            if (isPinned)
                EnviroStatus.App.UpdateTile();
        }
#endif

        private void uiMap_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WedleMapy));
        }

        private async System.Threading.Tasks.Task ConvertTemplatesXMLToJSONAsync()
        {
            // 2022.08.11, do usunięcia po migracji
            // konwersja, bo przenoszę do VBlib templaty ze zmianą XML na JSON

            Windows.Storage.StorageFolder oFold = Windows.Storage.ApplicationData.Current.LocalFolder;
            var oSer = new System.Xml.Serialization.XmlSerializer(typeof(VBlib.JedenPomiar));

            foreach (Windows.Storage.StorageFile oFile in await oFold.GetFilesAsync())
            {
                if (!oFile.Name.EndsWith(".xml")) continue;

                string sContent = System.IO.File.ReadAllText(oFile.Path);
                var oRdr = new System.IO.StringReader(sContent);
                VBlib.JedenPomiar oTemplate = oSer.Deserialize(oRdr) as VBlib.JedenPomiar;
                string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(oTemplate, Newtonsoft.Json.Formatting.Indented);

                string sFileSavePath = oFile.Path.Replace(".xml", ".json");
                System.IO.File.WriteAllText(sFileSavePath, sJson);

                await oFile.DeleteAsync();
            }
        }

    }
}

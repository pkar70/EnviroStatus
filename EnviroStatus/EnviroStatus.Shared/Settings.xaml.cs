
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using System.Linq;
using System;
using Windows.UI.Xaml.Controls;
//using MUXC = Microsoft.UI.Xaml.Controls;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public sealed partial class Settings : Page
    {

        public Settings()
        {
            this.InitializeComponent();
        }

        // combo wyboru start place (z zapamietanych), oraz "nic" (bez autostart)
        // LifeTile: yes/no, combo lista pomiarow z aktualnego miejsca (app.mo..)
        // GPS (60 min), last fav (30 min)
        // alerts: yes/no, combo: średnia roczna, średnia dzienna, 2x dzienna
        // alert OK: gdy spadnie ponizej 90 % limitu
        // DataLogs yes/no
        // pamięć ostatniej lokalizacji jak DailyItinerary - dopiero wtedy zmiana stacji

        private void FillCombo(ComboBox uiCombo, string sResCurr, string sList, string sResAdditItem)
        {
            uiCombo.Items.Clear();

            string sCurrent = p.k.GetSettingsString(sResCurr);

            ComboBoxItem oCBI;
            if (!string.IsNullOrEmpty(sResAdditItem))
            {
                oCBI = new ComboBoxItem();
                string sAdditItem = p.k.GetLangString(sResAdditItem);
                oCBI.Content = sAdditItem;
                if (string.IsNullOrEmpty(sCurrent) || (sCurrent == sAdditItem))
                    oCBI.IsSelected = true;
                uiCombo.Items.Add(oCBI);
            }


            string sTxt = p.k.GetSettingsString(sList);
            var aNames = sTxt.Split('|');
            foreach (string sName in aNames)
            {
                oCBI = new ComboBoxItem();
                oCBI.Content = sName;
                if (!string.IsNullOrEmpty(sCurrent) && (sCurrent == oCBI.Content.ToString()))
                    oCBI.IsSelected = true;
                uiCombo.Items.Add(oCBI);
            }
        }

        private void FillComboLiveTile(ComboBox uiCombo, string sResCurr, string sResAdditItem)
        {
            uiCombo.Items.Clear();

            string sCurrent = p.k.GetSettingsString(sResCurr);

            var oCBI = new ComboBoxItem();
            if (!string.IsNullOrEmpty(sResAdditItem))
            {
                oCBI.Content = p.k.GetLangString(sResAdditItem);
                if (!string.IsNullOrEmpty(sCurrent) && (sCurrent == oCBI.Content.ToString()))
                    oCBI.IsSelected = true;
                uiCombo.Items.Add(oCBI);
            }

            foreach (EnviroStatus.JedenPomiar oItem in EnviroStatus.App.moPomiaryAll)
            {
                if (!oItem.bDel)
                {
                    oCBI = new ComboBoxItem();
                    oCBI.Content = oItem.sPomiar + " (" + oItem.sSource + ")";
                    if (!string.IsNullOrEmpty(sCurrent) && (oItem.sPomiar ?? "") == (sCurrent ?? ""))
                        oCBI.IsSelected = true;
                    uiCombo.Items.Add(oCBI);
                }
            }
        }

        private void ComboAlerts(ComboBox uiCombo, string sCurr)
        {
            string sCurrent = p.k.GetSettingsString(sCurr);

            int iInd;// = -1;
            var loopTo = uiCombo.Items.Count - 1;
            for (iInd = 0; iInd <= loopTo; iInd++)
            {
                if ((uiCombo.Items.ElementAt(iInd) as ComboBoxItem).Content.ToString() == sCurrent)
                {
                    uiCombo.SelectedIndex = iInd;
                    break;
                }
            }
        }

        private void SetComboWedlug()
        {   // 2021.09.23

            int iWg = p.k.GetSettingsInt("uiLimitWgCombo", -1);
            if(iWg == -1)
            {
                iWg = 0;
                if (p.k.GetSettingsBool("settingsWHO", true))
                    iWg = 1;
            }
            uiLimitWgCombo.SelectedIndex = iWg;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            uiVersion.Text = p.k.GetLangString("msgVersion") + " " + p.k.GetAppVers();

            FillCombo(uiStartLoc, "settingStartPage", "favNames", "resNoAutostart");
            FillComboLiveTile(uiLiveTile, "settingsLiveTile", "resDeadTile");
            ComboAlerts(uiAlerts, "settingsAlerts");
            SetComboWedlug();

            p.k.GetSettingsBool(uiLiveTileClock, "settingsLiveClock");
            p.k.GetSettingsBool(uiDataLogs, "settingsDataLog");

#if _PK_NUMBOX_
            p.k.GetSettingsInt(uiKubatura, "higroKubatura", 100, 0);
            p.k.GetSettingsInt(uiIntTemp, "higroTemp", 1, 22);
#else
            //uiKubatura.Text = (p.k.GetSettingsInt("higroKubatura", 0) / 100.0).ToString();
            //uiIntTemp.Text = (p.k.GetSettingsInt("higroTemp", 22)).ToString();
#endif 

            //uiLongitude.Text = App.moGpsPoint.Y.ToString();
            //uiLatitude.Text = App.moGpsPoint.X.ToString();
            p.k.GetSettingsString(uiLatitude, "gpsEmulationLat", App.moGpsPoint.Latitude.ToString());
            p.k.GetSettingsString(uiLongitude, "gpsEmulationLon", App.moGpsPoint.Longitude.ToString());

            p.k.GetSettingsBool(uiFileCache, "settingsFileCache");
        }

        private string VerifyDataOK()
        {

            if (string.IsNullOrEmpty(uiKubatura.Text))
                uiKubatura.Text = "0";

            if (string.IsNullOrEmpty(uiIntTemp.Text))
                uiIntTemp.Text = "0";

#if !_PK_NUMBOX_
            double dTmp;// = 0;

            if (!double.TryParse(uiKubatura.Text, out dTmp))
                return "ERROR: to nie liczba";
            if (dTmp < 0)
                return "ERROR: musi być > 0!";

            dTmp = 0;
            if (!double.TryParse(uiIntTemp.Text, out dTmp))
                return "ERROR: to nie liczba";

            if (uiSimulGPS.IsOn)
            {
                if (!double.TryParse(uiLatitude.Text, out dTmp))
                    return "ERROR: to nie liczba";
                if (dTmp < -90 || dTmp > 90)
                    return "ERROR: Latitude poza zakresem";
                if (!double.TryParse(uiLongitude.Text, out dTmp))
                    return "ERROR: to nie liczba";
                if (dTmp < 0 || dTmp > 360)
                    return "ERROR: Longitude poza zakresem";
            }
#endif

            return "";
        }

        private async void uiSave_Click(object sender, RoutedEventArgs e)
        {
            string sMsg = VerifyDataOK();
            if (!string.IsNullOrEmpty(sMsg))
            {
                await p.k.DialogBoxAsync(sMsg);
                return;
            }

            // App.moSrc_Airly.ConfigRead(uiStackConfig)
            // App.moSrc_RadioAtHome.ConfigRead(uiStackConfig)
            // App.moSrc_GIOS.ConfigRead(uiStackConfig)

            if (uiStartLoc.SelectedValue != null)
            {
                try
                {
                    p.k.SetSettingsString("settingStartPage", (uiStartLoc.SelectedValue as ComboBoxItem).Content.ToString());
                }
                catch (Exception ex)
                {
                }
            }

            if (uiLiveTile.SelectedValue != null)
            {
                try
                {
                    p.k.SetSettingsString("settingsLiveTile", (uiLiveTile.SelectedValue as ComboBoxItem).Content.ToString());
                }
                catch (Exception ex)
                {
                }
            }

            try
            {
                p.k.SetSettingsString("settingsAlerts", (uiAlerts.SelectedValue as ComboBoxItem).Content.ToString());
            }
            catch (Exception ex)
            {
            }

            //p.k.SetSettingsBool("settingsWHO", uiLimitWg.IsOn);
            p.k.SetSettingsInt("uiLimitWgCombo", uiLimitWgCombo.SelectedIndex);


            p.k.SetSettingsBool("settingsLiveClock", uiLiveTileClock.IsOn);
            p.k.SetSettingsBool("settingsDataLog", uiDataLogs.IsOn);

#if _PK_NUMBOX_
            p.k.SetSettingsInt(uiKubatura, "higroKubatura", 100);
            p.k.SetSettingsInt(uiIntTemp, "higroTemp");
#else
            double dTmp;// = 0;
            if (double.TryParse(uiKubatura.Text, out dTmp))
                p.k.SetSettingsInt("higroKubatura", dTmp * 100);
            
            int iTmp;
            int.TryParse(uiIntTemp.Text, out iTmp);
            p.k.SetSettingsInt("higroTemp", iTmp);
#endif

            p.k.SetSettingsBool("simulateGPS", uiSimulGPS.IsOn);
            if (uiSimulGPS.IsOn)
            {
                try
                {
                    p.k.SetSettingsString(uiLatitude, "gpsEmulationLat");
                    p.k.SetSettingsString(uiLongitude, "gpsEmulationLon");
#if _PK_NUMBOX_
                    EnviroStatus.App.moGpsPoint.Latitude = uiLatitude.Value;
                    EnviroStatus.App.moGpsPoint.Longitude = uiLongitude.Value;
#else
                    double dTmpDeg;
                    double.TryParse(uiLatitude.Text, out dTmpDeg);
                    EnviroStatus.App.moGpsPoint.Latitude = dTmpDeg;
                    double.TryParse(uiLongitude.Text, out dTmpDeg);
                    EnviroStatus.App.moGpsPoint.Longitude = dTmpDeg;
#endif
                }
                catch (Exception ex)
                {
                    p.k.SetSettingsBool("simulateGPS", false);
                }
            }

            p.k.SetSettingsBool(uiFileCache, "settingsFileCache");
            // SetSettingsBool(uiDelToastOnOpen, "settingsDelToastOnOpen")


            Frame.GoBack();
        }

        //private void uiDataLogs_Toggled(object sender, RoutedEventArgs e)
        //{
        //    if (uiDataLogs.IsOn)
        //        uiOpenLogs.IsEnabled = true;
        //    else
        //        uiOpenLogs.IsEnabled = false;
        //}

        private async void uiOpenLogs_Click(object sender, RoutedEventArgs e)
        {
#if NETFX_CORE

            Windows.Storage.StorageFolder oFolder = null;
            oFolder = await p.k.GetLogFolderRootAsync();
            
            if (oFolder == null)
                return;

            oFolder.OpenExplorer();
#endif
        }
        private void uiDataSources_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Zrodelka));
        }

        private void uiSimulGPS_Toggled(object sender, RoutedEventArgs e)
        {
            if (uiSimulGPS.IsOn)
                uiGridGPS.Visibility = Visibility.Visible;
            else
                uiGridGPS.Visibility = Visibility.Collapsed;
        }

        private void uiSettSharing_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsSharing));
        }
    }
}

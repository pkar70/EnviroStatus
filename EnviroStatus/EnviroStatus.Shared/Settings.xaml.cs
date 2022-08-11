
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using System.Linq;
using System;
using Windows.UI.Xaml.Controls;
using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;

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

            string sCurrent = vb14.GetSettingsString(sResCurr);

            ComboBoxItem oCBI;
            if (!string.IsNullOrEmpty(sResAdditItem))
            {
                oCBI = new ComboBoxItem();
                string sAdditItem = vb14.GetLangString(sResAdditItem);
                oCBI.Content = sAdditItem;
                if (string.IsNullOrEmpty(sCurrent) || (sCurrent == sAdditItem))
                    oCBI.IsSelected = true;
                uiCombo.Items.Add(oCBI);
            }


            string sTxt = vb14.GetSettingsString(sList);
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

            string sCurrent = vb14.GetSettingsString(sResCurr);

            var oCBI = new ComboBoxItem();
            if (!string.IsNullOrEmpty(sResAdditItem))
            {
                oCBI.Content = vb14.GetLangString(sResAdditItem);
                if (!string.IsNullOrEmpty(sCurrent) && (sCurrent == oCBI.Content.ToString()))
                    oCBI.IsSelected = true;
                uiCombo.Items.Add(oCBI);
            }

            foreach (VBlib.JedenPomiar oItem in VBlib.App.moPomiaryAll)
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
            string sCurrent = vb14.GetSettingsString(sCurr);

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

            int iWg = vb14.GetSettingsInt("uiLimitWgCombo", -1);
            if(iWg == -1)
            {
                iWg = 0;
                if (vb14.GetSettingsBool("settingsWHO", true))
                    iWg = 1;
            }
            uiLimitWgCombo.SelectedIndex = iWg;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            uiVersion.Text = vb14.GetLangString("msgVersion") + " " + p.k.GetAppVers();

            FillCombo(uiStartLoc, "settingStartPage", "favNames", "resNoAutostart");
            FillComboLiveTile(uiLiveTile, "settingsLiveTile", "resDeadTile");
            ComboAlerts(uiAlerts, "settingsAlerts");
            SetComboWedlug();

            uiLiveTileClock.GetSettingsBool("settingsLiveClock");
            uiDataLogs.GetSettingsBool("settingsDataLog");

#if _PK_NUMBOX_
            uiKubatura.Value = (vb14.GetSettingsInt("higroKubatura", 0) / 100.0);
            uiIntTemp.Value = vb14.GetSettingsInt("higroTemp", 22);
#else
            //uiKubatura.Text = (p.k.GetSettingsInt("higroKubatura", 0) / 100.0).ToString();
            //uiIntTemp.Text = (p.k.GetSettingsInt("higroTemp", 22)).ToString();
#endif 

            //uiLongitude.Text = App.moGpsPoint.Y.ToString();
            //uiLatitude.Text = App.moGpsPoint.X.ToString();
            
            // 2022.08.11 - po przenosinach do VB? wejście do settings jako pierwsza rzecz zrobiona daje NULL
            if(VBlib.App.moGpsPoint is null)
            {
                uiLatitude.Text = vb14.GetSettingsString("gpsEmulationLat");
                uiLongitude.Text = vb14.GetSettingsString("gpsEmulationLon");
            }
            else
            {
                uiLatitude.Text = vb14.GetSettingsString("gpsEmulationLat", VBlib.App.moGpsPoint.Latitude.ToString());
                uiLongitude.Text = vb14.GetSettingsString("gpsEmulationLon", VBlib.App.moGpsPoint.Longitude.ToString());
            }
            //p.k.GetSettingsString(uiLatitude, "gpsEmulationLat", App.moGpsPoint.Latitude.ToString());
            //p.k.GetSettingsString(uiLongitude, "gpsEmulationLon", App.moGpsPoint.Longitude.ToString());

            uiFileCache.GetSettingsBool("settingsFileCache");
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
                await vb14.DialogBoxAsync(sMsg);
                return;
            }

            // App.moSrc_Airly.ConfigRead(uiStackConfig)
            // App.moSrc_RadioAtHome.ConfigRead(uiStackConfig)
            // App.moSrc_GIOS.ConfigRead(uiStackConfig)

            if (uiStartLoc.SelectedValue != null)
            {
                try
                {
                    vb14.SetSettingsString("settingStartPage", (uiStartLoc.SelectedValue as ComboBoxItem).Content.ToString());
                }
                catch
                {
                }
            }

            if (uiLiveTile.SelectedValue != null)
            {
                try
                {
                    vb14.SetSettingsString("settingsLiveTile", (uiLiveTile.SelectedValue as ComboBoxItem).Content.ToString());
                }
                catch 
                {
                }
            }

            try
            {
                vb14.SetSettingsString("settingsAlerts", (uiAlerts.SelectedValue as ComboBoxItem).Content.ToString());
            }
            catch 
            {
            }

            //p.k.SetSettingsBool("settingsWHO", uiLimitWg.IsOn);
            vb14.SetSettingsInt("uiLimitWgCombo", uiLimitWgCombo.SelectedIndex);


            uiLiveTileClock.SetSettingsBool("settingsLiveClock");
            uiDataLogs.SetSettingsBool("settingsDataLog");

#if _PK_NUMBOX_
            vb14.SetSettingsInt("higroKubatura", (int)uiKubatura.Value * 100);
            vb14.SetSettingsInt("higroTemp", (int)uiIntTemp.Value);
#else
            double dTmp;// = 0;
            if (double.TryParse(uiKubatura.Text, out dTmp))
                p.k.SetSettingsInt("higroKubatura", dTmp * 100);
            
            int iTmp;
            int.TryParse(uiIntTemp.Text, out iTmp);
            p.k.SetSettingsInt("higroTemp", iTmp);
#endif

            uiSimulGPS.SetSettingsBool("simulateGPS");
            if (uiSimulGPS.IsOn)
            {
                try
                {
                    vb14.SetSettingsString("gpsEmulationLat", uiLatitude.Value.ToString());
                    vb14.SetSettingsString("gpsEmulationLon", uiLongitude.Value.ToString());
#if _PK_NUMBOX_
                    VBlib.App.moGpsPoint.Latitude = uiLatitude.Value;
                    VBlib.App.moGpsPoint.Longitude = uiLongitude.Value;
#else
                    double dTmpDeg;
                    double.TryParse(uiLatitude.Text, out dTmpDeg);
                    App.moGpsPoint.Latitude = dTmpDeg;
                    double.TryParse(uiLongitude.Text, out dTmpDeg);
                    App.moGpsPoint.Longitude = dTmpDeg;
#endif
                }
                catch 
                {
                    vb14.SetSettingsBool("simulateGPS", false);
                }
            }

            uiFileCache.SetSettingsBool("settingsFileCache");
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

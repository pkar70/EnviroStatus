using System;
//using System.Threading.Tasks;
//using Microsoft.VisualBasic;
//using EnviroStatus;
//using Microsoft.VisualBasic.CompilerServices;
using System.Collections.ObjectModel;
using Windows.Foundation;


namespace EnviroStatus
{

    public class Source_SeismicPortal : Source_Base
    {
        protected override string SRC_SETTING_NAME { get;  } = "sourceSeismicEU";
        protected override string SRC_SETTING_HEADER { get;  } = "SeismicPortal EU";
        protected override string SRC_RESTURI_BASE { get;  } = "https://www.seismicportal.eu/fdsnws/event/1/query?callback=angular.callbacks._1&format=jsonp&limit=50&offset=1&orderby=time";
        public override string SRC_POMIAR_SOURCE { get;  } = "SeismicEU";

        protected override string SRC_URI_ABOUT_EN { get; } = "https://www.seismicportal.eu/";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://www.seismicportal.eu/";

        private double EnergyKJoulesFromMag(double dMag)
        {
            // 4.94065645841246544E-324 ... 1.79769313486231570E+308
            double dTmp = Math.Pow(10, 2.24 + 1.44 * dMag);
            return dTmp;
        }

        private string PowerToPrefix(int iPower)
        {
            if (iPower < -9)
                return "p";
            if (iPower < -6)
                return "n";
            if (iPower < -3)
                return "μ";
            if (iPower < 0)
                return "m";

            if (iPower < 3)
                return "";
            if (iPower < 6)
                return "k";
            if (iPower < 9)
                return "M";
            if (iPower < 12)
                return "G";
            if (iPower < 15)
                return "T";
            if (iPower < 18)
                return "P";

            return "";
        }
        private string BigNumPrefix(double dValue, int iPower)
        {
            if (dValue < 1)
            {
                do
                {
                    dValue *= 1000;
                    iPower -= 3;
                    if (dValue > 1)
                        return ((int)(dValue)).ToString() + " " + PowerToPrefix(iPower);
                    if (iPower < -9)
                        return dValue.ToString() + " p";
                }
                while (true);
            }

            do
            {
                if (dValue < 1000)
                    return ((int)(dValue)).ToString() + " " + PowerToPrefix(iPower);
                if (iPower > 17)
                    return dValue.ToString("###########################################0") + " P";
                dValue /= 1000;
                iPower += 3;
            }
            while (true);
        }

        private string MakeOpisFromKJoules(double dKJoules)
        {
            double dMJoul;
            dMJoul = dKJoules / 1000;

            double dMWh = dMJoul / 3600; // 1 W = 1 J/s ; 1Wh = 1J /s * 3600 s ; 1 MWh = 1MJ * 3600
            double dTonTNT = dKJoules / (4.184 * 1000 * 1000); // "ton of TNT" = 4.184 gigajoules
            double dHirosz = dTonTNT / 15000;     // 15 kT
            double dAnnih = dMJoul / 299792458.0 / 299792.458; // E = mc², czyli m = E/c²; kropka przesunięta co robi z MJ zwykle J, czyli efekt w kg jest, co dzielimy przez 1000, by miec zwykle gramy
            double dWorldDayEnergy = 365 * dMWh / (26614800.0 * 1000);  // 26614800 GWh rocznie 2018

            string sTxt;
            sTxt = "Released energy (about):\n";
            sTxt = sTxt + "= " + BigNumPrefix(dMJoul, 6) + "J, \n";
            sTxt = sTxt + "= " + BigNumPrefix(dMWh, 6) + "Wh, \n";

            int kWh = p.k.GetSettingsInt(SRC_SETTING_NAME + "_homekWh", p.k.IsThisMoje() ? 150 : 0);
            if (kWh > 0)
                sTxt = sTxt + "= " + ((int)(dMWh * 1000 / kWh)).ToString() + " " + p.k.GetSettingsString("resSeismicEU_HomekWh") + ", \n";

            kWh = p.k.GetSettingsInt(SRC_SETTING_NAME + "_krajTWh", p.k.IsThisMoje() ? 170 : 0) / 12;
            if (kWh > 0)
            {
                kWh = (int)(dMWh / 1000 / 1000 / kWh);
                if(kWh>0)
                    sTxt = sTxt + "= " + kWh + " " + p.k.GetSettingsString("resSeismicEU_KrajTWh") + ", \n";
            }


            if (dTonTNT < 10)
            {
                dTonTNT *= 1000;   // na kg
                if (dTonTNT > 1)
                    sTxt = sTxt + "= " + BigNumPrefix(dTonTNT, 3) + "g TNT, \n";
                else
                    sTxt = sTxt + "= " + BigNumPrefix(dTonTNT, 1) + "g TNT, \n";
            }
            else
                sTxt = sTxt + "= " + BigNumPrefix(dTonTNT, 1) + "ton TNT, \n";

            if (dHirosz > 0.1)
                sTxt = sTxt + "= " + BigNumPrefix(dHirosz, 1) + " Hiroshima bombs, \n";

            sTxt = sTxt + "= " + BigNumPrefix(dAnnih, 1) + "g (of matter), \n";

            if (dWorldDayEnergy > 1)
                sTxt = sTxt + "= " + BigNumPrefix(dWorldDayEnergy, 1) + " days of energy production";

            return sTxt;
        }

        private string MakeOpisDokladnySingle(double dMag)
        {
            double dKJoules = EnergyKJoulesFromMag(dMag);
            return MakeOpisFromKJoules(dKJoules);
        }
        private string MakeOpisDokladnySum(double dKJoules, int iCount, string sOldestTimestamp, string sNewestTStamp)
        {
            // mamy już sumę energii w dValue, oraz licznik zdarzeń w iCount
            // przeliczanie w Details na kT, Hirosima, Car, gram anihilacji, kJ, produkcja roczna energii na swiecie na 2017 rok (albo miesiac)
            return "Total eartquakes: " + iCount.ToString() + "\n("
                + sOldestTimestamp + " - " + sNewestTStamp + ")\n\n"
                + MakeOpisFromKJoules(dKJoules);
        }

        private async System.Threading.Tasks.Task<Collection<JedenPomiar>> WczytujDane(Windows.Devices.Geolocation.BasicGeoposition oPos, bool bInTimer)
        {
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            string sPage = await GetREST(SRC_RESTURI_BASE);
            if (sPage.Length < 10)
                return moListaPomiarow;


            int iInd;
            // iInd = sPage.IndexOf("{")
            // sPage = sPage.Substring(iInd)

            iInd = sPage.IndexOf("\"features");
            sPage = "{" + sPage.Substring(iInd);
            iInd = sPage.LastIndexOf(")");
            sPage = sPage.Substring(0, iInd);


            // {"arr": [{ "geometry" {

            bool bError = false;
            string sErr = "";
            Newtonsoft.Json.Linq.JObject oJson = null;
            try
            {
                oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage);
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                bError = true;
            }
            if (bError)
            {
                if (!bInTimer)
                    await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting sensor data (" + SRC_POMIAR_SOURCE + ")\n " + sErr);
                return moListaPomiarow;
            }

            Newtonsoft.Json.Linq.JArray oJsonArr;
            oJsonArr = oJson.GetObject().GetNamedArray("features");


            var oNewSum = new JedenPomiar()
            {
                sSource = SRC_POMIAR_SOURCE,
                sPomiar = "magΣ",
                sUnit = "mag"
            };
            var oNearest = new JedenPomiar()
            {
                sSource = SRC_POMIAR_SOURCE,
                sPomiar = "mag",
                sUnit = "mag"
            };

            double dMaxMag = 0;    // najmocniejszy "skuteczny" w liscie
            int iZasieg = DistanceNum2Metry(p.k.GetSettingsInt(SRC_SETTING_NAME + "_distance")); // liczone do SUMA

            foreach (Newtonsoft.Json.Linq.JToken oVal in oJsonArr)
            {
                Newtonsoft.Json.Linq.JToken oJsonProp = oVal.GetObject().GetNamedToken("properties");

                double dLat, dLon, dOdl, dMag;
                dLat = oJsonProp.GetObject().GetNamedNumber("lat", 0);
                dLon = oJsonProp.GetObject().GetNamedNumber("lon", 0);
                dOdl = (int)oPos.DistanceTo(dLat, dLon);     
                dMag = oJsonProp.GetObject().GetNamedNumber("mag", 0);

                // suma tych w zadanym promieniu
                if (dOdl / 1000 < iZasieg)
                {
                    oNewSum.dLat += 1;   // licznik zdarzen
                    oNewSum.dLon = oNewSum.dLon + EnergyKJoulesFromMag(dMag);
                    oNewSum.dCurrValue = Math.Max(oNewSum.dCurrValue, dMag);   // tu bedzie max
                    oNewSum.sCurrValue = oJsonProp.GetObject().GetNamedString("time", "");
                    if (oNewSum.sTimeStamp == "")
                        oNewSum.sTimeStamp = oNewSum.sCurrValue;
                }

                // najsilniej odczuwane - zakladam malenie z kwadratem 
                double dOdlTmp = Math.Max(0.5, dOdl / 1000); // zeby nie poleciało do nieskonczonosci przy bliskich
                dOdlTmp = dOdlTmp * dOdlTmp;
                if (dMag / dOdlTmp > dMaxMag)
                {
                    dMaxMag = dMag / dOdlTmp;
                    oNearest.dLat = dLat;
                    oNearest.dLon = dLon;
                    oNearest.dOdl = dOdl;
                    oNearest.dCurrValue = dMag;
                    oNearest.sTimeStamp = oJsonProp.GetObject().GetNamedString("time", "");
                    oNearest.dWysok = oJsonProp.GetObject().GetNamedNumber("depth", 0);
                    oNearest.sAdres = oJsonProp.GetObject().GetNamedString("flynn_region", "");
                }
            }

            oNearest.sCurrValue = oNearest.dCurrValue.ToString() + " " + oNearest.sUnit;
            oNearest.sOdl = Odleglosc2String(oNearest.dOdl);
            oNearest.sAddit = MakeOpisDokladnySingle(oNearest.dCurrValue);
            oNearest.sTimeStamp = oNearest.sTimeStamp.Replace("T", " ");

            oNewSum.sTimeStamp = oNewSum.sTimeStamp.Replace("T", " ");   // timestamp najnowszego
            oNewSum.sCurrValue = oNewSum.sCurrValue.Replace("T", " ");   // timestamp najstarszego
            oNewSum.sAddit = MakeOpisDokladnySum(oNewSum.dLon, (int)oNewSum.dLat, oNewSum.sCurrValue, oNewSum.sTimeStamp);
            oNewSum.sCurrValue = oNewSum.dCurrValue.ToString() + " " + oNewSum.sUnit;
            oNewSum.dLat = 0;
            oNewSum.dLon = 0;


            if (oNearest.dCurrValue > 0)
                moListaPomiarow.Add(oNearest);
            if (oNewSum.dCurrValue > 0)
                moListaPomiarow.Add(oNewSum);
            return moListaPomiarow;
        }

        public override async System.Threading.Tasks.Task<Collection<JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            return await WczytujDane(oPos, false);
        }

        public override async System.Threading.Tasks.Task<Collection<JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            Windows.Devices.Geolocation.BasicGeoposition oPos = p.k.NewGeoPos(double.Parse(sId), double.Parse(sAddit));
            return await WczytujDane(oPos, bInTimer);
        }


        private int DistanceNum2Metry(int iDist)
        {
            switch (iDist)
            {
                case 1:
                        return 10;
                case 2:
                        return 100;
                case 3:
                        return 1000;
                case 4:
                        return 10000;
                case 5:
                        return 100000;
                default:
                        return 0;
            }
        }

        private string DistanceNum2Opis(int iDist)
        {
            switch (iDist)
            {
                case 1:
                        return "10 km";
                case 2:
                        return "100 km";
                case 3:
                        return "1000 km";
                case 4:
                        return "10 000 km";
                case 5:
                        return p.k.GetLangString("resSeismicEU_DistAll");
                default:
                        return "???";
            }
        }

        public void uiSettDistance_Changed(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Windows.UI.Xaml.Controls.Slider oSld;
            oSld = sender as Windows.UI.Xaml.Controls.Slider;
            if (oSld != null)
            {
                var oGrid = oSld.Parent as Windows.UI.Xaml.Controls.Grid;
                foreach (Windows.UI.Xaml.UIElement oItem in oGrid.Children)
                {
                    Windows.UI.Xaml.Controls.TextBlock oTB;
                    oTB = oItem as Windows.UI.Xaml.Controls.TextBlock;
                    if (oTB != null)
                    {
                        if (oTB.Name == "uiConfig_SeismicEU_Text")
                            oTB.Text = DistanceNum2Opis((int)oSld.Value);
                    }
                }
            }
        }

        public override void ReadResStrings()
        {
            p.k.SetSettingsString("resSeismicEU_KrajTWh", p.k.GetLangString("resSeismicEU_KrajTWh"));
            p.k.SetSettingsString("resSeismicEU_HomekWh", p.k.GetLangString("resSeismicEU_HomekWh"));
        }


        public override void ConfigCreate(Windows.UI.Xaml.Controls.StackPanel oStack)
        {
            base.ConfigCreate(oStack);

            Windows.UI.Xaml.Data.Binding oBind = new Windows.UI.Xaml.Data.Binding();
            oBind.ElementName = "uiConfig_" + SRC_SETTING_NAME;
            oBind.Path = new Windows.UI.Xaml.PropertyPath("IsOn");

            var oSld = new Windows.UI.Xaml.Controls.Slider();
            oSld.Name = "uiConfig_SeismicEU_Slider";
            oSld.Minimum = 1;
            oSld.Maximum = 5;
            oSld.Value = p.k.GetSettingsInt(SRC_SETTING_NAME + "_distance", 2);
            oSld.Header = p.k.GetLangString("resSeismicEU_SldHdr");
            oSld.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            oSld.ValueChanged += uiSettDistance_Changed;
            oSld.SetBinding(Windows.UI.Xaml.Controls.Slider.IsEnabledProperty, oBind);

            var oTB = new Windows.UI.Xaml.Controls.TextBlock();
            oTB.Name = "uiConfig_SeismicEU_Text";
            oTB.Text = DistanceNum2Opis((int)oSld.Value);

            var oCol1 = new Windows.UI.Xaml.Controls.ColumnDefinition();
            oCol1.Width = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Star);
            var oCol2 = new Windows.UI.Xaml.Controls.ColumnDefinition();
            oCol2.Width = new Windows.UI.Xaml.GridLength(0, Windows.UI.Xaml.GridUnitType.Auto);

            var oGrid = new Windows.UI.Xaml.Controls.Grid();
            oGrid.ColumnDefinitions.Add(oCol1);
            oGrid.ColumnDefinitions.Add(oCol2);

            oGrid.Children.Add(oSld);
            oGrid.Children.Add(oTB);

            Windows.UI.Xaml.Controls.Grid.SetColumn(oSld, 0);
            Windows.UI.Xaml.Controls.Grid.SetColumn(oTB, 1);

            oStack.Children.Add(oGrid);

#if _PK_NUMBOX_

            var oHomekWh = new Microsoft.UI.Xaml.Controls.NumberBox();
            oHomekWh.Name = "uiConfig_SeismicEU_HomekWh";
            oHomekWh.Value = p.k.GetSettingsInt(SRC_SETTING_NAME + "_homekWh", p.k.IsThisMoje() ? 150 : 0);
            oHomekWh.Header = p.k.GetLangString("resSeismicEU_HomekWh_Hdr") + " (kWh)";
            oHomekWh.Minimum = 0;
            oHomekWh.SpinButtonPlacementMode = Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Compact;
            oHomekWh.SetBinding(Microsoft.UI.Xaml.Controls.NumberBox.IsEnabledProperty, oBind);
            oStack.Children.Add(oHomekWh);

            var oKrajTWh = new Microsoft.UI.Xaml.Controls.NumberBox();
            oKrajTWh.Name = "uiConfig_SeismicEU_KrajTWh";
            oKrajTWh.Value = p.k.GetSettingsInt(SRC_SETTING_NAME + "_krajTWh", p.k.IsThisMoje() ? 170 : 0);
            oKrajTWh.Header = p.k.GetLangString("resSeismicEU_KrajTWh_Hdr") + " (TWh)";
            oKrajTWh.SpinButtonPlacementMode = Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Compact;
            oKrajTWh.Minimum = 0;
            oKrajTWh.SetBinding(Microsoft.UI.Xaml.Controls.NumberBox.IsEnabledProperty, oBind);
            oStack.Children.Add(oKrajTWh);

#else
            Windows.UI.Xaml.Input.InputScope oInpScope = new Windows.UI.Xaml.Input.InputScope();
            Windows.UI.Xaml.Input.InputScopeName scopeName = new Windows.UI.Xaml.Input.InputScopeName();
            scopeName.NameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Number;
            oInpScope.Names.Add(scopeName);

            var oHomekWh = new Windows.UI.Xaml.Controls.TextBox();
            oHomekWh.Name = "uiConfig_SeismicEU_HomekWh";
            oHomekWh.Text = p.k.GetSettingsInt(SRC_SETTING_NAME + "_homekWh", p.k.IsThisMoje() ? 150 : 0).ToString();
            oHomekWh.Header = p.k.GetLangString("resSeismicEU_HomekWh_Hdr") + " (kWh)";
            oHomekWh.InputScope = oInpScope;
            oStack.Children.Add(oHomekWh);


            oInpScope = new Windows.UI.Xaml.Input.InputScope();
            scopeName = new Windows.UI.Xaml.Input.InputScopeName();
            scopeName.NameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Number;
            oInpScope.Names.Add(scopeName);

            var oKrajTWh = new Windows.UI.Xaml.Controls.TextBox();
            oKrajTWh.Name = "uiConfig_SeismicEU_KrajTWh";
            oKrajTWh.Text = p.k.GetSettingsInt(SRC_SETTING_NAME + "_krajTWh", p.k.IsThisMoje() ? 170 : 0).ToString();
            oKrajTWh.Header = p.k.GetLangString("resSeismicEU_KrajTWh_Hdr") + " (TWh)";
            oKrajTWh.InputScope = oInpScope;
            oStack.Children.Add(oKrajTWh);

#endif


        }

        public override void ConfigRead(Windows.UI.Xaml.Controls.StackPanel oStack)
        {
            base.ConfigRead(oStack);

            foreach (Windows.UI.Xaml.UIElement oItem in oStack.Children)
            {
                // Dim oTS As ToggleSwitch
                // oTS = TryCast(oItem, ToggleSwitch)
                // If oTS IsNot Nothing Then
                // If oTS.Name = "uiConfig_SeismicEU_MaxAll" Then SetSettingsBool(SRC_SETTING_NAME & "_MaxAll", oTS.IsOn)
                // End If

                Windows.UI.Xaml.Controls.Grid oGrid;
                oGrid = oItem as Windows.UI.Xaml.Controls.Grid;
                if (oGrid != null)
                {
                    foreach (var oChild in oGrid.Children)
                    {
                        Windows.UI.Xaml.Controls.Slider oSld;
                        oSld = oChild as Windows.UI.Xaml.Controls.Slider;
                        if (oSld != null)
                        {
                            if (oSld.Name == "uiConfig_SeismicEU_Slider")
                                p.k.SetSettingsInt(SRC_SETTING_NAME + "_distance", (int)oSld.Value);
                        }
                    }
                }

#if _PK_NUMBOX_
                Microsoft.UI.Xaml.Controls.NumberBox oTBox;
                oTBox = oItem as Microsoft.UI.Xaml.Controls.NumberBox;
                if (oTBox != null)
                {
                    if (oTBox.Name == "uiConfig_SeismicEU_HomekWh")
                        p.k.SetSettingsInt(SRC_SETTING_NAME + "_homekWh", oTBox.Value);
                    if (oTBox.Name == "uiConfig_SeismicEU_KrajTWh")
                        p.k.SetSettingsInt(SRC_SETTING_NAME + "_krajTWh", oTBox.Value);
                }

#else
                Windows.UI.Xaml.Controls.TextBox oTBox;
                oTBox = oItem as Windows.UI.Xaml.Controls.TextBox;

                if (oTBox != null)
                {
                    if(oTBox.Name == "uiConfig_SeismicEU_HomekWh")
                        p.k.SetSettingsInt(SRC_SETTING_NAME + "_homekWh", int.Parse(oTBox.Text));
                    if (oTBox.Name == "uiConfig_SeismicEU_KrajTWh")
                        p.k.SetSettingsInt(SRC_SETTING_NAME + "_krajTWh", int.Parse(oTBox.Text));
                }
#endif

            }
        }
    }

}
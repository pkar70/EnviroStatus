using Windows.Foundation;
using System.Threading.Tasks;
//using Microsoft.VisualBasic;
using System.Linq;
using System.Collections.ObjectModel;
using System;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public partial class Source_DarkSky : Source_Base
    {

        protected override string SRC_SETTING_NAME { get; } = "sourceDarkSky";
        protected override string SRC_SETTING_HEADER { get; } = "Dark Sky";
        protected override string SRC_RESTURI_BASE { get; } = "https://api.darksky.net/forecast/";
        public override string SRC_POMIAR_SOURCE { get;  } = "DarkSky";
        protected override bool SRC_HAS_KEY { get; set; } = true;
        protected override string SRC_KEY_LOGIN_LINK { get; } = "https://darksky.net/dev/register";
        public override bool SRC_IN_TIMER { get;  } = true;
        protected override string SRC_URI_ABOUT_EN { get; } = "https://darksky.net/";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://darksky.net/";

        public override void ReadResStrings()
        {
            p.k.SetSettingsString("resTempOdczuwana", p.k.GetLangString("resTempOdczuwana"));
            p.k.SetSettingsString("resPomiarWidocz", p.k.GetLangString("resPomiarWidocz"));
            p.k.SetSettingsString("resPomiarRosa", p.k.GetLangString("resPomiarRosa"));
            p.k.SetSettingsString("resPomiarZachm", p.k.GetLangString("resPomiarZachm"));
        }

        public override async Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            return await GetDataFromFavSensor(oPos.Latitude.ToString(), oPos.Longitude.ToString(), false);    // bo tak :) 
        }

        public override async Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            // w tym wypadku to Lat i Long
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            string sPage = await GetREST(sId + "," + sAddit + "?units=si&exclude=minutely,hourly,daily");
            if (sPage.Length < 10)
                return moListaPomiarow;

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

            // Public Property sPomiar As String = "" ' jaki pomiar (np. PM10)
            // Public Property sCurrValue As String = "" ' etap 2: wartosc
            // Public Property dCurrValue As Double = 0
            // Public Property sUnit As String = ""
            // Public Property sTimeStamp As String = "" ' etap 2: kiedy
            // Public Property sAddit As String = ""
            // Public Property sOdl As String = ""
            // oItem.sAlert - wykrzykniki
            // oNew.sSource = SRC_POMIAR_SOURCE

            // "flags"
            // {
            // "sources":	["meteoalarm","cmc","gfs","icon","isd","madis"],
            // "meteoalarm-license":"Based on data from EUMETNET - MeteoAlarm [https://www.meteoalarm.eu/]. Time delays between this website and the MeteoAlarm website are possible; for the most up to date information about alert levels as published by the participating National Meteorological Services please use the MeteoAlarm website.",
            // "nearest-station":14.302,	[km]

            double dOdl = 654321.0;    // jak nie ma podanej, to uznaj że daleka (654 km)
            try
            {
                Newtonsoft.Json.Linq.JToken oJsonFlags;
                oJsonFlags = oJson.GetObject().GetNamedToken("flags");
                dOdl = oJsonFlags.GetObject().GetNamedNumber("nearest-station") * 1000;  // z km na metry
            }
            catch
            {
            }

            // If oJson.GetObject.Values.Contains("alerts") Then

            if (sPage.Contains("\"alerts\""))
            {
                try
                {
                    // "alerts" [
                    // {
                    // "title": "Flood Watch for Mason, WA",
                    // "time": 1509993360,
                    // "expires": 1510036680,
                    // "description": "...FLOOD WATCH REMAINS IN EFFECT THROUGH LATE MONDAY NIGHT...\nTHE FLOOD WATCH CONTINUES FOR\n* A PORTION OF NORTHWEST WASHINGTON...INCLUDING THE FOLLOWING\nCOUNTY...MASON.\n* THROUGH LATE FRIDAY NIGHT\n* A STRONG WARM FRONT WILL BRING HEAVY RAIN TO THE OLYMPICS\nTONIGHT THROUGH THURSDAY NIGHT. THE HEAVY RAIN WILL PUSH THE\nSKOKOMISH RIVER ABOVE FLOOD STAGE TODAY...AND MAJOR FLOODING IS\nPOSSIBLE.\n* A FLOOD WARNING IS IN EFFECT FOR THE SKOKOMISH RIVER. THE FLOOD\nWATCH REMAINS IN EFFECT FOR MASON COUNTY FOR THE POSSIBILITY OF\nAREAL FLOODING ASSOCIATED WITH A MAJOR FLOOD.\n",
                    // "uri": "http://alerts.weather.gov/cap/wwacapget.php?x=WA1255E4DB8494.FloodWatch.1255E4DCE35CWA.SEWFFASEW.38e78ec64613478bb70fc6ed9c87f6e6"
                    // },
                    Newtonsoft.Json.Linq.JArray oJsonAlerts;
                    oJsonAlerts = oJson.GetObject().GetNamedArray("alerts");

                    int iCnt = 1;

                    foreach (Newtonsoft.Json.Linq.JToken oJSonAlert in oJsonAlerts)
                    {
                        var oNew = new JedenPomiar();
                        oNew.sSource = SRC_POMIAR_SOURCE;
                        oNew.dOdl = dOdl;
                        oNew.sOdl = "≥ " + ((int)(dOdl / 1000)).ToString() + " km";
                        oNew.sAlert = "!!";  // w miarę ważne
                        oNew.sTimeStamp = App.UnixTimeToTime((long)oJSonAlert.GetObject().GetNamedNumber("time"));
                        oNew.sCurrValue = oJSonAlert.GetObject().GetNamedString("title");
                        string sTmp;
                        sTmp = oJSonAlert.GetObject().GetNamedString("description"); // description & expires
                        sTmp = sTmp.Replace("%lf", "\n");  // się takie zdażyło
                        oNew.sAddit = sTmp;

                        oNew.sPomiar = "Alert" + iCnt;

                        switch (oJSonAlert.GetObject().GetNamedString("severity"))
                        {
                            case "advisory":
                                    oNew.sAlert = "!";
                                    break;
                            case "watch":
                                    oNew.sAlert = "!!";
                                    break;
                            case "warning":
                                    oNew.sAlert = "!!!";
                                    break;
                        }

                        moListaPomiarow.Add(oNew);

                        iCnt += 1;
                    }
                }
                catch 
                {
                }
            }

            try
            {
                // "currently"
                // {
                // "time":1553454247,
                // "apparentTemperature":7.54,
                // "dewPoint":2.88,
                // "cloudCover":1,
                // "uvIndex":0,
                // "visibility":9.51,
                // "ozone":344.5
                var oTemplate = new JedenPomiar();
                oTemplate.sSource = SRC_POMIAR_SOURCE;

                Newtonsoft.Json.Linq.JToken oJsonCurrent;
                oJsonCurrent = oJson.GetObject().GetNamedToken("currently");
                oTemplate.sTimeStamp = App.UnixTimeToTime((long)oJsonCurrent.GetObject().GetNamedNumber("time"));
                oTemplate.dOdl = dOdl;
                oTemplate.sOdl = "≥ " + ((int)(dOdl / 1000)).ToString() + " km";

                // i to powtorzyc dla kazdego pomiaru
                try
                {
                    var oNew = new JedenPomiar() { sSource = oTemplate.sSource, sTimeStamp = oTemplate.sTimeStamp, dOdl = oTemplate.dOdl, sOdl = oTemplate.sOdl };
                    oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("apparentTemperature");
                    oNew.sUnit = " °C";
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;
                    oNew.sPomiar = p.k.GetSettingsString("resTempOdczuwana");
                    moListaPomiarow.Add(oNew);
                }
                catch 
                {
                }

                try
                {
                    var oNew = new JedenPomiar() { sSource = oTemplate.sSource, sTimeStamp = oTemplate.sTimeStamp, dOdl = oTemplate.dOdl, sOdl = oTemplate.sOdl };
                    oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("dewPoint");
                    oNew.sUnit = " °C";
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;
                    oNew.sPomiar = p.k.GetSettingsString("resPomiarRosa");
                    moListaPomiarow.Add(oNew);
                }
                catch 
                {
                }

                try
                {
                    var oNew = new JedenPomiar() { sSource = oTemplate.sSource, sTimeStamp = oTemplate.sTimeStamp, dOdl = oTemplate.dOdl, sOdl = oTemplate.sOdl };
                    oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("cloudCover") * 100;
                    oNew.sUnit = " %";
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;
                    oNew.sPomiar = p.k.GetSettingsString("resPomiarZachm");
                    moListaPomiarow.Add(oNew);
                }
                catch 
                {
                }

                try
                {
                    var oNew = new JedenPomiar() { sSource = oTemplate.sSource, sTimeStamp = oTemplate.sTimeStamp, dOdl = oTemplate.dOdl, sOdl = oTemplate.sOdl };
                    oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("uvIndex");
                    oNew.sUnit = "";
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;
                    oNew.sPomiar = "UV index";
                    // przekroczenia
                    // http://www.who.int/uv/publications/en/UVIGuide.pdf
                    if (oNew.dCurrValue >= 6)
                        oNew.sAlert = "!";
                    if (oNew.dCurrValue >= 8)
                        oNew.sAlert = "!!";
                    if (oNew.dCurrValue >= 11)
                        oNew.sAlert = "!!!";
                    // moderate, very high - poniewaz Tab jest za daleko...
                    oNew.sLimity = "WHO exposure categories\n" + "Low\t <3\n" + "Moderate\t 3..5\n" + "High\t 6..7 (seek shade during midday)\n" + "Very high\t 8..10 (avoid being outside midday)\n" + "Extreme\t >10\n";
                    moListaPomiarow.Add(oNew);
                }
                catch 
                {
                }

                try
                {
                    var oNew = new JedenPomiar() { sSource = oTemplate.sSource, sTimeStamp = oTemplate.sTimeStamp, dOdl = oTemplate.dOdl, sOdl = oTemplate.sOdl };
                    oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("visibility");
                    if (oNew.dCurrValue > 10)
                    {
                        int iRounder = (int)(oNew.dCurrValue * 100);
                        double dRounder = ((double)iRounder) / 100;

                        oNew.sUnit = " km"; // z zaokrągleniem do setnych, żeby się nie myliło z tysięcznymi
                        oNew.sCurrValue = dRounder.ToString() + oNew.sUnit;
                    }
                    else
                    {
                        oNew.sUnit = " m";
                        oNew.sCurrValue = (oNew.dCurrValue*1000).ToString("") + oNew.sUnit;

                    }
                    oNew.sPomiar = p.k.GetSettingsString("resPomiarWidocz");
                    moListaPomiarow.Add(oNew);
                }
                catch 
                {
                }
            }
            catch 
            {
            }


            if (moListaPomiarow.Count < 1)
            {
                if (!bInTimer)
                    await p.k.DialogBoxAsync("ERROR: data parsing error " + SRC_POMIAR_SOURCE);
                return moListaPomiarow;
            }

            return moListaPomiarow;
        }
    }
}

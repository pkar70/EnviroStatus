// 2019.10.30 SolarWindTemp może być null - wtedy liczy jako zero.

// Partial Public Class App
// Public Shared moSrc_NoaaWind As Source_NoaaWind = New Source_NoaaWind
// End Class

using Windows.Foundation;
//using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using System;

namespace EnviroStatus
{
    public class Source_NoaaWind : Source_Base
    {
        // ułatwienie dodawania następnych
        protected override string SRC_SETTING_NAME { get;  } = "sourceNoaaWind";
        protected override string SRC_SETTING_HEADER { get;  } = "NOAA solar wind";
        protected override string SRC_RESTURI_BASE { get;  } = "https://services.swpc.noaa.gov/products/solar-wind/plasma-5-minute.json";
        public override string SRC_POMIAR_SOURCE { get; } = "NOAAwind";
        public override bool SRC_NO_COMPARE { get; } = true;
        protected override string SRC_URI_ABOUT_EN { get; } = "https://www.swpc.noaa.gov/products/real-time-solar-wind";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://www.swpc.noaa.gov/products/real-time-solar-wind";

        public override void ReadResStrings()
        {
            p.k.SetSettingsString("resPomiarSolarWindDensity", p.k.GetLangString("resPomiarSolarWindDensity"));
            p.k.SetSettingsString("resPomiarSolarWindSpeed", p.k.GetLangString("resPomiarSolarWindSpeed"));
            p.k.SetSettingsString("resPomiarAdditSolarWindDensity", p.k.GetLangString("resPomiarAdditSolarWindDensity"));
            p.k.SetSettingsString("resPomiarAdditSolarWindSpeed", p.k.GetLangString("resPomiarAdditSolarWindSpeed"));
            p.k.SetSettingsString("resPomiarSolarWindTemp", p.k.GetLangString("resPomiarSolarWindTemp"));
            p.k.SetSettingsString("resPomiarAdditSolarWindTemp", p.k.GetLangString("resPomiarAdditSolarWindTemp"));
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            return await GetDataFromFavSensor("", "", false);
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            string sPage = await GetREST("");
            if (sPage.Length < 10)
                return moListaPomiarow;

            bool bError = false;
            string sErr = "";
            Newtonsoft.Json.Linq.JArray oJsonArray = null;
            try
            {
                oJsonArray = Newtonsoft.Json.Linq.JArray.Parse(sPage);
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

            // [
            // ["time_tag","density","speed","temperature"],
            // ["2019-05-31 10:54:00.000","2.95","433.7","56729"],
            // ["2019-05-31 10:55:00.000","2.98","432.2","57292"],
            // ["2019-05-31 10:56:00.000","2.93","431.0","54333"]
            // ]
            // density: 1 / cm³, speed: km/ s, temp °K [5.76E4, 54333]

            Newtonsoft.Json.Linq.JArray oJSonLast;
            oJSonLast = (Newtonsoft.Json.Linq.JArray)oJsonArray[oJsonArray.Count()-1];

            DateTime oDate;
            string sTime;
            sTime = oJSonLast[0].ToString();
            if (DateTime.TryParseExact(sTime, "yyyy-MM-dd HH:mm:ss.000", null, System.Globalization.DateTimeStyles.AssumeUniversal, out oDate))
                sTime = oDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            else
                sTime = sTime + " UTC";


            // density
            var oNew = new JedenPomiar();
            oNew.sSource = SRC_POMIAR_SOURCE;
            oNew.sTimeStamp = sTime;
            oNew.sUnit = "/cm³";
            oNew.sCurrValue = oJSonLast[1].ToString();
            //oNew.dCurrValue =  double.Parse(oNew.sCurrValue);
            oNew.dCurrValue = oNew.sCurrValue.ParseDouble(0);

            oNew.sCurrValue = oNew.sCurrValue + "/cm³";
            oNew.sPomiar = p.k.GetSettingsString("resPomiarSolarWindDensity");
            oNew.sAddit = p.k.GetSettingsString("resPomiarAdditSolarWindDensity");

            moListaPomiarow.Add(oNew);

            // speed
            oNew = new JedenPomiar();
            oNew.sSource = SRC_POMIAR_SOURCE;
            oNew.sTimeStamp = sTime;
            oNew.sUnit = "km/s";
            oNew.sCurrValue = oJSonLast[2].ToString();
            //oNew.dCurrValue = double.Parse(oNew.sCurrValue);
            oNew.dCurrValue = oNew.sCurrValue.ParseDouble(0);
            oNew.sCurrValue = oNew.sCurrValue + " " + oNew.sUnit;
            oNew.sPomiar = p.k.GetSettingsString("resPomiarSolarWindSpeed");
            oNew.sAddit = p.k.GetSettingsString("resPomiarAdditSolarWindSpeed");
            // oNew.sAddit = "= " & oNew.dCurrValue * 3600 & " km/h" - bez sensu! 400 km/s dawaloby 1.4 mln km/h

            moListaPomiarow.Add(oNew);

            // temp
            oNew = new JedenPomiar();
            oNew.sSource = SRC_POMIAR_SOURCE;
            oNew.sTimeStamp = sTime;
            oNew.sUnit = " K";
            try
            {
                oNew.sCurrValue = oJSonLast[3].ToString();
            }
            catch 
            {
                oNew.sCurrValue = "0";
            }
            //oNew.dCurrValue = double.Parse(oNew.sCurrValue);
            oNew.dCurrValue = oNew.sCurrValue.ParseDouble(0);
            if (oNew.sCurrValue.Length > 4)
                oNew.sCurrValue = oNew.sCurrValue.Substring(0, oNew.sCurrValue.Length - 3) + " " + oNew.sCurrValue.Substring(oNew.sCurrValue.Length - 3);
            oNew.sCurrValue = oNew.sCurrValue + " " + oNew.sUnit;
            oNew.sPomiar = p.k.GetSettingsString("resPomiarSolarWindTemp");
            oNew.sAddit = p.k.GetSettingsString("resPomiarAdditSolarWindTemp");

            moListaPomiarow.Add(oNew);


            return moListaPomiarow;
        }
    }
}

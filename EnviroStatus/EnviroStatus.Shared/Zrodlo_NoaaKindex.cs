// Partial Public Class App
// Public Shared moSrc_NoaaKind As Source_NoaaKindex = New Source_NoaaKindex
// End Class

using Windows.Foundation;
//using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System;

namespace EnviroStatus
{
    public class Source_NoaaKindex : Source_Base
    {
        protected override string SRC_SETTING_NAME { get;  } = "sourceNoaaKind";
        protected override string SRC_SETTING_HEADER { get;  } = "NOAA planetary K-index (magnetic activity)";
        protected override string SRC_RESTURI_BASE { get;  } = "https://services.swpc.noaa.gov/products/noaa-planetary-k-index-forecast.json";
        public override string SRC_POMIAR_SOURCE { get;  } = "NOAAkind";
        public override bool SRC_IN_TIMER { get;  } = true;
        public override bool SRC_NO_COMPARE { get; } = true;
        protected override string SRC_URI_ABOUT_EN { get; } = "https://www.swpc.noaa.gov/products/planetary-k-index";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://www.swpc.noaa.gov/products/planetary-k-index";

        public override void ReadResStrings()
        {
            p.k.SetSettingsString("resPomiarNoaaKindexPredicted", p.k.GetLangString("resPomiarNoaaKindexPredicted"));
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
            // ["time_tag","kp","observed","noaa_scale"],
            // ["2019-05-31 06:00:00","2","observed",null],
            // ["2019-05-31 12:00:00","1","estimated",null],
            // ["2019-06-01 00:00:00","3","predicted",null]
            // ]
            // G0 (Kp<5), G1 (Kp=5), G2 (Kp=6), G3 (Kp=7), G4 (Kp=8), G5 (Kp>=9)

            var oNew = new JedenPomiar();
            oNew.sSource = SRC_POMIAR_SOURCE;
            oNew.sPomiar = "Kp index";
            string sNowyPomiar = "";
            string sNowyTime = "";

            foreach (Newtonsoft.Json.Linq.JToken oJsonOdczyt in oJsonArray)
            {
                if (oJsonOdczyt[2].ToString() == "estimated")
                {
                    sNowyPomiar = oJsonOdczyt[1].ToString();
                    sNowyTime = oJsonOdczyt[0].ToString();
                    break;
                }
                if (oJsonOdczyt[2].ToString() != "observed")
                    break;
                oNew.sTimeStamp = oJsonOdczyt[0].ToString();
                oNew.sCurrValue = oJsonOdczyt[1].ToString();
            }

            if (string.IsNullOrEmpty(oNew.sTimeStamp))
                return moListaPomiarow;

            DateTime oDate;
            string sTime = oNew.sTimeStamp;
            if (DateTime.TryParseExact(sTime, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out oDate))
                sTime = oDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            else
                sTime = sTime + " UTC";
            oNew.sTimeStamp = sTime;
            oNew.dCurrValue = oNew.sCurrValue.ParseDouble(0);

            if (DateTime.TryParseExact(sNowyTime, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out oDate))
                sNowyTime = oDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            else
                sNowyTime = sNowyTime + " UTC";

            oNew.sAddit = p.k.GetSettingsString("resPomiarNoaaKindexPredicted") + " " + sNowyTime + ": " + sNowyPomiar;

            if (oNew.dCurrValue >= 7)
                oNew.sAlert = "!";
            if (oNew.dCurrValue >= 8)
                oNew.sAlert = "!!";
            if (oNew.dCurrValue >= 9)
                oNew.sAlert = "!!!";

            moListaPomiarow.Add(oNew);


            return moListaPomiarow;
        }
    }
}

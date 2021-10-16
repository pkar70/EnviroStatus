
using Windows.Foundation;
//using System.Threading.Tasks;
//using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public class Source_NoaaAlert : Source_Base
    {
        protected override string SRC_SETTING_NAME { get;  } = "sourceNoaaAlert";
        protected override string SRC_SETTING_HEADER { get;  } = "NOAA alerts";
        protected override string SRC_RESTURI_BASE { get;  } = "https://services.swpc.noaa.gov/products/alerts.json";
        public override string SRC_POMIAR_SOURCE { get;  } = "NOAAalert";
        public override bool SRC_IN_TIMER { get;  } = true;
        public override bool SRC_NO_COMPARE { get; } = true;
        protected override string SRC_URI_ABOUT_EN { get; } = "https://www.swpc.noaa.gov/products/alerts-watches-and-warnings";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://www.swpc.noaa.gov/products/alerts-watches-and-warnings";

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
            // {"product_id":"EF3A","issue_datetime":"2019-05-31 08:59:39.047","message":"Space Weather Message Code: ALTEF3\r\nSerial Number: 2945\r\nIssue Time: 2019 May 31 0859 UTC\r\n\r\nCONTINUED ALERT: Electron 2MeV Integral Flux exceeded 1000pfu\r\nContinuation of Serial Number: 2944\r\nBegin Time: 2019 May 30 1535 UTC\r\nYesterday Maximum 2MeV Flux: 1766 pfu\r\n\r\nNOAA Space Weather Scale descriptions can be found at\r\nwww.swpc.noaa.gov\/noaa-scales-explanation\r\n\r\nPotential Impacts: Satellite systems may experience significant charging resulting in increased risk to satellite systems."},
            // czasem ma: Valid Until:
            // Valid To: 2019 May 29 0900 UTC
            // Valid Until: 2019 May 29 1500 UTC

            // ]
            // robimy inaczej - pokazuje tylko nowsze niz widziany poprzednio

            // Dim sPrevLastTime As String = GetSettingsString("NOAAalertTLastimestamp")
            int iGuard = 0; // limit ostrzezen (potrzebne przy pierwszym uruchomieniu)
                            // Dim sLastTime As String = "1999-01-01"
            foreach (Newtonsoft.Json.Linq.JToken oJsonVal in oJsonArray)
            {
                string sThisTime = oJsonVal.GetObject().GetNamedString("issue_datetime");
                // If sThisTime > sPrevLastTime Then
                // If sLastTime < sThisTime Then sLastTime = sThisTime
                iGuard += 1;
                if (iGuard > 5)
                    break;

                var oNew = new JedenPomiar();
                oNew.sSource = SRC_POMIAR_SOURCE;
                oNew.sTimeStamp = sThisTime;

                int iInd;
                iInd = sThisTime.IndexOf(".");
                if (iInd > 0)
                    sThisTime = sThisTime.Substring(0, iInd);

                DateTime oDate;
                if (DateTime.TryParseExact(sThisTime, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out oDate))
                {
                    oNew.sTimeStamp = oDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    if (oDate.AddDays(1) < DateTime.Now)
                        continue;
                }
                else
                    oNew.sTimeStamp = sThisTime + " UTC";

                if (iGuard > 1)
                    oNew.sPomiar = "Alert" + iGuard.ToString();
                else
                    oNew.sPomiar = "Alert";

                string sMsg = oJsonVal.GetObject().GetNamedString("message");
                sMsg = sMsg.Replace("NOAA Space Weather Scale descriptions can be found at", "");
                sMsg = sMsg.Replace("www.swpc.noaa.gov/noaa-scales-explanation", "");
                sMsg = sMsg.Replace(@"\r\n\r\n", @"\r\n");
                sMsg = sMsg.Replace(@"\r\n", "\n");
                var aTmp = sMsg.Split('\n');
                foreach (string sLine in aTmp)
                {
                    iInd = sLine.IndexOf(":");

                    if (sLine.Contains("Space Weather Message Code"))
                        oNew.sPomiar = "N." + sLine.Substring(iInd + 1).Trim();
                    // *TODO* ewentualnie nie wedle Alert/Watch/Warning, ale zapisanej w środku skali: G1- Minor etc.
                    if (sLine.Contains("ALERT:"))
                    {
                        oNew.sCurrValue = sLine.Substring(iInd + 1).Trim();
                        oNew.sAlert = "!!!";
                    }

                    if (sLine.Contains("WATCH:"))
                    {
                        oNew.sCurrValue = sLine.Substring(iInd + 1).Trim();
                        oNew.sAlert = "!!";
                    }

                    if (sLine.Contains("WARNING:"))
                    {
                        oNew.sCurrValue = sLine.Substring(iInd + 1).Trim();
                        oNew.sAlert = "!";
                    }
                } // linie w 'message'

                oNew.sAddit = sMsg;
                moListaPomiarow.Add(oNew);
            }

            // If sLastTime > sPrevLastTime Then SetSettingsString("NOAAalertTLastimestamp", sLastTime)

            return moListaPomiarow;
        }
    }
}

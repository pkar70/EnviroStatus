using Windows.Foundation;
//using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using System;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public partial class Source_Airly : Source_Base
    {
        protected override string SRC_SETTING_NAME { get; } = "sourceAirly";
        protected override string SRC_SETTING_HEADER { get; } = "Airly";
        protected override string SRC_RESTURI_BASE { get; } = "https://airapi.airly.eu/";
        public override string SRC_POMIAR_SOURCE { get;  } = "airly";
        protected override bool SRC_HAS_TEMPLATES { get; } = true;
        protected override bool SRC_HAS_KEY { get; set; } = true;
        protected override string SRC_KEY_LOGIN_LINK { get; } = "https://developer.airly.eu/login";
        public override bool SRC_IN_TIMER { get;  } = true;
        
        //protected override string SRC_MY_KEY { get; } = PSWD_SRC_MY_KEY;

        protected override string SRC_URI_ABOUT_EN { get; } = "https://airly.org/en/";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://airly.org/pl/";

        // --header 'Accept: application/json' \
        // 'https://airapi.airly.eu/v2/installations/nearest?lat=50.062006&lng=19.940984&maxDistanceKM=5&maxResults=3'

        private void AddPomiar(JedenPomiar oNew)
        {
            foreach (EnviroStatus.JedenPomiar oItem in moListaPomiarow)
            {
                if ((oItem.sPomiar ?? "") == (oNew.sPomiar ?? ""))
                {
                    // porownanie dat

                    // porownanie odleglosci
                    if (oItem.dOdl > oNew.dOdl)
                        // moListaPomiarow.Remove(oItem)
                        oItem.bDel = true;
                    else
                        return;// mamy nowszy pomiar, czyli oNew nas nie interesuje
                }
            }
            moListaPomiarow.Add(oNew);
        }

        private string NormalizePomiarName(string sPomiar)
        {
            if ((sPomiar ?? "") == "PM10")
                return "PM₁₀";
            if ((sPomiar ?? "") == "PM1")
                return "PM₁";
            if ((sPomiar ?? "") == "PM25")
                return "PM₂₅";
            if ((sPomiar.Substring(0, 2) ?? "") == "PM")
                return sPomiar;   // inny jakis PM :)

            return sPomiar.Substring(0, 1) + sPomiar.Substring(1).ToLower();
        }

        private string Unit4Pomiar(string sPomiar)
        {
            if ((sPomiar.Substring(0, 2) ?? "") == "PM")
                return " μg/m³";

            switch (sPomiar)
            {
                case "PRESSURE":
                        return " hPa";
                case "HUMIDITY":
                        return " %";
                case "TEMPERATURE":
                        return " °C";
            }
            return "";
        }


        private async System.Threading.Tasks.Task GetPomiary(JedenPomiar oTemplate, bool bInTimer)
        {
            // do moListaPomiarow dodaj wszystkie pomiary robione przez oTemplate.sId

            string sCmd;
            string sErr="";
            sCmd = "v2/measurements/installation?installationId=" + oTemplate.sId;
            string sPage = await GetREST(sCmd);
            if (sPage.Length < 10)
                return;

            bool bError = false;
            Newtonsoft.Json.Linq.JObject oJson = null;
            try
            {
                oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage);
            }
            catch (Exception ex)
            {
                bError = true;
                sErr = ex.Message;
            }
            if (bError)
            {
                if (!bInTimer)
                    await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting measurements (Airly)\n" + sErr);
                return ;
            }

            Newtonsoft.Json.Linq.JToken oJsonCurrent;
            try
            {
                oJsonCurrent = oJson.GetNamedToken("current");
                oTemplate.sTimeStamp = oJsonCurrent.GetNamedString("fromDateTime");

                Newtonsoft.Json.Linq.JArray oJsonValues;

                oJsonValues = oJsonCurrent.GetNamedArray("values");

                foreach (Newtonsoft.Json.Linq.JObject oJsonMeasurement in oJsonValues)
                {
                    var oNew = new JedenPomiar()
                    {
                        sSource = oTemplate.sSource,
                        sId = oTemplate.sId,
                        dLon = oTemplate.dLon,
                        dLat = oTemplate.dLat,
                        dWysok = oTemplate.dWysok,
                        dOdl = oTemplate.dOdl,
                        sOdl = Odleglosc2String(oTemplate.dOdl),
                        sSensorDescr = oTemplate.sSensorDescr,
                        sAdres = oTemplate.sAdres,
                        sTimeStamp = oTemplate.sTimeStamp
                    };

                    oNew.sPomiar = oJsonMeasurement.GetNamedString("name");
                    oNew.dCurrValue = oJsonMeasurement.GetNamedNumber("value");
                    oNew.sUnit = Unit4Pomiar(oNew.sPomiar);
                    if ((oNew.sPomiar ?? "") == "HUMIDITY" || (oNew.sPomiar ?? "") == "PRESSURE")
                    {
                        int iInt = (int)oNew.dCurrValue;
                        oNew.sCurrValue = iInt.ToString();
                    }
                    else
                        oNew.sCurrValue = oNew.dCurrValue.ToString();
                    if (oNew.sCurrValue.Length > 5)
                        oNew.sCurrValue = oNew.sCurrValue.Substring(0, 5);
                    oNew.sCurrValue = oNew.sCurrValue + oNew.sUnit;

                    oNew.sPomiar = NormalizePomiarName(oNew.sPomiar);
                    AddPomiar(oNew);
                }
            }
            catch 
            {

            }

            
        }

        // bardzo podobnie powinna dzialac funkcja sprawdzania pomiarow z favourite, ale nie z GPS a z listy punktow? 
        // Albo zawsze w ten sposob, wedle lokalizacji?
        // tylko wtedy moze nie 5 stacji, tylko mniej?
        // 1000 requests / day = 40 / hr
        // 50 requests / min
        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            double dMaxOdl = 10;
            string sErr = "";

            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceAirly", SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            if (p.k.GetSettingsString("sourceAirly_apikey").Length < 8)
                return moListaPomiarow;

            string sCmd;
            sCmd = "v2/installations/nearest?lat=" + oPos.Latitude.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "&lng=" + oPos.Longitude.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "&maxDistanceKM=" + dMaxOdl.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "&maxResults=5";
            string sPage = await GetREST(sCmd);
            if (sPage.Length < 10)
                return moListaPomiarow;


            bool bError = false;
            Newtonsoft.Json.Linq.JArray oJson = null;
            try
            {
                oJson = Newtonsoft.Json.Linq.JArray.Parse(sPage);
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                bError = true;
            }
            if (bError)
            {
                await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting nearest (Airly)\n" + sErr);
                return moListaPomiarow;
            }


            try
            {
                if (oJson.Count == 0)
                    return moListaPomiarow;     // brak bliskich?
            }
            catch 
            {
                return moListaPomiarow;
            }// ale jesli cos jest nie tak z oJson, to tez wracaj pusto


            // przeiteruj, tworząc JedenPomiar dla kazdego pomiaru i kazdej lokalizacji
            try
            {
                foreach (Newtonsoft.Json.Linq.JObject oJsonSensor in oJson)
                {

                    // tylko Airly nas interesuje, inne pomijamy (GIOS mamy po swojemu)
                    if (!(bool)oJsonSensor["airly"])
                        continue;

                    var oTemplate = new JedenPomiar();
                    oTemplate.sSource = "airly";
                    oTemplate.sId = oJsonSensor.GetNamedString("id");

                    Newtonsoft.Json.Linq.JToken oJsonPoint;
                    oJsonPoint = oJsonSensor.GetNamedToken("location");
                    oTemplate.dLon = oJsonPoint.GetNamedNumber("longitude");
                    oTemplate.dLat = oJsonPoint.GetNamedNumber("latitude");
                    oTemplate.dWysok = oJsonSensor.GetNamedNumber("elevation",0);
                    oTemplate.dOdl = (int)oPos.DistanceTo(oTemplate.dLat, oTemplate.dLon);

                    Newtonsoft.Json.Linq.JToken oJsonSponsor;
                    oJsonSponsor = oJsonSensor.GetNamedToken("sponsor");
                    oTemplate.sSensorDescr = oJsonSponsor.GetNamedString("name", "");

                    Newtonsoft.Json.Linq.JToken oJsonAdres;
                    oJsonAdres = oJsonSensor.GetObject().GetNamedToken("address");
                    oTemplate.sAdres = oJsonAdres.GetObject().GetNamedString("city", "") + ", " + oJsonAdres.GetObject().GetNamedString("street", "") + " " + oJsonAdres.GetObject().GetNamedString("number", "");


                    // ' oPomiar.sPomiar As String   ' jaki pomiar (np. PM10)
                    // ' oPomiar.sCurrValue As String ' etap 2: wartosc
                    // ' oPomiar.sTimeStamp As String ' etap 2: kiedy
                    // ' oPomiar.sLogoUri As String   ' logo, np. Airly etc., ktore warto pokazywac

                    await GetPomiary(oTemplate, false);
                }
            }
            catch 
            {
            }

            return moListaPomiarow;
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceAirly", SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            if (p.k.GetSettingsString(SRC_SETTING_NAME + "_apikey").Length < 8)
            {
                if (!bInTimer)
                    await p.k.DialogBoxAsync("ERROR: bad API key?");
                return moListaPomiarow;
            }


            // przeiteruj, tworząc JedenPomiar dla kazdego pomiaru i kazdej lokalizacji
            JedenPomiar oTemplate;// = new JedenPomiar();

            // wczytaj dane template dla danego favname
            var oFile = await EnviroStatus.App.GetDataFile(false, "airly_" + sId + ".xml", false);
            if (oFile != null)
            {
                var oSer = new System.Xml.Serialization.XmlSerializer(typeof(JedenPomiar));
                var oStream = await oFile.OpenStreamForReadAsync();
                oTemplate = oSer.Deserialize(oStream) as JedenPomiar;
                oStream.Dispose();   // == fclose
            }
            else
                oTemplate = new JedenPomiar();

            oTemplate.sSource = "airly";  // to tak na wszelki wypadek
            oTemplate.sId = sId;

            await GetPomiary(oTemplate, bInTimer);

            return moListaPomiarow;
        }
    }
}

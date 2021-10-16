using Windows.Foundation;
//using System.Threading.Tasks;
using System.IO;
//using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public class Source_GIOS : Source_Base
    {
        protected override string SRC_SETTING_NAME { get;  } = "sourceGIOS";
        protected override string SRC_SETTING_HEADER { get;  } = "GIOŚ";
        protected override string SRC_RESTURI_BASE { get;  } = "http://api.gios.gov.pl/pjp-api/rest/";
        public override string SRC_POMIAR_SOURCE { get;  } = "gios";
        protected override bool SRC_HAS_TEMPLATES { get;  } = true;
        public override bool SRC_IN_TIMER { get;  } = true;
        protected override string SRC_URI_ABOUT_EN { get; } = "http://powietrze.gios.gov.pl/pjp/current";
        protected override string SRC_URI_ABOUT_PL { get; } = "http://powietrze.gios.gov.pl/pjp/current";

        // API: http://powietrze.gios.gov.pl/pjp/content/api
        // LIMIT: 2x na godzinę

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
            switch (sPomiar)
            {
                case "C6H6":
                        return "C₆H₆";
                case "SO2":
                        return "SO₂";
                case "NO2":
                        return "NO₂";
                case "O3":
                        return "O₃";
                case "PM10":
                        return "PM₁₀";
                case "PM1":
                        return "PM₁";
                case "PM2.5":
                        return "PM₂₅";
            }
            return sPomiar;
        }

        private string Unit4Pomiar(string sPomiar)
        {
            return " μg/m³";
        }


        private async System.Threading.Tasks.Task GetPomiary(JedenPomiar oTemplate, bool bInTimer)
        {
            // do moListaPomiarow dodaj wszystkie pomiary robione przez oTemplate.sId

            string sCmd;
            sCmd = "station/sensors/" + oTemplate.sId;
            string sPage = await GetREST(sCmd);
            if (sPage.Length < 10)
                return;

            bool bError = false;
            string sErr = "";
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
                if (!bInTimer)
                    await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting sensor data (" + SRC_POMIAR_SOURCE + ")\n " + sErr);
                return;
            }

            try
            {
                foreach (Newtonsoft.Json.Linq.JToken oJsonMeasurement in oJson)
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
                        sAdres = oTemplate.sAdres
                    };
                    // .sTimeStamp = oTemplate.sTimeStamp

                    oNew.sAddit = oJsonMeasurement.GetObject().GetNamedNumber("id").ToString();

                    Newtonsoft.Json.Linq.JToken oJsonVal;
                    oJsonVal = oJsonMeasurement.GetObject().GetNamedToken("param");

                    oNew.sPomiar = oJsonVal.GetObject().GetNamedString("paramCode");
                    AddPomiar(oNew);
                }
            }
            catch 
            {
            }

        }

        private async System.Threading.Tasks.Task GetWartosci()
        {
            try
            {
                foreach (EnviroStatus.JedenPomiar oItem in moListaPomiarow)
                {
                    if (oItem.bDel)
                        continue;

                    string sCmd;
                    sCmd = "data/getData/" + oItem.sAddit;
                    string sPage = await GetREST(sCmd);
                    if (sPage.Length < 10)
                        return;

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
                        await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting values (" + SRC_POMIAR_SOURCE + ")\n " + sErr);
                        return;
                    }

                    Newtonsoft.Json.Linq.JArray oJsonArr;
                    oJsonArr = oJson.GetObject().GetNamedArray("values");

                    foreach (Newtonsoft.Json.Linq.JToken oJsonMeasurement in oJsonArr)
                    {
                        oItem.sTimeStamp = oJsonMeasurement.GetObject().GetNamedString("date");
                        oItem.sCurrValue = "";
                        try
                        {
                            Newtonsoft.Json.Linq.JToken oVal;
                            oVal = oJsonMeasurement.GetObject().GetNamedToken("value");
                            if (oVal.Type != Newtonsoft.Json.Linq.JTokenType.Null )
                                oItem.sCurrValue = oJsonMeasurement.GetObject().GetNamedNumber("value").ToString();
                        }
                        catch 
                        {
                        }
                        if (!string.IsNullOrEmpty(oItem.sCurrValue))
                        {
                            oItem.dCurrValue = oItem.sCurrValue.ParseDouble(0);
                            // oItem.sCurrValue = Conversions.ToString(oItem.dCurrValue);
                            oItem.sUnit = Unit4Pomiar(oItem.sPomiar);
                            if (oItem.sCurrValue.Length > 5)
                                oItem.sCurrValue = oItem.sCurrValue.Substring(0, 5);
                            oItem.sCurrValue = oItem.sCurrValue + oItem.sUnit;
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(oItem.sCurrValue))
                    {
                        oItem.bDel = true;
                        oItem.dCurrValue = 0;
                    }
                    else
                        oItem.sPomiar = NormalizePomiarName(oItem.sPomiar);
                }
            }
            catch 
            {
            }


        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            // ale w efekcie jest kilka GIOSów jednego parametru
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceGIOS", SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            var oTemplate = new JedenPomiar();

            // wczytaj dane template dla danego favname
            var oFile = await EnviroStatus.App.GetDataFile(false, "gios_" + sId + ".xml", false);
            if (oFile != null)
            {
                var oSer = new System.Xml.Serialization.XmlSerializer(typeof(JedenPomiar));
                var oStream = await oFile.OpenStreamForReadAsync();
                oTemplate = oSer.Deserialize(oStream) as JedenPomiar;
                oStream.Dispose();   // == fclose
            }
            else
                oTemplate = new JedenPomiar();

            oTemplate.sSource = SRC_POMIAR_SOURCE;  // to tak na wszelki wypadek
            oTemplate.sId = sId;
            // oTemplate.dLon = oJsonObj.GetNamedstring("gegrLon")
            // oTemplate.dLat = oJsonObj.GetNamedstring("gegrLat")
            // oTemplate.dWysok = 0    ' brak danych
            // oTemplate.dOdl = App.GPSdistanceDwa(oPos.X, oPos.Y,
            // oTemplate.dLat, oTemplate.dLon)
            // oTemplate.sOdl = oTemplate.dOdl & " m"
            // oTemplate.sSensorDescr = oJsonObj.GetNamedString("stationName")
            // oTemplate.sAdres = oJsonObj.GetNamedString("addressStreet")


            await GetPomiary(oTemplate, bInTimer);

            // teraz odczytaj wartosci!
            await GetWartosci();

            return moListaPomiarow;
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceGIOS", SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            if (!oPos.IsInsidePoland())
                return moListaPomiarow;

            double dMaxOdl = 10;

            string sCmd;
            sCmd = "station/findAll";
            string sPage = await GetREST(sCmd);
            if (sPage.Length < 10)
                return moListaPomiarow;

            bool bError = false;
            string sErr = "";
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
                await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting nearest (" + SRC_POMIAR_SOURCE + ")\n " + sErr);
                return moListaPomiarow;
            }

            try
            {
                if (oJson.Count == 0)
                    return moListaPomiarow;     // brak bliskich?


                // przeiteruj, tworząc JedenPomiar dla kazdego pomiaru i kazdej lokalizacji

                foreach (Newtonsoft.Json.Linq.JToken oJsonSensor in oJson)
                {
                    Newtonsoft.Json.Linq.JToken oJsonObj;
                    oJsonObj = oJsonSensor.GetObject();

                    var oTemplate = new JedenPomiar();
                    oTemplate.sSource = SRC_POMIAR_SOURCE;
                    oTemplate.sId = oJsonObj.GetNamedNumber("id").ToString();

                    oTemplate.dLon = oJsonObj.GetNamedString("gegrLon").ParseDouble(0);
                    oTemplate.dLat = oJsonObj.GetNamedString("gegrLat").ParseDouble(0);

                    oTemplate.dWysok = 0;    // brak danych

                    oTemplate.dOdl = (int)oPos.DistanceTo(oTemplate.dLat, oTemplate.dLon);

                    if (oTemplate.dOdl / 1000 < dMaxOdl)
                    {
                        // teraz cos, co chce dodac

                        oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl);

                        oTemplate.sSensorDescr = oJsonObj.GetNamedString("stationName");

                        oTemplate.sAdres = oJsonObj.GetNamedString("addressStreet");
                        if (string.IsNullOrEmpty(oTemplate.sAdres))
                        {
                            try
                            {
                                Newtonsoft.Json.Linq.JToken oJsonAdres;
                                oJsonAdres = oJsonObj.GetNamedToken("city");
                                oTemplate.sAdres = oJsonAdres.GetNamedString("name");
                                Newtonsoft.Json.Linq.JToken oJsonComm;
                                oJsonComm = oJsonAdres.GetNamedToken("commune").GetObject();
                                oTemplate.sAdres = oTemplate.sAdres + "\n(";
                                if (!string.IsNullOrEmpty(oJsonComm.GetNamedString("communeName")))
                                    oTemplate.sAdres = oTemplate.sAdres + "gmina " + oJsonComm.GetNamedString("communeName") + "\n";
                                if (!string.IsNullOrEmpty(oJsonComm.GetNamedString("districtName")))
                                    oTemplate.sAdres = oTemplate.sAdres + "powiat " + oJsonComm.GetNamedString("districtName") + "\n";
                                if (!string.IsNullOrEmpty(oJsonComm.GetNamedString("provinceName")))
                                    oTemplate.sAdres = oTemplate.sAdres + oJsonComm.GetNamedString("provinceName") + "\n";
                            }
                            catch 
                            {
                            }
                        }

                        await GetPomiary(oTemplate, false);
                    }
                }

                // teraz odczytaj wartosci!
                await GetWartosci();
            }
            catch 
            {
            }

            return moListaPomiarow;
        }
    }
}

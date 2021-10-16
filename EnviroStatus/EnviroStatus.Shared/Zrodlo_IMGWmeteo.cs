// 2019.10.30 uwzględniona inna postac oJsonSensor jako "null" 

using Windows.Foundation;
//using System.Threading.Tasks;
using Windows.UI.Xaml;
using System.IO;
//using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System;
using Windows.UI.Xaml.Controls;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public class Source_IMGWmeteo : Source_Base
    {
        protected override string SRC_SETTING_NAME { get;  } = "sourceImgwMeteo";
        protected override string SRC_SETTING_HEADER { get;  } = "IMGW meteo";
        protected override string SRC_RESTURI_BASE { get; } = "http://hydro.imgw.pl/"; // http://monitor.pogodynka.pl/";
        public override string SRC_POMIAR_SOURCE { get;  } = "IMGWmet";
        protected override bool SRC_HAS_TEMPLATES { get; } = true;
        protected override string SRC_URI_ABOUT_EN { get; } = "https://hydro.imgw.pl/";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://hydro.imgw.pl/";

        public override void ReadResStrings()
        {
            p.k.SetSettingsString("resPomiarWind", p.k.GetLangString("resPomiarWind"));
            p.k.SetSettingsString("resPomiarOpad", p.k.GetLangString("resPomiarOpad"));
        }


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

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            double dMaxOdl = 10;

            Collection<EnviroStatus.JedenPomiar> oListaPomiarow;
            oListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceImgwMeteo", SRC_DEFAULT_ENABLE))
                return oListaPomiarow;

            if (!oPos.IsInsidePoland())
                return moListaPomiarow;

            string sPage = await GetREST("api/map/?category=meteo");
            // [ {"pd":"2019-02-25T10:00:00Z","pv":0.0,"i":"250180590","n":"RYBNIK-STODOŁY","a":10,"s":"no-precip","lo":18.483055555555556,"la":50.154444444444444} ...]
            if (sPage.Length < 10)
                return moListaPomiarow;

            bool bError = false;
            string sErr = "";

            Newtonsoft.Json.Linq.JArray oJson = null;

            if (string.IsNullOrEmpty(sPage))
                bError = true;
            else
            {
                try
                {
                    oJson = Newtonsoft.Json.Linq.JArray.Parse(sPage);
                }
                catch (Exception ex)
                {
                    sErr = ex.Message;
                    bError = true;
                }
            }

            if (bError)
            {
                await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting nearest (" + SRC_POMIAR_SOURCE + ")\n " + sErr);
                return oListaPomiarow;
            }

            if (oJson.Count < 1)
                return oListaPomiarow;

            double dMinOdl = 1000000;
            double dMinOdlAdd = 1000000;

            foreach (Newtonsoft.Json.Linq.JToken oJsonSensor in oJson)
            {
                // {"cd":"2019-02-25T10:40:00Z","cv":187,"i":"150190340","n":"KRAKÓW-BIELANY","a":1,"s":"normal","lo":19.843333333333334,"la":50.040277777777774} ...]

                var oTemplate = new JedenPomiar();
                oTemplate.sSource = "IMGWmet";
                oTemplate.sPomiar = "Meteo";
                oTemplate.sId = oJsonSensor.GetObject().GetNamedString("i");
                oTemplate.dLon = oJsonSensor.GetObject().GetNamedNumber("lo");
                oTemplate.dLat = oJsonSensor.GetObject().GetNamedNumber("la");

                oTemplate.dOdl = (int)oPos.DistanceTo(oTemplate.dLat, oTemplate.dLon);
                dMinOdl = Math.Min(dMinOdl, oTemplate.dOdl);
                if (oTemplate.dOdl > dMaxOdl * 1000)
                    continue;

                // jesli do dalszego sensora, to nie chcemy go - potem i tak bedzie usuwanie dalszych
                if (oTemplate.dOdl > dMinOdlAdd)
                    continue;
                dMinOdlAdd = oTemplate.dOdl;

                oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl);
                oTemplate.sAdres = EnviroStatus.App.String2SentenceCase(oJsonSensor.GetObject().GetNamedString("n"));

                oListaPomiarow.Add(oTemplate);
            }

            if (oListaPomiarow.Count < 1)
            {
                await p.k.DialogBoxAsync(@"ERROR: data parsing error IMGWmeteo\sPage\0");
                return oListaPomiarow;
            }

            // znajdz najblizszy, reszte zrob del
            // dMinOdlAdd to najblizszy wstawiony, ale i tak policzymy sobie
            dMinOdlAdd = 100000;
            foreach (EnviroStatus.JedenPomiar oItem in oListaPomiarow)
                dMinOdlAdd = Math.Min(dMinOdlAdd, oItem.dOdl);
            // a teraz usuwamy
            foreach (EnviroStatus.JedenPomiar oItem in oListaPomiarow)
            {
                if (oItem.dOdl > dMinOdlAdd)
                    oItem.bDel = true;
            }

            // dodaj pomiary
            moListaPomiarow = new Collection<JedenPomiar>();
            foreach (EnviroStatus.JedenPomiar oItem in oListaPomiarow)
            {
                if (!oItem.bDel)
                    return await GetDataFromSensor(oItem, false);
            }

            return oListaPomiarow;
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceImgwMeteo", SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            var oTemplate = new JedenPomiar();

            // wczytaj dane template dla danego favname
            var oFile = await EnviroStatus.App.GetDataFile(false, "IMGWmeteo_" + sId + ".xml", false);
            if (oFile != null)
            {
                var oSer = new System.Xml.Serialization.XmlSerializer(typeof(JedenPomiar));
                var oStream = await oFile.OpenStreamForReadAsync();
                oTemplate = oSer.Deserialize(oStream) as JedenPomiar;
                oStream.Dispose();   // == fclose
            }
            else
                oTemplate = new JedenPomiar();

            oTemplate.sId = sId;    // wychodzi na to ze nie ma template zapisanego?
            return await GetDataFromSensor(oTemplate, bInTimer);
        }

        private async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromSensor(JedenPomiar oTemplate, bool bInTimer)
        {
            // dodaj pomiary:
            // opady / precip (tenMinutesPrecipRecords LUB hourlyPrecipRecords) ' {"date":"2019-02-25T10:00:00Z","value":0.0,"dreId":9234,"operationId":"SUM10MIN_OPAD_TELEMETRYCZNY","parameterId":"B00608A","versionId":-1,"id":2603646704200} , wedle sourceImgwMeteo10min
            // temp (temperatureAutoRecords)         {"date":"2019-02-23T10:00:00Z","value":-1.36,"dreId":9234,"operationId":"250190470_B00302A","parameterId":"B00302A","versionId":-1,"id":2601427195700}
            // szybk wiatru (windVelocityTelRecords) {"date":"2019-02-18T10:00:00Z","value":0.30}
            // IF szybk>0 kier wiatru (windDirectionTelRecords) {"date":"2019-02-18T10:00:00Z","value":27.0}

            string sPage = await GetREST("api/station/meteo/?id=" + oTemplate.sId);
            if (sPage.Length < 10)
                return moListaPomiarow;

            bool bError = false;
            string sErr = "";

            Newtonsoft.Json.Linq.JObject oJsonSensor = null;
            try
            {
                oJsonSensor = Newtonsoft.Json.Linq.JObject.Parse(sPage);
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                bError = true;
            }
            if (bError || oJsonSensor == null || (oJsonSensor.ToString() ?? "") == "null")
            {
                if (!bInTimer)
                    await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting sensor data (" + SRC_POMIAR_SOURCE + ")\n " + sErr);
                return moListaPomiarow;
            }


            oTemplate.sSource = "IMGWmet";
            oTemplate.sId = oJsonSensor.GetObject().GetNamedString("id");    // chociaż jest w template...
                                                                             // odczytane z template
                                                                             // oTemplate.dLon = oJsonSensor.GetObject.GetNamedNumber("lo")
                                                                             // oTemplate.dLat = oJsonSensor.GetObject.GetNamedNumber("la")

            // oTemplate.dOdl = App.GPSdistanceDwa(oPos.X, oPos.Y, oTemplate.dLat, oTemplate.dLon)
            // dMinOdl = Math.Min(dMinOdl, oTemplate.dOdl)
            // If oTemplate.dOdl > dMaxOdl * 1000 Then Continue For
            // oTemplate.sOdl = oTemplate.dOdl & " m"

            Newtonsoft.Json.Linq.JToken oJsonValStatus;
            oJsonValStatus = oJsonSensor.GetObject().GetNamedToken("status");

            oTemplate.sAdres = EnviroStatus.App.String2SentenceCase(oJsonSensor.GetObject().GetNamedString("name"));

            // opady / precip (tenMinutesPrecipRecords LUB hourlyPrecipRecords) ' {"date":"2019-02-25T10:00:00Z","value":0.0,"dreId":9234,"operationId":"SUM10MIN_OPAD_TELEMETRYCZNY","parameterId":"B00608A","versionId":-1,"id":2603646704200} , wedle sourceImgwMeteo10min
            try        // dodajemy opad
            {
                var oNew = new JedenPomiar();
                oNew.sSource = oTemplate.sSource; // = "IMGWmet"
                oNew.sId = oTemplate.sId;
                oNew.dLon = oTemplate.dLon;
                oNew.dLat = oTemplate.dLat;
                oNew.dOdl = oTemplate.dOdl;
                oNew.sOdl = oTemplate.sOdl;
                oNew.sPomiar = p.k.GetSettingsString("resPomiarOpad"); // GetLangString("resPomiarOpad")
                oNew.sUnit = " cm";
                oNew.sAdres = oTemplate.sAdres;

                Newtonsoft.Json.Linq.JArray oJsonArr;
                if (p.k.GetSettingsBool("sourceImgwMeteo10min", true))
                    oJsonArr = oJsonSensor.GetObject().GetNamedArray("tenMinutesPrecipRecords");
                else
                    oJsonArr = oJsonSensor.GetObject().GetNamedArray("hourlyPrecipRecords");

                if (oJsonArr.Count > 2)
                {
                    Newtonsoft.Json.Linq.JToken oJsonVal;
                    oJsonVal = oJsonArr[oJsonArr.Count - 1];
                    oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                    // tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                    oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value");
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;
                    if (oNew.dCurrValue > 0)
                        oNew.sAlert = "!";

                    try
                    {
                        oJsonVal = oJsonArr[oJsonArr.Count - 2];
                        oNew.sAddit = "Poprzednio " + oJsonVal.GetObject().GetNamedNumber("value").ToString() + oNew.sUnit;
                        string sPrevDate = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                        oNew.sAddit = oNew.sAddit + " @ " + EnviroStatus.App.ShortPrevDate(oNew.sTimeStamp, sPrevDate);
                    }
                    catch 
                    {
                    }
                    moListaPomiarow.Add(oNew);
                }
            }
            catch 
            {
            }

            // temp (temperatureAutoRecords)         {"date":"2019-02-23T10:00:00Z","value":-1.36,"dreId":9234,"operationId":"250190470_B00302A","parameterId":"B00302A","versionId":-1,"id":2601427195700}
            try
            {
                var oNew = new JedenPomiar();
                oNew.sSource = oTemplate.sSource; // = "IMGWmet"
                oNew.sId = oTemplate.sId;
                oNew.dLon = oTemplate.dLon;
                oNew.dLat = oTemplate.dLat;
                oNew.dOdl = oTemplate.dOdl;
                oNew.sOdl = oTemplate.sOdl;
                oNew.sPomiar = "Temp";
                oNew.sUnit = " °C";
                oNew.sAdres = oTemplate.sAdres;

                Newtonsoft.Json.Linq.JArray oJsonArr;
                oJsonArr = oJsonSensor.GetObject().GetNamedArray("temperatureAutoRecords");

                if (oJsonArr.Count > 2)
                {
                    Newtonsoft.Json.Linq.JToken oJsonVal;
                    oJsonVal = oJsonArr[oJsonArr.Count - 1];
                    oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                    // tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                    oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value");
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;

                    try
                    {
                        oJsonVal = oJsonArr[oJsonArr.Count - 2];
                        oNew.sAddit = "Poprzednio " + oJsonVal.GetObject().GetNamedNumber("value").ToString() + oNew.sUnit;
                        string sPrevDate = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                        oNew.sAddit = oNew.sAddit + " @ " + EnviroStatus.App.ShortPrevDate(oNew.sTimeStamp, sPrevDate);
                    }
                    catch 
                    {
                    }
                    moListaPomiarow.Add(oNew);
                }
            }
            catch 
            {
            }

            // szybk wiatru (windVelocityTelRecords) {"date":"2019-02-18T10:00:00Z","value":0.30}
            try        // dodajemy opad
            {
                var oNew = new JedenPomiar();
                oNew.sSource = oTemplate.sSource; // = "IMGWmet"
                oNew.sId = oTemplate.sId;
                oNew.dLon = oTemplate.dLon;
                oNew.dLat = oTemplate.dLat;
                oNew.dOdl = oTemplate.dOdl;
                oNew.sOdl = oTemplate.sOdl;
                oNew.sPomiar = p.k.GetSettingsString("resPomiarWind"); // GetLangString("resPomiarWind")
                oNew.sUnit = " m/s";
                oNew.sAdres = oTemplate.sAdres;

                Newtonsoft.Json.Linq.JArray oJsonArr;
                oJsonArr = oJsonSensor.GetObject().GetNamedArray("windVelocityTelRecords");

                if (oJsonArr.Count > 2)
                {
                    Newtonsoft.Json.Linq.JToken oJsonVal;
                    oJsonVal = oJsonArr[oJsonArr.Count - 1];
                    oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                    // tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                    oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value");
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;

                    oNew.sAddit = "= " + (oNew.dCurrValue * 3.6).ToString() + " km/h";
                    try
                    {
                        oJsonVal = oJsonArr[oJsonArr.Count - 2];
                        oNew.sAddit = oNew.sAddit + "\nPoprzednio " + oJsonVal.GetObject().GetNamedNumber("value").ToString() + oNew.sUnit;
                        string sPrevDate = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                        oNew.sAddit = oNew.sAddit + " @ " + EnviroStatus.App.ShortPrevDate(oNew.sTimeStamp, sPrevDate);
                    }
                    catch 
                    {
                    }

                    oJsonArr = oJsonSensor.GetObject().GetNamedArray("windDirectionTelRecords");
                    if (oJsonArr.Count > 2)
                    {
                        oJsonVal = oJsonArr[oJsonArr.Count - 1];
                        oNew.sAddit = oNew.sAddit + "\nKierunek: " + oJsonVal.GetObject().GetNamedNumber("value").ToString() + "°";
                    }

                    moListaPomiarow.Add(oNew);
                }
            }
            catch 
            {
            }

            try        // dodajemy opad
            {
                var oNew = new JedenPomiar();
                oNew.sSource = oTemplate.sSource; // = "IMGWmet"
                oNew.sId = oTemplate.sId;
                oNew.dLon = oTemplate.dLon;
                oNew.dLat = oTemplate.dLat;
                oNew.dOdl = oTemplate.dOdl;
                oNew.sOdl = oTemplate.sOdl;
                oNew.sPomiar = p.k.GetSettingsString("resPomiarWind") + " max"; // GetLangString("resPomiarWind")
                oNew.sUnit = " m/s";
                oNew.sAdres = oTemplate.sAdres;

                Newtonsoft.Json.Linq.JArray oJsonArr;
                oJsonArr = oJsonSensor.GetObject().GetNamedArray("windMaxVelocityRecords");

                if (oJsonArr.Count > 2)
                {
                    Newtonsoft.Json.Linq.JToken oJsonVal;
                    oJsonVal = oJsonArr[oJsonArr.Count - 1];
                    oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                    // tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                    oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value");
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;

                    oNew.sAddit = "= " + (oNew.dCurrValue * 3.6).ToString() + " km/h";
                    try
                    {
                        oJsonVal = oJsonArr[oJsonArr.Count - 2];
                        oNew.sAddit = oNew.sAddit + "\nPoprzednio " + oJsonVal.GetObject().GetNamedNumber("value").ToString() + oNew.sUnit;
                        string sPrevDate = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                        oNew.sAddit = oNew.sAddit + " @ " + EnviroStatus.App.ShortPrevDate(oNew.sTimeStamp, sPrevDate);
                    }
                    catch 
                    {
                    }

                    // oJsonArr = oJsonSensor.GetObject.GetNamedArray("windDirectionTelRecords")
                    // If oJsonArr.Count > 2 Then
                    // oJsonVal = oJsonArr.Item(oJsonArr.Count - 1)
                    // oNew.sAddit = oNew.sAddit & vbCrLf & "Kierunek: " & oJsonVal.GetObject.GetNamedNumber("value") & "°"
                    // End If

                    moListaPomiarow.Add(oNew);
                }
            }
            catch 
            {
            }



            return moListaPomiarow;
        }




        public override void ConfigCreate(StackPanel oStack)
        {
            base.ConfigCreate(oStack);

            Windows.UI.Xaml.Data.Binding oBind = new Windows.UI.Xaml.Data.Binding();
            oBind.ElementName = "uiConfig_" + SRC_SETTING_NAME;
            oBind.Path = new Windows.UI.Xaml.PropertyPath("IsOn");

            var oTS = new ToggleSwitch();

            oTS = new ToggleSwitch();
            oTS.Name = "uiConfig_ImgwMeteo10MIN";
            oTS.IsOn = p.k.GetSettingsBool("sourceImgwMeteo10min", true);
            oTS.OnContent = p.k.GetLangString("resImgwMeteo10minON");
            oTS.OffContent = p.k.GetLangString("resImgwMeteo10minOFF");
            oTS.SetBinding(Windows.UI.Xaml.Controls.ToggleSwitch.IsEnabledProperty, oBind);

            oStack.Children.Add(oTS);
        }

        public override void ConfigRead(StackPanel oStack)
        {
            base.ConfigRead(oStack);

            foreach (UIElement oItem in oStack.Children)
            {
                ToggleSwitch oTS;
                oTS = oItem as ToggleSwitch;
                if (oTS != null)
                {
                    // If oTS.Name = "uiConfig_ImgwMeteo" Then SetSettingsBool("sourceImgwMeteo", oTS.IsOn)
                    if ((oTS.Name ?? "") == "uiConfig_ImgwMeteo10MIN")
                        p.k.SetSettingsBool("sourceImgwMeteo10min", oTS.IsOn);
                }
            }
        }
    }
}

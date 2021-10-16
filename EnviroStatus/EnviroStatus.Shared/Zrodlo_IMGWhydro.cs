
using Windows.Foundation;
//using System.Threading.Tasks;
using Windows.UI.Xaml;
using System.IO;
//using Microsoft.VisualBasic;
using System.Linq;
using System.Collections.ObjectModel;
using System;
using Windows.UI.Xaml.Controls;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public class Source_IMGWhydro : Source_Base
    {
        protected override string SRC_SETTING_NAME { get;  } = "sourceImgwHydro";
        protected override string SRC_SETTING_HEADER { get;  } = "IMGW hydro";
        protected override string SRC_RESTURI_BASE { get;  } = "http://hydro.imgw.pl/"; // http://monitor.pogodynka.pl/";
        public override string SRC_POMIAR_SOURCE { get;  } = "IMGWhyd";
        protected override bool SRC_HAS_TEMPLATES { get;  } = true;
        public override bool SRC_IN_TIMER { get; } = true;
        public override bool SRC_NO_COMPARE { get; } = true;
        protected override string SRC_URI_ABOUT_EN { get; } = "https://hydro.imgw.pl/";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://hydro.imgw.pl/";

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
            return "Hydro";
        }

        private string Unit4Pomiar(string sPomiar)
        {
            return "cm";
        }


        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            double dMaxOdl = 25;


            Collection<EnviroStatus.JedenPomiar> oListaPomiarow;
            oListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceImgwHydro", SRC_DEFAULT_ENABLE))
                return oListaPomiarow;

            if (!oPos.IsInsidePoland())
                return oListaPomiarow;

            string sPage = await GetREST("api/map/?category=hydro");
            // [ {"cd":"2019-02-25T10:40:00Z","cv":187,"i":"150190340","n":"KRAKÓW-BIELANY","a":1,"s":"normal","lo":19.843333333333334,"la":50.040277777777774} ...]
            if (sPage.Length < 10)
                return oListaPomiarow;

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
                oTemplate.sSource = SRC_POMIAR_SOURCE;
                oTemplate.sPomiar = "Hydro";
                oTemplate.sId = oJsonSensor.GetObject().GetNamedString("i");
                oTemplate.dLon = oJsonSensor.GetObject().GetNamedNumber("lo");
                oTemplate.dLat = oJsonSensor.GetObject().GetNamedNumber("la");

                oTemplate.dOdl = (int)oPos.DistanceTo(oTemplate.dLat, oTemplate.dLon);
                dMinOdl = Math.Min(dMinOdl, oTemplate.dOdl);
                if (oTemplate.dOdl > dMaxOdl * 1000)
                    continue;

                // jesli do dalszego sensora, to nie chcemy go - potem i tak bedzie usuwanie dalszych
                // If oTemplate.dOdl > dMinOdlAdd Then Continue For
                // dMinOdlAdd = oTemplate.dOdl

                oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl);
                oTemplate.sAdres = EnviroStatus.App.String2SentenceCase(oJsonSensor.GetObject().GetNamedString("n"));

                // oTemplate.sTimeStamp = oJsonSensor.GetObject.GetNamedString("cd")

                // Try
                // ' bo tam moze być NULL!
                // oTemplate.dCurrValue = oJsonSensor.GetObject.GetNamedNumber("cv")
                // Catch ex As Exception
                // oTemplate.dCurrValue = -1
                // End Try
                // If oTemplate.dCurrValue = -1 Then Continue For

                // oTemplate.sCurrValue = oTemplate.dCurrValue & " cm"

                // ' niewykorzystane: a:
                // oTemplate.sAddit = "Status: " & oJsonSensor.GetObject.GetNamedString("s")
                // Select Case oJsonSensor.GetObject.GetNamedString("s")
                // Case "low"
                // oTemplate.sAlert = "!"
                // Case "high"
                // oTemplate.sAlert = "!"
                // Case "warning"
                // oTemplate.sAlert = "!!"
                // Case "alarm"    ' choc nie wiem czy taki jest status
                // oTemplate.sAlert = "!!!"
                // Case "unknown"
                // Case "normal"

                // End Select
                // oTemplate.sUnit = " cm"

                oListaPomiarow.Add(oTemplate);
            }

            if (oListaPomiarow.Count < 1)
            {
                await p.k.DialogBoxAsync(@"ERROR: data parsing error IMGWhydro\sPage");
                return oListaPomiarow;
            }

            // znajdz najblizszy, reszte zrob del
            // dMinOdlAdd to najblizszy wstawiony, ale i tak policzymy sobie
            // For Each oItem As JedenPomiar In oListaPomiarow
            // 'If dMinOdlAdd > oItem.dOdl Then
            // '    dMinOdlAdd = oItem.dOdl
            // '    'sMinRzeka = 'ale tu jeszcze nie ma rzeki, niestety
            // 'End If
            // dMinOdlAdd = Math.Min(dMinOdlAdd, oItem.dOdl)
            // Next
            // ' a teraz usuwamy
            // For Each oItem As JedenPomiar In oListaPomiarow
            // If oItem.dOdl > dMinOdlAdd Then oItem.bDel = True
            // Next

            // dodaj pomiary
            moListaPomiarow = new Collection<JedenPomiar>();

            foreach (EnviroStatus.JedenPomiar oItem in oListaPomiarow)
            {
                if (!oItem.bDel)
                    moListaPomiarow.Concat(await GetDataFromSensor(oItem, false)); // Return Await GetDataFromSensor(oItem, False)
            }

            if (!p.k.GetSettingsBool("sourceImgwHydroAll"))
            {

                // sprawdz co jest najblizej
                dMinOdlAdd = 100000;
                string sMinRzeka = "";
                foreach (EnviroStatus.JedenPomiar oItem in moListaPomiarow)
                {
                    if (dMinOdlAdd > oItem.dOdl)
                    {
                        dMinOdlAdd = oItem.dOdl;
                        sMinRzeka = oItem.sPomiar;
                    }
                }

                // usun inne rzeki
                int iInd;
                iInd = sMinRzeka.IndexOf(" ");
                if (iInd > 0)
                    sMinRzeka = sMinRzeka.Substring(0, iInd);

                foreach (EnviroStatus.JedenPomiar oItem in moListaPomiarow)
                {
                    if (!oItem.sPomiar.StartsWith(sMinRzeka))
                        oItem.bDel = true;
                }
            }

            return moListaPomiarow;
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            moListaPomiarow= new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceImgwHydro", SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            var oTemplate = new JedenPomiar();

            // wczytaj dane template dla danego favname
            var oFile = await EnviroStatus.App.GetDataFile(false, "IMGWhyd_" + sId + ".xml", false);
            if (oFile != null)
            {
                var oSer = new System.Xml.Serialization.XmlSerializer(typeof(JedenPomiar));
                var oStream = await oFile.OpenStreamForReadAsync();
                oTemplate = oSer.Deserialize(oStream) as JedenPomiar;
                oStream.Dispose();   // == fclose
            }
            else
                oTemplate = new JedenPomiar();

            return await GetDataFromSensor(oTemplate, bInTimer);
        }

        private async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromSensor(JedenPomiar oTemplate, bool bInTimer)
        {
            // dodaj pomiary:
            // RZEKA cm, z alertami
            // RZEKA °C
            // RZEKA m³

            string sPage = await GetREST("api/station/hydro/?id=" + oTemplate.sId);
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
            if (bError)
            {
                if (!bInTimer)
                    await p.k.DialogBoxAsync("ERROR: JSON parsing error - getting sensor data (" + SRC_POMIAR_SOURCE + ")\n " + sErr);
                return moListaPomiarow;
            }

            oTemplate.sSource = "IMGWhyd";
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

            // sPomiar to nazwa rzeki
            int iInd;
            oTemplate.sPomiar = oJsonValStatus.GetObject().GetNamedString("river");
            iInd = oTemplate.sPomiar.LastIndexOf("(");
            if (iInd > 0)
                oTemplate.sPomiar = oTemplate.sPomiar.Substring(0, iInd).Trim();

            oTemplate.sAdres = App.String2SentenceCase(oJsonSensor.GetObject().GetNamedString("name"));


            try        // dodajemy pomiar w cm
            {
                var oNewCm = new JedenPomiar();
                oNewCm.sSource = oTemplate.sSource; // = "IMGWhyd"
                oNewCm.sId = oTemplate.sId;
                oNewCm.dLon = oTemplate.dLon;
                oNewCm.dLat = oTemplate.dLat;
                oNewCm.dOdl = oTemplate.dOdl;
                oNewCm.sOdl = oTemplate.sOdl;
                oNewCm.sPomiar = oTemplate.sPomiar + " cm";
                oNewCm.sUnit = " cm";
                oNewCm.sAdres = oTemplate.sAdres;

                // 2021.01.28 wreszcie poprawione, bo zmienili format; nie jest (current|previous)(Date|Value),
                // tylko date/value w (current|previous)State
                Newtonsoft.Json.Linq.JToken oJsonValCurrState;
                oJsonValCurrState = oJsonValStatus.GetObject().GetNamedToken("currentState");

                oNewCm.sTimeStamp = oJsonValCurrState?.GetObject().GetNamedString("date").Replace("T", " ");

                // tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                oNewCm.dCurrValue = oJsonValCurrState.GetObject().GetNamedNumber("value");
                oNewCm.sCurrValue = oNewCm.dCurrValue.ToString() + " cm";

                // niewykorzystane: a:
                oNewCm.sAddit = "Status: " + oJsonSensor.GetObject().GetNamedString("state");
                try
                {
                    // "currentDate":"2019-02-25T12:00:00Z",
                    // "previousDate":"2019-02-25T11:50:00Z"
                    oJsonValCurrState = oJsonValStatus.GetObject().GetNamedToken("previousState");

                    oNewCm.sAddit = oNewCm.sAddit + "\nPoprzednio " + oJsonValCurrState?.GetObject().GetNamedNumber("value").ToString();
                    // Dim iTmp As Integer
                    string sPrevDate = oJsonValCurrState?.GetObject().GetNamedString("date").Replace("T", " ");
                    oNewCm.sAddit = oNewCm.sAddit + " @ " + EnviroStatus.App.ShortPrevDate(oNewCm.sTimeStamp, sPrevDate);
                }
                catch 
                {
                }

                // Dim sTmp As String
                // Dim iInd As Integer
                // sTmp = oJsonValStatus.GetObject.GetNamedString("river")
                // iInd = sTmp.LastIndexOf("(")
                // If iInd > 0 Then sTmp = sTmp.Substring(0, iInd).Trim
                // oTemplate.sSensorDescr = sTmp & " (" & oJsonValStatus.GetObject.GetNamedNumber("riverCourseKm") & " km)"

                // z Try, jakby ktores bylo null
                double dAlarm, dWarn, dHigh, dLow;

                // 2021.01.28: było oJsonValStatus , ale tam są tylko alarm/warning, nie ma high/low?
                //      a w oJsonSensor są wszystkie cztery
                dAlarm = oJsonSensor.GetObject().GetNamedNumber("alarmValue", 0);
                dWarn = oJsonSensor.GetObject().GetNamedNumber("warningValue", 0);
                dHigh = oJsonSensor.GetObject().GetNamedNumber("highValue", 0);
                dLow = oJsonSensor.GetObject().GetNamedNumber("lowValue", 0);

                string sLimity = "";
                if (dAlarm > 0)
                    sLimity = sLimity + "Alarm: " + dAlarm.ToString() + " cm\n";
                if (dWarn > 0)
                    sLimity = sLimity + "Warn: " + dWarn.ToString() + " cm\n";
                if (dHigh > 0)
                    sLimity = sLimity + "High: " + dHigh.ToString() + " cm\n";
                if (dLow > 0)
                    sLimity = sLimity + "Low: " + dLow.ToString() + " cm\n";

                oNewCm.sLimity = sLimity;
                if (oNewCm.dCurrValue <= dLow || oNewCm.dCurrValue >= dHigh)
                    oNewCm.sAlert = "!";
                if (oNewCm.dCurrValue >= dWarn)
                    oNewCm.sAlert = "!!";
                if (oNewCm.dCurrValue >= dAlarm)
                    oNewCm.sAlert = "!!!";

                moListaPomiarow.Add(oNewCm);
            }
            catch 
            {
            }

            // moze jest temperatura wody?
            try        // dodajemy pomiar w °C
            {
                var oNew = new JedenPomiar();
                oNew.sSource = oTemplate.sSource; // = "IMGWhyd"
                oNew.sId = oTemplate.sId;
                oNew.dLon = oTemplate.dLon;
                oNew.dLat = oTemplate.dLat;
                oNew.dOdl = oTemplate.dOdl;
                oNew.sOdl = oTemplate.sOdl;
                oNew.sUnit = " °C";
                oNew.sPomiar = oTemplate.sPomiar + oNew.sUnit;
                oNew.sAdres = oTemplate.sAdres;

                Newtonsoft.Json.Linq.JArray oJsonArr;
                oJsonArr = oJsonSensor.GetObject().GetNamedArray("waterTemperatureAutoRecords");

                // dwa ostatnie Value:
                // {"date":"2019-02-25T12:00:00Z","value":3.10,"dreId":1099,"operationId":"150190340_B00102A","parameterId":"B00102A","versionId":-1,"id":2603922134800}
                if (oJsonArr.Count > 2)
                {
                    Newtonsoft.Json.Linq.JToken oJsonVal;
                    oJsonVal = oJsonArr[oJsonArr.Count - 1];
                    oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ");
                    // tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                    oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value");
                    oNew.sCurrValue = oNew.dCurrValue.ToString() + " °C";

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

            // moze jest przeplyw wody?
            try        // dodajemy pomiar w °C
            {
                var oNew = new JedenPomiar();
                oNew.sSource = oTemplate.sSource; // = "IMGWhyd"
                oNew.sId = oTemplate.sId;
                oNew.dLon = oTemplate.dLon;
                oNew.dLat = oTemplate.dLat;
                oNew.dOdl = oTemplate.dOdl;
                oNew.sOdl = oTemplate.sOdl;
                oNew.sUnit = " m³/s";
                oNew.sPomiar = oTemplate.sPomiar + oNew.sUnit;
                oNew.sAdres = oTemplate.sAdres;

                Newtonsoft.Json.Linq.JArray oJsonArr;
                oJsonArr = oJsonSensor.GetObject().GetNamedArray("dischargeRecords");

                // dwa ostatnie Value:
                // {"date":"2019-02-23T08:00:00Z","value":0.35,"dreId":1116,"operationId":"Przepływ operacyjny","parameterId":"B00050W","versionId":-1,"id":2601481083100}
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

                    // limity
                    // z Try, jakby ktores bylo null
                    double dAlarmLow, dAlarmHigh, dHigh, dLow, dAvgRok;

                    dAlarmHigh = oJsonSensor.GetObject().GetNamedNumber("highestHighDischargeValue", 0);
                    dAlarmLow = oJsonSensor.GetObject().GetNamedNumber("lowestLowDischargeValue", 0);
                    dAvgRok = oJsonSensor.GetObject().GetNamedNumber("mediumOfYearMediumsDischargeValue", 0);
                    dHigh = oJsonSensor.GetObject().GetNamedNumber("highDischargeValue", 0);
                    dLow = oJsonSensor.GetObject().GetNamedNumber("lowDischargeValue", 0);
                    dLow = 0;

                    string sLimity = "";
                    if (dAlarmHigh > 0)
                        sLimity = sLimity + "Najwyższy: " + dAlarmHigh.ToString() + " m³/s \n";
                    if (dHigh > 0)
                        sLimity = sLimity + "Wysoki: " + dHigh.ToString() + " m³/s \n";
                    if (dAvgRok > 0)
                        sLimity = sLimity + "Średni roczny: " + dAvgRok.ToString() + " m³/s \n";
                    if (dLow > 0)
                        sLimity = sLimity + "Niski: " + dLow.ToString() + " m³/s \n";
                    if (dAlarmLow > 0)
                        sLimity = sLimity + "Najniższy: " + dAlarmLow.ToString() + " m³/s \n";


                    oNew.sLimity = sLimity;
                    if (dLow > 0 && oNew.dCurrValue <= dLow)
                        oNew.sAlert = "!";
                    if (dHigh > 0 && oNew.dCurrValue >= dHigh)
                        oNew.sAlert = "!";
                    if (dAlarmLow > 0 && oNew.dCurrValue <= dAlarmLow)
                        oNew.sAlert = "!!";
                    if (dAlarmHigh > 0 && oNew.dCurrValue >= dAlarmHigh)
                        oNew.sAlert = "!!";

                    moListaPomiarow.Add(oNew);
                }
                else
                {
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
            oTS.Name = "uiConfig_ImgwHydroAll";
            oTS.IsOn = p.k.GetSettingsBool("sourceImgwHydroAll");
            oTS.OnContent = p.k.GetLangString("resImgwHydroAllON");
            oTS.OffContent = p.k.GetLangString("resImgwHydroAllOFF");
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
                    if ((oTS.Name ?? "") == "uiConfig_ImgwHydroAll")
                        p.k.SetSettingsBool("sourceImgwHydroAll", oTS.IsOn);
                }
            }
        }
    }
}

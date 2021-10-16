using Windows.Foundation;
//using System.Threading.Tasks;
using System.IO;
//using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public class Source_EEAair : Source_Base
    {
        protected override string SRC_SETTING_NAME { get;  } = "sourceEEAair";
        protected override string SRC_SETTING_HEADER { get;  } = "EEA air";
        protected override string SRC_RESTURI_BASE { get;  } = "https://discomap.eea.europa.eu/Map/UTDViewer/dataService/";
        public override string SRC_POMIAR_SOURCE { get;  } = "EEAair";
        public override bool SRC_IN_TIMER { get;  } = true;
        protected override bool SRC_HAS_TEMPLATES { get;  } = true;

        protected override string SRC_URI_ABOUT_EN { get; } = "https://www.eea.europa.eu/data-and-maps/explore-interactive-maps/up-to-date-air-quality-data";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://www.eea.europa.eu/data-and-maps/explore-interactive-maps/up-to-date-air-quality-data";

        // teoretycznie nie ma powodu robić template, bo aktualne dane sa w pliku z lista sensorow,
        // ale plik z listą sensorów 62..260 kB, a plik z historią sensora tylko 41 kB

        private string NormalizePomiarName(string sPomiar)
        {
            switch (sPomiar)
            {
                case "CO":
                        return "CO";
                case "SO2":
                        return "SO₂";
                case "NO2":
                        return "NO₂";
                case "O3":
                        return "O₃";
                case "PM10":
                        return "PM₁₀";
                case "PM2.5":
                        return "PM₂₅";
            }

            return sPomiar;
        }

        private string NormalizeUnitName(string sPomiar)
        {
            switch (sPomiar)
            {
                case "CO":
                        return " mg/m³";
                case "SO2":
                        return " μg/m³";
                case "NO2":
                        return " μg/m³";
                case "O3":
                        return " μg/m³";
                case "PM10":
                        return " μg/m³";
                case "PM2.5":
                        return " μg/m³";
            }

            return sPomiar;
        }

        private async System.Threading.Tasks.Task GetPomiary(JedenPomiar oTemplate, bool bInTimer)
        {
            // do moListaPomiarow dodaj wszystkie pomiary robione przez oTemplate.sId
            // a kiedy: Alert, Limity, sCurrValue, dCurrValue, sPomiar

            if (!p.k.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE))
                return;

            string sTmp;
            sTmp = "SamplingPoint?spo=" + oTemplate.sId;
            // SAMPLINGPOINT_LOCALID,DATETIME_BEGIN,DATETIME_END,PROPERTY,VALUE_NUMERIC,UNIT,STATIONCLASSIFICATION,AREACLASSIFICATION,ALTITUDE,STATIONCODE,STATIONNAME,LONGITUDE,LATITUDE,MUNICIPALITY
            // SPO.DE_DEST091_PM1_dataGroup1,20191215070000,20191215080000,PM10,8.55,Âµg/m3,traffic,urban,61.0000,DEST091,Dessau Albrechtsplatz,1363080.8934,6771350.7223,Dessau-RoÃŸlau
            // SPO_PL0012A_5_001,20191215070000,20191215080000,PM10,27.5748,Âµg/m3,traffic,urban,207.0000,PL0012A,"KrakÃ³w, Aleja KrasiÅ„skiego",2218173.2129,6456270.6528,KrakÃ³w

            string sPage = await GetREST(sTmp);
            if (sPage.Length < 10)
                return;

            var aLines = sPage.Split('\n');

            var aFields = aLines[0].Split(',');

            // najpierw sprawdzamy kolumny (tak na wszelki wypadek, bo to i tak nic nie kosztuje)
            int iVal = 0, iProp = 0, iDate = 0;
            for (int i = 0, loopTo = aFields.GetUpperBound(0); i <= loopTo; i++)
            {
                if ((aFields[i].ToUpper() ?? "") == "VALUE_NUMERIC")
                    iVal = i;
                if ((aFields[i].ToUpper() ?? "") == "PROPERTY")
                    iProp = i;
                if ((aFields[i].ToUpper() ?? "") == "DATETIME_END")
                    iDate = i;
            }

            // interesuje nas ostatnia linijka
            sTmp = aLines[aLines.GetUpperBound(0)];
            if (sTmp.Length < 15)
                sTmp = aLines[aLines.GetUpperBound(0) - 1];
            aFields = sTmp.Split(',');
            double dVal;
            if (!double.TryParse(aFields[iVal], out dVal))
                return; // nie liczba? error
            if (dVal == -1)
                return;    // "invalid"

            var oNew = new JedenPomiar()
            {
                sSource = oTemplate.sSource,
                sId = oTemplate.sId,
                dLon = oTemplate.dLon,
                dLat = oTemplate.dLat,
                dWysok = oTemplate.dWysok,
                dOdl = oTemplate.dOdl,
                sOdl = Odleglosc2String(oTemplate.dOdl),
                sAdres = oTemplate.sAdres
            };

            oNew.sPomiar = NormalizePomiarName(aFields[iProp]);
            oNew.sUnit = NormalizeUnitName(aFields[iProp]);
            oNew.dCurrValue = dVal;
            oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit;
            sTmp = aFields[iDate];
            if (sTmp.Length == 14)
                // 20191215080000
                sTmp = sTmp.Substring(0, 4) + "." + sTmp.Substring(4, 2) + "." + sTmp.Substring(6, 2) + " " + sTmp.Substring(8, 2) + ":" + sTmp.Substring(10, 2) + ":" + sTmp.Substring(12, 2);
            oNew.sTimeStamp = sTmp;
            moListaPomiarow.Add(oNew);
            
        }


        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            // ma zwrocic pełną liste, save template pozniej zostanie wywolane

            double dMaxOdl = 10;

            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            string sCmd;
            string sData = DateTime.UtcNow.AddMinutes(-15).ToString("yyyyMMddHH") + "0000";
            foreach (string sPolu in new[] { "PM10", "PM25", "NO2", "O3", "SO2", "CO" })
            {
                sCmd = "Hourly?polu=" + sPolu + "&dt=" + sData;
                // SAMPLINGPOINT_LOCALID,DATETIME_BEGIN,DATETIME_END,PROPERTY,VALUE_NUMERIC,UNIT,STATIONCLASSIFICATION,AREACLASSIFICATION,ALTITUDE,STATIONCODE,STATIONNAME,LONGITUDE,LATITUDE,MUNICIPALITY
                // SPO.DE_DEST091_PM1_dataGroup1,20191215070000,20191215080000,PM10,8.55,Âµg/m3,traffic,urban,61.0000,DEST091,Dessau Albrechtsplatz,1363080.8934,6771350.7223,Dessau-RoÃŸlau
                // SPO_PL0012A_5_001,20191215070000,20191215080000,PM10,27.5748,Âµg/m3,traffic,urban,207.0000,PL0012A,"KrakÃ³w, Aleja KrasiÅ„skiego",2218173.2129,6456270.6528,KrakÃ³w

                string sPage = await GetREST(sCmd);
                if (sPage.Length < 10)
                    return moListaPomiarow;

                var aLines = sPage.Split('\n');

                var aFields = aLines[0].Split(',');

                // najpierw sprawdzamy kolumny (tak na wszelki wypadek, bo to i tak nic nie kosztuje)
                int iId = 0, iName = 0, iLon = 0, iLat = 0, iAlt = 0;
                for (int i = 0, loopTo = aFields.GetUpperBound(0); i <= loopTo; i++)
                {
                    if ((aFields[i].ToUpper() ?? "") == "SAMPLINGPOINT_LOCALID")
                        iId = i;
                    if ((aFields[i].ToUpper() ?? "") == "STATIONNAME")
                        iName = i;
                    if ((aFields[i].ToUpper() ?? "") == "LONGITUDE")
                        iLon = i;
                    if ((aFields[i].ToUpper() ?? "") == "LATITUDE")
                        iLat = i;
                    if ((aFields[i].ToUpper() ?? "") == "ALTITUDE")
                        iAlt = i;
                }

                int iMax = Math.Max(iId, iName);
                iMax = Math.Max(iMax, iLon);
                iMax = Math.Max(iMax, iLat);
                iMax = Math.Max(iMax, iAlt);

                string sTmp, sName;
                int iInd;

                for (int i = 1, loopTo1 = aLines.GetUpperBound(0); i <= loopTo1; i++)
                {
                    sTmp = aLines[i];

                    // usuniemy jak są cudzysłowy
                    sName = "";

                    // ominiecie bledu: """Kochla"""
                    // "SPO-EE0019A_00005_100,20191215110000,20191215120000,PM10,2.577,Âµg/m3,industrial,urban,60.0000,EE0019A,"" "" ""Kohtla-JÃ¤rve"""""",3036642.2738,8269476.1822,Kohtla-JÃ¤rve" & vbCr
                    sTmp = sTmp.Replace("\"\"", "\"");
                    sTmp = sTmp.Replace("\"\"", "\"");
                    sTmp = sTmp.Replace("\"\"", "\"");

                    iInd = sTmp.IndexOf("\"");
                    if (iInd > 0)
                    {
                        int iInd1 = sTmp.IndexOf("\"", iInd + 1);
                        if (iInd1 > 0)
                        {
                            sName = sTmp.Substring(iInd + 1, iInd1 - iInd - 1);
                            sTmp = sTmp.Substring(0, iInd) + "PKremovedPK" + sTmp.Substring(iInd1 + 1);
                        }
                        else
                        {
                            // SPO.IE.IE004APSample2_5,20191215110000,20191215120000,PM10,6,Âµg/m3,traffic,suburban,12.0000,IE004AP,"Dublin Ringsend Recycling Centre
                            // ma zmianę linii w środku!
                            sName = sTmp.Substring(iInd + 1);
                            sTmp = sTmp.Substring(0, iInd) + "PKremovedPK";
                        }
                    }

                    aFields = sTmp.Split(',');
                    // jesli brakuje pól - pomijamy
                    if (aFields.GetUpperBound(0) < iMax)
                        continue;

                    double dLat, dLon;
                    if (!double.TryParse(aFields[iLat], out dLat))
                        continue;
                    if (!double.TryParse(aFields[iLon], out dLon))
                        continue;

                    // rekonfiguracja wedle leaflet

                    double constR = 6378137;
                    // double constMAX_LATITUDE = 85.0511287798;
                    double constD = Math.PI / 180;

                    dLon = dLon / constR / constD;
                    dLat = Math.Asin((Math.Exp(2 * dLat / constR) - 1) / (Math.Exp(2 * dLat / constR) + 1)) / constD;

                    var oTemplate = new JedenPomiar();
                    oTemplate.sSource = SRC_POMIAR_SOURCE;
                    oTemplate.sId = aFields[iId];
                    oTemplate.dLon = dLon;
                    oTemplate.dLat = dLat;
                    var argresult = oTemplate.dWysok;
                    if (!double.TryParse(aFields[iAlt], out argresult))
                        oTemplate.dWysok = 0;
                    oTemplate.dOdl = (int)oPos.DistanceTo(oTemplate.dLat, oTemplate.dLon);
                    oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl);
                    oTemplate.sAdres = aFields[iName];
                    if ((oTemplate.sAdres ?? "") == "PKremovedPK")
                        oTemplate.sAdres = sName;

                    // depolit, bo plik ma niepoprawne kodowanie
                    oTemplate.sAdres = oTemplate.sAdres.Replace("Ã³", "ó");
                    oTemplate.sAdres = oTemplate.sAdres.Replace("Å", "ń");

                    if (oTemplate.dOdl < dMaxOdl * 1000)
                        await GetPomiary(oTemplate, false);
                }
            }

            return moListaPomiarow;
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            // wywolywane dla kazdego z Template
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            // przeiteruj, tworząc JedenPomiar dla kazdego pomiaru i kazdej lokalizacji
            var oTemplate = new JedenPomiar();

            // wczytaj dane template dla danego favname
            var oFile = await EnviroStatus.App.GetDataFile(false, SRC_POMIAR_SOURCE + "_" + sId + ".xml", false);
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

            await GetPomiary(oTemplate, bInTimer);

            return moListaPomiarow;
        }
    }
}

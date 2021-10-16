
using Windows.Foundation;
//using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using System;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public class Source_RadioAtHome : Source_Base
    {
        protected override string SRC_SETTING_NAME { get;  } = "sourceRAH";
        protected override string SRC_SETTING_HEADER { get;  } = "Radioactive@Home";
        protected override string SRC_RESTURI_BASE { get;  } = "http://radioactiveathome.org/map/";
        public override string SRC_POMIAR_SOURCE { get;  } = "ra@h";
        protected override string SRC_URI_ABOUT_EN { get; } = "http://radioactiveathome.org/en/";
        protected override string SRC_URI_ABOUT_PL { get; } = "http://radioactiveathome.org/pl/";

        public override void ReadResStrings()
        {
            p.k.SetSettingsString("resRAHdailyAvg", p.k.GetLangString("resRAHdailyAvg"));
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

        private string NormalizePomiarName(string sPomiar)
        {
            return "Radiation";
        }

        private string Unit4Pomiar(string sPomiar)
        {
            return "μSv/h";
        }


        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            double dMaxOdl = 50;

            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceRAH", SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            string sPage = await GetREST("");
            if (sPage.Length < 10)
                return moListaPomiarow;

            // map.addOverlay(createMarker(new GLatLng(49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
            int iInd;
            iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng");

            try
            {
                while (iInd > 0)
                {
                    var oNew = new JedenPomiar();
                    oNew.sSource = SRC_POMIAR_SOURCE;

                    sPage = sPage.Substring(iInd + "map.addOverlay(createMarker(new GLatLng".Length + 1);

                    // 49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
                    iInd = sPage.IndexOf(",");
                    oNew.dLat = sPage.Substring(0, iInd).ParseDouble(0);
                    sPage = sPage.Substring(iInd + 1);
                    // 18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                    iInd = sPage.IndexOf(")");
                    oNew.dLon = sPage.Substring(0, iInd).ParseDouble(0);
                    sPage = sPage.Substring(iInd + 2);
                    // 51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                    oNew.dOdl = (int)oPos.DistanceTo(oNew.dLat, oNew.dLon);
                    // sprawdzamy odleglosc - czy w zakresie
                    if (oNew.dOdl / 1000 < dMaxOdl)
                    {
                        // teraz cos, co chce dodac

                        oNew.sOdl = Odleglosc2String(oNew.dOdl);

                        iInd = sPage.IndexOf(",");
                        oNew.sId = sPage.Substring(0, iInd);
                        sPage = sPage.Substring(iInd + 1);
                        // 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                        oNew.dWysok = 0;    // brak danych

                        iInd = sPage.IndexOf(":");
                        sPage = sPage.Substring(iInd + 2);
                        iInd = sPage.IndexOf("<");
                        oNew.sCurrValue = sPage.Substring(0, iInd).Replace("uSv", "μSv").Trim();
                        oNew.dCurrValue = oNew.sCurrValue.Replace("μSv/h", "").ParseDouble(0);
                        if (oNew.dCurrValue == 0)
                            // jesli wyszlo zero, to moze trzeba zmienic kropke na przecinek?
                            oNew.dCurrValue = double.Parse(oNew.sCurrValue.Replace("μSv/h", "").Replace(".", ","));
                        oNew.sUnit = Unit4Pomiar(oNew.sPomiar);

                        sPage = sPage.Substring(iInd + 1);
                        // br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                        iInd = sPage.IndexOf(":");
                        sPage = sPage.Substring(iInd + 2);
                        iInd = sPage.IndexOf("<");
                        oNew.sTimeStamp = sPage.Substring(0, iInd);

                        sPage = sPage.Substring(iInd + 1);
                        // br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                        iInd = sPage.IndexOf(":");
                        sPage = sPage.Substring(iInd + 2);
                        iInd = sPage.IndexOf("<");
                        oNew.sAddit = p.k.GetSettingsString("resRAHdailyAvg") + ": " + sPage.Substring(0, iInd).Replace("uSv", "μSv");


                        oNew.sSensorDescr = "";
                        try
                        {
                            iInd = sPage.IndexOf("Team:");
                            sPage = sPage.Substring(iInd + 6);
                            iInd = sPage.IndexOf("<");
                            oNew.sSensorDescr = sPage.Substring(0, iInd);

                            iInd = sPage.IndexOf("Nick:");
                            sPage = sPage.Substring(iInd + 6);
                            iInd = sPage.IndexOf("'");
                            oNew.sSensorDescr = oNew.sSensorDescr + ", " + sPage.Substring(0, iInd);
                        }
                        catch 
                        {
                        }

                        oNew.sAdres = "";        // brak danych
                        oNew.sPomiar = "μSv/h";

                        AddPomiar(oNew);
                    }
                    iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng");
                }
            }
            catch
            {
            }

            if (moListaPomiarow.Count < 1)
            {
                await p.k.DialogBoxAsync("ERROR: no station in range");
                return moListaPomiarow;
            }

            return moListaPomiarow;
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool("sourceRAH", SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            string sPage = await GetREST("");
            if (sPage.Length < 10)
                return moListaPomiarow;

            // map.addOverlay(createMarker(new GLatLng(49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
            int iInd;
            iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng");

            while (iInd > 0)
            {
                var oNew = new JedenPomiar();

                // ' wczytaj dane template dla danego favname
                // Dim oFile As Windows.Storage.StorageFile =
                // Await App.GetDataFile(False, "rah_" & sFavName, False)
                // If oFile IsNot Nothing Then
                // Dim oSer As Xml.Serialization.XmlSerializer =
                // New Xml.Serialization.XmlSerializer(GetType(JedenPomiar))
                // Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
                // oNew = TryCast(oSer.Deserialize(oStream), JedenPomiar)
                // oStream.Dispose()   ' == fclose
                // Else
                // oNew = New JedenPomiar
                // End If

                oNew.sSource = SRC_POMIAR_SOURCE;

                sPage = sPage.Substring(iInd + "map.addOverlay(createMarker(new GLatLng".Length + 1);

                // 49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
                iInd = sPage.IndexOf(",");
                oNew.dLat = sPage.Substring(0, iInd).ParseDouble(0);
                sPage = sPage.Substring(iInd + 1);
                // 18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                iInd = sPage.IndexOf(")");
                oNew.dLon = sPage.Substring(0, iInd).ParseDouble(0);
                sPage = sPage.Substring(iInd + 2);
                // 51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                oNew.dOdl = (int)App.moGpsPoint.DistanceTo(oNew.dLat, oNew.dLon);
                // sprawdzamy odleglosc - czy w zakresie

                oNew.sOdl = Odleglosc2String(oNew.dOdl);

                iInd = sPage.IndexOf(",");
                oNew.sId = sPage.Substring(0, iInd);
                sPage = sPage.Substring(iInd + 1);
                // 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
                if ((oNew.sId ?? "") == (sId ?? ""))
                {
                    // teraz cos, co chce dodac

                    oNew.dWysok = 0;    // brak danych

                    iInd = sPage.IndexOf(":");
                    sPage = sPage.Substring(iInd + 2);
                    iInd = sPage.IndexOf("<");
                    oNew.sCurrValue = sPage.Substring(0, iInd).Replace("uSv", "μSv");
                    oNew.dCurrValue = double.Parse(oNew.sCurrValue.Replace("μSv/h", "").Trim());

                    sPage = sPage.Substring(iInd + 1);
                    // br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                    iInd = sPage.IndexOf(":");
                    sPage = sPage.Substring(iInd + 2);
                    iInd = sPage.IndexOf("<");
                    oNew.sTimeStamp = sPage.Substring(0, iInd);

                    sPage = sPage.Substring(iInd + 1);
                    // br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                    iInd = sPage.IndexOf(":");
                    sPage = sPage.Substring(iInd + 2);
                    iInd = sPage.IndexOf("<");
                    oNew.sAddit = "średnia dobowa: " + sPage.Substring(0, iInd).Replace("uSv", "μSv");


                    oNew.sSensorDescr = "";
                    try
                    {
                        iInd = sPage.IndexOf("Team:");
                        sPage = sPage.Substring(iInd + 6);
                        iInd = sPage.IndexOf("<");
                        oNew.sSensorDescr = sPage.Substring(0, iInd);

                        iInd = sPage.IndexOf("Nick:");
                        sPage = sPage.Substring(iInd + 6);
                        iInd = sPage.IndexOf("'");
                        oNew.sSensorDescr = oNew.sSensorDescr + ", " + sPage.Substring(0, iInd);
                    }
                    catch 
                    {
                    }

                    oNew.sAdres = "";        // brak danych
                    oNew.sPomiar = "μSv/h";

                    moListaPomiarow.Add(oNew);
                }
                iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng");
            }

            if (moListaPomiarow.Count < 1)
            {
                if (!bInTimer)
                    await p.k.DialogBoxAsync("ERROR: data parsing error - sPage");
            }

            return moListaPomiarow;
        }
    }
}

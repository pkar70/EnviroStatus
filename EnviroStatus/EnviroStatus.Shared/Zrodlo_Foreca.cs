using Windows.Foundation;
//using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using System;

namespace EnviroStatus
{
    public class Source_Foreca : Source_Base
    {

        // ułatwienie dodawania następnych
        protected override string SRC_SETTING_NAME { get;  } = "sourceForeca";
        protected override string SRC_SETTING_HEADER { get;  } = "Foreca record";
        protected override string SRC_RESTURI_BASE { get;  } = "https://www.foreca.pl/World";
        protected override string SRC_RESTURI_BASE_PKAR { get;  } = "https://www.foreca.pl/Poland/Lesser-Poland-Voivodeship/Krak%C3%B3w/10-day-forecast";
        public override string SRC_POMIAR_SOURCE { get;  } = "Foreca";
        public override bool SRC_NO_COMPARE { get; } = true;
        protected override string SRC_URI_ABOUT_EN { get; } = "https://www.foreca.pl";
        protected override string SRC_URI_ABOUT_PL { get; } = "https://www.foreca.pl";

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            return await GetDataFromFavSensor("", "", false);    // bo tak :) 
        }

        public override async System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer)
        {
            moListaPomiarow = new Collection<JedenPomiar>();
            if (!p.k.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE))
                return moListaPomiarow;

            string sPage = await GetREST("");
            if (sPage.Length < 10)
                return moListaPomiarow;

            // map.addOverlay(createMarker(new GLatLng(49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
            int iInd;
            iInd = sPage.IndexOf("trivia-warm.png");
            if (iInd > 0)
            {
                try
                {
                    var oNew = new JedenPomiar();
                    oNew.sSource = SRC_POMIAR_SOURCE;
                    oNew.sPomiar = "Tmax";
                    oNew.sUnit = "°C";

                    // Public Property sCurrValue As String = "" ' etap 2: wartosc
                    // Public Property dCurrValue As Double = 0

                    // Public Property sJedn As String
                    string sTmp;
                    sTmp = sPage.Substring(iInd);
                    iInd = sTmp.IndexOf("<a href");
                    sTmp = sTmp.Substring(iInd + 10);
                    iInd = sTmp.IndexOf("\"");
                    oNew.sAdres = sTmp.Substring(0, iInd);

                    iInd = sTmp.IndexOf("&deg");
                    sTmp = sTmp.Substring(0, iInd).TrimEnd();
                    iInd = sTmp.LastIndexOf(">");
                    sTmp = sTmp.Substring(iInd + 1).TrimStart();

                    sTmp = sTmp.Replace(",", ".");
                    double dTmp;
                    if(!double.TryParse(sTmp, out dTmp))
                    {
                            sTmp = sTmp.Replace(".", ",");
                            double.TryParse(sTmp, out dTmp);
                    }
                    oNew.dCurrValue = dTmp;
                    oNew.sCurrValue = sTmp + " " + oNew.sUnit;

                    // <a href="/Cameroon/North-Province/Garoua">Garoua</a></p>	<p class="obs warm u_metrickmh">+39,0 &deg;C
                    moListaPomiarow.Add(oNew);
                }
                catch 
                {
                }
            }

            iInd = sPage.IndexOf("trivia-cold.png");
            if (iInd > 0)
            {
                try
                {
                    var oNew = new JedenPomiar();
                    oNew.sSource = SRC_POMIAR_SOURCE;
                    oNew.sPomiar = "Tmin";
                    oNew.sUnit = "°C";

                    // Public Property sCurrValue As String = "" ' etap 2: wartosc
                    // Public Property dCurrValue As Double = 0

                    // Public Property sJedn As String
                    string sTmp;
                    sTmp = sPage.Substring(iInd);
                    iInd = sTmp.IndexOf("<a href");
                    sTmp = sTmp.Substring(iInd + 10);
                    iInd = sTmp.IndexOf("\"");
                    oNew.sAdres = sTmp.Substring(0, iInd);

                    iInd = sTmp.IndexOf("&deg");
                    sTmp = sTmp.Substring(0, iInd).TrimEnd();
                    iInd = sTmp.LastIndexOf(">");
                    sTmp = sTmp.Substring(iInd + 1).TrimStart();

                    sTmp = sTmp.Replace(",", ".");
                    double dTmp;
                    if (!double.TryParse(sTmp, out dTmp))
                    {
                        sTmp = sTmp.Replace(".", ",");
                        double.TryParse(sTmp, out dTmp);
                    }
                    oNew.dCurrValue = dTmp;
                    oNew.sCurrValue = sTmp + " " + oNew.sUnit;

                    moListaPomiarow.Add(oNew);
                }
                // <a href="/Cameroon/North-Province/Garoua">Garoua</a></p>	<p class="obs warm u_metrickmh">+39,0 &deg;C
                catch 
                {
                }
            }
            if (moListaPomiarow.Count < 1)
            {
                if (!bInTimer)
                    await p.k.DialogBoxAsync(@"ERROR: data parsing error ra@h\sPage");
                return moListaPomiarow;
   
            }

            return moListaPomiarow;
  
        }
    }
}

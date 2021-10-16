
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using System.IO;
using Windows.UI.Xaml.Controls;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public sealed partial class Info : Page
    {
        public Info()
        {
            this.InitializeComponent();
        }

        private void uiOk_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private string GetHtmlHelpPage()
        {
            string sTmp = GetHelpHeader();
            if (System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower() == "pl")
                sTmp += GetHelpBodyPL();
            else
                sTmp += GetHelpBodyEN();
            sTmp += GetHelpFooter();

            return sTmp;

            // wersja z plikami
            //StreamReader oFile;
            //string sTxt;
            //sTxt = @"Assets\Guide-" + System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName + ".htm";
            //if (!File.Exists(sTxt))
            //    sTxt = @"Assets\Guide-En.htm";

            //oFile = File.OpenText(sTxt);
            //sTxt = "";
            //while (!oFile.EndOfStream)
            //    sTxt = sTxt + oFile.ReadLine();
            //oFile.Dispose();

            //if (uiTitle.RequestedTheme == ElementTheme.Dark)
            //    sTxt = sTxt.Replace("<body>", "<body bgcolor='#000000' style='color:#eeeeee'>");

            //return sTxt;
        }



        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            uiVers.Text = "v" + p.k.GetAppVers();

            uiWeb.NavigateToString(GetHtmlHelpPage());
        }

        private void uiMail_Click(object sender, RoutedEventArgs e)
        {
#if NETFX_CORE || __ANDROID__
            var oMsg = new Windows.ApplicationModel.Email.EmailMessage();
            oMsg.Subject = "Smogometr - feedback";
            oMsg.To.Add(new Windows.ApplicationModel.Email.EmailRecipient("pkar.apps@outlook.com"));
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(oMsg);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#else
            try
            {
                Xamarin.Essentials.EmailMessage message = new Xamarin.Essentials.EmailMessage
                { 
                    Subject = "Smogometr - feedback",
                    To = { "pkar.apps@outlook.com" }
                };
                Xamarin.Essentials.Email.ComposeAsync(message);
            }
            catch 
            {
                // Some other exception occurred
            }

#endif

        }

        private void UiWeb_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri == null)
                return;

            args.Cancel = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Windows.System.Launcher.LaunchUriAsync(args.Uri);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }


        private string GetHelpHeader()
        {
            string sTxt = @"<html>
<meta http-equiv='Content-Language' content='pl'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style type='text/css'>
.center {
				text-align: center;
}
</style>
</head>
";

#if NETFX_CORE
            if (uiTitle.RequestedTheme == ElementTheme.Dark)
                sTxt += " <body bgcolor='#000000' style='color:#eeeeee'>";
            else
#endif
            sTxt += "<body>";
            return sTxt;
        }

        private string GetHelpFooter()
        {
            return "</body></html>";
        }

        private string GetHelpBodyPL()
        {
            return @"
    <p>Aplikacja pokazująca smutną prawdę o skażeniu powietrza.</p>
    <p>Jej zadaniem jest zebranie danych z różnych źródeł, są to:</p>
    <!-- gwiazdka, i br, zeby nie bylo zmniejszenia tekstu -->
    * GIOŚ, czyli Główny Inspektorat Ochrony Środowiska - jego sensory mierzą wiele parametrów powietrza<br />
    * IMGW hydrologiczne, podaje stan wód (najbliższej rzeki, lub wszystkich w promieniu 25 km)<br />
    * IMGW meteo, w tym dane o wietrze<br />
    * EEAair, czyli unijny GIOŚ (European Environmental Agency)<br />
    * Airly, którego sensory mierzą głównie zapylenie ('PeeMy')<br />
    * DarkSky, który co prawda jest serwisem prognozy pogody a nie siecią sensorów, ale udostępnia dane o oficjalnych alertach pogodowych, a także m.in. tzw. UV index, stopień zachmurzenia, widzialność, punkt rosy oraz temperaturę odczuwalną (i te dane prezentuję)<br />
    * radioactiveathome, sensory uczestniczące w tym projekcie mierzą promieniowanie tła<br />
    * Foreca, z której biorę dane o najzimniejszym oraz najcieplejszym miejscu na świecie<br />
    * Seismic, czyli dane o trzęsieniach ziemi (najsilniej odczuwane w danej lokalizacji, oraz suma w zadanym promieniu)<br />
    * NOAA, czyli pogoda kosmiczna (wiatr kosmiczny, zakłócenia łączności, etc.)
    <p>
        Uwzględniane są czujniki odległe co najwyżej 10 km (Airly, GIOS, IMGW meteo, EEA), 25 km (IMGW hydro) lub 50 km (radioactive).
 Na terenie Polski proszę wybierać GIOS, poza Polską - EEA; nie oba jednocześnie (bo się dane będa pokrywać).
    </p>

    <p>
        Ze względów technicznych, część danych ściągana jest są za pomocą API,
        zaś pozostałe przez symulację strony.
        Ze względu zaś na warunki udostępniania danych przez Airly oraz DarkSky,
        należy sobie założyć na ich portalu konto.
        W ten sposob uzyskany API key należy wpisać w Ustawieniach aplikacji (bez niego aplikacja nie dostanie się do ich pomiarów)
    </p>
    <p>
        Każdy pomiar można zamienić na ikonkę na pulpicie.
    <p>
        W Szczegółach parametru można zobaczyć <a href='https://www.who.int/news-room/fact-sheets/detail/ambient-(outdoor)-air-quality-and-health'>zalecenia WHO</a>,
        a także <a href='http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031'>poziomy wedle prawa polskiego</a>,
        które jest niemal równoważne obowiązującej <a href='https://eur-lex.europa.eu/legal-content/PL/ALL/?uri=CELEX:32008L0050'>Dyrektywie UE.</a>
        Aplikacja oznacza wykrzyknikami przekroczenie albo zaleceń WHO (uzupełnione unijnym limitem benzenu),
        albo limitów UE. Gdy wzorcem jest WHO, to jeden wykrzyknik oznacza przekroczenie normy rocznej, dwa - normy dziennej, trzy - dwukrotne przekroczenie normy dziennej.
        Gdy wzorcem jest UE, to jeden wykrzyknik oznacza przekroczenie poziomu dopuszczalnego (docelowego),
        dwa - przekroczenie poziomu informowania, a trzy - poziomu alarmowania.
    </p>
    <p>
        Aplikacja pozwala stworzyć listę lokalizacji - pod podaną nazwą, zapamiętywane są współrzędne
        oraz lista znalezionych czujników. Aplikacja nie sprawdza później, czy nie zostały zainstalowane
        nowe czujniki; można jednak zawsze powtórzyć wyszukanie czujników wedle GPS i ponownie zapisać
        miejsce używając tej samej nazwy.
    </p>
    <p>
        Dodatkowo, aplikacja przelicza jaka byłaby wilgotność w pomieszczeniu o podanej kubaturze,
        gdyby całkowicie wymienić powietrze, i zmienić temperaturę (np. w zimie - podgrzać);
        oraz ile wody należałoby dodać by wilgotność była w zalecanym zakresie (40-60 %).
        Efekty przeliczeń prezentowane są po wywołaniu menu kontekstowego dla pomiaru wilgotności.
    </p>
    <p>
        Aplikacja tworzy także własny pomiar (wyliczany): temperaturę odczuwalną.
        Ale to nie jest rzeczywista temperatura odczuwalna, bo aplikacja korzysta tylko z temperatury i wilgotności,
        pomija efekt wiatru oraz nasłonecznienia.
    </p>

    <p>Miłego używania :)</p>
    <p>Pełny podręcznik: <a href='http://beskid.geo.uj.edu.pl/p/ms/help/SmogMeter-Guide.htm'>tutaj</a>
"; 
        }

            private string GetHelpBodyEN()
            {
            return @"
    <p>This app shows (sad) truth about air pollution in your vincinity.</p>
    <p>It simply collects (merges) data from different sources:</p>
    <!-- gwiazdka, i br, zeby nie bylo zmniejszenia tekstu -->
    * DarkSky, although it is weather forecast service and not a sensors network, but it also presents official weather alerts, and e.g. UV index, cloud coverage, visibility, dew point and apparent temperature<br />
    * Foreca, for current maximum and minimum world temperature<br />
    * Seismic, earthquakes data (with strongest effect in your location, and sum of quakes within range)<br />
    * NOAA, cosmic weather (solar wind, radio perturbances, etc.)
    * radioactiveathome, radiation sensors (world)<br />
    * Airly, sensors mainly for Particulate Matter (mainly Europe)<br />
    * EEAair, air pollution data from European Environmental Agency <br />
    * IMGW hydrology, Polish official hydrology service, status of rivers<br />
    * IMGW meteo, Polish official meteo service, mainly wind data<br />
    * GIOS, polish government's agency - sensors takes many measurements<br />
    <p>
        Sensors should be less than 10 km (Airly, GIOS, EEA, IMGW meteo), 25 km (IMGW hydro) or less than 50 km (radioactive).
You should not enable both EEA and GIOS - as EEA uses data from GIOS.
 
    </p>
    <p>
        For technical reasons, data from three former sources are received by API,
        and from rest - via web page simulation.
        And for Airly and DarkSky rules, you have to create account on their portal.
        After creating account, you get API key - please enter it in app Settings.
    </p>
    <p>
        In measurement Details you can see <a href='https://www.who.int/news-room/fact-sheets/detail/ambient-(outdoor)-air-quality-and-health'>WHO standards</a>,
        <a href='http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031'>levels from polish law</a>,
        almost identical as levels in <a href='https://eur-lex.europa.eu/legal-content/EN/ALL/?uri=CELEX:32008L0050'>UE Directive.</a>

        You can select what app marks (with exclamations) - values over WHO or over EU limits.
        In 'WHO mode', one exclamation means value over annual average, two - daily average, and three - 2× daily average.
        In 'EU mode', one exclamation means value over limit, two - over information threshold, and two - over alert treshold.
    </p>
    <p>
        App allows you to create list of locations - under given name, it stores latitude/longitude
        and list of nearest sensors. App doesn't check for new sensors' instalations;
        but you can use 'GPS' option and save location under same name as before.
    </p>

    <p>
        Also, app calculate how humidity would change if you replace whole air in room,
        and change temperature (volume of room, and target temperature, can be set in Settings).
        You get relative humidity, and how many water you should pump into air to get recommended humidity (40-60 %).
        To see this data, please right click (tap) on Humidity measurement.
    </p>
    <p>
        App creates own (calculated) measurement: apparent temperature.
        But it is not real apparent temperature, as app uses only ambient temperature and humidity,
        and doesn't have wind speed nor radiation effect included.
    </p>

    <p>Try this app and be happy :)</p>
    <p>Full user guide: <a href='http://beskid.geo.uj.edu.pl/p/ms/help/SmogMeter-Guide.htm'>here</a>
";

        }

    }
}

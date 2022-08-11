'Imports Windows.ApplicationModel
'Imports Windows.UI.Xaml
'Imports Windows.UI.Xaml.Controls
''using Microsoft.VisualBasic.CompilerServices;

Public Class Info


    Public Shared Function GetHtmlHelpPage(bReverse As Boolean) As String
        Dim sTmp As String = GetHelpHeader(bReverse)

        If GetLangString("_lang").ToLower = "pl" Then
            sTmp += GetHelpBodyPL()
        Else
            sTmp += GetHelpBodyEN()
        End If

        sTmp += GetHelpFooter()
        Return sTmp


    End Function


    Private Shared Function GetHelpHeader(bReverse As Boolean) As String
        Dim sTxt = "<html>
    <meta http-equiv='Content-Language' content='pl'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <style type='text/css'>
    .center {
    				text-align: center;
    }
    </style>
    </head>
    "

        If bReverse Then
            sTxt += " <body bgcolor='#000000' style='color:#eeeeee'>"
        Else
            sTxt += "<body>"
        End If

        Return sTxt
    End Function

    Private Shared Function GetHelpFooter() As String
        Return "</body></html>"
    End Function

    Private Shared Function GetHelpBodyPL() As String
        Return "
        <p>aplikacja pokazująca smutną prawdę o skażeniu powietrza.</p>
        <p>jej zadaniem jest zebranie danych z różnych źródeł, są to:</p>
        <!-- gwiazdka, i br, zeby nie bylo zmniejszenia tekstu -->
        * gioś, czyli główny inspektorat ochrony środowiska - jego sensory mierzą wiele parametrów powietrza<br />
        * imgw hydrologiczne, podaje stan wód (najbliższej rzeki, lub wszystkich w promieniu 25 km)<br />
        * imgw meteo, w tym dane o wietrze<br />
        * eeaair, czyli unijny gioś (european environmental agency)<br />
        * airly, którego sensory mierzą głównie zapylenie ('peemy')<br />
        * darksky, który co prawda jest serwisem prognozy pogody a nie siecią sensorów, ale udostępnia dane o oficjalnych alertach pogodowych, a także m.in. tzw. uv index, stopień zachmurzenia, widzialność, punkt rosy oraz temperaturę odczuwalną (i te dane prezentuję)<br />
        * radioactiveathome, sensory uczestniczące w tym projekcie mierzą promieniowanie tła<br />
        * foreca, z której biorę dane o najzimniejszym oraz najcieplejszym miejscu na świecie<br />
        * seismic, czyli dane o trzęsieniach ziemi (najsilniej odczuwane w danej lokalizacji, oraz suma w zadanym promieniu)<br />
        * noaa, czyli pogoda kosmiczna (wiatr kosmiczny, zakłócenia łączności, etc.)
        <p>
            uwzględniane są czujniki odległe co najwyżej 10 km (airly, gios, imgw meteo, eea), 25 km (imgw hydro) lub 50 km (radioactive).
     na terenie polski proszę wybierać gios, poza polską - eea; nie oba jednocześnie (bo się dane będa pokrywać).
        </p>

        <p>
            ze względów technicznych, część danych ściągana jest są za pomocą api,
            zaś pozostałe przez symulację strony.
            ze względu zaś na warunki udostępniania danych przez airly oraz darksky,
            należy sobie założyć na ich portalu konto.
            w ten sposob uzyskany api key należy wpisać w ustawieniach aplikacji (bez niego aplikacja nie dostanie się do ich pomiarów)
        </p>
        <p>
            każdy pomiar można zamienić na ikonkę na pulpicie.
        <p>
            w szczegółach parametru można zobaczyć <a href='https://www.who.int/news-room/fact-sheets/detail/ambient-(outdoor)-air-quality-and-health'>zalecenia who</a>,
            a także <a href='http://prawo.sejm.gov.pl/isap.nsf/docdetails.xsp?id=wdu20120001031'>poziomy wedle prawa polskiego</a>,
            które jest niemal równoważne obowiązującej <a href='https://eur-lex.europa.eu/legal-content/pl/all/?uri=celex:32008l0050'>dyrektywie ue.</a>
            aplikacja oznacza wykrzyknikami przekroczenie albo zaleceń who (uzupełnione unijnym limitem benzenu),
            albo limitów ue. gdy wzorcem jest who, to jeden wykrzyknik oznacza przekroczenie normy rocznej, dwa - normy dziennej, trzy - dwukrotne przekroczenie normy dziennej.
            gdy wzorcem jest ue, to jeden wykrzyknik oznacza przekroczenie poziomu dopuszczalnego (docelowego),
            dwa - przekroczenie poziomu informowania, a trzy - poziomu alarmowania.
        </p>
        <p>
            aplikacja pozwala stworzyć listę lokalizacji - pod podaną nazwą, zapamiętywane są współrzędne
            oraz lista znalezionych czujników. aplikacja nie sprawdza później, czy nie zostały zainstalowane
            nowe czujniki; można jednak zawsze powtórzyć wyszukanie czujników wedle gps i ponownie zapisać
            miejsce używając tej samej nazwy.
        </p>
        <p>
            dodatkowo, aplikacja przelicza jaka byłaby wilgotność w pomieszczeniu o podanej kubaturze,
            gdyby całkowicie wymienić powietrze, i zmienić temperaturę (np. w zimie - podgrzać);
            oraz ile wody należałoby dodać by wilgotność była w zalecanym zakresie (40-60 %).
            efekty przeliczeń prezentowane są po wywołaniu menu kontekstowego dla pomiaru wilgotności.
        </p>
        <p>
            aplikacja tworzy także własny pomiar (wyliczany): temperaturę odczuwalną.
            ale to nie jest rzeczywista temperatura odczuwalna, bo aplikacja korzysta tylko z temperatury i wilgotności,
            pomija efekt wiatru oraz nasłonecznienia.
        </p>

        <p>miłego używania :)</p>
        <p>pełny podręcznik: <a href='https://github.com/pkar70/envirostatus/wiki/instrukcja'>tutaj</a>
    "
    End Function

    Private Shared Function GetHelpBodyEN() As String
        Return "
        <p>this app shows (sad) truth about air pollution in your vincinity.</p>
        <p>it simply collects (merges) data from different sources:</p>
        <!-- gwiazdka, i br, zeby nie bylo zmniejszenia tekstu -->
        * darksky, although it is weather forecast service and not a sensors network, but it also presents official weather alerts, and e.g. uv index, cloud coverage, visibility, dew point and apparent temperature<br />
        * foreca, for current maximum and minimum world temperature<br />
        * seismic, earthquakes data (with strongest effect in your location, and sum of quakes within range)<br />
        * noaa, cosmic weather (solar wind, radio perturbances, etc.)
        * radioactiveathome, radiation sensors (world)<br />
        * airly, sensors mainly for particulate matter (mainly europe)<br />
        * eeaair, air pollution data from european environmental agency <br />
        * imgw hydrology, polish official hydrology service, status of rivers<br />
        * imgw meteo, polish official meteo service, mainly wind data<br />
        * gios, polish government's agency - sensors takes many measurements<br />
        <p>
            sensors should be less than 10 km (airly, gios, eea, imgw meteo), 25 km (imgw hydro) or less than 50 km (radioactive).
    you should not enable both eea and gios - as eea uses data from gios.

        </p>
        <p>
            for technical reasons, data from three former sources are received by api,
            and from rest - via web page simulation.
            and for airly and darksky rules, you have to create account on their portal.
            after creating account, you get api key - please enter it in app settings.
        </p>
        <p>
            in measurement details you can see <a href='https://www.who.int/news-room/fact-sheets/detail/ambient-(outdoor)-air-quality-and-health'>who standards</a>,
            <a href='http://prawo.sejm.gov.pl/isap.nsf/docdetails.xsp?id=wdu20120001031'>levels from polish law</a>,
            almost identical as levels in <a href='https://eur-lex.europa.eu/legal-content/en/all/?uri=celex:32008l0050'>ue directive.</a>

            you can select what app marks (with exclamations) - values over who or over eu limits.
            in 'who mode', one exclamation means value over annual average, two - daily average, and three - 2× daily average.
            in 'eu mode', one exclamation means value over limit, two - over information threshold, and two - over alert treshold.
        </p>
        <p>
            app allows you to create list of locations - under given name, it stores latitude/longitude
            and list of nearest sensors. app doesn't check for new sensors' instalations;
            but you can use 'gps' option and save location under same name as before.
        </p>

        <p>
            also, app calculate how humidity would change if you replace whole air in room,
            and change temperature (volume of room, and target temperature, can be set in settings).
            you get relative humidity, and how many water you should pump into air to get recommended humidity (40-60 %).
            to see this data, please right click (tap) on humidity measurement.
        </p>
        <p>
            app creates own (calculated) measurement: apparent temperature.
            but it is not real apparent temperature, as app uses only ambient temperature and humidity,
            and doesn't have wind speed nor radiation effect included.
        </p>

        <p>try this app and be happy :)</p>
        <p>full user guide: <a href='https://github.com/pkar70/envirostatus/wiki/instrukcja'>here</a>
    "
    End Function
End Class


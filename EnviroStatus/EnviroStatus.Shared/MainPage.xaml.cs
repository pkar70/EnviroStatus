
/*
<moze SO2 - x.xx a nie x.xxx - co odrozni tysiace od ulamka?>

 Może to-do:
  * settings: simulate GPS - mozna ewentualnie zrobic pola Lat,Long jako binding do Toggle.IsOn; tylko czy Uno to umie?
  *     trzeba będzie spróbować - see Readme.Txt

2021.10.16
 * ZrodloBase.PrivateSwitch jako przełącznik do trybu prywatnego; moje APIKEY przerzucone do pswd.cs (a to jest w .gitignore)

2021.09.27
 * ZrodloBase ma SRC_URI_ABOUT_EN oraz _PL, ale niestety mało co ma sensowny About, a więc - to jest bez sensu :)

2021.09.26
 * do LastToast dodaje także NOAA, żeby się nie powtarzało (co prawda niby nie powinno, ale jednak się powtarza) - czyli wycofanie zmiany 2021.05.02

2021.09.23
 * limity WHO nowe: https://apps.who.int/iris/handle/10665/345329: Settings.xaml, App.PoziomyWHO, App.DodajPrzekroczenia

2021.05.05
  * SetSettingsBool("cleanAir", bCleanAir) zeby nie powtarzało msg CleanAir w kółko

2021.05.02
  * App, dla NOAA alerts, remnięte // sToastMemory = sToastMemory + sTmp; -
   
STORE  

2021.04.30
  * Andro: 3.7.0.dev-144
  
2021.04.12
  * ponieważ Uno umie Bindings, to Setup log data -> IsEnabled do guziczka Openlog. Nie przełączam Lat/Long, bo wtedy i tak trzeba byłoby dodać konwerter, czyli wcale to nie upraszcza kodu
  * SourcesBase, powiązanie APIkey z włączeniem konkretnego source (DarkSky, Airly) [IsEnabled; Visibility byłoby z Converter a tego nie umiem z kodu)]
  * Sources, dodatkowe pola (IMGW*, Seismic) - IsEnabled jako Binding do IsOn

2020.03.25
  * Droid: -> Uno 3.6-dev, i Android 11
  * jeśli dodaje się miejsce "pktoasttest", to wtedy robi testowy Toast - do testowania po rekompilacjach. I w pierwszym Timer na pewno pokaże Toast (do sprawdzania czy Timer działa)

 2020.02.02
  * po spędzeniu dużo czasu nad Registry już mogę debug dla SDK >17134 :)

 2021.01.28
  * do (clean air) uwzględnia tylko to co z airly, gios, eeair (czyli nie rzeki)
  * zmiana do IMGWhydro - bo zmiana formatu danych json wysylanych przez serwer (low/high, oraz current cm)
  * przestało działać, wylatywało na source_IMGWmet, ale nie dało się debug (NUMBOX, >17134). Przy próbach skasowałem KoncowkaPokazywaniaDanych, odtwarzałem z wersji VB
  *     na czas debuga wyłączyłem NUMBOXy - i działa. Nie rozumiem.

 2020.11.14
  * poprawka w IMGWhydro - zamiast !IsInPoland było IsInPoland, od błąd od 2019.01.09 w Nearest

 2020.11.02
 * podmiana Uno na 3.2.0-dev.265 (based on 3.1.6)
 * podmiana pkModuleShared lokalne na wersję wspólną (w tym MainPage:ProgressRing na ProgRingBar)
 * WinUI 2 NumberBox (strona Settings): lat/long emulacji GPS, oraz kubatura i temp dla wilgotności
 * lat/lon emulacji GPS zapisywana do Settings - nie było!
 
 2020.09.19
 * rekompilacja Android (z nowszym Uno: moim 3.1.0-dev.163), bo Gogus wymusza targetSDK Android 10.
 * zmiana REST_URI dla IMGW z http://monitor.pogodynka.pl/ na http://hydro.imgw.pl/

 STORE: 2005

 2020.05.06
 * MainPage: pierwszy Start - MsgBox ze trzeba zrobić setup i przeskok do Zrodelka (na potrzeby AmazonStore)
 * Info: link do pełnego user guide


 2020.03.09
 * ZrodloBase:
    * zwłoka 100 ms pomiędzy odwołaniami do tego samego serwera, a nie w ogóle przy dostępie do internet (dla 10 źródeł oznacza to 1 sekundę zysku)
    * timeout (w GetREST) ustawiam na 10 sekund (domyślnie był: 100 sekund)
    * gdy GetREST zwraca "", to wtedy bez szukania danych (błąd w SeismicPortal, iInd=-1, ale może nie tylko tam tak było - choć większość była obudowana Try/Catch)


ver.2002 DROID
ver.2002 UWPSTORE


 2020.02.19
 * nie wczytuje danych jak poziom alertowania jest null (poprzednio wczytywał tylko nie toastował)

 2020.02.18
 * [android] praca w tle (dzięki mojemu PR do PlatformUno), wraz z toastami

 2020.02.12
 * [android] aktualizacja pkmodule
 * [android] dodania splashscreen 

 2020.01.24 ZrodloBase: bez dawanie Header do ToggleSwitch w Sources.Xaml - do sprawdzenia przy nastepnej kompilacji
 - dalej nie działa, więc przywracam header... ale właściwie przywracam patch (w Uno.945)

ver.2001.2 DROID

 2019.01.09 ZrodloBase: bool InsidePoland() [zgrubne]
 2019.01.09 GIOS, IMGW* - jesli !InsidePoland, to w ogóle nie szuka czy coś jest w odległości

 2019.01.07 domyslnie bylo w wiekszosci "true", gdy bez settings zrodla. teraz korzysta z SRC_DEFAULT_ENABLE
 2019.01.07 SaveAs ustawia bieżący Favourite (do timera), wcześniej trzeba było wejść raz na Fav wedle Name.
 2019.01.07 double.Parse => string.ParseDouble

ver. 2001 UWPSTORE

 2019.12.31 przy SecondaryTile, kontrola czy to nie XBox - jeśli tak, pomija nieistniejące tam secondaryTiles.

 2019.12.29 MainPage:Compare

 2019.12.26 toasty mają nagłówek SmogMeter (pojawiało się z pustym, ale wcześniej chyba nazwa app zawsze była?)
 2019.12.26 Seismis details/setup: domowego miesięcznego zużycia, rocznej produkcji krajowej

 2019.12.22 zabezpieczenie przed pokazaniem serii Details (błąd Barbara52?)
 2019.12.22 sourcebase property SRC_NO_COMPARE default: false - przygotowanie do porównywania
 2019.12.22 "mag", trzeci guzik w Details - do mapy, na razie tylko UWP (ale w Uno już chyba jest?)
 2019.12.22 DarkSky:visibility , gdy <10 km to podaje w metrach, a gdy w km - z ograniczeniem do setnych (bez tysięcznych)
 
 2019.12.18 wszystkie źródła przeniesione do newtonsoft.json i sprawdzone
 2019.12.18 posortowane źródła wedle: świat, euro, polska (kolejność widoczna potem w Setup)
 2019.12.18 DarkSky/Airly: jeśli key=ZrodloBase.PrivateSwitch, i IsThisMoje, to wtedy wstawia mój key [dopiero podczas czytania danych, nie w Setup!]
 2019.12.18 Info: z plików na @"" w kodzie
 2019.12.18 MainPage: doubleTap pokazuje Details (dla Android jedyna droga, dla Windows - skrót bez contextMenu)
 2019.12.18 MainPage:Details: podaje także nazwę pomiaru
 2019.12.18 przywrócenie(?) has_templates dla IMGW meteo

 2019.12.17 poprawka: chyba nie działało sięganie do Airly (do innego Sett zapisywał Key, z innego korzystał później)
 2019.12.17 Uno:Airly uruchomienie źródła

 2019.12.17 przeniesienie do Uno:
    - dla Android bez mapki, bez Info (bo strona HTML z ms-appx) [potem dodałem Info]
    - dla Android: kasowanie plików i odtwarzanie, bo nie mamy DateModified
    - dla Android: nie ma AddTile, nie ma Timer
    - migracja do NewtonSoft.JSon, z Extension w ZrodloBase - dzieki czemu mniej roboty (mniej zmian)

 2019.12.15 nowe źródło: EEAair (do info: please, nie używaj jednocześnie GIOS i EEAair, będą się pokrywać)
 2019.12.15 nowe źródło: Quake (trzęsienia ziemi), sumowanie oraz najbliższe
 2019.12.15 wyszukiwanie punktu wedle mapy (i dla niego pokazywanie danych)
 2019.12.15 IMGWhyd: gdy nie ma w danych poziomów alertów (null), to wtedy nie wykrzyknikuje

 ver. 4.1912

 2019.12.04 GIOS już też ma limity
 2019.12.04 IMGWhydro: jedna, albo wiele rzek

 2019.10.30 NOAA, SolarWindTemp może być null - wtedy liczy jako zero.
 2019.10.30 IMGWmeteo, uwzględniona inna postac oJsonSensor jako "null" 
 2019.10.07 IMGWmeteo, jsonsensor null już bez exception

 ver. 4.1908
 2019.07.10 poprawka NOAA toast: nie ma powtarzania Toast co drugi Timer
 2019.07.23 odległość >10 km jest podawana nie w m, ale w km

 ver. 4.1907
 2019.06.27 poprawka czytania cache (sytuacja: brak pliku w cache)
 2019.06.29 zmiana ikonki 'Share' na 'Send' (bo: Failed to create a 'Windows.UI.Xaml.Controls.AppBarButton' from the text 'Share'. [Line: 73 Position: 47]')
 2019.06.29 zapisywanie Cache: niezależnie local i roam
 2019.06.29 gdy nie ma nic, to zwraca w remsys "(empty)"

 ver. 4.1906
 2019.05.31 NOAA alert, w getfav, wywolywane zawsze (bo jak nie ma nic, to w Fav nie ma itemka do niego, wiec normalnie by go nie wywolal)
 2019.06.01 NOAA alert, oraz DarkSky alert - w Toast pokazywane wiecej info (sCurrValue, nie pomiar)
 2019.06.02 DarkSky, UV index: pokazuje limity (wedle WHO)
 2019.06.02 RA@H: pokazuje info "24 godzinna" przy (od zawsze podawanej) wartosci sredniej
 2019.06.02 Settings: Save data cache, zapis po odczycie i odczyt gdy na starcie app nie ma nic w pamieci
 2019.06.04 zapisywanie pliku roam/local po odczycie danych, i jego odczyt przy starcie jak w pamieci nie ma danych
 2019.06.04 AppService/RemoteSystems - zwracanie darksky api key oraz danych (uwaga: dlugosc pliku moze byc za duza!)
 2019.06.04 AppService/RemoteSystems - zwracanie prostego "jest wykrzyknik"
 2019.06.04 MainPage - gdy wczytane dane z cache, to podaje z kiedy one są
 2019.06.05 otwieranie z toastu - nie zawisa, tylko pokazuje stronę główną
 2019.06.05 intimer: już nie powinno wylatywać z errorem (a tak było, gdy np. GIOS zwracał błąd, i próbowało się zrobić DialogBox?)
 2019.06.06 App:Cache_Load: nowszy plik z roam/local
 2019.06.11 DarkSky:limity dla UV index, poprawka tekstu: very high od 8, nie od 9 (wykrzyknikowanie było OK)
 2019.06.11 App:RemSys poprawka odsyłania danych (wcześniej wysyłał puste dane)
 2019.06.12 DarkSky Alert: %lf » vbCrLf
 2019.06.13 NOAAalerts: pokazuje te, ktore issue_time nie minęło 24 godzin, max. 5, a nie tylko nowsze niz poprzednim razem
 2019.06.14 Details: nie MsgBox, a z dwoma guzikami: OK oraz Copy

 ver. 3.1906
 2019.05.02 Settings: pokazuje numer kompilacji (w Info też jest, ale Info mam nie w kazdej app)
 2019.05.02 DarkSky:UV index: wykrzyknikowanie wedle WHO 
 2019.05.17 DarkSky:Alerty: wykrzyknikowanie
 2019.05.27 Mainpage:Details:DarkSky nie pokazuje lat/long, bo i tak nie są znane
 2019.05.31 nowe źródło: NOAA solar wind, NOAA K-index, NOAA alerts
 2019.05.31 przerobienie struktury tak, by było prościej dodawać sourcesy (lista source, bez niezależnych zmiennych dla każdego)

 ver 3.1905
 2019.03.31 Foreca: nie bylo jej w Settings/Sources
 2019.03.31 Toasts: błąd w logice (nie działały przy włączonym DarkSky) - App.mResString nie były ustawione, i wylatywało na tworzeniu toastów po skanowaniu w tle, teraz resString brane są z Settings
 2019.03.31 Toasts: powtarzał (ok), bo po aArr = Split(vbCrLf) były itemami od vbLf, więc na '=' nie trafiało
 2019.04.06 Zrodelka: scrollbar listy włącz/wyłącz
 2019.04.08 Source_Base, z którego pozostałe mają Inherits/Overloads (przygotowanie do kolejnych Src)

 VER 3.1904
 2019.03.24 Foreca: gdy IsThisMoje to z linku krakowskiego, w przeciwnym wypadku - swiatowego
 2019.03.25 nowe źródło: DarSky
 2019.03.29 Foreca: nie tylko String, ale i Double z danymi (dla Details page potrzebne)
 2019.03.30 MainPage: z Fav - pokazywanie stanu po kazdym zrodle

 */

using Windows.Foundation;
//using System.Threading.Tasks;
using Windows.UI.Xaml;
//using Microsoft.VisualBasic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using Windows.UI.Xaml.Controls;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public sealed partial class MainPage : Page
    {
        

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void uiSetup_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
        }

        private double CalculateWilgAbs(double dTemp, double dWilgWzgl)
        {
            // https://klimapoint.pl/kalkulator-wilgotnosci-bezwzglednej/
            return 216.7 * (dWilgWzgl / 100 * 6.112 * Math.Exp(17.62 * dTemp / (243.12 + dTemp)) / (273.15 + dTemp));
        }

        private string ConvertHumidity(double dHigroExt)
        {
            double dKubatura = ((double)p.k.GetSettingsInt("higroKubatura", 0)) / 100.0;
            if (dKubatura == 0)
                return "";

            int iIntTemp = p.k.GetSettingsInt("higroTemp", 22);
            if (iIntTemp == 0)
                return "";

            double dExtTemp = -1000;

            foreach (EnviroStatus.JedenPomiar oItem in EnviroStatus.App.moPomiaryAll)
            {
                if (oItem.sPomiar.ToLower().IndexOf("temp") > -1 && oItem.sSource.ToLower() != "noaawind")
                {
                    dExtTemp = oItem.dCurrValue;
                    break;
                }
            }

            if (dExtTemp == -1000)
                return "";      // nie bylo temperatury!

            double dWilgAbs = CalculateWilgAbs(dExtTemp, dHigroExt);

            double dWilgInt;
            // https://klimapoint.pl/kalkulator-wilgotnosci-bezwzglednej/

            dWilgInt = 100 * (dWilgAbs / 216.7) * (273.15 + iIntTemp) / (6.112 * Math.Exp(17.62 * iIntTemp / (243.12 + iIntTemp)));

            double dWoda40 = -(dWilgAbs - CalculateWilgAbs(iIntTemp, 40)) * dKubatura;
            double dWoda60 = -(dWilgAbs - CalculateWilgAbs(iIntTemp, 60)) * dKubatura;

            return p.k.GetLangString("msgWilgInt") + ": " + dWilgInt.ToString("##0") + " %\n" + "ΔH₂0 40 % = " + dWoda40.ToString("####0.00;-####0.00") + " g\n" + "ΔH₂0 60 % = " + dWoda60.ToString("####0.00;-####0.00") + " g\n";
        }

        private DateTime moLastDetails = DateTime.Now;

        private async System.Threading.Tasks.Task ShowDetails(JedenPomiar oItem)
        {
            // zabezpieczenie przed wielokrotnym okienkiem - telefon Barbara52 pokazywał kilka okienek, może to pomoże
            if (moLastDetails.AddMilliseconds(250) > DateTime.Now) return;
            moLastDetails = DateTime.Now;

            string sMsg;

            if ((oItem.sSource ?? "") == "me")
                sMsg = p.k.GetLangString("resCalculated") + "\n";
            else
            {
                if (!string.IsNullOrEmpty(oItem.sId))
                    sMsg = "Sensor from " + oItem.sSource + " (id=" + oItem.sId;
                else
                    sMsg = "Data from " + oItem.sSource;

                if ((oItem.sSource ?? "") == "gios")
                    sMsg = sMsg + ", " + oItem.sAddit;
                if (sMsg.IndexOf("(") > 0)
                    sMsg = sMsg + ")";
                sMsg = sMsg + "\n";

                if (!string.IsNullOrEmpty(oItem.sSensorDescr))
                    sMsg = sMsg + oItem.sSensorDescr + "\n";
                sMsg = sMsg + "\n";

                sMsg = sMsg + oItem.sAdres + "\n";
                if (!string.IsNullOrEmpty(oItem.sOdl))
                {
                    sMsg = sMsg + "Odl: " + oItem.sOdl + "\n";

                    if ((oItem.sSource ?? "") != "DarkSky")
                    {
                        sMsg = sMsg + "(lat: " + oItem.dLat + ", " + "lon: " + oItem.dLon;
                        if (oItem.dWysok > 0)
                            sMsg = sMsg + ",\n";

                        if ((oItem.sSource ?? "") != "SeismicEU")
                            sMsg = sMsg + p.k.GetLangString("resWysokosc") + ": " + oItem.dWysok + " m";
                        else
                            sMsg = sMsg + p.k.GetLangString("resGlebokosc") + ": " + oItem.dWysok + " km";

                        sMsg = sMsg + ")\n";
                    }
                }
            }

            sMsg = sMsg + "\n";
            if (!string.IsNullOrEmpty(oItem.sTimeStamp))
                sMsg = sMsg + "@" + oItem.sTimeStamp + "\n";

            sMsg = sMsg + oItem.sPomiar + ", ";

            if ((oItem.sSource ?? "") != "SeismicEU")
                sMsg = sMsg + "value: ";
            else
                sMsg = sMsg + "max value: ";
            sMsg = sMsg + oItem.dCurrValue + " " + oItem.sUnit;

            if (!string.IsNullOrEmpty(oItem.sAddit) && (oItem.sSource ?? "") != "gios")
                sMsg = sMsg + "\n" + oItem.sAddit;
            // dla gios, sAddit to dodatkowy id, i pokazywany jest wczesniej

            sMsg = sMsg + "\n";
            if (!string.IsNullOrEmpty(oItem.sLimity))
                sMsg = sMsg + "\n" + oItem.sLimity;

            if ((oItem.sPomiar ?? "") == "Humidity")
            {
                string sTmp;
                sTmp = ConvertHumidity(oItem.dCurrValue);
                if (!string.IsNullOrEmpty(sTmp))
                    sMsg = sMsg + "\n" + sTmp;
            }

            //if (await p.k.DialogBoxYN(sMsg, , "OK"))
            //    p.k.ClipPut(sMsg);
            { // kod tutaj - bo trzeci guzik (go map) dla trzęsień ziemi
                Windows.UI.Popups.MessageDialog oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
                string sYes = p.k.GetLangString("msgCopyDetails");
                Windows.UI.Popups.UICommand oClip = new Windows.UI.Popups.UICommand(sYes);
                Windows.UI.Popups.UICommand oOk = new Windows.UI.Popups.UICommand("OK");
                Windows.UI.Popups.UICommand oMap = new Windows.UI.Popups.UICommand("Map");
                uint iDefButt = 1;

                // wersja UWP:Phone ma limit dwu guzików.
                if (p.k.GetPlatform("uwp") && p.k.IsFamilyMobile())
                { }
                else
                    if (oItem.sSource == "SeismicEU" && oItem.sPomiar == "mag") // a nie "magΣ"
                    {
                        oMsg.Commands.Add(oMap);
                        iDefButt = 2;
                    }

                oMsg.Commands.Add(oClip);
                oMsg.Commands.Add(oOk);
                oMsg.DefaultCommandIndex = iDefButt;    // default: No
                oMsg.CancelCommandIndex = iDefButt;
                Windows.UI.Popups.IUICommand oCmd = await oMsg.ShowAsync();
                if (oCmd.Label == sYes) p.k.ClipPut(sMsg);
                if (oCmd.Label == "Map")
                    p.k.OpenBrowser("https://bing.com/maps/default.aspx?lvl=7&cp=" + oItem.dLat.ToString() + "~" + oItem.dLon.ToString());

            }

        }

        private async void uiShowDetails_DClick(object sender, RoutedEventArgs e)
        {// bo zarowno z TextBlock, jak i z Grid moze przyjsc
            Grid oGrid;
            oGrid = sender as Grid;
            if (oGrid != null)
                await ShowDetails(oGrid.DataContext as JedenPomiar);
            else
            {
                TextBlock oTB;
                oTB = sender as TextBlock;
                if (oTB != null)
                    await ShowDetails(oTB.DataContext as JedenPomiar);
            }

        }

        private async void uiDetails_Click(object sender, RoutedEventArgs e)
        {
            // EnviroStatus.JedenPomiar oItem;
            await ShowDetails((sender as MenuFlyoutItem).DataContext as JedenPomiar);
        }


        private async System.Threading.Tasks.Task WczytajDanePunktu(Windows.Devices.Geolocation.BasicGeoposition oPoint)
        {
            // uruchamiamy kazde zrodlo - niech sobie WWW sciaga rownolegle
            p.k.DebugOut("WczytajDanePunktu() started");

            var aoWait = new List<System.Threading.Tasks.Task<Collection<JedenPomiar>>>();

            foreach (EnviroStatus.Source_Base oZrodlo in EnviroStatus.App.gaSrc)
                aoWait.Add(oZrodlo.GetNearest(oPoint));

            p.k.DebugOut("WczytajDanePunktu() got all oWait objects");


            EnviroStatus.App.moPomiaryAll = new Collection<JedenPomiar>();

            // zbieramy rezultaty od zrodel

            Collection<EnviroStatus.JedenPomiar> oPomiary;

            p.k.ProgRingShow(true, false, 0, aoWait.Count);

            p.k.DebugOut("WczytajDanePunktu() rozpoczynam petle");

            foreach (System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> oTask in aoWait)
            {
                p.k.DebugOut("WczytajDanePunktu() kolejny await zaczynam");
                // App.moPomiaryAll.Concat(Await oTask)
                oPomiary = await oTask; // App.moSrc_Airly.GetNearest(oPoint, 10)
                p.k.DebugOut("WczytajDanePunktu() kolejny await udany");

                if (oPomiary is null)
                {
                    // await p.k.DialogBoxAsync("Error from task...");
                }
                else
                {
                    // zmiana na Concat: 2021.02.02
                    //App.moPomiaryAll.Concat(oPomiary);

                    foreach (EnviroStatus.JedenPomiar oPomiar in oPomiary)
                        App.moPomiaryAll.Add(oPomiar);

                    if (App.moPomiaryAll.Count() > 0)
                        uiList.ItemsSource = (from c in EnviroStatus.App.moPomiaryAll
                                              orderby c.sPomiar
                                              where c.bDel == false
                                              select c).ToList();
                }
                p.k.ProgRingInc();
            }

            await KoncowkaPokazywaniaDanych("", false);

            p.k.ProgRingShow(false);

            uiRefresh.IsEnabled = true;

            // uiAdd.Visibility = Visibility.Visible
            uiAdd.IsEnabled = true;

            p.k.DebugOut("WczytajDanePunktu() ended");
        }

        private async void uiGPS_Click(object sender, RoutedEventArgs e)
        {
            p.k.DebugOut("uiGPS_Click() started");
            if (!p.k.NetIsIPavailable(false))
            {
                await p.k.DialogBoxResAsync("errNoNet");
                return;
            }

            uiRefresh.IsEnabled = false;
            UsunCompare("#tegoNieMa#"); // tak naprawde: włącz wszystkie
            App.mbComparing = false;

            p.k.ProgRingShow(true);

            Windows.Devices.Geolocation.BasicGeoposition oPoint;
            oPoint = await App.GetCurrentPoint();
            p.k.DebugOut("uiGPS_Click() got point");
            await WczytajDanePunktu(oPoint);
            p.k.DebugOut("uiGPS_Click() data wczytane");
            p.k.ProgRingShow(false);

            uiTimestamp.Text = "";
        }

        private void uiInfo_Click(object sender, RoutedEventArgs e)
        {
            // przejscie do podstrony
            Frame.Navigate(typeof(Info));
        }

        private void WypelnMenuFavs()
        {
            // wczytaj ze zmniennej liste miejsc i dopisz do menuflyout
            string sLista = p.k.GetSettingsString("favNames");
            var sFavs = sLista.Split('|');

            foreach (string sName in sFavs)
            {
                if (sName.Length > 2)
                {
                    string sUIname = "uiFav" + sName;

                    // nie dodajemy jak juz jest
                    bool bFound = false;
                    foreach (MenuFlyoutItemBase oItem in uiFavMenu.Items) 
                    {
                        if (oItem.Name == sUIname )
                        {
                            bFound = true;
                            break;
                        }
                    }

                    if (!bFound)
                    {
                        var oMFI = new MenuFlyoutItem();
                        oMFI.Name = sUIname;
                        oMFI.Text = sName;
                        oMFI.Click += uiFav_Click;
                        uiFavMenu.Items.Add(oMFI);

                        oMFI = new MenuFlyoutItem();
                        oMFI.Name = "cmp" + sUIname;
                        oMFI.Text = sName;
                        oMFI.Click += uiCompareFav_Click;
                        uiCompareMenu.Items.Add(oMFI);
                    }
                }
            }
        }


        private async void RegisterTrigger(int iMin)
        {
            if (!await p.k.CanRegisterTriggersAsync()) return;

            p.k.UnregisterTriggers("EnviroStat_");

            var oCondit = new Windows.ApplicationModel.Background.SystemCondition(
                        Windows.ApplicationModel.Background.SystemConditionType.InternetAvailable);

            p.k.RegisterTimerTrigger("EnviroStat_Timer", (uint)iMin, false, oCondit);

            //// nie wykorzystuję p.k., bo ma SystemCondition
            //// https://docs.microsoft.com/en-us/windows/uwp/launch-resume/create-And-register-an-inproc-background-task
            //var builder = new Windows.ApplicationModel.Background.BackgroundTaskBuilder();
            //Windows.ApplicationModel.Background.BackgroundTaskRegistration oRet;

            //builder.SetTrigger(new Windows.ApplicationModel.Background.TimeTrigger((uint)iMin, false));
            //builder.AddCondition(
            //    );

            //builder.Name = "EnviroStat_Timer";
            //oRet = builder.Register();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // App.mCurrLang = Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            WypelnMenuFavs();
            p.k.ProgRingInit(true, true);

            EnviroStatus.App.ReadResStrings();    // to, co potrzebne dla BackGround


            if (EnviroStatus.App.moPoint.HasValue )
            {
                // czyli jestesmy po wskazaniu na mapie
                uiRefresh.IsEnabled = false;

                UsunCompare("#tegoNieMa#"); // tak naprawde: włącz wszystkie
                App.mbComparing = false;

                p.k.ProgRingShow(true);
                await WczytajDanePunktu(EnviroStatus.App.moPoint.Value );
                EnviroStatus.App.moPoint = null;
                p.k.ProgRingShow(false);

                uiTimestamp.Text = "";

            }

            if (EnviroStatus.App.moPomiaryAll.Count() < 1)
            {
                string sAutoStart = p.k.GetSettingsString("settingStartPage");
                if (!string.IsNullOrEmpty(sAutoStart) && (sAutoStart.Substring(0, 1) ?? "") != "(")
                    await GetFavData(sAutoStart);
                else
                {
                    // odczytaj plik zawsze - bo moze odczyta plik roaming ustawiany gdzies indziej?
                    var oDTO = await EnviroStatus.App.Cache_Load();
                    if (oDTO.Year > 2010)
                    {
                        uiTimestamp.Text = "(" + oDTO.ToString("d-MM HH:mm") + ")";
                    }
                }
            }
            else
                // są dane w pamięci, skorzystaj z tego - tylko napisz z kiedy to są dane
                if(App.moLastPomiar.HasValue)
                    if (App.moLastPomiar.Value < DateTime.Now.AddMinutes(-10))
                        uiTimestamp.Text = "(" + App.moLastPomiar.Value.ToString("d-MM HH:mm") + ")";

            if (App.moPomiaryAll.Count() > 0)
            {
                uiCompare.IsEnabled = !App.mbComparing;

                uiList.ItemsSource = from c in EnviroStatus.App.moPomiaryAll
                                        orderby c.sPomiar
                                        where c.bDel == false
                                        select c;
            }
            else
                uiCompare.IsEnabled = false;


            // Settings: On = GPS (60 min), Off = lastposition (30 min)
                int iMin = 30;
            if (p.k.GetSettingsBool("settingsLiveClock"))
                iMin = 60;
            RegisterTrigger(iMin);

            if (App.moPomiaryAll.Count() < 1 && !p.k.GetSettingsBool("wasSetup"))
            {
                await p.k.DialogBoxResAsync("msgFirstRunGoSetup");
                Frame.Navigate(typeof(Zrodelka));
            }

        }

        private async void uiStore_Click(object sender, RoutedEventArgs e)
        {
            // uiAdd.Visibility = Visibility.Collapsed
            uiAdd.IsEnabled = false;

            string sTmp = "";
            foreach (EnviroStatus.JedenPomiar oItem in EnviroStatus.App.moPomiaryAll)
            {
                if (!oItem.bDel)
                {
                    // Dim sSensor As String = oItem.sSource & "#" & oItem.sId & "#" & oItem.sAddit & "|"
                    string sSensor = oItem.sSource + "#" + oItem.sId + "#|";
                    if (sTmp.IndexOf(sSensor) < 0)
                        sTmp = sTmp + sSensor;
                }
            }

            if (string.IsNullOrEmpty(sTmp))
            {
                await p.k.DialogBoxAsync("Error: current sensor list is empty");
                return;
            }

            string sName = await p.k.DialogBoxInputResAsync("resNazwa", "", "resSaveFav");
            if (string.IsNullOrEmpty(sName))
                return;
#if __ANDROID__
            if(sName == "pktoasttest")
            {
                p.k.MakeToast("tekst 1", "tekst 2");
                p.k.SetSettingsBool("testTimera", true);
                return;
            }
#endif 

            p.k.SetSettingsString("fav_" + sName, sTmp);
            p.k.SetSettingsString("favgps_" + sName, App.moGpsPoint.Latitude + "|" + App.moGpsPoint.Longitude);

            foreach (EnviroStatus.Source_Base oZrodlo in EnviroStatus.App.gaSrc)
                await oZrodlo.SaveFavTemplate();

            p.k.SetSettingsString("currentFav", sName);

            sTmp = p.k.GetSettingsString("favNames");
            if (sTmp.IndexOf(sName + "|") > -1)
                return;

            sTmp = sTmp + sName + "|";
            p.k.SetSettingsString("favNames", sTmp);

            var oMFI = new MenuFlyoutItem();
            oMFI.Name = "uiFav" + sName;
            oMFI.Text = sName;
            oMFI.Click += uiFav_Click;
            uiFavMenu.Items.Add(oMFI);

            oMFI = new MenuFlyoutItem();
            oMFI.Name = "cmpuiFav" + sName;
            oMFI.Text = sName;
            oMFI.IsEnabled = false;
            oMFI.Click += uiCompareFav_Click;
            uiCompareMenu.Items.Add(oMFI);


        }



        private async System.Threading.Tasks.Task KoncowkaPokazywaniaDanych(string sTitle, bool bInProgress)
        {

            p.k.DebugOut("mainpage:KoncowkaPokazywaniaDanych(" + sTitle + "," + bInProgress.ToString() + ") started");

            if (!bInProgress)
                await EnviroStatus.App.KoncowkaPokazywaniaDanych();
            // App.UpdateTile() - jest juz w App.Koncowka...

            string sTmp = p.k.GetLangString("manifestAppName");
            if (!string.IsNullOrEmpty(sTitle))
                sTmp = sTmp + " - " + sTitle;
            uiTitle.Text = sTitle;

            if (App.moPomiaryAll.Count() < 1)
            {
                if (!bInProgress)
                    await p.k.DialogBoxResAsync("resNoSensorInRange");
            }
            else
            {
                uiList.ItemsSource = from c in EnviroStatus.App.moPomiaryAll
                                     orderby c.sPomiar
                                     where c.bDel == false
                                     select c;

                // jesli cos wczytalismy, to mozemy porownac
                uiCompare.IsEnabled = true;
            }
        }


        private async System.Threading.Tasks.Task GetFavData(string sFavName)
        {
            p.k.SetSettingsString("currentFav", sFavName);

            if(!App.mbComparing) UsunCompare(sFavName);     // nie zmieniaj usuniecia, jesli porownuje

            uiRefresh.IsEnabled = false;
            p.k.ProgRingShow(true);

            App.ZmianaDanych += ZmianaDanychEvent;
            await EnviroStatus.App.GetFavData(sFavName, false);
            App.ZmianaDanych -= ZmianaDanychEvent;

            uiRefresh.IsEnabled = false;

            await KoncowkaPokazywaniaDanych(sFavName, false);

            uiRefresh.IsEnabled = true;
            p.k.ProgRingShow(false);

            // uiAdd.Visibility = Visibility.Collapsed
            uiAdd.IsEnabled = false;
            uiTimestamp.Text = "";
            
        }

        private void UsunCompare(string sFavName)
        {// usun z listy Fav to co jest wczytywane (bez porównywania z samym sobą)
            foreach(MenuFlyoutItemBase oItem in uiCompareMenu.Items)
            {
                oItem.IsEnabled = !(oItem.Name.EndsWith(sFavName));
            }
        }

        private async void ZmianaDanychEvent()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ZmianaDanychEventUI);
        }

        private async void ZmianaDanychEventUI()
        {
            await KoncowkaPokazywaniaDanych("", true);
        }

        private async void uiFav_Click(object sender, RoutedEventArgs e)
        { // fav do pokazania

            if (!p.k.NetIsIPavailable(false))
            {
                await p.k.DialogBoxResAsync("errNoNet");
                return;
            }

            var oMFI = sender as MenuFlyoutItem;
            string sName = oMFI.Name.Replace("uiFav", "");
            App.mbComparing = false;

            await GetFavData(sName);
        }

        private async void uiCompareFav_Click(object sender, RoutedEventArgs e)
        {// fav do porównania
            if (!p.k.NetIsIPavailable(false))
            {
                await p.k.DialogBoxResAsync("errNoNet");
                return;
            }

            var oMFI = sender as MenuFlyoutItem;
            string sName = oMFI.Name.Replace("cmpuiFav", "");
            App.mbComparing = true;

            await GetFavData(sName);
        }


#if NETFX_CORE

        private async void uiAddSecTile_Click(object sender, RoutedEventArgs e)
        {
            // XBox nie ma secondary tile
            if(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily.ToLower().Contains("xbox"))
            {
                await p.k.DialogBoxResAsync("errXBoxNot");
                return;
            }

            EnviroStatus.JedenPomiar oItem;
            oItem = (sender as MenuFlyoutItem).DataContext as JedenPomiar;
            string sName = EnviroStatus.App.GetNameForSecTile(oItem);
            // sName = "alamakota (nawias)"
            var oSTile = new Windows.UI.StartScreen.SecondaryTile(sName, sName, sName, new Uri("ms-appx:///Assets/EmptyTile.png"), Windows.UI.StartScreen.TileSize.Square150x150);
            bool isPinned = await oSTile.RequestCreateAsync();

            if (isPinned)
                EnviroStatus.App.UpdateTile();
        }
#endif

        private void uiMap_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WedleMapy));
        }
    }
}

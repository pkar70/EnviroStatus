
' 2019.12.15 nowe źródło: EEAair (do info: please, nie używaj jednocześnie GIOS i EEAair, będą się pokrywać)
' 2019.12.15 nowe źródło: Quake (trzęsienia ziemi), sumowanie oraz najbliższe
' 2019.12.15 wyszukiwanie punktu wedle mapy (i dla niego pokazywanie danych)
' 2019.12.15 IMGWhyd: gdy nie ma w danych poziomów alertów (null), to wtedy nie wykrzyknikuje

' ver. 4.1912

' 2019.12.04 GIOS już też ma limity
' 2019.12.04 IMGWhydro: jedna, albo wiele rzek

' 2019.10.30 NOAA, SolarWindTemp może być null - wtedy liczy jako zero.
' 2019.10.30 IMGWmeteo, uwzględniona inna postac oJsonSensor jako "null" 
' 2019.10.07 IMGWmeteo, jsonsensor null już bez exception

' ver. 4.1908
' 2019.07.10 poprawka NOAA toast: nie ma powtarzania Toast co drugi Timer
' 2019.07.23 odległość >10 km jest podawana nie w m, ale w km

' ver. 4.1907
' 2019.06.27 poprawka czytania cache (sytuacja: brak pliku w cache)
' 2019.06.29 zmiana ikonki 'Share' na 'Send' (bo: Failed to create a 'Windows.UI.Xaml.Controls.AppBarButton' from the text 'Share'. [Line: 73 Position: 47]')
' 2019.06.29 zapisywanie Cache: niezależnie local i roam
' 2019.06.29 gdy nie ma nic, to zwraca w remsys "(empty)"

' ver. 4.1906
' 2019.05.31 NOAA alert, w getfav, wywolywane zawsze (bo jak nie ma nic, to w Fav nie ma itemka do niego, wiec normalnie by go nie wywolal)
' 2019.06.01 NOAA alert, oraz DarkSky alert - w Toast pokazywane wiecej info (sCurrValue, nie pomiar)
' 2019.06.02 DarkSky, UV index: pokazuje limity (wedle WHO)
' 2019.06.02 RA@H: pokazuje info "24 godzinna" przy (od zawsze podawanej) wartosci sredniej
' 2019.06.02 Settings: Save data cache, zapis po odczycie i odczyt gdy na starcie app nie ma nic w pamieci
' 2019.06.04 zapisywanie pliku roam/local po odczycie danych, i jego odczyt przy starcie jak w pamieci nie ma danych
' 2019.06.04 AppService/RemoteSystems - zwracanie darksky api key oraz danych (uwaga: dlugosc pliku moze byc za duza!)
' 2019.06.04 AppService/RemoteSystems - zwracanie prostego "jest wykrzyknik"
' 2019.06.04 MainPage - gdy wczytane dane z cache, to podaje z kiedy one są
' 2019.06.05 otwieranie z toastu - nie zawisa, tylko pokazuje stronę główną
' 2019.06.05 intimer: już nie powinno wylatywać z errorem (a tak było, gdy np. GIOS zwracał błąd, i próbowało się zrobić DialogBox?)
' 2019.06.06 App:Cache_Load: nowszy plik z roam/local
' 2019.06.11 DarkSky:limity dla UV index, poprawka tekstu: very high od 8, nie od 9 (wykrzyknikowanie było OK)
' 2019.06.11 App:RemSys poprawka odsyłania danych (wcześniej wysyłał puste dane)
' 2019.06.12 DarkSky Alert: %lf » vbCrLf
' 2019.06.13 NOAAalerts: pokazuje te, ktore issue_time nie minęło 24 godzin, max. 5, a nie tylko nowsze niz poprzednim razem
' 2019.06.14 Details: nie MsgBox, a z dwoma guzikami: OK oraz Copy

' ver. 3.1906
' 2019.05.02 Settings: pokazuje numer kompilacji (w Info też jest, ale Info mam nie w kazdej app)
' 2019.05.02 DarkSky:UV index: wykrzyknikowanie wedle WHO 
' 2019.05.17 DarkSky:Alerty: wykrzyknikowanie
' 2019.05.27 Mainpage:Details:DarkSky nie pokazuje lat/long, bo i tak nie są znane
' 2019.05.31 nowe źródło: NOAA solar wind, NOAA K-index, NOAA alerts
' 2019.05.31 przerobienie struktury tak, by było prościej dodawać sourcesy (lista source, bez niezależnych zmiennych dla każdego)

' ver 3.1905
' 2019.03.31 Foreca: nie bylo jej w Settings/Sources
' 2019.03.31 Toasts: błąd w logice (nie działały przy włączonym DarkSky) - App.mResString nie były ustawione, i wylatywało na tworzeniu toastów po skanowaniu w tle, teraz resString brane są z Settings
' 2019.03.31 Toasts: powtarzał (ok), bo po aArr = Split(vbCrLf) były itemami od vbLf, więc na '=' nie trafiało
' 2019.04.06 Zrodelka: scrollbar listy włącz/wyłącz
' 2019.04.08 Source_Base, z którego pozostałe mają Inherits/Overloads (przygotowanie do kolejnych Src)

' VER 3.1904
' 2019.03.24 Foreca: gdy IsThisMoje to z linku krakowskiego, w przeciwnym wypadku - swiatowego
' 2019.03.25 nowe źródło: DarSky
' 2019.03.29 Foreca: nie tylko String, ale i Double z danymi (dla Details page potrzebne)
' 2019.03.30 MainPage: z Fav - pokazywanie stanu po kazdym zrodle



Public NotInheritable Class MainPage
    Inherits Page

    Private Sub uiSetup_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Settings))
    End Sub

    Private Function CalculateWilgAbs(ByVal dTemp As Double, ByVal dWilgWzgl As Double) As Double
        ' https://klimapoint.pl/kalkulator-wilgotnosci-bezwzglednej/
        Return 216.7 * (((dWilgWzgl / 100) * 6.112 * Math.Exp((17.62 * dTemp) / (243.12 + dTemp))) / (273.15 + dTemp))
    End Function

    Private Function ConvertHumidity(dHigroExt As Double) As String
        Dim dKubatura = GetSettingsInt("higroKubatura", 0) / 100.0
        If dKubatura = 0 Then Return ""

        Dim iIntTemp = GetSettingsInt("higroTemp", 22)
        If iIntTemp = 0 Then Return ""

        Dim dExtTemp As Double = -1000

        For Each oItem As JedenPomiar In App.moPomiaryAll
            If oItem.sPomiar.ToLower.IndexOf("temp") > -1 Then
                dExtTemp = oItem.dCurrValue
                Exit For
            End If
        Next

        If dExtTemp = -1000 Then Return ""      ' nie bylo temperatury!

        Dim dWilgAbs As Double = CalculateWilgAbs(dExtTemp, dHigroExt)

        Dim dWilgInt As Double
        ' https://klimapoint.pl/kalkulator-wilgotnosci-bezwzglednej/

        dWilgInt = 100 * (dWilgAbs / 216.7) * (273.15 + iIntTemp) / (6.112 * Math.Exp((17.62 * iIntTemp) / (243.12 + iIntTemp)))

        Dim dWoda40 As Double = -(dWilgAbs - CalculateWilgAbs(iIntTemp, 40)) * dKubatura
        Dim dWoda60 As Double = -(dWilgAbs - CalculateWilgAbs(iIntTemp, 60)) * dKubatura

        Return GetLangString("msgWilgInt") & ": " & dWilgInt.ToString("##0") & " %" & vbCrLf &
            "ΔH₂0 40 % = " & dWoda40.ToString("####0.00;-####0.00") & " g" & vbCrLf &
            "ΔH₂0 60 % = " & dWoda60.ToString("####0.00;-####0.00") & " g" & vbCrLf

    End Function


    Private Async Sub uiDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As JedenPomiar
        oItem = TryCast(sender, MenuFlyoutItem).DataContext
        Dim sMsg As String

        If oItem.sSource = "me" Then
            sMsg = GetLangString("resCalculated") & vbCrLf
        Else
            If oItem.sId <> "" Then
                sMsg = "Sensor from " & oItem.sSource & " (id=" & oItem.sId
            Else
                sMsg = "Data from " & oItem.sSource
            End If

            If oItem.sSource = "gios" Then sMsg = sMsg & ", " & oItem.sAddit
            If sMsg.IndexOf("(") > 0 Then sMsg = sMsg & ")"
            sMsg = sMsg & vbCrLf

            If oItem.sSensorDescr <> "" Then sMsg = sMsg & oItem.sSensorDescr & vbCrLf
            sMsg = sMsg & vbCrLf

            sMsg = sMsg & oItem.sAdres & vbCrLf
            If oItem.sOdl <> "" Then
                sMsg = sMsg & "Odl: " & oItem.sOdl & vbCrLf

                If oItem.sSource <> "DarkSky" Then
                    sMsg = sMsg & "(lat: " & oItem.dLat & ", " & "lon: " & oItem.dLon
                    If oItem.dWysok > 0 Then sMsg = sMsg & "," & vbCrLf

                    If oItem.sSource <> "SeismicEU" Then
                        sMsg = sMsg & GetLangString("resWysokosc") & ": " & oItem.dWysok & " m"
                    Else
                        sMsg = sMsg & GetLangString("resGlebokosc") & ": " & oItem.dWysok & " km"
                    End If

                    sMsg = sMsg & ")" & vbCrLf
                    End If
                End If
        End If

        sMsg = sMsg & vbCrLf
        If oItem.sTimeStamp <> "" Then sMsg = sMsg & "@" & oItem.sTimeStamp & vbCrLf
        If oItem.sSource <> "SeismicEU" Then
            sMsg = sMsg & "Value: "
        Else
            sMsg = sMsg & "Max value: "
        End If
        sMsg = sMsg & oItem.dCurrValue & " " & oItem.sUnit

        If oItem.sAddit <> "" AndAlso oItem.sSource <> "gios" Then sMsg = sMsg & vbCrLf & oItem.sAddit
        ' dla gios, sAddit to dodatkowy id, i pokazywany jest wczesniej

        sMsg = sMsg & vbCrLf
        If oItem.sLimity <> "" Then sMsg = sMsg & vbCrLf & oItem.sLimity

        If oItem.sPomiar = "Humidity" Then
            Dim sTmp As String
            sTmp = ConvertHumidity(oItem.dCurrValue)
            If sTmp <> "" Then sMsg = sMsg & vbCrLf & sTmp
        End If

        If Await DialogBoxYN(sMsg, GetLangString("msgCopyDetails"), "OK") Then ClipPut(sMsg)

    End Sub


    Private Async Function WczytajDanePunktu(oPoint As Point) As Task
        ' uruchamiamy kazde zrodlo - niech sobie WWW sciaga rownolegle

        Dim aoWait As List(Of Task(Of Collection(Of JedenPomiar))) = New List(Of Task(Of Collection(Of JedenPomiar)))

        For Each oZrodlo As Source_Base In App.gaSrc
            aoWait.Add(oZrodlo.GetNearest(oPoint))
        Next


        App.moPomiaryAll = New Collection(Of JedenPomiar)

        ' zbieramy rezultaty od zrodel

        Dim oPomiary As Collection(Of JedenPomiar)

        For Each oTask As Task(Of Collection(Of JedenPomiar)) In aoWait
            'App.moPomiaryAll.Concat(Await oTask)
            oPomiary = Await oTask ' App.moSrc_Airly.GetNearest(oPoint, 10)
            For Each oPomiar As JedenPomiar In oPomiary
                App.moPomiaryAll.Add(oPomiar)
            Next
            If App.moPomiaryAll.Count > 0 Then uiList.ItemsSource = From c In App.moPomiaryAll Order By c.sPomiar Where c.bDel = False
        Next

        Await KoncowkaPokazywaniaDanych("", False)

        uiRefresh.IsEnabled = True

        ' uiAdd.Visibility = Visibility.Visible
        uiAdd.IsEnabled = True

    End Function

    Private Async Sub uiGPS_Click(sender As Object, e As RoutedEventArgs)

        If Not NetIsIPavailable(False) Then
            Await DialogBoxRes("errNoNet")
            Exit Sub
        End If

        uiRefresh.IsEnabled = False

        ProgresywnyRing(True)

        Dim oPoint As Point
        oPoint = Await App.GetCurrentPoint()

        Await WczytajDanePunktu(oPoint)
        ProgresywnyRing(False)

        uiTimestamp.Text = ""

        ' wytnij powtorki, biorac pod uwage tylko te najblizsze dla kazdego typu pomiaru
        ' albo wycinanie tylko na ekran, a pamieta wszystkie pomiary?
        ' i refresh sprawdza te najblizsze (czyli czasem 1 stacja, a nie 5?)

        ' lista pomiarow globalna? i nowy pomiar to usuniecie z danej stacji istniejacych, oraz dopisanie nowych?

    End Sub

    Private Sub uiInfo_Click(sender As Object, e As RoutedEventArgs)
        ' przejscie do podstrony
        Me.Frame.Navigate(GetType(Info))
    End Sub

    Private Sub WypelnMenuFavs()
        ' wczytaj ze zmniennej liste miejsc i dopisz do menuflyout
        Dim sLista As String = GetSettingsString("favNames")
        Dim sFavs As String() = sLista.Split("|")

        For Each sName As String In sFavs
            If sName.Length > 2 Then
                Dim sUIname As String = "uiFav" & sName

                ' nie dodajemy jak juz jest
                Dim bFound As Boolean = False
                For Each oItem In uiFavMenu.Items
                    If oItem.Name = sUIname Then
                        bFound = True
                        Exit For
                    End If
                Next

                If Not bFound Then
                    Dim oMFI As MenuFlyoutItem = New MenuFlyoutItem
                    oMFI.Name = sUIname
                    oMFI.Text = sName
                    AddHandler oMFI.Click, AddressOf uiFav_Click
                    uiFavMenu.Items.Add(oMFI)
                End If

            End If
        Next
    End Sub

    Private Async Sub RegisterTrigger(iMin As Integer)

        Dim oBAS As Background.BackgroundAccessStatus
        oBAS = Await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync()

        If oBAS = Windows.ApplicationModel.Background.BackgroundAccessStatus.AlwaysAllowed OrElse
            oBAS = Windows.ApplicationModel.Background.BackgroundAccessStatus.AllowedSubjectToSystemPolicy Then

            For Each oTask In Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks
                If oTask.Value.Name.IndexOf("EnviroStat_") = 0 Then oTask.Value.Unregister(True)
            Next

            ' https://docs.microsoft.com/en-us/windows/uwp/launch-resume/create-And-register-an-inproc-background-task
            Dim builder As Background.BackgroundTaskBuilder = New Background.BackgroundTaskBuilder
            Dim oRet As Background.BackgroundTaskRegistration

            builder.SetTrigger(New Background.TimeTrigger(iMin, False))
            builder.Name = "EnviroStat_Timer"
            oRet = builder.Register()

            ' bo ikonke przerabiamy po odczytaniu danych w Timer
            'builder.SetTrigger(New Background.SystemTrigger(Windows.ApplicationModel.Background.SystemTriggerType.UserPresent, False))
            'builder.Name = "EnviroStat_UserPresent"
            'oRet = builder.Register()

        End If

    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        ' App.mCurrLang = Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        WypelnMenuFavs()
        App.ReadResStrings()    ' to, co potrzebne dla BackGround

        If App.moPoint <> Nothing Then
            ' czyli jestesmy po wskazaniu na mapie
            uiRefresh.IsEnabled = False

            ProgresywnyRing(True)
            Await WczytajDanePunktu(App.moPoint)
            App.moPoint = Nothing
            ProgresywnyRing(False)

            uiTimestamp.Text = ""

        Else


            If App.moPomiaryAll.Count < 1 Then
                Dim sAutoStart As String = GetSettingsString("settingStartPage")
                If sAutoStart <> "" AndAlso sAutoStart.Substring(0, 1) <> "(" Then
                    Await GetFavData(sAutoStart)
                Else
                    ' odczytaj plik zawsze - bo moze odczyta plik roaming ustawiany gdzies indziej?
                    Dim oDTO As DateTimeOffset = Await App.Cache_Load
                    If oDTO.Year > 2010 Then
                        uiTimestamp.Text = "(" & oDTO.ToString("d-MM HH:mm") & ")"
                    End If
                End If
            Else
                ' są dane w pamięci, skorzystaj z tego - tylko napisz z kiedy to są dane
                If App.moLastPomiar < Date.Now.AddMinutes(-10) Then
                    uiTimestamp.Text = "(" & App.moLastPomiar.ToString("d-MM HH:mm") & ")"
                End If
            End If

            If App.moPomiaryAll.Count > 0 Then
                uiList.ItemsSource = From c In App.moPomiaryAll Order By c.sPomiar Where c.bDel = False
            End If
        End If


        ' Settings: On = GPS (60 min), Off = lastposition (30 min)
        Dim iMin As Integer = 30
        If GetSettingsBool("settingsLiveClock") Then iMin = 60
        RegisterTrigger(iMin)
        'tbModif.Text = "Build " & Package.Current.Id.Version.Major & "." &
        '    Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build

        'If GetSettingsBool("settingsDelToastOnOpen") Then
        '    Dim oTCM1 = Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier
        '    oTCM1.
        '    Dim oTCM As Windows.UI.Notifications.ToastCollectionManager = Windows.UI.Notifications.ToastNotification
        '    Await oTCM.RemoveAllToastCollectionsAsync
        'End If

    End Sub

    Private Async Sub uiStore_Click(sender As Object, e As RoutedEventArgs)
        'uiAdd.Visibility = Visibility.Collapsed
        uiAdd.IsEnabled = False

        Dim sTmp As String = ""
        For Each oItem As JedenPomiar In App.moPomiaryAll
            If Not oItem.bDel Then
                ' Dim sSensor As String = oItem.sSource & "#" & oItem.sId & "#" & oItem.sAddit & "|"
                Dim sSensor As String = oItem.sSource & "#" & oItem.sId & "#|"
                If sTmp.IndexOf(sSensor) < 0 Then sTmp = sTmp & sSensor
            End If
        Next

        If sTmp = "" Then
            Await DialogBox("Error: current sensor list is empty")
            Exit Sub
        End If

        Dim sName As String = Await DialogBoxInput("resNazwa", "", "resSaveFav")
        If sName = "" Then Exit Sub

        SetSettingsString("fav_" & sName, sTmp)
        SetSettingsString("favgps_" & sName, App.moGpsPoint.X & "|" & App.moGpsPoint.Y)

        For Each oZrodlo As Source_Base In App.gaSrc
            Await oZrodlo.SaveFavTemplate()
        Next

        sTmp = GetSettingsString("favNames")
        If sTmp.IndexOf(sName & "|") > -1 Then Exit Sub

        sTmp = sTmp & sName & "|"
        SetSettingsString("favNames", sTmp)

        Dim oMFI As MenuFlyoutItem = New MenuFlyoutItem
        oMFI.Name = "uiFav" & sName
        oMFI.Text = sName
        AddHandler oMFI.Click, AddressOf uiFav_Click
        uiFavMenu.Items.Add(oMFI)

    End Sub



    Private Async Function KoncowkaPokazywaniaDanych(sTitle As String, bInProgress As Boolean) As Task

        If Not bInProgress Then Await App.KoncowkaPokazywaniaDanych()
        ' App.UpdateTile() - jest juz w App.Koncowka...

        Dim sTmp As String = GetLangString("manifestAppName")
        If sTitle <> "" Then sTmp = sTmp & " - " & sTitle
        uiTitle.Text = sTitle

        If App.moPomiaryAll.Count < 1 Then
            If Not bInProgress Then Await DialogBoxRes("resNoSensorInRange")
        Else
            uiList.ItemsSource = From c In App.moPomiaryAll Order By c.sPomiar Where c.bDel = False
        End If


    End Function

    Private Sub ProgresywnyRing(sStart As Boolean)
        If sStart Then
            Dim dVal As Double
            dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
            uiProcesuje.Width = dVal
            uiProcesuje.Height = dVal

            uiProcesuje.Visibility = Visibility.Visible
            uiProcesuje.IsActive = True
        Else
            uiProcesuje.IsActive = False
            uiProcesuje.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Async Function GetFavData(sFavName As String) As Task

        SetSettingsString("currentFav", sFavName)

        uiRefresh.IsEnabled = False
        ProgresywnyRing(True)

        AddHandler App.ZmianaDanych, AddressOf ZmianaDanychEvent
        Await App.GetFavData(sFavName, False)
        RemoveHandler App.ZmianaDanych, AddressOf ZmianaDanychEvent

        uiRefresh.IsEnabled = False

        Await KoncowkaPokazywaniaDanych(sFavName, False)

        uiRefresh.IsEnabled = True
        ProgresywnyRing(False)

        'uiAdd.Visibility = Visibility.Collapsed
        uiAdd.IsEnabled = False
        uiTimestamp.Text = ""
    End Function

    Private Async Sub ZmianaDanychEvent()
        Await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, AddressOf ZmianaDanychEventUI)
    End Sub

    Private Async Sub ZmianaDanychEventUI()
        Await KoncowkaPokazywaniaDanych("", True)
    End Sub

    Private Async Sub uiFav_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        Dim sName As String = oMFI.Name.Replace("uiFav", "")

        If Not NetIsIPavailable(False) Then
            Await DialogBoxRes("errNoNet")
            Exit Sub
        End If
        Await GetFavData(sName)
    End Sub


    Private Async Sub uiAddSecTile_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As JedenPomiar
        oItem = TryCast(sender, MenuFlyoutItem).DataContext
        Dim sName As String = App.GetNameForSecTile(oItem)
        'sName = "alamakota (nawias)"
        Dim oSTile As Windows.UI.StartScreen.SecondaryTile =
            New Windows.UI.StartScreen.SecondaryTile(sName, sName, sName, New Uri("ms-appx:///Assets/EmptyTile.png"), Windows.UI.StartScreen.TileSize.Square150x150)
        Dim isPinned As Boolean = Await oSTile.RequestCreateAsync()

        If isPinned Then App.UpdateTile()
    End Sub

    Private Sub uiMap_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(WedleMapy))
    End Sub
End Class

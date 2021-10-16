''' <summary>
''' Provides application-specific behavior to supplement the default Application class.
''' </summary>
Partial NotInheritable Class App
    Inherits Application

    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>
    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()

            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed
            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

            If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
                ' TODO: Load state from previously suspended application
            End If
            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If

        If e.PrelaunchActivated = False Then
            If rootFrame.Content Is Nothing Then
                ' When the navigation stack isn't restored navigate to the first page,
                ' configuring the new page by passing required information as a navigation
                ' parameter
                rootFrame.Navigate(GetType(MainPage), e.Arguments)
            End If

            ' Ensure the current window is active
            Window.Current.Activate()
        End If
    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub

    ' wedle https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/send-local-toast
    ' foreground activation

    Protected Overrides Sub OnActivated(e As IActivatedEventArgs)
        Dim rootFrame As Frame
        rootFrame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()

            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed
            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If

        If rootFrame.Content Is Nothing Then
            'MakeDebugToast("OnActivated - OPEN NULL")
            rootFrame.Navigate(GetType(MainPage))
        End If

        Window.Current.Activate()
    End Sub

    Public Shared moGpsPoint As Point
#Region "GPS"

    Public Shared Async Function GetCurrentPoint() As Task(Of Point)
        ' Dim oPoint As Point

        ' udajemy GPSa
        If GetSettingsBool("simulateGPS") Then Return moGpsPoint

        ' na pewno ma byc wedle GPS

        moGpsPoint.X = 50.0 '1985 ' latitude - dane domku, choc mała precyzja
        moGpsPoint.Y = 19.9 '7872

        Dim rVal As Windows.Devices.Geolocation.GeolocationAccessStatus = Await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync()
        If rVal <> Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed Then
            'If Not GetSettingsBool("noGPSshown") Then
            Await DialogBoxRes("resErrorNoGPSAllowed")
            '    SetSettingsBool("noGPSshown", True)
            'End If
            Return moGpsPoint
        End If

        Dim oDevGPS As Windows.Devices.Geolocation.Geolocator = New Windows.Devices.Geolocation.Geolocator()

        Dim oPos As Windows.Devices.Geolocation.Geoposition
        ' oDevGPS.DesiredAccuracyInMeters = GetSettingsInt("gpsPrec", 75) ' dla 4 km/h; 100 m = 90 sec, 75 m = 67 sec
        Dim oCacheTime As TimeSpan = New TimeSpan(0, 2, 0)  ' 2 minuty 
        Dim oTimeout As TimeSpan = New TimeSpan(0, 0, 5)    ' timeout 
        Dim bErr As Boolean = False
        Try
            oPos = Await oDevGPS.GetGeopositionAsync(oCacheTime, oTimeout)
            moGpsPoint.X = oPos.Coordinate.Point.Position.Latitude
            moGpsPoint.Y = oPos.Coordinate.Point.Position.Longitude

        Catch ex As Exception   ' zapewne timeout
            bErr = True
        End Try
        If bErr Then
            ' po tym wyskakuje później z błędem, więc może oPoint jest zepsute?
            ' dodaję zarówno ustalenie oPoint i mSpeed na defaulty, jak i Speed.HasValue
            Await DialogBoxRes("resErrorGettingPos")

            moGpsPoint.X = 50.0 '1985 ' latitude - dane domku, choc mała precyzja
            moGpsPoint.Y = 19.9 '7872
            'mSpeed = GetSettingsInt("walkSpeed", 4)
        End If

        Return moGpsPoint

    End Function


#End Region

    Public Shared Async Function GetDataFile(bRoam As Boolean, sName As String, bCreate As Boolean) As Task(Of Windows.Storage.StorageFile)
        Dim oFold As Windows.Storage.StorageFolder
        If bRoam Then
            oFold = Windows.Storage.ApplicationData.Current.RoamingFolder
        Else
            oFold = Windows.Storage.ApplicationData.Current.LocalFolder
        End If

        If oFold Is Nothing Then
            Await DialogBoxRes("errNoRoamFolder")
            Return Nothing
        End If

        Dim bErr As Boolean = False
        Dim oFile As Windows.Storage.StorageFile = Nothing
        Try
            If bCreate Then
                'oFile = Await oFold.TryGetItemAsync(sName)
                'If oFile IsNot Nothing Then Await oFile.DeleteAsync
                oFile = Await oFold.CreateFileAsync(sName, Windows.Storage.CreationCollisionOption.ReplaceExisting)
            Else
                oFile = Await oFold.TryGetItemAsync(sName)
            End If
        Catch ex As Exception
            bErr = True
        End Try

        If oFile Is Nothing Then bErr = True
        If bErr Then
            Return Nothing
        End If

        Return oFile
    End Function

    Public Shared Async Function TryDataLog() As Task
        If Not GetSettingsBool("settingsDataLog") Then Exit Function

        Try
            Dim externalDevices As Windows.Storage.StorageFolder = Windows.Storage.KnownFolders.RemovableDevices
            Dim oCards As IReadOnlyList(Of Windows.Storage.StorageFolder) = Await externalDevices.GetFoldersAsync()
            Dim sdCard As Windows.Storage.StorageFolder = oCards.FirstOrDefault()
            If sdCard Is Nothing Then Exit Function

            Dim oFolder As Windows.Storage.StorageFolder = Await sdCard.CreateFolderAsync("DataLogs", Windows.Storage.CreationCollisionOption.OpenIfExists)
            If oFolder Is Nothing Then Exit Function

            oFolder = Await oFolder.CreateFolderAsync("Smogometr", Windows.Storage.CreationCollisionOption.OpenIfExists)
            If oFolder Is Nothing Then Exit Function

            oFolder = Await oFolder.CreateFolderAsync(Date.Now.ToString("yyyy"), Windows.Storage.CreationCollisionOption.OpenIfExists)
            If oFolder Is Nothing Then Exit Function

            oFolder = Await oFolder.CreateFolderAsync(Date.Now.ToString("MM"), Windows.Storage.CreationCollisionOption.OpenIfExists)
            If oFolder Is Nothing Then Exit Function

            Dim sFileName As String = Date.Now.ToString("yyyy.MM.dd.HH.mm") & ".xml"
            Dim oFile As Windows.Storage.StorageFile =
                Await oFolder.CreateFileAsync(sFileName, Windows.Storage.CreationCollisionOption.OpenIfExists)

            Dim oSer As Xml.Serialization.XmlSerializer =
                        New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenPomiar)))

            Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
            oSer.Serialize(oStream, App.moPomiaryAll)
            oStream.Dispose()   ' == fclose

        Catch ex As Exception

        End Try

    End Function

    Private Shared Sub UsunPowtorki()
        For i0 As Integer = 0 To App.moPomiaryAll.Count - 1
            Dim oPomiar As JedenPomiar = App.moPomiaryAll.Item(i0)
            For i1 As Integer = 0 To App.moPomiaryAll.Count - 1
                If i0 <> i1 Then
                    Dim oPomiar1 As JedenPomiar = App.moPomiaryAll.Item(i1)
                    ' na pozniej >=, ale na razie trzeba wychwycic czemu rosnie
                    If oPomiar1.sSource = oPomiar.sSource AndAlso
                            oPomiar1.sPomiar = oPomiar.sPomiar AndAlso
                            oPomiar1.dOdl > oPomiar.dOdl Then
                        oPomiar1.bDel = True
                    End If
                End If
            Next
        Next
    End Sub

    ' Private Shared msLastToast As String = ""

    Private Shared Sub ZrobToasty()
        ' a teraz toasty
        Dim sToastSett As String = GetSettingsString("settingsAlerts")
        Dim iInd As Integer
        iInd = sToastSett.IndexOf("(!")
        If iInd < 0 Then Exit Sub
        sToastSett = sToastSett.Substring(iInd + 1).Replace(")", "")
        ' sToastSett = !|!!|!!!
        Dim sLastToast As String = GetSettingsString("lastToast")

        'If sToastSett.IndexOf("!") > 0 Then iToastMode = 1
        'If sToastSett.IndexOf("!!") > 0 Then iToastMode = 2
        'If sToastSett.IndexOf("!!!") > 0 Then iToastMode = 3
        'If iToastMode = 0 Then Exit Function

        Dim sToastMsg As String = ""
        Dim sToastMemory As String = ""

        Debug.WriteLine("Poprzednie alerty: " & sLastToast)

        Dim aLastAlerts As String() = sLastToast.Replace(vbCrLf, vbCr).Trim.Split(vbCr)

        For Each oItem As JedenPomiar In App.moPomiaryAll

            If oItem.bDel Then Continue For

            Dim sAlertTmp As String = oItem.sAlert
            If sAlertTmp.Length < sToastSett.Length Then sAlertTmp = ""             ' !!

            Dim sOneParam As String = oItem.sPomiar & " (" & oItem.sSource & ")"    ' PM10 (Airly)

            If oItem.sSource = "NOAAalert" Then
                ' dla DarkSky toast, oraz NOAAalert, ma pokazac pelniejsze info
                ' toastMemory - nie zapisujemy, bo i tak nie odczyta drugi raz tego samego
                ' tylko do wyswietlenia podaje wiecej
                Dim sTmp As String
                sTmp = oItem.sAlert & " " & oItem.sCurrValue & " (" & oItem.sSource & ")" & vbCrLf
                sToastMemory = sToastMemory & sTmp
                If Not sLastToast.Contains(oItem.sCurrValue) Then sToastMsg = sToastMsg & sTmp
            Else

                Dim sOneParamAlert As String = sAlertTmp & " " & sOneParam              ' !! PM10 (Airly)
                ' (a) dokladnie to samo bylo wczesniej

                Debug.WriteLine(" analiza aktualnego: " & sOneParamAlert)

                Dim iPoprzedniStatus As Integer = 0
                For Each sPrevAlert As String In aLastAlerts
                    Debug.WriteLine("  poprzedni wpis: " & sPrevAlert)
                    If sPrevAlert.Trim = sOneParamAlert.Trim Then    ' .Trim, bo vbLf & "! PM10 (airly)
                        iPoprzedniStatus = 1
                        Debug.WriteLine("- byl taki sam")
                        Exit For
                    ElseIf sPrevAlert.Contains(sOneParamAlert) Then
                        iPoprzedniStatus = 2
                        Debug.WriteLine("- byl krotszy")
                        Exit For
                    ElseIf sPrevAlert.Contains(sOneParam) Then
                        iPoprzedniStatus = 3
                        Debug.WriteLine("- byl dluzszy")
                        Exit For
                    End If
                Next

                Select Case iPoprzedniStatus
                    Case 0, 3  ' nie bylo, bądź było mniejsze
                        If sAlertTmp <> "" Then
                            If oItem.sPomiar.StartsWith("Alert") AndAlso oItem.sSource = "DarkSky" Then
                                sToastMsg = sToastMsg & oItem.sAlert & " " & oItem.sCurrValue & " (" & oItem.sSource & ")" & vbCrLf
                            Else
                                sToastMsg = sToastMsg & sOneParamAlert & vbCrLf
                            End If
                            sToastMemory = sToastMemory & sOneParamAlert & vbCrLf
                        End If
                    Case 1  ' bylo takie samo
                        ' If na wypadek gdy błąd
                        If sAlertTmp <> "" Then sToastMemory = sToastMemory & sOneParamAlert & vbCrLf
                    Case 2  ' bylo wieksze
                        If sAlertTmp <> "" Then
                            sToastMemory = sToastMemory & sOneParamAlert & vbCrLf
                        Else
                            sToastMsg = sToastMsg & "(ok) " & sOneParam & vbCrLf
                        End If
                End Select
            End If

        Next

        Debug.WriteLine("nowy toastmemory" & sToastMemory)
        Debug.WriteLine("toast string" & sToastMsg)

        SetSettingsString("lastToast", sToastMemory)
        If sToastMemory = "" Then
            If sLastToast = "" Then Exit Sub

            sToastMsg = GetSettingsString("resAllOk") ' GetLangString("msgAllOk")
        End If

        If sToastMsg <> "" Then MakeToast(sToastMsg)

    End Sub

    Public Shared Async Function KoncowkaPokazywaniaDanych() As Task

        App.DodajPrzekroczenia()
        UsunPowtorki()
        'MakeToast("po UsunPowtorki")
        DodajTempOdczuwana()
        'MakeToast("po Tapp")

        Await App.Cache_Save()
        moLastPomiar = Date.Now

        Await App.TryDataLog
        'MakeToast("po datalog")
        UpdateTile()
        'MakeToast("po tile")
    End Function

    Private Shared Function GetTileXml(sPomiar As String, dValue As Double) As String
        Dim sTmp As String
        sTmp = "<tile><visual>"
        sTmp = sTmp & "<binding template='TileSmall' branding='none' hint-textStacking='center' >"
        sTmp = sTmp & "<text hint-style='title' hint-align='center'>" & dValue.ToString("###0") & "</text>"
        sTmp = sTmp & "<text hint-style='caption' hint-align='center'>" & sPomiar & "</text>"
        sTmp = sTmp & "</binding>"

        sTmp = sTmp & "<binding template='TileMedium' hint-textStacking='center'>"
        sTmp = sTmp & "<text hint-style='title' hint-align='center'>" & dValue.ToString("###0") & "</text>"
        sTmp = sTmp & "<text hint-style='caption' hint-align='center'>" & sPomiar & "</text>"
        sTmp = sTmp & "</binding>"

        sTmp = sTmp & "</visual></tile>"

        Return sTmp
    End Function

    Private Shared Function GetTileObject(sPomiar As String, dValue As Double) As Windows.UI.Notifications.TileNotification
        Dim oTile As Windows.UI.Notifications.TileNotification = Nothing

        Try
            Dim sXml As String = GetTileXml(sPomiar, dValue)
            Dim oXml As Windows.Data.Xml.Dom.XmlDocument = New Windows.Data.Xml.Dom.XmlDocument
            oXml.LoadXml(sXml)
            oTile = New Windows.UI.Notifications.TileNotification(oXml)
            oTile.ExpirationTime = Date.Now.AddHours(2)
        Catch ex As Exception

        End Try

        Return oTile
    End Function

    Private Shared Function GetEmptyTileXml(sPomiar As String, sSource As String) As String
        Dim sTmp As String
        sTmp = "<tile><visual>"
        sTmp = sTmp & "<binding template='TileSmall' branding='none' hint-textStacking='center' >"
        sTmp = sTmp & "<text hint-style='title' hint-align='center'>" & sPomiar & "</text>"
        sTmp = sTmp & "<text hint-style='caption' hint-align='center'>" & sSource & "</text>"
        sTmp = sTmp & "</binding>"

        sTmp = sTmp & "<binding template='TileMedium' hint-textStacking='center'>"
        sTmp = sTmp & "<text hint-style='title' hint-align='center'>" & sPomiar & "</text>"
        sTmp = sTmp & "<text hint-style='caption' hint-align='center'>" & sSource & "</text>"
        sTmp = sTmp & "</binding>"

        sTmp = sTmp & "</visual></tile>"

        Return sTmp
    End Function

    Private Shared Function GetEmptyTileObject(sPomiar As String, sSource As String) As Windows.UI.Notifications.ScheduledTileNotification
        Dim oTile As Windows.UI.Notifications.ScheduledTileNotification = Nothing
        Try
            Dim sXml As String = GetEmptyTileXml(sPomiar, sSource)
            Dim oXml As Windows.Data.Xml.Dom.XmlDocument = New Windows.Data.Xml.Dom.XmlDocument
            oXml.LoadXml(sXml)
            oTile = New Windows.UI.Notifications.ScheduledTileNotification(oXml, Date.Now.AddHours(2))
        Catch ex As Exception

        End Try

        Return oTile
    End Function

    Public Shared Function GetNameForSecTile(oPomiar As JedenPomiar) As String
        ' stąd, oraz z mainpage. Nie może być spacji w nazwie!
        Dim sName As String
        sName = oPomiar.sPomiar & "(" & oPomiar.sSource & ")"
        sName = sName.Replace(" ", "_") ' rzeka_cm
        Return sName
    End Function


    Public Shared Sub UpdateTile()
        ' *TODO* mozna sie pobawic jeszcze w kolorki:
        ' SecondaryTileVisualElements.BackgroundColor


        Try     ' jesli sie cos nie uda, to zignoruj robienie Tile

            Dim sReqPomiar As String = GetSettingsString("settingsLiveTile")
            Dim oTile As Windows.UI.Notifications.TileNotification
            Dim oTileEmpty As Windows.UI.Notifications.ScheduledTileNotification


            For Each oPomiar As JedenPomiar In App.moPomiaryAll

                ' przygotowanie zawartości Tile
                oTile = GetTileObject(oPomiar.sPomiar, oPomiar.dCurrValue)
                If oTile Is Nothing Then Exit For
                oTileEmpty = GetEmptyTileObject(oPomiar.sPomiar, oPomiar.sSource)

                Dim oTUPS As Windows.UI.Notifications.TileUpdater
                ' jeśli nie ma empty, a jest oTile, to mozna sprobowac primary empty - dlatego nie exit
                If oTileEmpty IsNot Nothing Then
                    Dim sName As String = App.GetNameForSecTile(oPomiar)

                    If Windows.UI.StartScreen.SecondaryTile.Exists(sName) Then
                        Try
                            oTUPS = Windows.UI.Notifications.TileUpdateManager.CreateTileUpdaterForSecondaryTile(sName)
                        Catch ex As Exception
                            oTUPS = Nothing
                        End Try

                        If oTUPS IsNot Nothing Then
                            ' próba ustawienia secondary Tile
                            oTUPS.Clear()

                            oTUPS.Update(oTile)
                            oTUPS.AddToSchedule(oTileEmpty)
                        End If
                    End If
                End If

                ' próba ustawienia primary Tile
                If sReqPomiar = oPomiar.sPomiar & " (" & oPomiar.sSource & ")" Then
                    oTUPS = Windows.UI.Notifications.TileUpdateManager.CreateTileUpdaterForApplication
                    oTUPS.Update(oTile)
                    ' Exit For     ' bo tylko jeden pomiar, pierwszy ktory trafi 
                End If
            Next

        Catch ex As Exception

        End Try

    End Sub

    Private moTaskDeferal As Background.BackgroundTaskDeferral = Nothing
    Private moAppConn As AppService.AppServiceConnection


    Protected Overrides Async Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)
        ' tile update / warnings
        Dim oTimerDeferal As Background.BackgroundTaskDeferral
        oTimerDeferal = args.TaskInstance.GetDeferral()

        Select Case args.TaskInstance.Task.Name
            Case "EnviroStat_Timer"
                If Not NetIsIPavailable(False) Then Return

                Dim sFavName As String = GetSettingsString("settingStartPage")
                sFavName = GetSettingsString("currentFav", sFavName)

                If sFavName <> "" Then
                    Await GetFavData(sFavName, True)
                    'MakeToast("po GetFavData")
                    Await KoncowkaPokazywaniaDanych()
                    'MakeToast("po Koncowka")
                    ZrobToasty()

                End If
            Case "EnviroStat_UserPresent"
                ' UpdateTile()
            Case Else
                Dim oDetails As AppService.AppServiceTriggerDetails =
            TryCast(args.TaskInstance.TriggerDetails, AppService.AppServiceTriggerDetails)
                If oDetails IsNot Nothing Then
                    ' zrob co trzeba
                    moTaskDeferal = args.TaskInstance.GetDeferral()
                    AddHandler args.TaskInstance.Canceled, AddressOf OnTaskCanceled
                    moAppConn = oDetails.AppServiceConnection
                    AddHandler moAppConn.RequestReceived, AddressOf OnRequestReceived
                    ' AddHandler moAppConn.ServiceClosed, AddressOf OnServiceClosed
                End If

        End Select


        oTimerDeferal.Complete()

    End Sub

    Private Sub OnTaskCanceled(sender As Background.IBackgroundTaskInstance, reason As Background.BackgroundTaskCancellationReason)
        If moTaskDeferal IsNot Nothing Then
            moTaskDeferal.Complete()
            moTaskDeferal = Nothing
        End If
        'If oAppConn IsNot Nothing Then
        '    oAppConn.Dispose()
        '    oAppConn = Nothing
        'End If
    End Sub

    Private Async Sub OnRequestReceived(sender As AppService.AppServiceConnection, args As AppService.AppServiceRequestReceivedEventArgs)
        'Get a deferral so we can use an awaitable API to respond to the message 
        Dim messageDeferral As AppService.AppServiceDeferral = args.GetDeferral()
        Dim oInputMsg As ValueSet = args.Request.Message
        Dim oResultMsg As ValueSet = New ValueSet()
        Dim sResult As String = "ERROR while processing command"
        Try
            Dim sCommand As String = CType(oInputMsg("command"), String)

            Select Case sCommand.ToLower
                Case "ping"
                    sResult = "pong" & vbCrLf &
                        Package.Current.Id.Version.Major & "." &
                            Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build
                    If Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile Then
                        sResult = sResult & vbCrLf & "WIFI"
                    Else
                        sResult = sResult & vbCrLf & "OTHER"
                    End If
                Case "net"
                    If Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile Then
                        sResult = "WIFI"
                    Else
                        sResult = "OTHER"
                    End If
                Case "ver"
                    sResult = Package.Current.Id.Version.Major & "." &
                        Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build
                Case "apikey"
                    If Not GetSettingsBool("settingsRemSysAPI") Then
                        sResult = "ERROR: no permission"
                    Else
                        sResult = "OK"
                        oResultMsg.Add("key", CType(GetSettingsString("sourceDarkSky_apikey"), String))
                    End If
                Case "datacache"
                    If Not GetSettingsBool("settingsRemSysData") Then
                        sResult = "ERROR: no permission"
                    Else

                        ' zawsze odczyt - bo jesli to tylko posrednik, to w pamieci ma stare dane, a plik moze byc nowszy
                        Await Cache_Load()

                        If moPomiaryAll.Count < 1 Then
                            sResult = "ERROR: empty data?"
                        Else
                            ' wyslij z pamieci
                            Dim oSer As Xml.Serialization.XmlSerializer
                            oSer = New Xml.Serialization.XmlSerializer(GetType(JedenPomiar))
                            Dim oStream As Stream = New MemoryStream
                            oSer.Serialize(oStream, App.moPomiaryAll)
                            oStream.Flush()
                            Dim oRdr As StreamReader = New StreamReader(oStream)
                            Dim sTmp As String = oRdr.ReadToEnd
                            If sTmp.Length > 28000 Then
                                sResult = "ERROR: too much data"
                            Else
                                oResultMsg.Add("data", CType(sTmp, String))
                                sResult = "OK"
                            End If
                        End If

                    End If
                Case "envirostatus"
                    If Not GetSettingsBool("settingsRemSysData") Then
                        sResult = "ERROR: no permission"
                    Else

                        ' zawsze odczyt - bo jesli to tylko posrednik, to w pamieci ma stare dane, a plik moze byc nowszy
                        Await Cache_Load()

                        If moPomiaryAll.Count < 1 Then
                            sResult = "ERROR: empty data?"
                        Else
                            Dim sStatus As String = ""
                            For Each oPomiar As JedenPomiar In App.moPomiaryAll
                                If oPomiar.sAlert.Length > sStatus.Length Then sStatus = oPomiar.sAlert
                            Next                            ' wyslij z pamieci
                            oResultMsg.Add("status", CType(sStatus, String))
                            sResult = "OK"
                        End If
                    End If
                Case "alerts"
                    ' odpowiednik msgboxu
                    Dim sToastMsg As String = ""
                    Dim sLevel As String = "!"
                    Try
                        sLevel = CType(oInputMsg("level"), String)
                    Catch ex As Exception
                    End Try
                    For Each oItem As JedenPomiar In App.moPomiaryAll
                        If oItem.sAlert.Length >= sLevel.Length Then
                            ' fragmenty zabrane z ZrobToast, ale tu mamy (byc moze) inny poziom alertowania
                            If oItem.sSource = "NOAAalert" Then
                                sToastMsg = sToastMsg & oItem.sAlert & " " & oItem.sCurrValue & " (" & oItem.sSource & ")" & vbCrLf
                            Else
                                If oItem.sPomiar.StartsWith("Alert") AndAlso oItem.sSource = "DarkSky" Then
                                    sToastMsg = sToastMsg & oItem.sAlert & " " & oItem.sCurrValue & " (" & oItem.sSource & ")"
                                Else
                                    Dim sOneParam As String = oItem.sPomiar & " (" & oItem.sSource & ")"    ' PM10 (Airly)
                                    Dim sOneParamAlert As String = oItem.sAlert & " " & sOneParam
                                    sToastMsg = sToastMsg & sOneParamAlert & vbCrLf
                                End If
                            End If
                        End If
                    Next
                    If sToastMsg = "" Then sToastMsg = "(empty)"
                    oResultMsg.Add("alerty", CType(sToastMsg, String))
                    sResult = "OK"

                Case Else
                    sResult = "ERROR unknown command"

            End Select
        Catch ex As Exception

        End Try

        ' odsylamy cokolwiek - zeby "tamta strona" cos zobaczyla
        oResultMsg.Add("result", CType(sResult, String))
        Await args.Request.SendResponseAsync(oResultMsg)

        messageDeferral.Complete()
    End Sub


    Private Shared Function Wykrzyknikuj(dCurrent As Double, dJeden As Double, dDwa As Double, dTrzy As Double) As String
        If dCurrent < dJeden Then Return ""
        If dCurrent < dDwa Then Return "!"
        If dCurrent < dTrzy Then Return "!!"
        Return "!!!"
    End Function

    Public Shared Function PoziomDopuszczalnyPL(sPomiar As String) As String
        ' http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
        Select Case sPomiar
            Case "PM₂₅"
                Return "Poziom dopuszalny (średnia roczna): 25 μg/m³ od 2015, 20 μg/m³ od 2020" & vbCrLf
            Case "PM₁₀"
                Return "Poziom dopuszalny (od 2005): średnia roczna 40 μg/m³, dobowa 50 μg/m³" & vbCrLf
            Case "C₆H₆"
                Return "Poziom dopuszalny (średnia roczna): 5 μg/m³, od 2010" & vbCrLf
            Case "NO₂"
                Return "Poziom dopuszalny (od 2010): 40 μg/m³ średnia roczna, 200 μg/m³ dobowa" & vbCrLf
            Case "NOx"
                Return "Poziom dopuszalny (średnia roczna): 30 μg/m³ od 2003" & vbCrLf
            Case "SO₂"
                Return "Poziom dopuszalny: 125 μg/m³ (średnia dobowa), 350 μg/m³ (godzinna), od 2005" & vbCrLf
            Case "Pb"
                Return "Poziom dopuszalny (średnia roczna): 0.5 μg/m³, od 2005" & vbCrLf
            Case "CO"
                Return "Poziom dopuszalny (średnia 8 godzinna): 10 000 μg/m³, od 2005" & vbCrLf
        End Select
        Return ""
    End Function

    Public Shared Function PoziomDocelowyPL(sPomiar As String) As String
        ' http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
        Select Case sPomiar
            Case "As"
                Return "Poziom docelowy (do 2013): 6 ng/m³ (średnia roczna)" & vbCrLf
            Case "benzoapiren"
                Return "Poziom docelowy (do 2013): 1 ng/m³ (średnia roczna)" & vbCrLf
            Case "Cd"
                Return "Poziom docelowy (do 2013): 5 ng/m³ (średnia roczna)" & vbCrLf
            Case "Ni"
                Return "Poziom docelowy (do 2013): 20 ng/m³ (średnia roczna)" & vbCrLf
            Case "O₃"
                Return "Poziom docelowy (do 2010): 120 μg/m³ (średnia 8 godzinna), okres wegetacji (1 V - 31 VII): 18 000" &
                    "Poziom długoterminowy (do 2020): 120/6000" & vbCrLf
            Case "PM₂₅"
                Return "Poziom docelowy (do 2010): 25 μg/m³ (średnia roczna)" & vbCrLf
        End Select
        Return ""
    End Function

    Public Shared Function PoziomAlarmuPL(sPomiar As String) As String
        ' http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
        Select Case sPomiar
            Case "NO₂"
                Return "Poziom alarmowania: 400 μg/m³ średnia godzinna" & vbCrLf
            Case "SO₂"
                Return "Poziom alarmowania: 500 μg/m³ średnia godzinna" & vbCrLf
            Case "O₃"
                Return "Poziom alarmowania: 240 μg/m³ średnia godzinna" & vbCrLf
            Case "PM₁₀"
                Return "Poziom alarmowania: 400 μg/m³ średnia dobowa" & vbCrLf
        End Select
        Return ""
    End Function

    Public Shared Function PoziomInformowaniaPL(sPomiar As String) As String
        ' http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
        Select Case sPomiar
            Case "O₃"
                Return "Poziom informowania: 180 μg/m³ średnia godzinna" & vbCrLf
            Case "PM₁₀"
                Return "Poziom informowania: 200 μg/m³ średnia dobowa" & vbCrLf
        End Select
        Return ""
    End Function

    Public Shared Function PoziomyWHO(sPomiar As String) As String
        ' https://www.who.int/news-room/fact-sheets/detail/ambient-(outdoor)-air-quality-and-health
        Select Case sPomiar
            Case "PM₂₅"
                Return "Limit WHO: 10 μg/m³ (średnia roczna), 25 μg/m³ (średnia dobowa)" & vbCrLf
            Case "PM₁₀"
                Return "Limit WHO: 20 μg/m³ (średnia roczna), 50 μg/m³ (średnia dobowa)" & vbCrLf
            Case "O₃"
                Return "Limit WHO: 100 μg/m³ (średnia 8-godzinna)" & vbCrLf
            Case "NO₂"
                Return "Limit WHO: 40 μg/m³ (średnia roczna), 200 μg/m³ (średnia godzinna)" & vbCrLf
            Case "SO₂"
                Return "Limit WHO: 20 μg/m³ (średnia dobowa), 500 μg/m³ (średnia 10-minutowa)" & vbCrLf
        End Select
        Return ""

    End Function

    Public Shared Sub DodajPrzekroczenia()
        ' http://ec.europa.eu/environment/air/quality/standards.htm
        For Each oItem As JedenPomiar In App.moPomiaryAll

            'If oItem.sSource = "IMGWhyd" Then Continue For
            'If oItem.sSource = "DarkSky" AndAlso oItem.sPomiar.StartsWith("Alert") Then Continue For
            'If oItem.sSource = "NOAAkind" Then Continue For

            If oItem.sSource <> "gios" AndAlso oItem.sSource <> "airly" AndAlso oItem.sSource <> "EEAair" Then Continue For

            oItem.sLimity = PoziomyWHO(oItem.sPomiar) &
                PoziomDocelowyPL(oItem.sPomiar) & PoziomDopuszczalnyPL(oItem.sPomiar) &
                PoziomInformowaniaPL(oItem.sPomiar) & PoziomAlarmuPL(oItem.sPomiar)

            If GetSettingsBool("settingsWHO", True) Then
                Select Case oItem.sPomiar
                    Case "PM₁"
                    Case "PM₂₅"
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 10, 25, 50)
                    Case "PM₁₀"
                        ' 20 μg/m³ średnia roczna, 50 μg/m³ średnia godzinna
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 20, 50, 100)
                    Case "μSv/h"
                    Case "C₆H₆" ' to jest nie WHO, bo WHO nie ma!
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 5, 1000, 1000)
                    Case "SO₂"
                        ' 20 μg/m³ średnia dobowa, 500 μg/m³ średnia 10 minutowa
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 20, 500, 1000)
                    Case "NO₂"
                        ' 40 μg/m³ średnia roczna, 200 μg/m³ średnia godzinna
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 40, 200, 400)
                    Case "O₃"
                        ' 100 μg/m³ średnia 8 h
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 100, 100, 200)
                    Case Else
                        oItem.sAlert = ""
                End Select
            Else
                Select Case oItem.sPomiar
                    ' poziom dopuszczalny/docelowy , informowania, alarmu
                    Case "PM₁"
                    Case "PM₂₅"
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 25, 1000, 2000)
                    Case "PM₁₀"
                        ' 20 μg/m³ średnia roczna, 50 μg/m³ średnia godzinna
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 40, 200, 400)
                    Case "μSv/h"
                    Case "C₆H₆"
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 5, 1000, 2000)
                    Case "SO₂"
                        ' 20 μg/m³ średnia dobowa, 500 μg/m³ średnia 10 minutowa
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 125, 125, 500)
                    Case "NO₂"
                        ' 40 μg/m³ średnia roczna, 200 μg/m³ średnia godzinna
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 40, 40, 400)
                    Case "O₃"
                        ' 100 μg/m³ średnia 8 h
                        oItem.sAlert = Wykrzyknikuj(oItem.dCurrValue, 120, 180, 240)

                    Case Else
                        oItem.sAlert = ""
                End Select

                ' przeniesione do IMGWmeteo
                'If oItem.sPomiar = GetSettingsString("resPomiarOpad") Then 'GetLangString("resPomiarOpad") Then
                '    If oItem.dCurrValue > 0 Then oItem.sAlert = "!"
                'End If

                ' przeniesione do DarkSky
                'If oItem.sPomiar = "UV index" Then
                '    ' http://www.who.int/uv/publications/en/UVIGuide.pdf
                '    If oItem.dCurrValue >= 6 Then oItem.sAlert = "!"
                '    If oItem.dCurrValue >= 8 Then oItem.sAlert = "!!"
                '    If oItem.dCurrValue >= 11 Then oItem.sAlert = "!!!"
                'End If
            End If

        Next
    End Sub

    Public Shared Async Function SourcesUsedInTimer() As Task(Of String)
        Dim sRet As String = ""
        Dim sId As String
        Dim iInd As Integer

        For Each oTile As Windows.UI.StartScreen.SecondaryTile In Await Windows.UI.StartScreen.SecondaryTile.FindAllAsync
            sId = oTile.TileId
            iInd = sId.LastIndexOf("(")
            If iInd > 0 Then sRet = sRet & "|" & sId.Substring(iInd + 1)
        Next

        sId = GetSettingsString("settingsLiveTile")
        iInd = sId.LastIndexOf("(")
        If iInd > 0 Then sRet = sRet & "|" & sId.Substring(iInd + 1)

        If GetSettingsString("settingsAlerts").IndexOf("!") < 0 Then Return sRet
        ' skoro maja byc Toasty, to dopiszmy to co jest wykrzyknikowalne

        'sRet = sRet & "|airly|gios|IMGWhyd|DarkSky|NOAAkind" ' hydro: poziom wody!, DarkSky - ostrzezenia
        ' czyli bez r@h, IMGWmet, foreca
        For Each oZrodlo As Source_Base In App.gaSrc
            If oZrodlo.SRC_IN_TIMER Then sRet = sRet & "|" & oZrodlo.SRC_POMIAR_SOURCE
        Next

        Return sRet
    End Function

    Public Shared Event ZmianaDanych()

    Public Shared Async Function GetFavData(sFavName As String, bInTimer As Boolean) As Task
        If Not NetIsIPavailable(False) Then Return

        Dim sSensors As String = GetSettingsString("fav_" & sFavName)
        If sSensors = "" Then Exit Function

        ' to chyba niepotrzebne, bo z load(template) odleglosc jest ustalona
        Dim sPunkt As String = GetSettingsString("favgps_" & sFavName)
        If sPunkt = "" Then Exit Function
        Dim aPunkt As String() = sPunkt.Split("|")
        App.moGpsPoint.X = aPunkt(0)
        App.moGpsPoint.Y = aPunkt(1)

        App.moPomiaryAll = New Collection(Of JedenPomiar)
        Dim oPomiary As Collection(Of JedenPomiar) = Nothing

        Dim sInTiles As String = ""
        If bInTimer Then sInTiles = Await SourcesUsedInTimer()

        Dim aSensory As String() = sSensors.Split("|")
        For Each sSensor As String In aSensory
            Dim aData As String() = sSensor.Split("#")
            If bInTimer AndAlso sInTiles.IndexOf(aData(0)) < 0 Then Continue For
            oPomiary = Nothing

            For Each oZrodlo As Source_Base In App.gaSrc
                If aData(0) = oZrodlo.SRC_POMIAR_SOURCE Then
                    If aData(0) = "DarkSky" OrElse aData(0) = "SeismicEU" Then
                        oPomiary = Await oZrodlo.GetDataFromFavSensor(App.moGpsPoint.X, App.moGpsPoint.Y, bInTimer)
                    Else
                        oPomiary = Await oZrodlo.GetDataFromFavSensor(aData(1), aData(2), bInTimer)
                    End If
                End If
            Next

            'Select Case aData(0)
            '    Case "airly"
            '    Case "ra@h"
            '        oPomiary = Await App.moSrc_RadioAtHome.GetDataFromFavSensor(aData(1), aData(2))
            '    Case "gios"
            '        oPomiary = Await App.moSrc_GIOS.GetDataFromFavSensor(aData(1), aData(2))
            '    Case "IMGWhyd"
            '        oPomiary = Await App.moSrc_ImgwHydro.GetDataFromFavSensor(aData(1), aData(2))
            '    Case "IMGWmet"
            '        oPomiary = Await App.moSrc_ImgwMeteo.GetDataFromFavSensor(aData(1), aData(2))
            '    Case "Foreca"
            '        oPomiary = Await App.moSrc_Foreca.GetDataFromFavSensor(aData(1), aData(2))
            '    Case "DarkSky"
            '        oPomiary = Await App.moSrc_DarkSky.GetDataFromFavSensor(App.moGpsPoint.X, App.moGpsPoint.Y)
            '    Case "NOAAwind"
            '        oPomiary = Await App.moSrc_NoaaWind.GetDataFromFavSensor("", "")
            '    Case "NOAAkind"
            '        oPomiary = Await App.moSrc_NoaaKind.GetDataFromFavSensor("", "")
            '    Case Else
            '        Continue For
            'End Select

            If oPomiary IsNot Nothing Then
                For Each oPomiar As JedenPomiar In oPomiary
                    App.moPomiaryAll.Add(oPomiar)
                Next
                If oPomiary.Count > 0 AndAlso Not bInTimer Then RaiseEvent ZmianaDanych()
            End If

        Next

        ' NOAA alert - musi byc wywolane nawet jak nie ma w Fav:Template
        If Not sSensors.Contains("NOAAalert") Then
            ' znajdz ktora to pozycja tabelki Zrodel
            For Each oZrodlo As Source_Base In App.gaSrc
                If oZrodlo.SRC_POMIAR_SOURCE = "NOAAalert" Then
                    oPomiary = Await oZrodlo.GetDataFromFavSensor("", "", bInTimer)
                    Exit For
                End If
            Next
            If oPomiary IsNot Nothing Then
                For Each oPomiar As JedenPomiar In oPomiary
                    App.moPomiaryAll.Add(oPomiar)
                Next
            End If
            If oPomiary.Count > 0 AndAlso Not bInTimer Then RaiseEvent ZmianaDanych()

        End If


    End Function

    Public Shared Sub DodajTempOdczuwana()

        Dim dTemp As Double = 1000
        Dim dWilg As Double = 1000

        'MakeToast("before loop in Tapp")
        For Each oItem As JedenPomiar In App.moPomiaryAll
            ' MakeToast("source: " & oItem.sSource & ", pomiar " & oItem.sPomiar)
            If Not oItem.bDel AndAlso oItem.sPomiar <> "" Then
                'MakeToast("value: " & oItem.dCurrValue)
                If oItem.sPomiar.ToLower = "humidity" Then dWilg = oItem.dCurrValue
                If oItem.sPomiar.ToLower.IndexOf("tempe") = 0 Then dTemp = oItem.dCurrValue ' airly tak, ale IMGW nie (bo tam jest temp)
            End If
        Next
        'MakeToast("Tapp, mam dane " & dTemp & ", " & dWilg)
        ' jesli ktorejs wartosci nie ma, to sie poddaj
        If dTemp = 1000 Then Exit Sub
        If dWilg = 1000 Then Exit Sub

        Dim oNew As JedenPomiar = New JedenPomiar With {
            .sSource = "me",
            .dOdl = 0,
            .sPomiar = GetSettingsString("resTempOdczuwana"), ' GetLangString("resTempOdczuwana"),
            .sUnit = " °C",
            .sTimeStamp = Date.Now,
            .sSensorDescr = GetSettingsString("resTempOdczuwana"),
            .sOdl = ""
        }

        ' http://www.bom.gov.au/info/thermal_stress/#apparent
        ' czyli Source: Norms of apparent temperature in Australia, Aust. Met. Mag., 1994, Vol 43, 1-16
        Dim dWP As Double ' water pressure, hPa
        Dim dWind As Double = 0 ' wind speed, na wysok 10 m, w m/s

        dWP = dWilg / 100 * 6.105 * Math.Exp((17.27 * dTemp) / (237.7 + dTemp))
        oNew.dCurrValue = Math.Round(dTemp + 0.33 * dWP - 0.7 * dWind - 4, 2)
        ' uwaga: dla wersji z naslonecznieniem jest inaczej
        oNew.sCurrValue = oNew.dCurrValue & " °C"

        ' wersja z naslonecznieniem:
        ' oraz kalkulator: https://planetcalc.com/2089/
        ' var e = (H/100)*6.105*Math.exp( (17.27*Ta)/(237.7+Ta) );
        'AT.SetValue(Ta + 0.348*e - 0.7*V - 4.25);

        App.moPomiaryAll.Add(oNew)

    End Sub

    Public Shared Function String2SentenceCase(sInput As String) As String
        ' założenie: wchodzi UPCASE
        Dim sOut As String = ""
        Dim bFirst = True

        For i As Integer = 0 To sInput.Length - 1
            Dim sChar As Char = sInput.ElementAt(i)
            If ("ABCDEFGHIJKLMNOPQRSTUVWXYZĄĆĘŁŃÓŚŻŹ").IndexOf(sChar) < 0 Then
                sOut = sOut & sChar
                bFirst = True
                Continue For
            End If

            If bFirst Then
                bFirst = False
            Else
                sChar = sChar.ToString.ToLower
            End If

            sOut = sOut & sChar
        Next

        Return sOut
    End Function

    Public Shared Function ShortPrevDate(sCurrDate As String, sPrevDate As String) As String

        If sCurrDate.Substring(0, 10) = sPrevDate.Substring(0, 10) Then Return sPrevDate.Substring(11, 5)
        ' miesiac/dzien
        If sCurrDate.Substring(0, 10) = sPrevDate.Substring(0, 10) Then Return sPrevDate.Substring(5, 11)
        ' calosc ale bez sekund
        Return sPrevDate.Substring(0, 16)

    End Function

    Public Shared Function UnixTimeToTime(lTime As Long) As String
        '1509993360
        Dim dtDateTime As DateTime = New DateTime(1970, 1, 1, 0, 0, 0, 0)
        dtDateTime = dtDateTime.AddSeconds(lTime)
        Return dtDateTime.ToString
    End Function

    Public Shared Async Function Cache_Load() As Task(Of DateTimeOffset)
        Dim oFile As Windows.Storage.StorageFile
        Dim oFile1 As Windows.Storage.StorageFile

        ' najpierw sprobuj plik lokalny odczytac, a jak sie nie uda - roaming
        oFile = Await App.GetDataFile(False, "data_cache.xml", False)
        oFile1 = Await App.GetDataFile(True, "data_cache.xml", False)

        If oFile IsNot Nothing AndAlso oFile1 IsNot Nothing Then
            If (Await oFile.GetBasicPropertiesAsync).DateModified < (Await oFile1.GetBasicPropertiesAsync).DateModified Then
                oFile = oFile1
            End If
        End If

        If oFile Is Nothing Then Return Date.Now.AddYears(-100)

        Dim oSer As Xml.Serialization.XmlSerializer =
                        New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenPomiar)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync

        Try
            App.moPomiaryAll = oSer.Deserialize(oStream)
        Catch ex As Exception
            ' jakby był błąd zapisu i plik miał długość zero :)
        End Try
        oStream.Dispose()   ' == fclose
        Dim oBP As Windows.Storage.FileProperties.BasicProperties = Await oFile.GetBasicPropertiesAsync
        Return oBP.DateModified
        'Return oFile.DateCreated
    End Function

    Public Shared Async Function Cache_Save() As Task
        Dim oFile As Windows.Storage.StorageFile

        ' local file
        If GetSettingsBool("settingsFileCache") Then
            oFile = Await App.GetDataFile(False, "data_cache.xml", True)
            If oFile IsNot Nothing Then

                Dim oSer As Xml.Serialization.XmlSerializer =
                                New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenPomiar)))
                Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
                oSer.Serialize(oStream, App.moPomiaryAll)
                oStream.Dispose()   ' == fclose
            End If
        End If


        ' roaming file
        If GetSettingsBool("settingsFileCacheRoam") Then
            oFile = Await App.GetDataFile(True, "data_cache.xml", True)
            If oFile IsNot Nothing Then

                Dim oSer As Xml.Serialization.XmlSerializer =
                                New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenPomiar)))
                Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
                oSer.Serialize(oStream, App.moPomiaryAll)
                oStream.Dispose()   ' == fclose
            End If
        End If

    End Function

    Public Shared Sub ReadResStrings()
        ' wczytanie tych stringow, ktore są potrzebne w background - gdy nie dziala GetLangString
        SetSettingsString("resAllOk", GetLangString("msgAllOk"))

        For Each oZrodlo As Source_Base In App.gaSrc
            oZrodlo.ReadResStrings()
        Next
    End Sub

    Public Shared gaSrc As Source_Base() = {
        New Source_Airly,
        New Source_RadioAtHome,
        New Source_GIOS,
        New Source_IMGWhydro,
        New Source_IMGWmeteo,
        New Source_Foreca,
        New Source_DarkSky,
        New Source_NoaaKindex,
        New Source_NoaaWind,
        New Source_NoaaAlert,
        New Source_EEAair,
        New Source_SeismicPortal
    }

    Public Shared moPomiaryAll As Collection(Of JedenPomiar) = New Collection(Of JedenPomiar)
    Public Shared moLastPomiar As Date = Nothing

    Public Shared moPoint As Point = Nothing
End Class

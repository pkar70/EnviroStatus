' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class Settings
    Inherits Page

    ' combo wyboru start place (z zapamietanych), oraz "nic" (bez autostart)
    ' LifeTile: yes/no, combo lista pomiarow z aktualnego miejsca (app.mo..)
    ' GPS (60 min), last fav (30 min)
    ' alerts: yes/no, combo: średnia roczna, średnia dzienna, 2x dzienna
    ' alert OK: gdy spadnie ponizej 90 % limitu
    ' DataLogs yes/no
    ' pamięć ostatniej lokalizacji jak DailyItinerary - dopiero wtedy zmiana stacji

    Private Sub FillCombo(uiCombo As ComboBox, sCurr As String, sList As String, sAdditItem As String)
        uiCombo.Items.Clear()

        Dim sCurrent As String = GetSettingsString(sCurr)

        Dim oCBI As ComboBoxItem = New ComboBoxItem
        If sAdditItem <> "" Then
            oCBI.Content = GetLangString(sAdditItem)
            If sCurrent <> "" And sCurrent = oCBI.Content Then oCBI.IsSelected = True
            uiCombo.Items.Add(oCBI)
        End If


        Dim sTxt As String = GetSettingsString(sList)
        Dim aNames As String() = sTxt.Split("|")
        For Each sName As String In aNames
            oCBI = New ComboBoxItem
            oCBI.Content = sName
            If sCurrent <> "" And sCurrent = oCBI.Content Then oCBI.IsSelected = True
            uiCombo.Items.Add(oCBI)
        Next

    End Sub

    Private Sub FillComboLiveTile(uiCombo As ComboBox, sCurr As String, sAdditItem As String)
        uiCombo.Items.Clear()

        Dim sCurrent As String = GetSettingsString(sCurr)

        Dim oCBI As ComboBoxItem = New ComboBoxItem
        If sAdditItem <> "" Then
            oCBI.Content = GetLangString(sAdditItem)
            If sCurrent <> "" And sCurrent = oCBI.Content Then oCBI.IsSelected = True
            uiCombo.Items.Add(oCBI)
        End If

        For Each oItem As JedenPomiar In App.moPomiaryAll
            If Not oItem.bDel Then
                oCBI = New ComboBoxItem
                oCBI.Content = oItem.sPomiar & " (" & oItem.sSource & ")"
                If sCurrent <> "" And oItem.sPomiar = sCurrent Then oCBI.IsSelected = True
                uiCombo.Items.Add(oCBI)
            End If
        Next
    End Sub

    Private Sub ComboAlerts(uiCombo As ComboBox, sCurr As String)
        Dim sCurrent As String = GetSettingsString(sCurr)

        Dim iInd As Integer = -1

        For iInd = 0 To uiCombo.Items.Count - 1
            If TryCast(uiCombo.Items.ElementAt(iInd), ComboBoxItem).Content = sCurrent Then
                uiCombo.SelectedIndex = iInd
                Exit For
            End If
        Next

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        uiVersion.Text = GetLangString("msgVersion") & " " & Package.Current.Id.Version.Major & "." &
            Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build

        FillCombo(uiStartLoc, "settingStartPage", "favNames", "resNoAutostart")
        FillComboLiveTile(uiLiveTile, "settingsLiveTile", "resDeadTile")
        ComboAlerts(uiAlerts, "settingsAlerts")
        GetSettingsBool(uiLimitWg, "settingsWHO", True)

        GetSettingsBool(uiLiveTileClock, "settingsLiveClock")
        GetSettingsBool(uiDataLogs, "settingsDataLog")

        uiKubatura.Text = GetSettingsInt("higroKubatura", 0) / 100.0
        uiIntTemp.Text = GetSettingsInt("higroTemp", 22)

        uiLatitude.Text = App.moGpsPoint.X
        uiLongitude.Text = App.moGpsPoint.Y

        GetSettingsBool(uiFileCache, "settingsFileCache")
        'GetSettingsBool(uiDelToastOnOpen, "settingsDelToastOnOpen")
    End Sub

    Private Function VerifyDataOK() As String
        Dim sMsg As String = ""

        'sMsg = App.moSrc_Airly.ConfigDataOk(uiStackConfig)
        'If sMsg <> "" Then Return sMsg

        'sMsg = App.moSrc_RadioAtHome.ConfigDataOk(uiStackConfig)
        'If sMsg <> "" Then Return sMsg

        'sMsg = App.moSrc_GIOS.ConfigDataOk(uiStackConfig)
        'If sMsg <> "" Then Return sMsg

        If uiKubatura.Text = "" Then uiKubatura.Text = "0"
        Dim dTmp As Double = 0
        If Not Double.TryParse(uiKubatura.Text, dTmp) Then Return "ERROR: to nie liczba"
        If dTmp < 0 Then Return "ERROR: musi być > 0!"

        If uiIntTemp.Text = "" Then uiIntTemp.Text = "0"
        dTmp = 0
        If Not Double.TryParse(uiIntTemp.Text, dTmp) Then Return "ERROR: to nie liczba"

        If uiSimulGPS.IsOn Then
            If Not Double.TryParse(uiLatitude.Text, dTmp) Then Return "ERROR: to nie liczba"
            If dTmp < -90 OrElse dTmp > 90 Then Return "ERROR: Latitude poza zakresem"
            If Not Double.TryParse(uiLongitude.Text, dTmp) Then Return "ERROR: to nie liczba"
            If dTmp < 0 OrElse dTmp > 360 Then Return "ERROR: Longitude poza zakresem"
        End If


        Return ""
    End Function

    Private Async Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        Dim sMsg As String = VerifyDataOK()
        If sMsg <> "" Then
            Await DialogBox(sMsg)
            Exit Sub
        End If

        'App.moSrc_Airly.ConfigRead(uiStackConfig)
        'App.moSrc_RadioAtHome.ConfigRead(uiStackConfig)
        'App.moSrc_GIOS.ConfigRead(uiStackConfig)

        If uiStartLoc.SelectedValue IsNot Nothing Then
            Try
                SetSettingsString("settingStartPage", TryCast(uiStartLoc.SelectedValue, ComboBoxItem).Content)
            Catch ex As Exception
            End Try
        End If

        If uiLiveTile.SelectedValue IsNot Nothing Then
            Try
                SetSettingsString("settingsLiveTile", TryCast(uiLiveTile.SelectedValue, ComboBoxItem).Content)
            Catch ex As Exception
            End Try
        End If

        Try
            SetSettingsString("settingsAlerts", TryCast(uiAlerts.SelectedValue, ComboBoxItem).Content)
        Catch ex As Exception
        End Try
        SetSettingsBool("settingsWHO", uiLimitWg.IsOn)


        SetSettingsBool("settingsLiveClock", uiLiveTileClock.IsOn)
        SetSettingsBool("settingsDataLog", uiDataLogs.IsOn)

        If uiKubatura.Text = "" Then uiKubatura.Text = "0"
        Dim dTmp As Double = 0
        If Double.TryParse(uiKubatura.Text, dTmp) Then SetSettingsInt("higroKubatura", dTmp * 100)

        If uiIntTemp.Text = "" Then uiIntTemp.Text = "0"
        SetSettingsInt("higroTemp", uiIntTemp.Text)

        SetSettingsBool("simulateGPS", uiSimulGPS.IsOn)
        If uiSimulGPS.IsOn Then
            Try
                App.moGpsPoint.X = uiLatitude.Text
                App.moGpsPoint.Y = uiLongitude.Text
            Catch ex As Exception
                SetSettingsBool("simulateGPS", False)
            End Try
        End If

        SetSettingsBool(uiFileCache, "settingsFileCache")
        'SetSettingsBool(uiDelToastOnOpen, "settingsDelToastOnOpen")


        Me.Frame.GoBack()
    End Sub

    Private Sub UiDataLogs_Toggled(sender As Object, e As RoutedEventArgs) Handles uiDataLogs.Toggled
        If uiDataLogs.IsOn Then
            uiOpenLogs.IsEnabled = True
        Else
            uiOpenLogs.IsEnabled = False
        End If
    End Sub

    Private Async Sub UiOpenLogs_Click(sender As Object, e As RoutedEventArgs) Handles uiOpenLogs.Click

        Dim sdCard As Windows.Storage.StorageFolder = Nothing
        Try
            Dim externalDevices As Windows.Storage.StorageFolder = Windows.Storage.KnownFolders.RemovableDevices
            Dim oCards As IReadOnlyList(Of Windows.Storage.StorageFolder) = Await externalDevices.GetFoldersAsync()
            sdCard = oCards.FirstOrDefault()
        Catch ex As Exception
            sdCard = Nothing
        End Try
        If sdCard Is Nothing Then Exit Sub

        Dim oFolder As Windows.Storage.StorageFolder = Nothing
        Try
            oFolder = Await sdCard.GetFolderAsync("DataLogs")
            If oFolder Is Nothing Then Exit Sub

            oFolder = Await oFolder.GetFolderAsync("Smogometr")
        Catch ex As Exception
            oFolder = Nothing
        End Try
        If oFolder Is Nothing Then Exit Sub


        Windows.System.Launcher.LaunchFolderAsync(oFolder)

    End Sub

    Private Sub uiDataSources_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Zrodelka))
    End Sub

    Private Sub uiSimulGPS_Toggled(sender As Object, e As RoutedEventArgs) Handles uiSimulGPS.Toggled
        If uiSimulGPS.IsOn Then
            uiGridGPS.Visibility = Visibility.Visible
        Else
            uiGridGPS.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Sub uiSettSharing_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(SettingsSharing))
    End Sub
End Class

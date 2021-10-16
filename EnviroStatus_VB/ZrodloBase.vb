' dodając:
' App.xaml.vb końcówka: dopisac do listy w tabelce
' App.xaml.vb DodajPrzekroczenia - jesli to ma byc w App, a nie podczas odczytywania

' juz zrobione (przez zrobienie gaSrc):
' App.xaml.vb końcówka, ReadResStrings: odczytywanie ewentualnych zmiennych tekstów
' App.xaml.vb GetFavData - odwolanie do source przy wczytywaniu
' App.xaml.vb SourcesUsedInTimer - jesli to jest uzywane w timer, to dodac
' MainPage.xaml.vb uiStore_Click - dodawanie template (na wszelki wypadek, jakby potem sie dodawało template)
' MainPage.xaml.vb uiGPS_Click - odczytywanie danych
' Zrodelka.xaml.vb


Public MustInherit Class Source_Base
    ' ułatwienie dodawania następnych
    Protected MustOverride Property SRC_SETTING_NAME As String
    Protected MustOverride Property SRC_SETTING_HEADER As String
    Protected MustOverride Property SRC_RESTURI_BASE As String
    Protected Overridable Property SRC_RESTURI_BASE_PKAR As String = ""
    Public MustOverride ReadOnly Property SRC_POMIAR_SOURCE As String
    Protected Overridable Property SRC_DEFAULT_ENABLE As Boolean = False
    Protected Overridable Property SRC_HAS_KEY As Boolean = False
    Protected Overridable Property SRC_KEY_LOGIN_LINK As String = ""
    Protected Overridable Property SRC_HAS_TEMPLATES As Boolean = False
    Public Overridable ReadOnly Property SRC_IN_TIMER As Boolean = False

    Protected Overridable Async Function GetREST(sCommand As String) As Task(Of String)

        Await Task.Delay(100)   'nie wolno zasypywac serwera

        Dim oHttp As Windows.Web.Http.HttpClient
        oHttp = New Windows.Web.Http.HttpClient

        If SRC_POMIAR_SOURCE = "airly" Then
            oHttp.DefaultRequestHeaders.Add("Accept", "application/json")
            oHttp.DefaultRequestHeaders.Add("apikey", GetSettingsString("airly_apikey"))
        End If

        If SRC_POMIAR_SOURCE = "DarkSky" Then
            sCommand = GetSettingsString(SRC_SETTING_NAME & "_apikey") & "/" & sCommand
        End If

        Dim oUri As Uri
        If IsThisMoje() AndAlso SRC_RESTURI_BASE_PKAR <> "" Then
            oUri = New Uri(SRC_RESTURI_BASE_PKAR & sCommand)
        Else
            oUri = New Uri(SRC_RESTURI_BASE & sCommand)
        End If

        Dim oRes As String = ""
        Try
            oRes = Await oHttp.GetStringAsync(oUri)
        Catch ex As Exception
            oRes = ""
        End Try

        Return oRes

    End Function

    Protected moListaPomiarow As Collection(Of JedenPomiar) = Nothing

    ' Public MustOverride Async Function GetNearest(oPos As Point, dMaxOdl As Double) As Task(Of Collection(Of JedenPomiar))

    Public MustOverride Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))

    Public MustOverride Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))

    Public Overridable Sub ReadResStrings()
        ' jakby cos bylo do przekopiowania z Resources do App.Settings
    End Sub


    Public Overridable Async Function SaveFavTemplate() As Task
        If Not SRC_HAS_TEMPLATES Then Return

        ' zapisz dane template sensorów
        Dim sNums As String = ""
        For Each oItem As JedenPomiar In App.moPomiaryAll
            If Not oItem.bDel AndAlso oItem.sSource = SRC_POMIAR_SOURCE Then

                If sNums.IndexOf(oItem.sId & "|") < 0 Then
                    Dim sFileName As String = SRC_POMIAR_SOURCE & "_" & oItem.sId & ".xml"
                    If SRC_POMIAR_SOURCE = "IMGWmet" Then
                        sFileName = "IMGWmeteo" & "_" & oItem.sId & ".xml"
                    Else
                        sFileName = SRC_POMIAR_SOURCE & "_" & oItem.sId & ".xml"
                    End If
                    Dim oFile As Windows.Storage.StorageFile =
                        Await App.GetDataFile(False, SRC_POMIAR_SOURCE & "_" & oItem.sId & ".xml", True)
                    If oFile Is Nothing Then Exit Function

                    Dim oSer As Xml.Serialization.XmlSerializer =
                        New Xml.Serialization.XmlSerializer(GetType(JedenPomiar))
                    Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
                    oSer.Serialize(oStream, oItem)
                    oStream.Dispose()   ' == fclose

                    sNums = sNums & oItem.sId & "|"
                End If

            End If
        Next
    End Function

    Public Overridable Sub ConfigCreate(oStack As StackPanel)
        Dim oTS As ToggleSwitch = New ToggleSwitch
        oTS.Header = SRC_SETTING_HEADER
        oTS.Name = "uiConfig_" & SRC_SETTING_NAME
        oTS.IsOn = GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE)
        oStack.Children.Add(oTS)

        If Not SRC_HAS_KEY Then Exit Sub

        Dim oTBox As TextBox = New TextBox
        oTBox.Header = SRC_SETTING_HEADER & " API key"
        oTBox.Name = "uiConfig_" & SRC_SETTING_NAME & "_Key"
        oTBox.Text = GetSettingsString(SRC_SETTING_NAME & "_apikey")
        oStack.Children.Add(oTBox)
        Dim oLink As HyperlinkButton = New HyperlinkButton
        oLink.Content = GetLangString("msgForAPIkey") ' "Aby uzyskać API key, zarejestruj się"
        oLink.NavigateUri = New Uri(SRC_KEY_LOGIN_LINK)
        oStack.Children.Add(oLink)

    End Sub

    Public Overridable Function ConfigDataOk(oStack As StackPanel) As String
        ' jesli nie ma Key, to na pewno poprawne
        If Not SRC_HAS_KEY Then Return ""

        ' jesli nie jest wlaczone, to tez jest poprawnie
        For Each oItem As UIElement In oStack.Children
            Dim oTS As ToggleSwitch
            oTS = TryCast(oItem, ToggleSwitch)
            If oTS IsNot Nothing Then
                If oTS.Name = "uiConfig_" & SRC_SETTING_NAME Then
                    If Not oTS.IsOn Then Return ""
                End If
            End If
        Next

        For Each oItem As UIElement In oStack.Children
            Dim oTB As TextBox
            oTB = TryCast(oItem, TextBox)
            If oTB IsNot Nothing Then
                If oTB.Name = "uiConfig_" & SRC_SETTING_NAME & "_Key" Then
                    If oTB.Text.Length > 8 Then Return ""
                    Return "Too short API key"
                    Exit For
                End If
            End If
        Next

        Return "UIError - no API key"

    End Function

    Public Overridable Sub ConfigRead(oStack As StackPanel)
        For Each oItem As UIElement In oStack.Children
            Dim oTS As ToggleSwitch
            oTS = TryCast(oItem, ToggleSwitch)
            If oTS IsNot Nothing Then
                If oTS.Name = "uiConfig_" & SRC_SETTING_NAME Then
                    SetSettingsBool(SRC_SETTING_NAME, oTS.IsOn)
                    Exit For
                End If
            End If
        Next

        If Not SRC_HAS_KEY Then Exit Sub

        ' tylko gdy jest wlaczony
        For Each oItem As UIElement In oStack.Children
            Dim oTB As TextBox
            oTB = TryCast(oItem, TextBox)
            If oTB IsNot Nothing Then
                If oTB.Name = "uiConfig_" & SRC_SETTING_NAME & "_Key" Then
                    SetSettingsString(SRC_SETTING_NAME & "_apikey", oTB.Text, True)
                    Exit For
                End If
            End If
        Next
    End Sub

    Public Function Odleglosc2String(dOdl As Double) As String
        If dOdl < 10000 Then Return dOdl & " m"
        Return CInt(dOdl / 1000).ToString & " km"
    End Function

    'Public Async Function GetHistoryFromSensor(sId As String) As Task(Of Collection(Of JedenPomiar))
    '    If GetSettingsString("airly_apikey").Length < 8 Then Return Nothing

    '    ' zwroc dane z tego sensora
    'End Function

    'Public Async Function GetForecastFromSensor(sId As String) As Task(Of Collection(Of JedenPomiar))
    '    If GetSettingsString("airly_apikey").Length < 8 Then Return Nothing

    '    ' zwroc dane z tego sensora
    'End Function

    'Private Sub AddPomiar(oNew As JedenPomiar)
    '    'For Each oItem As JedenPomiar In moListaPomiarow
    '    '    If oItem.sPomiar = oNew.sPomiar Then
    '    '        ' porownanie dat

    '    '        ' porownanie odleglosci
    '    '        If oItem.dOdl > oNew.dOdl Then
    '    '            ' moListaPomiarow.Remove(oItem)
    '    '            oItem.bDel = True
    '    '            ' oNew zostanie dodany po zakonczeniu petli
    '    '        Else
    '    '            Exit Sub    ' mamy nowszy pomiar, czyli oNew nas nie interesuje
    '    '        End If
    '    '    End If
    '    'Next
    '    'moListaPomiarow.Add(oNew)

    'End Sub

    'Private Function NormalizePomiarName(sPomiar As String) As String
    '    'If sPomiar.Substring(0, 2) = "PM" Then Return sPomiar
    '    'Return sPomiar.Substring(0, 1) & sPomiar.Substring(1).ToLower
    '    Return "Radiation"
    'End Function

    'Private Function Unit4Pomiar(sPomiar As String) As String
    '    Return "°C"
    '    'If sPomiar.Substring(0, 2) = "PM" Then Return " μg/m³"

    '    'Select Case sPomiar
    '    '    Case "PRESSURE"
    '    '        Return " hPa"
    '    '    Case "HUMIDITY"
    '    '        Return " %"
    '    '    Case "TEMPERATURE"
    '    '        Return "°C"
    '    'End Select
    '    'Return ""
    'End Function

End Class

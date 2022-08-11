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


' nie da się do VB, bo ConfigCreate / ConfigDataOk / ConfigRead są bezpośrednio związane z UI
'Partial Public Class Source_Base
'    Public Shared Sub VbLib_App_Public()

'    End Sub

'    Private Shared Sub VbLib_App_Priv()

'    End Sub

'End Class

Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Security.Cryptography

Partial Public MustInherit Class Source_Base
    ' ułatwienie dodawania następnych
    Public MustOverride ReadOnly Property SRC_SETTING_NAME As String  ' public dla ConfigDataOK
    Public MustOverride ReadOnly Property SRC_SETTING_HEADER As String ' public dla ConfigCreate
    Protected MustOverride ReadOnly Property SRC_RESTURI_BASE As String
    Protected MustOverride ReadOnly Property SRC_URI_ABOUT_EN As String
    Protected MustOverride ReadOnly Property SRC_URI_ABOUT_PL As String
    Protected Overridable ReadOnly Property SRC_RESTURI_BASE_PKAR As String = ""
    Public MustOverride ReadOnly Property SRC_POMIAR_SOURCE As String
    Public Overridable ReadOnly Property SRC_DEFAULT_ENABLE As Boolean = False ' public dla ConfigCreate
    Public Overridable Property SRC_HAS_KEY As Boolean = False  ' public dla ConfigDataOK
    Public Overridable ReadOnly Property SRC_KEY_LOGIN_LINK As String = "" ' public dla ConfigCreate
    Protected Overridable ReadOnly Property SRC_HAS_TEMPLATES As Boolean = False
    Public Overridable ReadOnly Property SRC_IN_TIMER As Boolean = False
    Protected Overridable ReadOnly Property SRC_MY_KEY As String = ""
    Public Overridable ReadOnly Property SRC_NO_COMPARE As Boolean = False

    Private _bMyNotPublic As Boolean = False
    Private _sTemplatePath As String = ""

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        _bMyNotPublic = bMyNotPublic
        _sTemplatePath = sTemplatePath
    End Sub

    Public MustOverride Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))

    ''' <summary>
    ''' oGpsPoint tylko w radio@home
    ''' </summary>
    ''' <param name="sId"></param>
    ''' <param name="sAddit"></param>
    ''' <param name="bInTimer"></param>
    ''' <param name="oGpsPoint"></param>
    ''' <returns></returns>
    Public MustOverride Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oGpsPoint As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))

    Protected Overridable Async Function GetREST(sCommand As String) As Task(Of String)
        ' przeciez jest rownolegle wiele serwerow odpytywanych, wiec sie przeplataja

        ' Windows.Web.Http.HttpClient oHttp = new Windows.Web.Http.HttpClient();
        Dim oHttp As New Net.Http.HttpClient()

        If SRC_HAS_KEY Then
            Dim sKey As String
            sKey = GetSettingsString(SRC_SETTING_NAME & "_apikey")

            If sKey.ToLower() = PrivateSwitch AndAlso _bMyNotPublic Then
                sKey = SRC_MY_KEY
                SetSettingsString(SRC_SETTING_NAME & "_apikey", sKey)
            End If

            If SRC_POMIAR_SOURCE = "DarkSky" Then
                sCommand = sKey & "/" & sCommand
            End If

            If SRC_POMIAR_SOURCE = "airly" Then
                oHttp.DefaultRequestHeaders.Add("Accept", "application/json")
                oHttp.DefaultRequestHeaders.Add("apikey", sKey)
            End If
        End If

        Dim oUri As Uri

        If _bMyNotPublic AndAlso Not String.IsNullOrEmpty(SRC_RESTURI_BASE_PKAR) Then
            oUri = New Uri(SRC_RESTURI_BASE_PKAR & sCommand)
        Else
            oUri = New Uri(SRC_RESTURI_BASE & sCommand)
        End If

        Dim mSeenUri As String = GetSettingsString("seenUri")   ' tylko w tej funkcji, ale ma być wspólne dla wszystkich klas!
        If mSeenUri.Contains("|" & oUri.Host & "|") Then
            Await Task.Delay(100)   ' nie wolno zasypywac serwera
        Else
            SetSettingsString("seenUri", mSeenUri & "|" & oUri.Host & "|")
        End If

        oHttp.Timeout = TimeSpan.FromSeconds(10)   ' domyslnie jest 100 sekund
        Dim oRes = ""

        Try
            oRes = Await oHttp.GetStringAsync(oUri)
        Catch
            oRes = ""
        End Try

        Return oRes
    End Function

    Protected moListaPomiarow As ObjectModel.Collection(Of JedenPomiar) = Nothing

    ''' <summary>
    ''' Wczytaj plik template
    ''' </summary>
    ''' <param name="sFileTitle">nazwa (bez ścieżki i bez extension</param>
    ''' <returns>z template lub empty</returns>
    Public Function FavTemplateLoad(sFileTitle As String, sSourceName As String) As JedenPomiar
        ' wersja poprzednia, bez VBLib, czytała z XML
        If Not SRC_HAS_TEMPLATES Then Return New JedenPomiar(sSourceName)
        If _sTemplatePath = "" Then Return New JedenPomiar(sSourceName)

        Dim sFilePath As String = IO.Path.Combine(_sTemplatePath, sFileTitle) & ".json"
        If IO.File.Exists(sFilePath) Then
            Try
                ' próba wczytania JSON
                Dim sContent As String = IO.File.ReadAllText(sFilePath)
                Return Newtonsoft.Json.JsonConvert.DeserializeObject(Of JedenPomiar)(sContent)
            Catch ex As Exception
            End Try
        End If

        Return New JedenPomiar(sSourceName)

    End Function

    Protected Sub FavTemplateSave(oItem As JedenPomiar)
        If Not SRC_HAS_TEMPLATES Then Return
        If _sTemplatePath = "" Then Return

        Dim sFileTitle As String = SRC_POMIAR_SOURCE & "_" & oItem.sId
        Dim sFilePath As String = IO.Path.Combine(_sTemplatePath, sFileTitle) & ".json"

        Dim sJson As String = Newtonsoft.Json.JsonConvert.SerializeObject(oItem, Newtonsoft.Json.Formatting.Indented)
        IO.File.WriteAllText(sFilePath, sJson)
    End Sub


    Public Sub FavTemplateSave()
        If Not SRC_HAS_TEMPLATES Then Return
        If _sTemplatePath = "" Then Return

        ' zapisz dane template sensorów
        Dim sNums = ""

        For Each oItem As JedenPomiar In App.moPomiaryAll

            If Not oItem.bDel AndAlso oItem.sSource = SRC_POMIAR_SOURCE Then
                If sNums.IndexOf(oItem.sId & "|") < 0 Then
                    FavTemplateSave(oItem)
                    sNums = sNums & oItem.sId & "|"
                End If
            End If
        Next
    End Sub

    'Public Overridable Sub ConfigCreate(ByVal oStack As StackPanel)
    '    ' potrzebne, gdy nie ma Headerow w ToggleSwitchach
    '    'if (!pkarmod.GetPlatform("uwp"))
    '    '{
    '    '    var oTH = new TextBlock();
    '    '    oTH.Text = SRC_SETTING_HEADER;
    '    '    oStack.Children.Add(oTH);
    '    '}

    '    Dim oTS = New ToggleSwitch()
    '    oTS.Header = SRC_SETTING_HEADER
    '    oTS.Name = "uiConfig_" & SRC_SETTING_NAME
    '    oTS.IsOn = pkarmod.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE)
    '    oStack.Children.Add(oTS)
    '    If Not SRC_HAS_KEY Then Return
    '    Dim oBind As Windows.UI.Xaml.Data.Binding = New Windows.UI.Xaml.Data.Binding()
    '    oBind.ElementName = oTS.Name
    '    oBind.Path = New PropertyPath("IsOn")
    '    Dim oTBox = New TextBox()
    '    oTBox.Header = SRC_SETTING_HEADER & " API key"
    '    oTBox.Name = "uiConfig_" & SRC_SETTING_NAME & "_Key"
    '    oTBox.Text = pkarmod.GetSettingsString(SRC_SETTING_NAME & "_apikey")
    '    oTBox.SetBinding(TextBox.IsEnabledProperty, oBind)
    '    oStack.Children.Add(oTBox)
    '    Dim oLink = New HyperlinkButton()
    '    oLink.Content = pkarmod.GetLangString("msgForAPIkey") ' "Aby uzyskać API key, zarejestruj się"
    '    oLink.NavigateUri = New Uri(SRC_KEY_LOGIN_LINK)
    '    oStack.Children.Add(oLink)
    'End Sub

    'Public Overridable Function ConfigDataOk(ByVal oStack As StackPanel) As String
    '    ' jesli nie ma Key, to na pewno poprawne
    '    If Not SRC_HAS_KEY Then Return ""

    '    ' jesli nie jest wlaczone, to tez jest poprawnie
    '    For Each oItem As UIElement In oStack.Children
    '        Dim oTS As ToggleSwitch
    '        oTS = TryCast(oItem, ToggleSwitch)

    '        If oTS IsNot Nothing Then
    '            If If(oTS.Name, "") Is If("uiConfig_" & SRC_SETTING_NAME, "") Then
    '                If Not oTS.IsOn Then Return ""
    '            End If
    '        End If
    '    Next

    '    For Each oItem As UIElement In oStack.Children
    '        Dim oTB As TextBox
    '        oTB = TryCast(oItem, TextBox)

    '        If oTB IsNot Nothing Then
    '            If If(oTB.Name, "") Is If("uiConfig_" & SRC_SETTING_NAME & "_Key", "") Then
    '                If oTB.Text.Length > 8 Then Return ""
    '                Return "Too short API key"
    '            End If
    '        End If
    '    Next

    '    Return "UIError - no API key"
    'End Function

    'Public Overridable Sub ConfigRead(ByVal oStack As StackPanel)
    '    For Each oItem As UIElement In oStack.Children
    '        Dim oTS As ToggleSwitch
    '        oTS = TryCast(oItem, ToggleSwitch)

    '        If oTS IsNot Nothing Then
    '            If If(oTS.Name, "") Is If("uiConfig_" & SRC_SETTING_NAME, "") Then
    '                pkarmod.SetSettingsBool(SRC_SETTING_NAME, oTS.IsOn)
    '                Exit For
    '            End If
    '        End If
    '    Next

    '    If Not SRC_HAS_KEY Then Return

    '    ' tylko gdy jest wlaczony
    '    For Each oItem As UIElement In oStack.Children
    '        Dim oTB As TextBox
    '        oTB = TryCast(oItem, TextBox)

    '        If oTB IsNot Nothing Then
    '            If If(oTB.Name, "") Is If("uiConfig_" & SRC_SETTING_NAME & "_Key", "") Then
    '                SetSettingsString(SRC_SETTING_NAME & "_apikey", oTB.Text, True)
    '                Exit For
    '            End If
    '        End If
    '    Next
    'End Sub

    Public Function Odleglosc2String(dOdl As Double) As String
        If dOdl < 10000 Then Return CInt(dOdl) & " m"
        Return CInt(dOdl / 1000) & " km"
    End Function
End Class

Partial Public Module Extensions
    ' z JVALUE

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedString(ByVal jVal As Newtonsoft.Json.Linq.JValue, sName As String, Optional sDefault As String = "") As String
        Dim sTmp As String

        Try
            sTmp = jVal(sName).ToString()
        Catch
            sTmp = sDefault
        End Try

        Return sTmp
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedNumber(ByVal jVal As Newtonsoft.Json.Linq.JValue, sName As String, Optional sDefault As Double = 0) As Double
        Dim sTmp As Double

        Try
            sTmp = CDbl(jVal(sName))
        Catch
            sTmp = sDefault
        End Try

        Return sTmp
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedValue(ByVal jVal As Newtonsoft.Json.Linq.JValue, sName As String) As Newtonsoft.Json.Linq.JValue
        Return TryCast(jVal(sName), Newtonsoft.Json.Linq.JValue)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedArray(ByVal jVal As Newtonsoft.Json.Linq.JValue, sName As String) As Newtonsoft.Json.Linq.JArray
        Return TryCast(jVal(sName), Newtonsoft.Json.Linq.JArray)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetObject(ByVal jVal As Newtonsoft.Json.Linq.JValue) As Newtonsoft.Json.Linq.JValue
        Return jVal
    End Function

    '  z JTOKEN
    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedString(ByVal jVal As Newtonsoft.Json.Linq.JToken, sName As String, Optional sDefault As String = "") As String
        Dim sTmp As String

        Try
            sTmp = jVal(sName).ToString()
        Catch
            sTmp = sDefault
        End Try

        Return sTmp
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedNumber(ByVal jVal As Newtonsoft.Json.Linq.JToken, sName As String, Optional sDefault As Double = 0) As Double
        Dim sTmp As Double

        Try
            If jVal(sName) Is Nothing Then Return sDefault
            sTmp = CDbl(jVal(sName))
        Catch
            sTmp = sDefault
        End Try

        Return sTmp
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedValue(ByVal jVal As Newtonsoft.Json.Linq.JToken, sName As String) As Newtonsoft.Json.Linq.JValue
        Return TryCast(jVal(sName), Newtonsoft.Json.Linq.JValue)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedArray(ByVal jVal As Newtonsoft.Json.Linq.JToken, sName As String) As Newtonsoft.Json.Linq.JArray
        Return TryCast(jVal(sName), Newtonsoft.Json.Linq.JArray)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedToken(ByVal jVal As Newtonsoft.Json.Linq.JToken, sName As String, Optional sDefault As String = "") As Newtonsoft.Json.Linq.JToken
        Return jVal(sName)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetObject(ByVal jVal As Newtonsoft.Json.Linq.JToken) As Newtonsoft.Json.Linq.JToken
        Return jVal
    End Function

    ' z JOBJECT

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedString(ByVal jVal As Newtonsoft.Json.Linq.JObject, ByVal sName As String, ByVal Optional sDefault As String = "") As String
        Dim sTmp As String

        Try
            sTmp = jVal(sName).ToString()
        Catch
            sTmp = sDefault
        End Try

        Return sTmp
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedToken(ByVal jVal As Newtonsoft.Json.Linq.JObject, ByVal sName As String, ByVal Optional sDefault As String = "") As Newtonsoft.Json.Linq.JToken
        Return jVal(sName)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedNumber(ByVal jVal As Newtonsoft.Json.Linq.JObject, ByVal sName As String, ByVal Optional sDefault As Double = 0) As Double
        Dim sTmp As Double

        If jVal(sName).Type = Newtonsoft.Json.Linq.JTokenType.Null Then Return sDefault

        Try
            sTmp = jVal(sName)
        Catch
            sTmp = sDefault
        End Try

        Return sTmp
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedValue(ByVal jVal As Newtonsoft.Json.Linq.JObject, sName As String) As Newtonsoft.Json.Linq.JValue
        Return TryCast(jVal(sName), Newtonsoft.Json.Linq.JValue)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedObject(ByVal jVal As Newtonsoft.Json.Linq.JObject, sName As String) As Newtonsoft.Json.Linq.JObject
        Return TryCast(jVal(sName), Newtonsoft.Json.Linq.JObject)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedArray(ByVal jVal As Newtonsoft.Json.Linq.JObject, sName As String) As Newtonsoft.Json.Linq.JArray
        Return TryCast(jVal(sName), Newtonsoft.Json.Linq.JArray)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetNamedBool(ByVal jVal As Newtonsoft.Json.Linq.JObject, sName As String) As Boolean
        Return CBool(jVal(sName))
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function GetObject(ByVal jVal As Newtonsoft.Json.Linq.JObject) As Newtonsoft.Json.Linq.JObject
        Return jVal
    End Function

    '' e, nie działa, bo musi być this, czyli dopiero dla zmiennej double może zadziałać?
    'Public Function ParseDefault(ByVal sStr As String, ByVal dDefault As Double) As Double
    '    Dim dDouble As Double
    '    If Not Double.TryParse(sStr, dDouble) Then dDouble = dDefault
    '    Return dDouble
    'End Function
End Module

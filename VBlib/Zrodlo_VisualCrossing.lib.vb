Imports System.Collections.ObjectModel
Imports pkar

Public Class Source_VisualCrossing
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceVisCross"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Visual Crossing"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "VisCross"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Public Overrides Property SRC_HAS_KEY As Boolean = True
    Public Overrides ReadOnly Property SRC_KEY_LOGIN_LINK As String = "https://www.visualcrossing.com/sign-up"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://www.visualcrossing.com/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://www.visualcrossing.com/"
    Public Overrides ReadOnly Property SRC_ZASIEG As Zasieg = Zasieg.World

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Public Overrides Async Function GetNearestAsync(oPos As BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        Return Await GetData(oPos, False)
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oGpsPoint As BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        Return Await GetData(oGpsPoint, bInTimer)
    End Function


    Private Async Function GetData(oGps As pkar.BasicGeopos, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        If GetSettingsString(SRC_SETTING_NAME & "_apikey").Length < 8 Then Return moListaPomiarow

        Dim sCmd As String = oGps.FormatLink("/%lat,%lon/") & Date.Now.ToString("yyyy-MM-ddTHH:00:00")
        sCmd &= "?include=fcst,current&unitGroup=metric&key=" & GetSettingsString(SRC_SETTING_NAME & "_apikey")
        sCmd &= "&elements=cloudcover,datetime,tzoffset,dew,feelslike,humidity,moonphase,snowdepth,stations,sunrise,sunset,moonrise,moonset,uvindex,visibility,windgust,windspeed"
        Dim sPage As String = Await GetREST(sCmd)
        'Dim sPage As String = "{""queryCost"":1,""latitude"":50.023,""longitude"":19.9791,""resolvedAddress"":""50.023,19.9791"",""address"":""50.023,19.9791"",""timezone"":""Europe/Warsaw"",""tzoffset"":1.0,""days"":[{""datetime"":""2023-01-17"",""tempmax"":40.0,""tempmin"":32.3,""temp"":34.3,""feelslikemax"":40.0,""feelslikemin"":32.3,""feelslike"":34.3,""dew"":32.5,""humidity"":82.2,""precip"":0.0,""preciptype"":[""rain"",""snow""],""snow"":0.0,""snowdepth"":0.0,""windgust"":9.5,""windspeed"":6.0,""winddir"":91.0,""pressure"":993.1,""cloudcover"":72.4,""visibility"":15.0,""uvindex"":1.0,""sunrise"":""07:31:35"",""sunset"":""16:09:03"",""moonphase"":0.88,""moonrise"":""02:45:10"",""moonset"":""11:39:37"",""icon"":""rain"",""stations"":null,""source"":""fcst""}],""stations"":{""E9997"":{""distance"":42986.0,""latitude"":49.967,""longitude"":20.574,""useCount"":0,""id"":""E9997"",""name"":""EW9997 Jasien PL"",""quality"":0,""contribution"":0.0},""EPKK"":{""distance"":14288.0,""latitude"":50.08,""longitude"":19.8,""useCount"":0,""id"":""EPKK"",""name"":""EPKK"",""quality"":50,""contribution"":0.0}},""currentConditions"":{""datetime"":""14:00:00"",""temp"":44.5,""feelslike"":39.1,""humidity"":65.6,""dew"":33.7,""precip"":null,""snow"":null,""snowdepth"":0.0,""preciptype"":null,""windgust"":null,""windspeed"":10.3,""winddir"":250.0,""pressure"":991.0,""visibility"":6.2,""cloudcover"":25.0,""uvindex"":null,""icon"":""partly-cloudy-day"",""stations"":[""E9997"",""EPKK""],""source"":""obs"",""sunrise"":""07:31:35"",""sunset"":""16:09:03"",""moonphase"":0.88}}"
        If sPage.Length < 10 Then Return moListaPomiarow

        Dim oData As VisCrosRoot = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(VisCrosRoot))

        If oData Is Nothing Then Return moListaPomiarow

        Dim templ As JedenPomiar = GetTemplate(sPage, oData)

        Dim oCurrCond As VisCrosCurrentDay = oData.currentConditions

        moListaPomiarow.Add(GetOnePomiar(templ, oCurrCond.feelslike, "°C", GetLangString("resTempOdczuwana")))
        moListaPomiarow.Add(GetOnePomiar(templ, oCurrCond.humidity, "%", GetLangString("resPomiarWilg")))
        moListaPomiarow.Add(GetOnePomiar(templ, oCurrCond.dew, "°C", GetLangString("resPomiarRosa")))
        moListaPomiarow.Add(GetOnePomiar(templ, oCurrCond.cloudcover, "%", GetLangString("resPomiarZachm")))

        If oData.days(0).uvindex IsNot Nothing Then
            Dim sngl As Double = GetDoubleOr0(oData.days(0).uvindex)
            Dim oUVindex As JedenPomiar = GetOnePomiar(templ, sngl, "", "UV index")
            If oUVindex.dCurrValue >= 6 Then oUVindex.sAlert = "!"
            If oUVindex.dCurrValue >= 8 Then oUVindex.sAlert = "!!"
            If oUVindex.dCurrValue >= 11 Then oUVindex.sAlert = "!!!"
            oUVindex.sLimity = "WHO exposure categories" & vbCrLf & "Low" & vbTab & " <3" & vbCrLf & "Moderate" & vbTab & " 3..5" & vbLf & "High" & vbTab & " 6..7 (seek shade during midday)" & vbLf & "Very high" & vbTab & " 8..10 (avoid being outside midday)" & vbLf & "Extreme" & vbTab & " >10" & vbCrLf
            moListaPomiarow.Add(oUVindex)
        End If

        moListaPomiarow.Add(GetOnePomiar(templ, oCurrCond.visibility, "km", GetLangString("resPomiarWidocz")))

        ' dodatkowe pomiary, które niby mogłyby nie być ściągane przy InTimer

        If oCurrCond.snowdepth > 0 Then
            moListaPomiarow.Add(GetOnePomiar(templ, oCurrCond.snowdepth, "", GetLangString("resSnowDepth")))
        End If
        moListaPomiarow.Add(GetOnePomiar(templ, oCurrCond.windspeed, "km/h", GetLangString("resPomiarWind")))
        If oData.days IsNot Nothing AndAlso oData.days.Count > 0 AndAlso oData.days(0).windgust IsNot Nothing Then
            Dim sngl As Double = GetDoubleOr0(oData.days(0).windgust)
            moListaPomiarow.Add(GetOnePomiar(templ, sngl, "km/h", GetLangString("resPomiarWindGust")))
        End If

        Dim sRiseSet As String = oCurrCond.sunrise & " - " & oCurrCond.sunset
        sRiseSet = sRiseSet.Replace(Date.Now.ToString("yyyy-MM-dd"), "").Replace("T", "")
        moListaPomiarow.Add(GetOnePomiar(templ, sRiseSet, "", GetLangString("resSlonce")))

        moListaPomiarow.Add(GetOnePomiar(templ, oCurrCond.moonphase, "%", GetLangString("resMoonPhase")))

        If oData.days IsNot Nothing AndAlso oData.days.Count > 0 Then
            sRiseSet = oData.days(0).moonrise & " - " & oData.days(0).moonset
            sRiseSet = sRiseSet.Replace(Date.Now.ToString("yyyy-MM-dd"), "").Replace("T", "")
            moListaPomiarow.Add(GetOnePomiar(templ, sRiseSet, "", GetLangString("resKsiezyc")))
        End If


        Return moListaPomiarow

        ' *TODO* alerty, ale na razie nie wiem jaką mają strukturę
        ' event: string, description: string, ...
    End Function


    Private Function GetDoubleOr0(value As Object) As Double
        If value Is Nothing Then Return 0
        Dim temp As Double
        temp = value
        Return temp
    End Function

    ''' <summary>
    ''' wygeneruj template - wpisz do niej source, oraz dane najbliższej stacji
    ''' </summary>
    Private Function GetTemplate(sJSON As String, oDataResult As VisCrosRoot) As JedenPomiar

        Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)

        Dim iInd As Integer = sJSON.IndexOf("""stations"":{""")
        If iInd < 10 Then Return oNew

        Dim stacje As String = sJSON.Substring(iInd)


        Dim iMinOdl As Integer = Integer.MaxValue
        For Each sStation As String In oDataResult.currentConditions.stations
            iInd = stacje.IndexOf(sStation)
            If iInd < 4 Then Continue For
            iInd = stacje.IndexOf("{", iInd)
            Dim iInd1 As Integer = stacje.IndexOf("}", iInd)

            Dim oStacja As VisCrosStation = Newtonsoft.Json.JsonConvert.DeserializeObject(stacje.Substring(iInd, iInd1 - iInd + 1), GetType(VisCrosStation))
            iMinOdl = Math.Min(iMinOdl, oStacja.distance)
        Next

        oNew.dOdl = iMinOdl
        oNew.sOdl = "≥ " & CInt((iMinOdl / 1000)) & " km"

        oNew.sTimeStamp = oDataResult.days(0).datetime & " " & oDataResult.currentConditions.datetime

        Return oNew
    End Function

    Private Function GetOnePomiar(oTemplate As JedenPomiar, dValue As Double, sUnit As String, sPomiar As String) As JedenPomiar

        Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = oTemplate.sOdl
                }
        oNew.dCurrValue = dValue
        oNew.sUnit = " " & sUnit
        oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
        oNew.sPomiar = sPomiar

        Return oNew
    End Function


    Private Function GetOnePomiar(oTemplate As JedenPomiar, sValue As String, sUnit As String, sPomiar As String) As JedenPomiar

        Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = oTemplate.sOdl
                }
        oNew.dCurrValue = 0
        oNew.sUnit = " " & sUnit
        oNew.sCurrValue = sValue & oNew.sUnit
        oNew.sPomiar = sPomiar

        Return oNew
    End Function

    Public Class VisCrosRoot
        Public Property queryCost As Integer
        'Public Property latitude As Single
        'Public Property longitude As Single
        'Public Property resolvedAddress As String
        'Public Property address As String
        'Public Property timezone As String
        Public Property tzoffset As Double
        'Public Property description As String
        Public Property days As List(Of VisCrosCurrentDay)
        'Public Property alerts() As VisCrosAlert
        ' Public Property stations As Stations
        Public Property currentConditions As VisCrosCurrentDay
    End Class

    'Public Class VisCrosAlert
    '    Public Property [event] As String
    '    Public Property description As String
    ' i pewnie coś jeszcze, teraz nieznane
    'End Class

    Public Class VisCrosCurrentDay
        Public Property datetime As String  ' w local time
        'Public Property datetimeEpoch As Integer
        'Public Property temp As Single
        'Public Property tempmax As Single   ' nie current
        'Public Property tempmin As Single   ' nie current
        Public Property feelslike As Double ' DarkSky
        'Public Property feelslikemax As Single  ' nie current
        'Public Property feelslikemin As Single  ' nie current
        Public Property humidity As Double
        Public Property dew As Double   ' DarkSky
        'Public Property precip As Single
        'Public Property precipprob As Integer
        'Public Property snow As Single
        Public Property snowdepth As Double
        'Public Property preciptype As Object
        'Public Property precipcover As Integer  ' nie current
        Public Property windgust As Object
        Public Property windspeed As Double
        'Public Property winddir As Integer
        'Public Property pressure As Integer
        Public Property visibility As Double   ' DarkSky
        Public Property cloudcover As Double   ' DarkSky
        'Public Property solarradiation As Integer
        'Public Property solarenergy As Single
        Public Property uvindex As Object   ' DarkSky
        'Public Property severerisk As Integer  ' nie current
        'Public Property conditions As String
        'Public Property icon As String
        Public Property stations As List(Of String)
        'Public Property source As String
        Public Property sunrise As String
        'Public Property sunriseEpoch As Integer
        Public Property sunset As String
        'Public Property sunsetEpoch As Integer
        Public Property moonrise As String
        Public Property moonset As String
        Public Property moonphase As Double
        'Public Property description As String  ' nie current

    End Class

    Public Class VisCrosStation
        Public Property distance As Single
        'Public Property latitude As Single
        'Public Property longitude As Single
        'Public Property useCount As Integer
        'Public Property id As String
        'Public Property name As String
        'Public Property quality As Integer
        'Public Property contribution As Single
    End Class

End Class

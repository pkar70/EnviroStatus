Partial Public Class Source_DarkSky
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceDarkSky"
    Protected Overrides Property SRC_SETTING_HEADER As String = "Dark Sky"
    Protected Overrides Property SRC_RESTURI_BASE As String = "https://api.darksky.net/forecast/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "DarkSky"
    Protected Overrides Property SRC_HAS_KEY As Boolean = True
    Protected Overrides Property SRC_KEY_LOGIN_LINK As String = "https://darksky.net/dev/register"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True

    Public Overrides Sub ReadResStrings()
        SetSettingsString("resTempOdczuwana", GetLangString("resTempOdczuwana"))
        SetSettingsString("resPomiarWidocz", GetLangString("resPomiarWidocz"))
        SetSettingsString("resPomiarRosa", GetLangString("resPomiarRosa"))
        SetSettingsString("resPomiarZachm", GetLangString("resPomiarZachm"))
    End Sub

    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Return Await GetDataFromFavSensor(oPos.X, oPos.Y, False)    ' bo tak :) 
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        ' w tym wypadku to Lat i Long
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        Dim sPage As String = Await GetREST(sId & "," & sAddit & "?units=si&exclude=minutely,hourly,daily")

        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonValue = Nothing
        Try
            oJson = Windows.Data.Json.JsonValue.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            If Not bInTimer Then Await DialogBox("ERROR: JSON parsing error - sPage " & SRC_POMIAR_SOURCE)
            Return moListaPomiarow
        End If

        'Public Property sPomiar As String = "" ' jaki pomiar (np. PM10)
        'Public Property sCurrValue As String = "" ' etap 2: wartosc
        'Public Property dCurrValue As Double = 0
        'Public Property sUnit As String = ""
        'Public Property sTimeStamp As String = "" ' etap 2: kiedy
        'Public Property sAddit As String = ""
        'Public Property sOdl As String = ""
        'oItem.sAlert - wykrzykniki
        'oNew.sSource = SRC_POMIAR_SOURCE

        '"flags"
        '{
        '"sources":	["meteoalarm","cmc","gfs","icon","isd","madis"],
        '	"meteoalarm-license":"Based on data from EUMETNET - MeteoAlarm [https://www.meteoalarm.eu/]. Time delays between this website and the MeteoAlarm website are possible; for the most up to date information about alert levels as published by the participating National Meteorological Services please use the MeteoAlarm website.",
        '	"nearest-station":14.302,	[km]

        Dim dOdl As Double = 654321.0    ' jak nie ma podanej, to uznaj że daleka (654 km)
        Try
            Dim oJsonFlags As Windows.Data.Json.IJsonValue
            oJsonFlags = oJson.GetObject().GetNamedValue("flags")
            dOdl = oJsonFlags.GetObject().GetNamedNumber("nearest-station") * 1000  ' z km na metry
        Catch ex As Exception
        End Try

        ' If oJson.GetObject.Values.Contains("alerts") Then

        If sPage.Contains("""alerts""") Then ' prymitywne, ale czasem zadziala
            Try
                '"alerts" [
                '  {
                '    "title": "Flood Watch for Mason, WA",
                '    "time": 1509993360,
                '    "expires": 1510036680,
                '    "description": "...FLOOD WATCH REMAINS IN EFFECT THROUGH LATE MONDAY NIGHT...\nTHE FLOOD WATCH CONTINUES FOR\n* A PORTION OF NORTHWEST WASHINGTON...INCLUDING THE FOLLOWING\nCOUNTY...MASON.\n* THROUGH LATE FRIDAY NIGHT\n* A STRONG WARM FRONT WILL BRING HEAVY RAIN TO THE OLYMPICS\nTONIGHT THROUGH THURSDAY NIGHT. THE HEAVY RAIN WILL PUSH THE\nSKOKOMISH RIVER ABOVE FLOOD STAGE TODAY...AND MAJOR FLOODING IS\nPOSSIBLE.\n* A FLOOD WARNING IS IN EFFECT FOR THE SKOKOMISH RIVER. THE FLOOD\nWATCH REMAINS IN EFFECT FOR MASON COUNTY FOR THE POSSIBILITY OF\nAREAL FLOODING ASSOCIATED WITH A MAJOR FLOOD.\n",
                '    "uri": "http://alerts.weather.gov/cap/wwacapget.php?x=WA1255E4DB8494.FloodWatch.1255E4DCE35CWA.SEWFFASEW.38e78ec64613478bb70fc6ed9c87f6e6"
                '  },
                Dim oJsonAlerts As Windows.Data.Json.JsonArray
                oJsonAlerts = oJson.GetObject().GetNamedArray("alerts")

                Dim iCnt As Integer = 1

                For Each oJSonAlert As Windows.Data.Json.IJsonValue In oJsonAlerts
                    Dim oNew As JedenPomiar = New JedenPomiar
                    oNew.sSource = SRC_POMIAR_SOURCE
                    oNew.dOdl = dOdl
                    oNew.sOdl = "≥ " & CInt(dOdl / 1000) & " km"
                    oNew.sAlert = "!!"  ' w miarę ważne
                    oNew.sTimeStamp = App.UnixTimeToTime(oJSonAlert.GetObject.GetNamedNumber("time"))
                    oNew.sCurrValue = oJSonAlert.GetObject.GetNamedString("title")
                    Dim sTmp As String
                    sTmp = oJSonAlert.GetObject.GetNamedString("description") ' description & expires
                    sTmp = sTmp.Replace("%lf", vbCrLf)  ' się takie zdażyło
                    oNew.sAddit = sTmp

                    oNew.sPomiar = "Alert" & iCnt

                    Select Case oJSonAlert.GetObject.GetNamedString("severity")
                        Case "advisory"
                            oNew.sAlert = "!"
                        Case "watch"
                            oNew.sAlert = "!!"
                        Case "warning"
                            oNew.sAlert = "!!!"
                    End Select

                    moListaPomiarow.Add(oNew)

                    iCnt += 1
                Next

            Catch ex As Exception

            End Try
        End If

        Try
            '"currently"
            '{
            '"time":1553454247,
            '"apparentTemperature":7.54,
            '"dewPoint":2.88,
            '"cloudCover":1,
            '"uvIndex":0,
            '"visibility":9.51,
            '"ozone":344.5
            Dim oTemplate As JedenPomiar = New JedenPomiar
            oTemplate.sSource = SRC_POMIAR_SOURCE

            Dim oJsonCurrent As Windows.Data.Json.IJsonValue
            oJsonCurrent = oJson.GetObject().GetNamedValue("currently")
            oTemplate.sTimeStamp = App.UnixTimeToTime(oJsonCurrent.GetObject().GetNamedNumber("time"))
            oTemplate.dOdl = dOdl
            oTemplate.sOdl = "≥ " & CInt(dOdl / 1000) & " km"

            ' i to powtorzyc dla kazdego pomiaru
            Try
                Dim oNew As JedenPomiar = New JedenPomiar With {
                    .sSource = oTemplate.sSource, .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl, .sOdl = oTemplate.sOdl}
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("apparentTemperature")
                oNew.sUnit = " °C"
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                oNew.sPomiar = GetSettingsString("resTempOdczuwana")
                moListaPomiarow.Add(oNew)
            Catch ex As Exception
            End Try

            Try
                Dim oNew As JedenPomiar = New JedenPomiar With {
                    .sSource = oTemplate.sSource, .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl, .sOdl = oTemplate.sOdl}
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("dewPoint")
                oNew.sUnit = " °C"
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                oNew.sPomiar = GetSettingsString("resPomiarRosa")
                moListaPomiarow.Add(oNew)
            Catch ex As Exception
            End Try

            Try
                Dim oNew As JedenPomiar = New JedenPomiar With {
                    .sSource = oTemplate.sSource, .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl, .sOdl = oTemplate.sOdl}
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("cloudCover") * 100
                oNew.sUnit = " %"
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                oNew.sPomiar = GetSettingsString("resPomiarZachm")
                moListaPomiarow.Add(oNew)
            Catch ex As Exception
            End Try

            Try
                Dim oNew As JedenPomiar = New JedenPomiar With {
                    .sSource = oTemplate.sSource, .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl, .sOdl = oTemplate.sOdl}
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("uvIndex")
                oNew.sUnit = ""
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                oNew.sPomiar = "UV index"
                ' przekroczenia
                ' http://www.who.int/uv/publications/en/UVIGuide.pdf
                If oNew.dCurrValue >= 6 Then oNew.sAlert = "!"
                If oNew.dCurrValue >= 8 Then oNew.sAlert = "!!"
                If oNew.dCurrValue >= 11 Then oNew.sAlert = "!!!"
                ' moderate, very high - poniewaz Tab jest za daleko...
                oNew.sLimity = "WHO exposure categories" & vbCrLf &
                    "Low" & vbTab & "<3" & vbCrLf &
                    "Moderate 3..5" & vbCrLf &
                    "High" & vbTab & "6..7 (seek shade during midday)" & vbCrLf &
                    "Very high 8..10 (avoid being outside midday)" & vbCrLf &
                    "Extreme" & vbTab & ">10" & vbCrLf
                moListaPomiarow.Add(oNew)
            Catch ex As Exception
            End Try

            Try
                Dim oNew As JedenPomiar = New JedenPomiar With {
                    .sSource = oTemplate.sSource, .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl, .sOdl = oTemplate.sOdl}
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("visibility")
                oNew.sUnit = " km"
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                oNew.sPomiar = GetSettingsString("resPomiarWidocz")
                moListaPomiarow.Add(oNew)
            Catch ex As Exception
            End Try

        Catch ex As Exception

        End Try


        If moListaPomiarow.Count < 1 Then
            If Not bInTimer Then Await DialogBox("ERROR: data parsing error " & SRC_POMIAR_SOURCE)
            Return moListaPomiarow
        End If

        Return moListaPomiarow

    End Function

End Class


' 2019.10.30 SolarWindTemp może być null - wtedy liczy jako zero.

'Partial Public Class App
'    Public Shared moSrc_NoaaWind As Source_NoaaWind = New Source_NoaaWind
'End Class

Public Class Source_NoaaWind
    Inherits Source_Base
    ' ułatwienie dodawania następnych
    Protected Overrides Property SRC_SETTING_NAME As String = "sourceNoaaWind"
    Protected Overrides Property SRC_SETTING_HEADER As String = "NOAA solar wind"
    Protected Overrides Property SRC_RESTURI_BASE As String = "https://services.swpc.noaa.gov/products/solar-wind/plasma-5-minute.json"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "NOAAwind"

    Public Overrides Sub ReadResStrings()
        SetSettingsString("resPomiarSolarWindDensity", GetLangString("resPomiarSolarWindDensity"))
        SetSettingsString("resPomiarSolarWindSpeed", GetLangString("resPomiarSolarWindSpeed"))
        SetSettingsString("resPomiarAdditSolarWindDensity", GetLangString("resPomiarAdditSolarWindDensity"))
        SetSettingsString("resPomiarAdditSolarWindSpeed", GetLangString("resPomiarAdditSolarWindSpeed"))
        SetSettingsString("resPomiarSolarWindTemp", GetLangString("resPomiarSolarWindTemp"))
        SetSettingsString("resPomiarAdditSolarWindTemp", GetLangString("resPomiarAdditSolarWindTemp"))

    End Sub

    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Return Await GetDataFromFavSensor("", "", False)
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME) Then Return moListaPomiarow

        Dim sPage As String = Await GetREST("")

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

        ' [
        ' ["time_tag","density","speed","temperature"],
        ' ["2019-05-31 10:54:00.000","2.95","433.7","56729"],
        ' ["2019-05-31 10:55:00.000","2.98","432.2","57292"],
        ' ["2019-05-31 10:56:00.000","2.93","431.0","54333"]
        ']
        'density: 1 / cm³, speed: km/ s, temp °K [5.76E4, 54333]

        Dim oJsonArray As Windows.Data.Json.JsonArray
        oJsonArray = oJson.GetArray
        Dim oJSonLast As Windows.Data.Json.JsonArray
        oJSonLast = oJsonArray.Last.GetArray

        Dim oDate As Date
        Dim sTime As String = oJSonLast.Item(0).GetString
        If Date.TryParseExact(sTime, "yyyy-MM-dd HH:mm:ss.000", Nothing, Globalization.DateTimeStyles.AssumeUniversal, oDate) Then
            sTime = oDate.ToLocalTime.ToString("yyyy-MM-dd HH:mm:ss")
        Else
            sTime = sTime & " UTC"
        End If


        ' density
        Dim oNew As JedenPomiar = New JedenPomiar
        oNew.sSource = SRC_POMIAR_SOURCE
        oNew.sTimeStamp = sTime
        oNew.sUnit = "/cm³"
        oNew.sCurrValue = oJSonLast.Item(1).GetString
        Double.TryParse(oNew.sCurrValue, oNew.dCurrValue)
        oNew.sCurrValue = oNew.sCurrValue & "/cm³"
        oNew.sPomiar = GetSettingsString("resPomiarSolarWindDensity")
        oNew.sAddit = GetSettingsString("resPomiarAdditSolarWindDensity")

        moListaPomiarow.Add(oNew)

        ' speed
        oNew = New JedenPomiar
        oNew.sSource = SRC_POMIAR_SOURCE
        oNew.sTimeStamp = sTime
        oNew.sUnit = "km/s"
        oNew.sCurrValue = oJSonLast.Item(2).GetString
        Double.TryParse(oNew.sCurrValue, oNew.dCurrValue)
        oNew.sCurrValue = oNew.sCurrValue & " " & oNew.sUnit
        oNew.sPomiar = GetSettingsString("resPomiarSolarWindSpeed")
        oNew.sAddit = GetSettingsString("resPomiarAdditSolarWindSpeed")
        ' oNew.sAddit = "= " & oNew.dCurrValue * 3600 & " km/h" - bez sensu! 400 km/s dawaloby 1.4 mln km/h

        moListaPomiarow.Add(oNew)

        ' temp
        oNew = New JedenPomiar
        oNew.sSource = SRC_POMIAR_SOURCE
        oNew.sTimeStamp = sTime
        oNew.sUnit = " K"
        Try
            oNew.sCurrValue = oJSonLast.Item(3).GetString
        Catch ex As Exception
            oNew.sCurrValue = "0"
        End Try
        Double.TryParse(oNew.sCurrValue, oNew.dCurrValue)
        If oNew.sCurrValue.Length > 4 Then
            oNew.sCurrValue = oNew.sCurrValue.Substring(0, oNew.sCurrValue.Length - 3) & " " & oNew.sCurrValue.Substring(oNew.sCurrValue.Length - 3)
        End If
        oNew.sCurrValue = oNew.sCurrValue & " " & oNew.sUnit
        oNew.sPomiar = GetSettingsString("resPomiarSolarWindTemp")
        oNew.sAddit = GetSettingsString("resPomiarAdditSolarWindTemp")

        moListaPomiarow.Add(oNew)


        Return moListaPomiarow
    End Function
End Class

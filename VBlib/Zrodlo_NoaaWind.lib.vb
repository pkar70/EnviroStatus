Imports System.Linq
Imports System.Collections.ObjectModel
Imports System.Globalization

Public Class Source_NoaaWind
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceNoaaWind"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "NOAA solar wind"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://services.swpc.noaa.gov/products/solar-wind/plasma-5-minute.json"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "NOAAwind"
    Public Overrides ReadOnly Property SRC_NO_COMPARE As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://www.swpc.noaa.gov/products/real-time-solar-wind"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://www.swpc.noaa.gov/products/real-time-solar-wind"

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Public Overrides Async Function GetNearestAsync(ByVal oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await GetDataFromFavSensorAsync("", "", False, Nothing)
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        Dim sPage As String = Await GetREST("")
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim bError As Boolean = False
        Dim sErr As String = ""
        Dim oJsonArray As Newtonsoft.Json.Linq.JArray = Nothing

        Try
            oJsonArray = Newtonsoft.Json.Linq.JArray.Parse(sPage)
        Catch ex As Exception
            sErr = ex.Message
            bError = True
        End Try

        If bError Then
            If Not bInTimer Then Await DialogBoxAsync($"ERROR: JSON parsing error - getting sensor data ({SRC_POMIAR_SOURCE})" & vbCrLf & " " & sErr)
            Return moListaPomiarow
        End If

        Dim oJSonLast As Newtonsoft.Json.Linq.JArray
        oJSonLast = CType(oJsonArray(oJsonArray.Count() - 1), Newtonsoft.Json.Linq.JArray)
        Dim oDate As DateTime
        Dim sTime As String
        sTime = oJSonLast(0)

        If DateTime.TryParseExact(sTime, "yyyy-MM-dd HH:mm:ss.000", Nothing, System.Globalization.DateTimeStyles.AssumeUniversal, oDate) Then
            sTime = oDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        Else
            sTime &= " UTC"
        End If

        Dim oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
        oNew.sTimeStamp = sTime
        oNew.sUnit = "/cm³"
        oNew.sCurrValue = oJSonLast(1).ToString()
        Double.TryParse(oNew.sCurrValue, Globalization.NumberStyles.Float, NumberFormatInfo.InvariantInfo, oNew.dCurrValue)
        oNew.sCurrValue = oNew.sCurrValue & "/cm³"
        oNew.sPomiar = GetLangString("resPomiarSolarWindDensity")
        oNew.sAddit = GetLangString("resPomiarAdditSolarWindDensity")
        moListaPomiarow.Add(oNew)
        oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
        oNew.sTimeStamp = sTime
        oNew.sUnit = "km/s"
        oNew.sCurrValue = oJSonLast(2).ToString()
        Double.TryParse(oNew.sCurrValue, Globalization.NumberStyles.Float, NumberFormatInfo.InvariantInfo, oNew.dCurrValue)
        oNew.sCurrValue = oNew.sCurrValue & " " + oNew.sUnit
        oNew.sPomiar = GetLangString("resPomiarSolarWindSpeed")
        oNew.sAddit = GetLangString("resPomiarAdditSolarWindSpeed")
        moListaPomiarow.Add(oNew)
        oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
        oNew.sTimeStamp = sTime
        oNew.sUnit = " K"

        Try
            oNew.sCurrValue = oJSonLast(3)
        Catch
            oNew.sCurrValue = "0"
        End Try

        Double.TryParse(oNew.sCurrValue, Globalization.NumberStyles.Float, NumberFormatInfo.InvariantInfo, oNew.dCurrValue)
        If oNew.sCurrValue.Length > 4 Then oNew.sCurrValue = oNew.sCurrValue.Substring(0, oNew.sCurrValue.Length - 3) & " " & oNew.sCurrValue.Substring(oNew.sCurrValue.Length - 3)
        oNew.sCurrValue = oNew.sCurrValue & " " & oNew.sUnit
        oNew.sPomiar = GetLangString("resPomiarSolarWindTemp")
        oNew.sAddit = GetLangString("resPomiarAdditSolarWindTemp")
        moListaPomiarow.Add(oNew)
        Return moListaPomiarow
    End Function
End Class

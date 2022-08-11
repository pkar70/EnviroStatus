Imports System.Collections.ObjectModel
Imports System

Public Class Source_NoaaKindex
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceNoaaKind"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "NOAA planetary K-index (magnetic activity)"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://services.swpc.noaa.gov/products/noaa-planetary-k-index-forecast.json"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "NOAAkind"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Public Overrides ReadOnly Property SRC_NO_COMPARE As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://www.swpc.noaa.gov/products/planetary-k-index"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://www.swpc.noaa.gov/products/planetary-k-index"
    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await GetDataFromFavSensorAsync("", "", False, Nothing)
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
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

        Dim oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
        oNew.sPomiar = "Kp index"
        Dim sNowyPomiar As String = ""
        Dim sNowyTime As String = ""

        For Each oJsonOdczyt As Newtonsoft.Json.Linq.JToken In oJsonArray

            If oJsonOdczyt(2).ToString() = "estimated" Then
                sNowyPomiar = oJsonOdczyt(1).ToString()
                sNowyTime = oJsonOdczyt(0).ToString()
                Exit For
            End If

            If oJsonOdczyt(2).ToString() <> "observed" Then Exit For
            oNew.sTimeStamp = oJsonOdczyt(0).ToString()
            oNew.sCurrValue = oJsonOdczyt(1).ToString()
        Next

        If String.IsNullOrEmpty(oNew.sTimeStamp) Then Return moListaPomiarow
        Dim oDate As DateTime
        Dim sTime As String = oNew.sTimeStamp

        If DateTime.TryParseExact(sTime, "yyyy-MM-dd HH:mm:ss", Nothing, System.Globalization.DateTimeStyles.AssumeUniversal, oDate) Then
            sTime = oDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        Else
            sTime = sTime & " UTC"
        End If

        oNew.sTimeStamp = sTime
        oNew.dCurrValue = oNew.sCurrValue

        If DateTime.TryParseExact(sNowyTime, "yyyy-MM-dd HH:mm:ss", Nothing, System.Globalization.DateTimeStyles.AssumeUniversal, oDate) Then
            sNowyTime = oDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        Else
            sNowyTime = sNowyTime & " UTC"
        End If

        oNew.sAddit = GetLangString("resPomiarNoaaKindexPredicted") & $" {sNowyTime}: {sNowyPomiar}"
        If oNew.dCurrValue >= 7 Then oNew.sAlert = "!"
        If oNew.dCurrValue >= 8 Then oNew.sAlert = "!!"
        If oNew.dCurrValue >= 9 Then oNew.sAlert = "!!!"
        moListaPomiarow.Add(oNew)
        Return moListaPomiarow
    End Function
End Class

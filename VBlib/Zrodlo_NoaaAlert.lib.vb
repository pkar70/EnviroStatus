Imports System.Collections.ObjectModel
Imports System

Public Class Source_NoaaAlert
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceNoaaAlert"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "NOAA alerts"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://services.swpc.noaa.gov/products/alerts.json"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "NOAAalert"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Public Overrides ReadOnly Property SRC_NO_COMPARE As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://www.swpc.noaa.gov/products/alerts-watches-and-warnings"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://www.swpc.noaa.gov/products/alerts-watches-and-warnings"

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await GetDataFromFavSensorAsync("", "", False, Nothing)
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(ByVal sId As String, ByVal sAddit As String, ByVal bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
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

        Dim iGuard As Integer = 0

        For Each oJsonVal As Newtonsoft.Json.Linq.JToken In oJsonArray
            Dim sThisTime As String = oJsonVal.GetObject().GetNamedString("issue_datetime")
            iGuard += 1
            If iGuard > 5 Then Exit For
            Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)
            oNew.sTimeStamp = sThisTime
            Dim iInd As Integer
            iInd = sThisTime.IndexOf(".")
            If iInd > 0 Then sThisTime = sThisTime.Substring(0, iInd)
            Dim oDate As DateTime

            If DateTime.TryParseExact(sThisTime, "yyyy-MM-dd HH:mm:ss", Nothing, System.Globalization.DateTimeStyles.AssumeUniversal, oDate) Then
                oNew.sTimeStamp = oDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                If oDate.AddDays(1) < DateTime.Now Then Continue For
            Else
                oNew.sTimeStamp = sThisTime & " UTC"
            End If

            If iGuard > 1 Then
                oNew.sPomiar = "Alert" & iGuard.ToString()
            Else
                oNew.sPomiar = "Alert"
            End If

            Dim sMsg As String = oJsonVal.GetObject().GetNamedString("message")
            sMsg = sMsg.Replace("NOAA Space Weather Scale descriptions can be found at", "")
            sMsg = sMsg.Replace("www.swpc.noaa.gov/noaa-scales-explanation", "")
            sMsg = sMsg.Replace("\r\n\r\n", "\r\n")
            sMsg = sMsg.Replace("\r\n", vbLf)
            Dim aTmp = sMsg.Split(vbLf)

            For Each sLine As String In aTmp
                iInd = sLine.IndexOf(":")
                If sLine.Contains("Space Weather Message Code") Then oNew.sPomiar = "N." & sLine.Substring(iInd + 1).Trim()

                If sLine.Contains("ALERT:") Then
                    oNew.sCurrValue = sLine.Substring(iInd + 1).Trim()
                    oNew.sAlert = "!!!"
                End If

                If sLine.Contains("WATCH:") Then
                    oNew.sCurrValue = sLine.Substring(iInd + 1).Trim()
                    oNew.sAlert = "!!"
                End If

                If sLine.Contains("WARNING:") Then
                    oNew.sCurrValue = sLine.Substring(iInd + 1).Trim()
                    oNew.sAlert = "!"
                End If
            Next

            oNew.sAddit = sMsg
            moListaPomiarow.Add(oNew)
        Next

        Return moListaPomiarow
    End Function
End Class

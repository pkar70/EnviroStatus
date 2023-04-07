Imports System.Linq
Imports System.Collections.ObjectModel

Partial Public Class Source_DarkSky
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceDarkSky"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Dark Sky"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://api.darksky.net/forecast/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "DarkSky"
    Public Overrides Property SRC_HAS_KEY As Boolean = True
    Public Overrides ReadOnly Property SRC_KEY_LOGIN_LINK As String = "https://darksky.net/dev/register"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://darksky.net/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://darksky.net/"

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Public Overrides Async Function GetNearestAsync(oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()
        Return Await GetDataFromFavSensorAsync(oPos.Latitude.ToString(), oPos.Longitude.ToString(), False, Nothing)
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        Dim sPage As String = Await GetREST(sId & "," & sAddit & "?units=si&exclude=minutely,hourly,daily")
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim bError As Boolean = False
        Dim sErr As String = ""
        Dim oJson As Newtonsoft.Json.Linq.JObject = Nothing

        Try
            oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage)
        Catch ex As Exception
            sErr = ex.Message
            bError = True
        End Try

        If bError Then
            If Not bInTimer Then Await DialogBoxAsync($"ERROR: JSON parsing error - getting sensor data ({SRC_POMIAR_SOURCE})" & vbCrLf & " " & sErr)
            Return moListaPomiarow
        End If

        Dim dOdl As Double = 654321.0

        Try
            Dim oJsonFlags As Newtonsoft.Json.Linq.JToken
            oJsonFlags = oJson.GetObject().GetNamedToken("flags")
            dOdl = oJsonFlags.GetObject().GetNamedNumber("nearest-station") * 1000
        Catch
        End Try

        If sPage.Contains("""alerts""") Then

            Try
                Dim oJsonAlerts As Newtonsoft.Json.Linq.JArray
                oJsonAlerts = oJson.GetObject().GetNamedArray("alerts")
                Dim iCnt As Integer = 1

                For Each oJSonAlert As Newtonsoft.Json.Linq.JToken In oJsonAlerts
                    Dim oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
                    oNew.dOdl = dOdl
                    oNew.sOdl = "≥ " & CInt((dOdl / 1000)) & " km"
                    oNew.sAlert = "!!"
                    oNew.sTimeStamp = DateTimeOffset.FromUnixTimeSeconds(CLng(oJSonAlert.GetObject().GetNamedNumber("time"))).ToString()
                    oNew.sCurrValue = oJSonAlert.GetObject().GetNamedString("title")
                    Dim sTmp As String
                    sTmp = oJSonAlert.GetObject().GetNamedString("description")
                    sTmp = sTmp.Replace("%lf", vbLf)
                    oNew.sAddit = sTmp
                    oNew.sPomiar = "Alert" & iCnt

                    Select Case oJSonAlert.GetObject().GetNamedString("severity")
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

            Catch
            End Try
        End If

        Try
            Dim oTemplate = New JedenPomiar(SRC_POMIAR_SOURCE)
            Dim oJsonCurrent As Newtonsoft.Json.Linq.JToken
            oJsonCurrent = oJson.GetObject().GetNamedToken("currently")
            oTemplate.sTimeStamp = DateTimeOffset.FromUnixTimeSeconds(CLng(oJsonCurrent.GetObject().GetNamedNumber("time"))).ToString()
            oTemplate.dOdl = dOdl
            oTemplate.sOdl = "≥ " & CInt((dOdl / 1000)) & " km"

            Try
                Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = oTemplate.sOdl
                }
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("apparentTemperature")
                oNew.sUnit = " °C"
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                oNew.sPomiar = GetLangString("resTempOdczuwana")
                moListaPomiarow.Add(oNew)
            Catch
            End Try

            Try
                Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = oTemplate.sOdl
                }
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("dewPoint")
                oNew.sUnit = " °C"
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                oNew.sPomiar = GetLangString("resPomiarRosa")
                moListaPomiarow.Add(oNew)
            Catch
            End Try

            Try
                Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = oTemplate.sOdl
                }
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("cloudCover") * 100
                oNew.sUnit = " %"
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                oNew.sPomiar = GetLangString("resPomiarZachm")
                moListaPomiarow.Add(oNew)
            Catch
            End Try

            Try
                Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = oTemplate.sOdl
                }
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("uvIndex")
                oNew.sUnit = ""
                oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit
                oNew.sPomiar = "UV index"
                If oNew.dCurrValue >= 6 Then oNew.sAlert = "!"
                If oNew.dCurrValue >= 8 Then oNew.sAlert = "!!"
                If oNew.dCurrValue >= 11 Then oNew.sAlert = "!!!"
                oNew.sLimity = "WHO exposure categories" & vbCrLf & "Low" & vbTab & " <3" & vbCrLf & "Moderate" & vbTab & " 3..5" & vbLf & "High" & vbTab & " 6..7 (seek shade during midday)" & vbLf & "Very high" & vbTab & " 8..10 (avoid being outside midday)" & vbLf & "Extreme" & vbTab & " >10" & vbCrLf
                moListaPomiarow.Add(oNew)
            Catch
            End Try

            Try
                Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sTimeStamp = oTemplate.sTimeStamp,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = oTemplate.sOdl
                }
                oNew.dCurrValue = oJsonCurrent.GetObject().GetNamedNumber("visibility")

                If oNew.dCurrValue > 10 Then
                    Dim iRounder As Integer = CInt((oNew.dCurrValue * 100))
                    Dim dRounder As Double = iRounder / 100
                    oNew.sUnit = " km"
                    oNew.sCurrValue = dRounder & oNew.sUnit
                Else
                    oNew.sUnit = " m"
                    oNew.sCurrValue = (oNew.dCurrValue * 1000) & oNew.sUnit
                End If

                oNew.sPomiar = GetLangString("resPomiarWidocz")
                moListaPomiarow.Add(oNew)
            Catch
            End Try

        Catch
        End Try

        If moListaPomiarow.Count < 1 Then
            If Not bInTimer Then Await DialogBoxAsync("ERROR: data parsing error " & SRC_POMIAR_SOURCE)
            Return moListaPomiarow
        End If

        Return moListaPomiarow
    End Function
End Class

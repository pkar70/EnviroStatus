Imports System.Collections.ObjectModel

Public Class Source_IMGWmeteo
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceImgwMeteo"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "IMGW meteo"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "http://hydro.imgw.pl/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "IMGWmet"
    Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://hydro.imgw.pl/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://hydro.imgw.pl/"
    Public Overrides ReadOnly Property SRC_ZASIEG As Zasieg = Zasieg.Poland


    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Private Sub AddPomiar(oNew As JedenPomiar)
        For Each oItem As JedenPomiar In moListaPomiarow

            If oItem.sPomiar = oNew.sPomiar Then

                If oItem.dOdl > oNew.dOdl Then
                    oItem.bDel = True
                Else
                    Return
                End If
            End If
        Next

        moListaPomiarow.Add(oNew)
    End Sub

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Dim dMaxOdl As Double = 10
        Dim oListaPomiarow As New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceImgwMeteo", SRC_DEFAULT_ENABLE) Then Return oListaPomiarow
        If Not oPos.IsInsidePoland() Then Return moListaPomiarow
        Dim sPage As String = Await GetREST("api/map/?category=meteo")
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim bError As Boolean = False
        Dim sErr As String = ""
        Dim oJson As Newtonsoft.Json.Linq.JArray = Nothing

        If String.IsNullOrEmpty(sPage) Then
            bError = True
        Else

            Try
                oJson = Newtonsoft.Json.Linq.JArray.Parse(sPage)
            Catch ex As Exception
                sErr = ex.Message
                bError = True
            End Try
        End If

        If bError Then
            Await DialogBoxAsync($"ERROR: JSON parsing error - getting nearest ({SRC_POMIAR_SOURCE})" & vbCrLf & " " & sErr)
            Return oListaPomiarow
        End If

        If oJson.Count < 1 Then Return oListaPomiarow
        Dim dMinOdl As Double = 1000000
        Dim dMinOdlAdd As Double = 1000000

        For Each oJsonSensor As Newtonsoft.Json.Linq.JToken In oJson
            Dim oTemplate = New JedenPomiar(SRC_POMIAR_SOURCE)
            oTemplate.sPomiar = "Meteo"
            oTemplate.sId = oJsonSensor.GetObject().GetNamedString("i")
            oTemplate.dLon = oJsonSensor.GetObject().GetNamedNumber("lo")
            oTemplate.dLat = oJsonSensor.GetObject().GetNamedNumber("la")
            oTemplate.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oTemplate.dLat, oTemplate.dLon))
            dMinOdl = Math.Min(dMinOdl, oTemplate.dOdl)
            If oTemplate.dOdl > dMaxOdl * 1000 Then Continue For
            If oTemplate.dOdl > dMinOdlAdd Then Continue For
            dMinOdlAdd = oTemplate.dOdl
            oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl)
            oTemplate.sAdres = VBlib.App.String2SentenceCase(oJsonSensor.GetObject().GetNamedString("n"))
            oListaPomiarow.Add(oTemplate)
        Next

        If oListaPomiarow.Count < 1 Then
            Await DialogBoxAsync("ERROR: data parsing error IMGWmeteo\sPage\0")
            Return oListaPomiarow
        End If

        dMinOdlAdd = 100000

        For Each oItem As JedenPomiar In oListaPomiarow
            dMinOdlAdd = Math.Min(dMinOdlAdd, oItem.dOdl)
        Next

        For Each oItem As JedenPomiar In oListaPomiarow
            If oItem.dOdl > dMinOdlAdd Then oItem.bDel = True
        Next

        moListaPomiarow = New Collection(Of JedenPomiar)()

        For Each oItem As JedenPomiar In oListaPomiarow
            If Not oItem.bDel Then Return Await GetDataFromSensorAsync(oItem, False)
        Next

        Return oListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool("sourceImgwMeteo", SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        Dim oTemplate = FavTemplateLoad("IMGWmeteo_" & sId, SRC_POMIAR_SOURCE)
        oTemplate.sId = sId
        Return Await GetDataFromSensorAsync(oTemplate, bInTimer)
    End Function

    Private Async Function GetDataFromSensorAsync(oTemplate As JedenPomiar, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        Dim sPage As String = Await GetREST("api/station/meteo/?id=" & oTemplate.sId)
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim bError As Boolean = False
        Dim sErr As String = ""
        Dim oJsonSensor As Newtonsoft.Json.Linq.JObject = Nothing

        Try
            oJsonSensor = Newtonsoft.Json.Linq.JObject.Parse(sPage)
        Catch ex As Exception
            sErr = ex.Message
            bError = True
        End Try

        If bError OrElse oJsonSensor Is Nothing OrElse (If(oJsonSensor.ToString(), "")) = "null" Then
            If Not bInTimer Then Await DialogBoxAsync($"ERROR: JSON parsing error - getting sensor data ({SRC_POMIAR_SOURCE})" & vbCrLf & " " & sErr)
            Return moListaPomiarow
        End If

        oTemplate.sSource = "IMGWmet"
        oTemplate.sId = oJsonSensor.GetObject().GetNamedString("id")
        Dim oJsonValStatus As Newtonsoft.Json.Linq.JToken
        oJsonValStatus = oJsonSensor.GetObject().GetNamedToken("status")
        oTemplate.sAdres = VBlib.App.String2SentenceCase(oJsonSensor.GetObject().GetNamedString("name"))

        Try
            Dim oNew = New JedenPomiar(oTemplate.sSource)
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sPomiar = GetLangString("resPomiarOpad")
            oNew.sUnit = " cm"
            oNew.sAdres = oTemplate.sAdres
            Dim oJsonArr As Newtonsoft.Json.Linq.JArray

            If GetSettingsBool("sourceImgwMeteo10min", True) Then
                oJsonArr = oJsonSensor.GetObject().GetNamedArray("tenMinutesPrecipRecords")
            Else
                oJsonArr = oJsonSensor.GetObject().GetNamedArray("hourlyPrecipRecords")
            End If

            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Newtonsoft.Json.Linq.JToken
                oJsonVal = oJsonArr(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue.ToString() & oNew.sUnit
                If oNew.dCurrValue > 0 Then oNew.sAlert = "!"

                Try
                    oJsonVal = oJsonArr(oJsonArr.Count - 2)
                    oNew.sAddit = "Poprzednio " & oJsonVal.GetObject().GetNamedNumber("value").ToString() & oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch
                End Try

                moListaPomiarow.Add(oNew)
            End If

        Catch
        End Try

        Try
            Dim oNew = New JedenPomiar(oTemplate.sSource)
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sPomiar = "Temp"
            oNew.sUnit = " °C"
            oNew.sAdres = oTemplate.sAdres
            Dim oJsonArr As Newtonsoft.Json.Linq.JArray
            oJsonArr = oJsonSensor.GetObject().GetNamedArray("temperatureAutoRecords")

            If oJsonArr?.Count > 2 Then
                Dim oJsonVal As Newtonsoft.Json.Linq.JToken
                oJsonVal = oJsonArr(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue.ToString() & oNew.sUnit

                Try
                    oJsonVal = oJsonArr(oJsonArr.Count - 2)
                    oNew.sAddit = "Poprzednio " & oJsonVal.GetObject().GetNamedNumber("value").ToString() & oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & VBlib.App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch
                End Try

                moListaPomiarow.Add(oNew)
            End If

        Catch
        End Try

        Try
            Dim oNew = New JedenPomiar(oTemplate.sSource)
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sPomiar = GetLangString("resPomiarWind")
            oNew.sUnit = " m/s"
            oNew.sAdres = oTemplate.sAdres
            Dim oJsonArr As Newtonsoft.Json.Linq.JArray
            oJsonArr = oJsonSensor.GetObject().GetNamedArray("windVelocityTelRecords")

            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Newtonsoft.Json.Linq.JToken
                oJsonVal = oJsonArr(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue.ToString() & oNew.sUnit
                oNew.sAddit = "= " & (oNew.dCurrValue * 3.6).ToString() & " km/h"

                Try
                    oJsonVal = oJsonArr(oJsonArr.Count - 2)
                    oNew.sAddit = oNew.sAddit & vbCrLf & "Poprzednio " & oJsonVal.GetObject().GetNamedNumber("value").ToString() & oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch
                End Try

                oJsonArr = oJsonSensor.GetObject().GetNamedArray("windDirectionTelRecords")

                If oJsonArr.Count > 2 Then
                    oJsonVal = oJsonArr(oJsonArr.Count - 1)
                    oNew.sAddit = oNew.sAddit & vbCrLf & "Kierunek: " & oJsonVal.GetObject().GetNamedNumber("value").ToString() & "°"
                End If

                moListaPomiarow.Add(oNew)
            End If

        Catch
        End Try

        Try
            Dim oNew = New JedenPomiar(oTemplate.sSource)
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sPomiar = GetLangString("resPomiarWind") & " max"
            oNew.sUnit = " m/s"
            oNew.sAdres = oTemplate.sAdres
            Dim oJsonArr As Newtonsoft.Json.Linq.JArray
            oJsonArr = oJsonSensor.GetObject().GetNamedArray("windMaxVelocityRecords")

            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Newtonsoft.Json.Linq.JToken
                oJsonVal = oJsonArr(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue.ToString() & oNew.sUnit
                oNew.sAddit = "= " & (oNew.dCurrValue * 3.6).ToString() & " km/h"

                Try
                    oJsonVal = oJsonArr(oJsonArr.Count - 2)
                    oNew.sAddit = oNew.sAddit & vbCrLf & "Poprzednio " & oJsonVal.GetObject().GetNamedNumber("value").ToString() + oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch
                End Try

                moListaPomiarow.Add(oNew)
            End If

        Catch
        End Try

        Return moListaPomiarow
    End Function

End Class

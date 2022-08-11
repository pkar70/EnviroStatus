Imports System.Linq
Imports System.Collections.ObjectModel

Public Class Source_IMGWhydro
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceImgwHydro"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "IMGW hydro"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "http://hydro.imgw.pl/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "IMGWhyd"
    Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Public Overrides ReadOnly Property SRC_NO_COMPARE As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://hydro.imgw.pl/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://hydro.imgw.pl/"

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub
    Private Sub AddPomiar(ByVal oNew As JedenPomiar)
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

    Private Function NormalizePomiarName(ByVal sPomiar As String) As String
        Return "Hydro"
    End Function

    Private Function Unit4Pomiar(ByVal sPomiar As String) As String
        Return "cm"
    End Function

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Dim dMaxOdl As Double = 25
        Dim oListaPomiarow As New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceImgwHydro", SRC_DEFAULT_ENABLE) Then Return oListaPomiarow
        If Not oPos.IsInsidePoland() Then Return oListaPomiarow
        Dim sPage As String = Await GetREST("api/map/?category=hydro")
        If sPage.Length < 10 Then Return oListaPomiarow
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
            oTemplate.sPomiar = "Hydro"
            oTemplate.sId = oJsonSensor.GetObject().GetNamedString("i")
            oTemplate.dLon = oJsonSensor.GetObject().GetNamedNumber("lo")
            oTemplate.dLat = oJsonSensor.GetObject().GetNamedNumber("la")
            oTemplate.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oTemplate.dLat, oTemplate.dLon))
            dMinOdl = Math.Min(dMinOdl, oTemplate.dOdl)
            If oTemplate.dOdl > dMaxOdl * 1000 Then Continue For
            oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl)
            oTemplate.sAdres = VBlib.App.String2SentenceCase(oJsonSensor.GetObject().GetNamedString("n"))
            oListaPomiarow.Add(oTemplate)
        Next

        If oListaPomiarow.Count < 1 Then
            Await DialogBoxAsync("ERROR: no station within range (IMGWhydro)")
            Return oListaPomiarow
        End If

        moListaPomiarow = New Collection(Of JedenPomiar)()

        For Each oItem As JedenPomiar In oListaPomiarow
            If Not oItem.bDel Then moListaPomiarow.Concat(Await GetDataFromSensorAsync(oItem, False))
        Next

        If Not GetSettingsBool("sourceImgwHydroAll") Then
            dMinOdlAdd = 100000
            Dim sMinRzeka As String = ""

            For Each oItem As JedenPomiar In moListaPomiarow

                If dMinOdlAdd > oItem.dOdl Then
                    dMinOdlAdd = oItem.dOdl
                    sMinRzeka = oItem.sPomiar
                End If
            Next

            Dim iInd As Integer
            iInd = sMinRzeka.IndexOf(" ")
            If iInd > 0 Then sMinRzeka = sMinRzeka.Substring(0, iInd)

            For Each oItem As JedenPomiar In moListaPomiarow
                If Not oItem.sPomiar.StartsWith(sMinRzeka) Then oItem.bDel = True
            Next
        End If

        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool("sourceImgwHydro", SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        Dim oTemplate = FavTemplateLoad("IMGWhyd_" & sId, SRC_POMIAR_SOURCE)
        Return Await GetDataFromSensorAsync(oTemplate, bInTimer)
    End Function

    Private Async Function GetDataFromSensorAsync(oTemplate As JedenPomiar, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        Dim sPage As String = Await GetREST("api/station/hydro/?id=" & oTemplate.sId)
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

        If bError Then
            If Not bInTimer Then Await DialogBoxAsync($"ERROR: JSON parsing error - getting sensor data ({SRC_POMIAR_SOURCE})" & vbCrLf & " " & sErr)
            Return moListaPomiarow
        End If

        oTemplate.sSource = "IMGWhyd"
        oTemplate.sId = oJsonSensor.GetObject().GetNamedString("id")
        Dim oJsonValStatus As Newtonsoft.Json.Linq.JToken
        oJsonValStatus = oJsonSensor.GetObject().GetNamedToken("status")
        Dim iInd As Integer
        oTemplate.sPomiar = oJsonValStatus.GetObject().GetNamedString("river")
        iInd = oTemplate.sPomiar.LastIndexOf("(")
        If iInd > 0 Then oTemplate.sPomiar = oTemplate.sPomiar.Substring(0, iInd).Trim()
        oTemplate.sAdres = VBlib.App.String2SentenceCase(oJsonSensor.GetObject().GetNamedString("name"))

        Try
            Dim oNewCm = New JedenPomiar(oTemplate.sSource)
            oNewCm.sId = oTemplate.sId
            oNewCm.dLon = oTemplate.dLon
            oNewCm.dLat = oTemplate.dLat
            oNewCm.dOdl = oTemplate.dOdl
            oNewCm.sOdl = oTemplate.sOdl
            oNewCm.sPomiar = oTemplate.sPomiar & " cm"
            oNewCm.sUnit = " cm"
            oNewCm.sAdres = oTemplate.sAdres

            Dim oJsonValCurrState As Newtonsoft.Json.Linq.JToken

            oJsonValCurrState = oJsonValStatus.GetObject().GetNamedToken("currentState")
            If oJsonValCurrState?.HasValues Then
                oNewCm.sTimeStamp = oJsonValCurrState?.GetObject().GetNamedString("date").Replace("T", " ")
                oNewCm.dCurrValue = oJsonValCurrState?.GetObject().GetNamedNumber("value")
                oNewCm.sCurrValue = oNewCm.dCurrValue.ToString() & " cm"
                oNewCm.sAddit = "Status: " & oJsonSensor.GetObject().GetNamedString("state")
            End If

            oJsonValCurrState = oJsonValStatus.GetObject().GetNamedToken("currentState")
            If oJsonValCurrState?.HasValues Then
                oJsonValCurrState = oJsonValStatus.GetObject().GetNamedToken("previousState")
                oNewCm.sAddit = oNewCm.sAddit & vbLf & "Poprzednio " & oJsonValCurrState?.GetObject().GetNamedNumber("value").ToString()
                Dim sPrevDate As String = oJsonValCurrState?.GetObject().GetNamedString("date").Replace("T", " ")
                oNewCm.sAddit = oNewCm.sAddit & " @ " & App.ShortPrevDate(oNewCm.sTimeStamp, sPrevDate)
            End If

            Dim dAlarm, dWarn, dHigh, dLow As Double
            dAlarm = oJsonSensor.GetObject().GetNamedNumber("alarmValue", 0)
            dWarn = oJsonSensor.GetObject().GetNamedNumber("warningValue", 0)
            dHigh = oJsonSensor.GetObject().GetNamedNumber("highValue", 0)
            dLow = oJsonSensor.GetObject().GetNamedNumber("lowValue", 0)
            Dim sLimity As String = ""
            If dAlarm > 0 Then sLimity = sLimity & "Alarm: " & dAlarm.ToString() & " cm" & vbLf
            If dWarn > 0 Then sLimity = sLimity & "Warn: " & dWarn.ToString() & " cm" & vbLf
            If dHigh > 0 Then sLimity = sLimity & "High: " & dHigh.ToString() & " cm" & vbLf
            If dLow > 0 Then sLimity = sLimity & "Low: " & dLow.ToString() & " cm" & vbLf
            oNewCm.sLimity = sLimity
            If oNewCm.dCurrValue <= dLow OrElse oNewCm.dCurrValue >= dHigh Then oNewCm.sAlert = "!"
            If oNewCm.dCurrValue >= dWarn Then oNewCm.sAlert = "!!"
            If oNewCm.dCurrValue >= dAlarm Then oNewCm.sAlert = "!!!"
            moListaPomiarow.Add(oNewCm)
        Catch
        End Try

        Try
            Dim oNew = New JedenPomiar(oTemplate.sSource)
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sUnit = " °C"
            oNew.sPomiar = oTemplate.sPomiar + oNew.sUnit
            oNew.sAdres = oTemplate.sAdres
            Dim oJsonArr As Newtonsoft.Json.Linq.JArray
            oJsonArr = oJsonSensor.GetObject().GetNamedArray("waterTemperatureAutoRecords")

            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Newtonsoft.Json.Linq.JToken
                oJsonVal = oJsonArr(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue.ToString() & " °C"

                Try
                    oJsonVal = oJsonArr(oJsonArr.Count - 2)
                    oNew.sAddit = "Poprzednio " & oJsonVal.GetObject().GetNamedNumber("value").ToString() + oNew.sUnit
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
            oNew.sUnit = " m³/s"
            oNew.sPomiar = oTemplate.sPomiar + oNew.sUnit
            oNew.sAdres = oTemplate.sAdres
            Dim oJsonArr As Newtonsoft.Json.Linq.JArray
            oJsonArr = oJsonSensor.GetObject().GetNamedArray("dischargeRecords")

            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Newtonsoft.Json.Linq.JToken
                oJsonVal = oJsonArr(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                oNew.dCurrValue = oJsonVal.GetObject().GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue.ToString() & oNew.sUnit

                Try
                    oJsonVal = oJsonArr(oJsonArr.Count - 2)
                    oNew.sAddit = "Poprzednio " & oJsonVal.GetObject().GetNamedNumber("value").ToString() + oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject().GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch
                End Try

                Dim dAlarmLow, dAlarmHigh, dHigh, dLow, dAvgRok As Double
                dAlarmHigh = oJsonSensor.GetObject().GetNamedNumber("highestHighDischargeValue", 0)
                dAlarmLow = oJsonSensor.GetObject().GetNamedNumber("lowestLowDischargeValue", 0)
                dAvgRok = oJsonSensor.GetObject().GetNamedNumber("mediumOfYearMediumsDischargeValue", 0)
                dHigh = oJsonSensor.GetObject().GetNamedNumber("highDischargeValue", 0)
                dLow = oJsonSensor.GetObject().GetNamedNumber("lowDischargeValue", 0)
                dLow = 0
                Dim sLimity As String = ""
                If dAlarmHigh > 0 Then sLimity = sLimity & "Najwyższy: " & dAlarmHigh.ToString() & " m³/s " & vbCrLf
                If dHigh > 0 Then sLimity = sLimity & "Wysoki: " & dHigh.ToString() & " m³/s " & vbCrLf
                If dAvgRok > 0 Then sLimity = sLimity & "Średni roczny: " & dAvgRok.ToString() & " m³/s " & vbCrLf
                If dLow > 0 Then sLimity = sLimity & "Niski: " & dLow.ToString() & " m³/s " & vbCrLf
                If dAlarmLow > 0 Then sLimity = sLimity & "Najniższy: " & dAlarmLow.ToString() & " m³/s " & vbCrLf
                oNew.sLimity = sLimity
                If dLow > 0 AndAlso oNew.dCurrValue <= dLow Then oNew.sAlert = "!"
                If dHigh > 0 AndAlso oNew.dCurrValue >= dHigh Then oNew.sAlert = "!"
                If dAlarmLow > 0 AndAlso oNew.dCurrValue <= dAlarmLow Then oNew.sAlert = "!!"
                If dAlarmHigh > 0 AndAlso oNew.dCurrValue >= dAlarmHigh Then oNew.sAlert = "!!"
                moListaPomiarow.Add(oNew)
            Else
            End If

        Catch
        End Try

        Return moListaPomiarow
    End Function

End Class

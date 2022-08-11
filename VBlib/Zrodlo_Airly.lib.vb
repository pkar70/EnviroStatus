Imports System.Collections.ObjectModel

Partial Public Class Source_Airly
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceAirly"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Airly"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://airapi.airly.eu/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "airly"
    Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides Property SRC_HAS_KEY As Boolean = True
    Public Overrides ReadOnly Property SRC_KEY_LOGIN_LINK As String = "https://developer.airly.eu/login"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://airly.org/en/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://airly.org/pl/"

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

    Private Shared Function NormalizePomiarName(sPomiar As String) As String
        If (If(sPomiar, "")) = "PM10" Then Return "PM₁₀"
        If (If(sPomiar, "")) = "PM1" Then Return "PM₁"
        If (If(sPomiar, "")) = "PM25" Then Return "PM₂₅"
        If (If(sPomiar.Substring(0, 2), "")) = "PM" Then Return sPomiar
        Return sPomiar.Substring(0, 1) & sPomiar.Substring(1).ToLower()
    End Function

    Private Shared Function Unit4Pomiar(sPomiar As String) As String
        If sPomiar.Substring(0, 2) = "PM" Then Return " μg/m³"

        Select Case sPomiar
            Case "PRESSURE"
                Return " hPa"
            Case "HUMIDITY"
                Return " %"
            Case "TEMPERATURE"
                Return " °C"
        End Select

        Return ""
    End Function

    Private Async Function GetPomiaryAsync(oTemplate As JedenPomiar, bInTimer As Boolean) As Task
        Dim sCmd As String
        Dim sErr As String = ""
        sCmd = "v2/measurements/installation?installationId=" & oTemplate.sId
        Dim sPage As String = Await GetREST(sCmd)
        If sPage.Length < 10 Then Return
        Dim bError As Boolean = False
        Dim oJson As Newtonsoft.Json.Linq.JObject = Nothing

        Try
            oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage)
        Catch ex As Exception
            bError = True
            sErr = ex.Message
        End Try

        If bError Then
            If Not bInTimer Then Await DialogBoxAsync("ERROR: JSON parsing error - getting measurements (Airly)" & vbCrLf & sErr)
            Return
        End If

        Dim oJsonCurrent As Newtonsoft.Json.Linq.JToken

        Try
            oJsonCurrent = oJson.GetNamedToken("current")
            oTemplate.sTimeStamp = oJsonCurrent.GetNamedString("fromDateTime")
            Dim oJsonValues As Newtonsoft.Json.Linq.JArray
            oJsonValues = oJsonCurrent.GetNamedArray("values")

            For Each oJsonMeasurement As Newtonsoft.Json.Linq.JObject In oJsonValues
                Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sId = oTemplate.sId,
                    .dLon = oTemplate.dLon,
                    .dLat = oTemplate.dLat,
                    .dWysok = oTemplate.dWysok,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = Odleglosc2String(oTemplate.dOdl),
                    .sSensorDescr = oTemplate.sSensorDescr,
                    .sAdres = oTemplate.sAdres,
                    .sTimeStamp = oTemplate.sTimeStamp
                }
                oNew.sPomiar = oJsonMeasurement.GetNamedString("name")
                oNew.dCurrValue = oJsonMeasurement.GetNamedNumber("value")
                oNew.sUnit = Unit4Pomiar(oNew.sPomiar)

                If oNew.sPomiar = "HUMIDITY" OrElse oNew.sPomiar = "PRESSURE" Then
                    Dim iInt As Integer = CInt(oNew.dCurrValue)
                    oNew.sCurrValue = iInt.ToString()
                Else
                    oNew.sCurrValue = oNew.dCurrValue.ToString()
                End If

                If oNew.sCurrValue.Length > 5 Then oNew.sCurrValue = oNew.sCurrValue.Substring(0, 5)
                oNew.sCurrValue &= oNew.sUnit
                oNew.sPomiar = NormalizePomiarName(oNew.sPomiar)
                AddPomiar(oNew)
            Next

        Catch
        End Try
    End Function

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Dim dMaxOdl As Double = 10
        Dim sErr As String = ""
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool("sourceAirly", SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        If GetSettingsString("sourceAirly_apikey").Length < 8 Then Return moListaPomiarow
        Dim sCmd As String
        sCmd = "v2/installations/nearest?lat=" & oPos.Latitude.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) & "&lng=" + oPos.Longitude.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) & "&maxDistanceKM=" & dMaxOdl.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) & "&maxResults=5"
        Dim sPage As String = Await GetREST(sCmd)
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim bError As Boolean = False
        Dim oJson As Newtonsoft.Json.Linq.JArray = Nothing

        Try
            oJson = Newtonsoft.Json.Linq.JArray.Parse(sPage)
        Catch ex As Exception
            sErr = ex.Message
            bError = True
        End Try

        If bError Then
            Await DialogBoxAsync("ERROR: JSON parsing error - getting nearest (Airly)" & vbCrLf & sErr)
            Return moListaPomiarow
        End If

        Try
            If oJson.Count = 0 Then Return moListaPomiarow
        Catch
            Return moListaPomiarow
        End Try

        Try

            For Each oJsonSensor As Newtonsoft.Json.Linq.JObject In oJson
                If Not CBool(oJsonSensor("airly")) Then Continue For
                Dim oTemplate = New JedenPomiar("airly")
                oTemplate.sId = oJsonSensor.GetNamedString("id")
                Dim oJsonPoint As Newtonsoft.Json.Linq.JToken
                oJsonPoint = oJsonSensor.GetNamedToken("location")
                oTemplate.dLon = oJsonPoint.GetNamedNumber("longitude")
                oTemplate.dLat = oJsonPoint.GetNamedNumber("latitude")
                oTemplate.dWysok = oJsonSensor.GetNamedNumber("elevation", 0)
                oTemplate.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oTemplate.dLat, oTemplate.dLon))
                Dim oJsonSponsor As Newtonsoft.Json.Linq.JToken
                oJsonSponsor = oJsonSensor.GetNamedToken("sponsor")
                oTemplate.sSensorDescr = oJsonSponsor.GetNamedString("name", "")
                Dim oJsonAdres As Newtonsoft.Json.Linq.JToken
                oJsonAdres = oJsonSensor.GetObject().GetNamedToken("address")
                oTemplate.sAdres = oJsonAdres.GetObject().GetNamedString("city", "") & ", " + oJsonAdres.GetObject().GetNamedString("street", "") & " " + oJsonAdres.GetObject().GetNamedString("number", "")
                Await GetPomiaryAsync(oTemplate, False)
            Next

        Catch
        End Try

        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool("sourceAirly", SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        If GetSettingsString(SRC_SETTING_NAME & "_apikey").Length < 8 Then
            If Not bInTimer Then Await DialogBoxAsync("ERROR: bad API key?")
            Return moListaPomiarow
        End If

        Dim oTemplate = FavTemplateLoad("airly_" & sId, "airly")

        oTemplate.sId = sId
        Await GetPomiaryAsync(oTemplate, bInTimer)
        Return moListaPomiarow
    End Function
End Class

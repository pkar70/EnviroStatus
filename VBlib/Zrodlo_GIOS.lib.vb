Imports System.IO
Imports System.Collections.ObjectModel
Imports System

Public Class Source_GIOS
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceGIOS"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "GIOŚ"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "http://api.gios.gov.pl/pjp-api/rest/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "gios"
    Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "http://powietrze.gios.gov.pl/pjp/current"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "http://powietrze.gios.gov.pl/pjp/current"
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

    Private Function NormalizePomiarName(sPomiar As String) As String
        Select Case sPomiar
            Case "C6H6"
                Return "C₆H₆"
            Case "SO2"
                Return "SO₂"
            Case "NO2"
                Return "NO₂"
            Case "O3"
                Return "O₃"
            Case "PM10"
                Return "PM₁₀"
            Case "PM1"
                Return "PM₁"
            Case "PM2.5"
                Return "PM₂₅"
        End Select

        Return sPomiar
    End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        Return " μg/m³"
    End Function

    Private Async Function GetPomiaryAsync(oTemplate As JedenPomiar, bInTimer As Boolean) As Task
        Dim sCmd As String
        sCmd = "station/sensors/" & oTemplate.sId
        Dim sPage As String = Await GetREST(sCmd)
        If sPage.Length < 10 Then Return
        Dim bError As Boolean = False
        Dim sErr As String = ""
        Dim oJson As Newtonsoft.Json.Linq.JArray = Nothing

        Try
            oJson = Newtonsoft.Json.Linq.JArray.Parse(sPage)
        Catch ex As Exception
            sErr = ex.Message
            bError = True
        End Try

        If bError Then
            If Not bInTimer Then Await DialogBoxAsync($"ERROR: JSON parsing error - getting sensor data ({SRC_POMIAR_SOURCE})" & vbCrLf & " " & sErr)
            Return
        End If

        Try

            For Each oJsonMeasurement As Newtonsoft.Json.Linq.JToken In oJson
                Dim oNew = New JedenPomiar(oTemplate.sSource) With {
                    .sId = oTemplate.sId,
                    .oGeo = oTemplate.oGeo,
                    .dOdl = oTemplate.dOdl,
                    .sOdl = Odleglosc2String(oTemplate.dOdl),
                    .sSensorDescr = oTemplate.sSensorDescr,
                    .sAdres = oTemplate.sAdres
                }
                oNew.sAddit = oJsonMeasurement.GetObject().GetNamedNumber("id").ToString()
                Dim oJsonVal As Newtonsoft.Json.Linq.JToken
                oJsonVal = oJsonMeasurement.GetObject().GetNamedToken("param")
                oNew.sPomiar = oJsonVal.GetObject().GetNamedString("paramCode")
                AddPomiar(oNew)
            Next

        Catch
        End Try
    End Function

    Private Async Function GetWartosciAsync() As Task
        Try

            For Each oItem As JedenPomiar In moListaPomiarow
                If oItem.bDel Then Continue For
                Dim sCmd As String
                sCmd = "data/getData/" & oItem.sAddit
                Dim sPage As String = Await GetREST(sCmd)
                If sPage.Length < 10 Then Return
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
                    Await DialogBoxAsync($"ERROR: JSON parsing error - getting values ({SRC_POMIAR_SOURCE})" & vbCrLf & " " & sErr)
                    Return
                End If

                Dim oJsonArr As Newtonsoft.Json.Linq.JArray
                oJsonArr = oJson.GetObject().GetNamedArray("values")

                For Each oJsonMeasurement As Newtonsoft.Json.Linq.JToken In oJsonArr
                    oItem.sTimeStamp = oJsonMeasurement.GetObject().GetNamedString("date")
                    oItem.sCurrValue = ""

                    Try
                        Dim oVal As Newtonsoft.Json.Linq.JToken
                        oVal = oJsonMeasurement.GetObject().GetNamedToken("value")
                        If oVal.Type <> Newtonsoft.Json.Linq.JTokenType.Null Then oItem.sCurrValue = oJsonMeasurement.GetObject().GetNamedNumber("value").ToString()
                    Catch
                    End Try

                    If Not String.IsNullOrEmpty(oItem.sCurrValue) Then
                        oItem.dCurrValue = oItem.sCurrValue
                        oItem.sUnit = Unit4Pomiar(oItem.sPomiar)
                        If oItem.sCurrValue.Length > 5 Then oItem.sCurrValue = oItem.sCurrValue.Substring(0, 5)
                        oItem.sCurrValue = oItem.sCurrValue + oItem.sUnit
                        Exit For
                    End If
                Next

                If String.IsNullOrEmpty(oItem.sCurrValue) Then
                    oItem.bDel = True
                    oItem.dCurrValue = 0
                Else
                    oItem.sPomiar = NormalizePomiarName(oItem.sPomiar)
                End If
            Next

        Catch
        End Try
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool("sourceGIOS", SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        Dim oTemplate = FavTemplateLoad("gios_" & sId, SRC_POMIAR_SOURCE)
        oTemplate.sSource = SRC_POMIAR_SOURCE
        oTemplate.sId = sId
        Await GetPomiaryAsync(oTemplate, bInTimer)
        Await GetWartosciAsync()
        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetNearestAsync(oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool("sourceGIOS", SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        If Not oPos.IsInsidePoland() Then Return moListaPomiarow
        Dim dMaxOdl As Double = 10
        Dim sCmd As String
        sCmd = "station/findAll"
        Dim sPage As String = Await GetREST(sCmd)
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim bError As Boolean = False
        Dim sErr As String = ""
        Dim oJson As Newtonsoft.Json.Linq.JArray = Nothing

        Try
            oJson = Newtonsoft.Json.Linq.JArray.Parse(sPage)
        Catch ex As Exception
            sErr = ex.Message
            bError = True
        End Try

        If bError Then
            Await DialogBoxAsync($"ERROR: JSON parsing error - getting nearest ({SRC_POMIAR_SOURCE})" & vbCrLf & " " & sErr)
            Return moListaPomiarow
        End If

        Try
            If oJson.Count = 0 Then Return moListaPomiarow

            For Each oJsonSensor As Newtonsoft.Json.Linq.JToken In oJson
                Dim oJsonObj As Newtonsoft.Json.Linq.JToken
                oJsonObj = oJsonSensor.GetObject()
                Dim oTemplate = New JedenPomiar(SRC_POMIAR_SOURCE)
                oTemplate.sId = oJsonObj.GetNamedNumber("id").ToString()
                oTemplate.oGeo = New pkar.BasicGeopos(oJsonObj.GetNamedString("gegrLon"), oJsonObj.GetNamedString("gegrLat"))
                oTemplate.dOdl = oPos.DistanceTo(oTemplate.oGeo)

                If oTemplate.dOdl / 1000 < dMaxOdl Then
                    oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl)
                    oTemplate.sSensorDescr = oJsonObj.GetNamedString("stationName")
                    oTemplate.sAdres = oJsonObj.GetNamedString("addressStreet")

                    If String.IsNullOrEmpty(oTemplate.sAdres) Then

                        Try
                            Dim oJsonAdres As Newtonsoft.Json.Linq.JToken
                            oJsonAdres = oJsonObj.GetNamedToken("city")
                            oTemplate.sAdres = oJsonAdres.GetNamedString("name")
                            Dim oJsonComm As Newtonsoft.Json.Linq.JToken
                            oJsonComm = oJsonAdres.GetNamedToken("commune").GetObject()
                            oTemplate.sAdres = oTemplate.sAdres & vbCrLf & "("
                            If Not String.IsNullOrEmpty(oJsonComm.GetNamedString("communeName")) Then oTemplate.sAdres = oTemplate.sAdres & "gmina " + oJsonComm.GetNamedString("communeName") & vbLf
                            If Not String.IsNullOrEmpty(oJsonComm.GetNamedString("districtName")) Then oTemplate.sAdres = oTemplate.sAdres & "powiat " + oJsonComm.GetNamedString("districtName") & vbLf
                            If Not String.IsNullOrEmpty(oJsonComm.GetNamedString("provinceName")) Then oTemplate.sAdres = oTemplate.sAdres + oJsonComm.GetNamedString("provinceName") & vbLf
                        Catch
                        End Try
                    End If

                    Await GetPomiaryAsync(oTemplate, False)
                End If
            Next

            Await GetWartosciAsync()
        Catch
        End Try

        Return moListaPomiarow
    End Function
End Class

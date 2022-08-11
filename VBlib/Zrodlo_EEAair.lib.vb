Imports System.Collections.ObjectModel

Public Class Source_EEAair
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceEEAair"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "EEA air"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://discomap.eea.europa.eu/Map/UTDViewer/dataService/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "EEAair"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://www.eea.europa.eu/data-and-maps/explore-interactive-maps/up-to-date-air-quality-data"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://www.eea.europa.eu/data-and-maps/explore-interactive-maps/up-to-date-air-quality-data"

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Private Function NormalizePomiarName(sPomiar As String) As String
        Select Case sPomiar
            Case "CO"
                Return "CO"
            Case "SO2"
                Return "SO₂"
            Case "NO2"
                Return "NO₂"
            Case "O3"
                Return "O₃"
            Case "PM10"
                Return "PM₁₀"
            Case "PM2.5"
                Return "PM₂₅"
        End Select

        Return sPomiar
    End Function

    Private Function NormalizeUnitName(sPomiar As String) As String
        Select Case sPomiar
            Case "CO"
                Return " mg/m³"
            Case "SO2"
                Return " μg/m³"
            Case "NO2"
                Return " μg/m³"
            Case "O3"
                Return " μg/m³"
            Case "PM10"
                Return " μg/m³"
            Case "PM2.5"
                Return " μg/m³"
        End Select

        Return sPomiar
    End Function

    Private Async Function GetPomiaryAsync(oTemplate As JedenPomiar, bInTimer As Boolean) As Task
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return
        Dim sTmp As String = "SamplingPoint?spo=" & oTemplate.sId
        Dim sPage As String = Await GetREST(sTmp)
        If sPage.Length < 10 Then Return
        Dim aLines = sPage.Split(vbLf)
        Dim aFields = aLines(0).Split(",")
        Dim iVal As Integer = 0, iProp As Integer = 0, iDate As Integer = 0
        Dim i As Integer = 0, loopTo As Integer = aFields.GetUpperBound(0)

        While i <= loopTo
            If (If(aFields(i).ToUpper(), "")) = "VALUE_NUMERIC" Then iVal = i
            If (If(aFields(i).ToUpper(), "")) = "PROPERTY" Then iProp = i
            If (If(aFields(i).ToUpper(), "")) = "DATETIME_END" Then iDate = i
            i += 1
        End While

        sTmp = aLines(aLines.GetUpperBound(0))
        If sTmp.Length < 15 Then sTmp = aLines(aLines.GetUpperBound(0) - 1)
        aFields = sTmp.Split(","c)
        Dim dVal As Double
        If Not Double.TryParse(aFields(iVal), dVal) Then Return
        If dVal = -1 Then Return
        Dim oNew = New JedenPomiar(oTemplate.sSource) With {
            .sId = oTemplate.sId,
            .dLon = oTemplate.dLon,
            .dLat = oTemplate.dLat,
            .dWysok = oTemplate.dWysok,
            .dOdl = oTemplate.dOdl,
            .sOdl = Odleglosc2String(oTemplate.dOdl),
            .sAdres = oTemplate.sAdres
        }
        oNew.sPomiar = NormalizePomiarName(aFields(iProp))
        oNew.sUnit = NormalizeUnitName(aFields(iProp))
        oNew.dCurrValue = dVal
        oNew.sCurrValue = oNew.dCurrValue.ToString() + oNew.sUnit
        sTmp = aFields(iDate)
        If sTmp.Length = 14 Then sTmp = sTmp.Substring(0, 4) & "." & sTmp.Substring(4, 2) & "." & sTmp.Substring(6, 2) & " " & sTmp.Substring(8, 2) & ":" & sTmp.Substring(10, 2) & ":" & sTmp.Substring(12, 2)
        oNew.sTimeStamp = sTmp
        moListaPomiarow.Add(oNew)
    End Function

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Dim dMaxOdl As Double = 10
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        Dim sCmd As String
        Dim sData As String = DateTime.UtcNow.AddMinutes(-15).ToString("yyyyMMddHH") & "0000"

        For Each sPolu As String In {"PM10", "PM25", "NO2", "O3", "SO2", "CO"}
            sCmd = "Hourly?polu=" & sPolu & "&dt=" & sData
            Dim sPage As String = Await GetREST(sCmd)
            If sPage.Length < 10 Then Return moListaPomiarow
            Dim aLines = sPage.Split(vbLf)
            Dim aFields = aLines(0).Split(","c)
            Dim iId As Integer = 0, iName As Integer = 0, iLon As Integer = 0, iLat As Integer = 0, iAlt As Integer = 0
            Dim i As Integer = 0, loopTo As Integer = aFields.GetUpperBound(0)

            While i <= loopTo
                If (If(aFields(i).ToUpper(), "")) = "SAMPLINGPOINT_LOCALID" Then iId = i
                If (If(aFields(i).ToUpper(), "")) = "STATIONNAME" Then iName = i
                If (If(aFields(i).ToUpper(), "")) = "LONGITUDE" Then iLon = i
                If (If(aFields(i).ToUpper(), "")) = "LATITUDE" Then iLat = i
                If (If(aFields(i).ToUpper(), "")) = "ALTITUDE" Then iAlt = i
                i += 1
            End While

            Dim iMax As Integer = Math.Max(iId, iName)
            iMax = Math.Max(iMax, iLon)
            iMax = Math.Max(iMax, iLat)
            iMax = Math.Max(iMax, iAlt)
            Dim sTmp, sName As String
            Dim iInd As Integer
            i = 1
            Dim loopTo1 As Integer = aLines.GetUpperBound(0)

            While i <= loopTo1
                sTmp = aLines(i)
                sName = ""
                sTmp = sTmp.Replace("""""", """")
                sTmp = sTmp.Replace("""""", """")
                sTmp = sTmp.Replace("""""", """")
                iInd = sTmp.IndexOf("""")

                If iInd > 0 Then
                    Dim iInd1 As Integer = sTmp.IndexOf("""", iInd + 1)

                    If iInd1 > 0 Then
                        sName = sTmp.Substring(iInd + 1, iInd1 - iInd - 1)
                        sTmp = sTmp.Substring(0, iInd) & "PKremovedPK" & sTmp.Substring(iInd1 + 1)
                    Else
                        sName = sTmp.Substring(iInd + 1)
                        sTmp = sTmp.Substring(0, iInd) & "PKremovedPK"
                    End If
                End If

                aFields = sTmp.Split(",")
                If aFields.GetUpperBound(0) < iMax Then Continue For
                Dim dLat, dLon As Double
                If Not Double.TryParse(aFields(iLat), dLat) Then Continue For
                If Not Double.TryParse(aFields(iLon), dLon) Then Continue For
                Dim constR As Double = 6378137
                Dim constD As Double = Math.PI / 180
                dLon = dLon / constR / constD
                dLat = Math.Asin((Math.Exp(2 * dLat / constR) - 1) / (Math.Exp(2 * dLat / constR) + 1)) / constD
                Dim oTemplate = New JedenPomiar(SRC_POMIAR_SOURCE)
                oTemplate.sId = aFields(iId)
                oTemplate.dLon = dLon
                oTemplate.dLat = dLat
                Dim argresult = oTemplate.dWysok
                If Not Double.TryParse(aFields(iAlt), argresult) Then oTemplate.dWysok = 0
                oTemplate.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oTemplate.dLat, oTemplate.dLon))
                oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl)
                oTemplate.sAdres = aFields(iName)
                If (If(oTemplate.sAdres, "")) = "PKremovedPK" Then oTemplate.sAdres = sName
                oTemplate.sAdres = oTemplate.sAdres.Replace("Ã³", "ó")
                oTemplate.sAdres = oTemplate.sAdres.Replace("Å" & ChrW(132), "ń")
                If oTemplate.dOdl < dMaxOdl * 1000 Then Await GetPomiaryAsync(oTemplate, False)
                i += 1
            End While
        Next

        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        Dim oTemplate = FavTemplateLoad(SRC_POMIAR_SOURCE & "_" & sId, SRC_POMIAR_SOURCE)
        oTemplate.sId = sId
        Await GetPomiaryAsync(oTemplate, bInTimer)
        Return moListaPomiarow
    End Function
End Class

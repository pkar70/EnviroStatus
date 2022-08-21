Imports System.Linq
Imports System.Collections.ObjectModel
Imports Newtonsoft

Public Class Source_EUradiation
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceEUrad"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "EU Radioactivity Monitoring"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://remap.jrc.ec.europa.eu/api/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "EUremon"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://remap.jrc.ec.europa.eu/Help/Simple.aspx"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://remap.jrc.ec.europa.eu/Help/Simple.aspx"
    Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides ReadOnly Property SRC_ZASIEG As Zasieg = Zasieg.Europe
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
        Return "Radiation"
    End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        Return "nSv/h"
    End Function

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Dim dMaxOdl As Double = 50
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        ' liczą się tylko aktywne w ostatnich dwu tygodniach
        Dim sUriDateStart As String = Date.Now.AddDays(-14).ToString("yyyyMMddHH0000")
        Dim sUriDateEnd As String = Date.Now.ToString("yyyyMMddHH0000")
        Dim sPage As String = Await GetREST($"stations?type=Last&startDate={sUriDateStart}&endDate={sUriDateEnd}")
        If sPage.Length < 10 Then Return moListaPomiarow


        ' wczytaj XML
        Dim xml As New Xml.XmlDocument
        xml.LoadXml(sPage)

        ' <ArrayOfStation xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/JRC.REM.EURDEP.Web.App.Map.Public.DAL.Models">
        Dim oArray As Xml.XmlNode = xml.FirstChild

        For Each oStation As Xml.XmlNode In oArray.ChildNodes
            '<Station>
            '<Code>ȴȡɅɅɅɄ</Code>
            '<Country>ȴȡ</Country>
            '<Date>2022-08-15T10:00:00Z</Date>
            '<Latitude>18127.9316</Latitude>
            '<Longitude>6096.81934</Longitude>
            '<Name i : nil = "true" />
            '<Value>33703.2</Value>
            '</Station>


            Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)

            oNew.sId = oStation.Item("Code").InnerText ' + decode
            oNew.dLat = oStation.Item("Latitude").InnerText ' + decode
            oNew.dLon = oStation.Item("Longitude").InnerText ' + decode
            oNew.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oNew.dLat, oNew.dLon))
            oNew.sOdl = Odleglosc2String(oNew.dOdl)

            If oNew.dOdl > dMaxOdl Then Continue For

            oNew.sPomiar = NormalizePomiarName(Nothing)
            oNew.sUnit = Unit4Pomiar(Nothing)

            oNew.dCurrValue = oStation.Item("Value").InnerText / 1000 ' decode (dzielić przez 100, wtedy będzie poprawnie pokazywane
            oNew.sCurrValue = oNew.dCurrValue & " " & oNew.sUnit

            oNew.sTimeStamp = oStation.Item("Date").InnerText

            oNew.sAdres = oStation.Item("Country").InnerText ' + decode

            moListaPomiarow.Add(oNew)
        Next

        If moListaPomiarow.Count < 1 Then
            Await DialogBoxAsync("ERROR: no station in range")
            Return moListaPomiarow
        End If

        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow


        Dim sUriDateStart As String = Date.Now.AddHours(-12).ToString("yyyyMMddHH0000")
        Dim sUriDateEnd As String = Date.Now.ToString("yyyyMMddHH0000")
        Dim sPage As String = Await GetREST($"timeseries/v1/stations/timeseries/{sUriDateStart}&endDate={sUriDateEnd}?codes={sId}")
        If sPage.Length < 10 Then Return moListaPomiarow

        ' wczytaj XML
        Dim xml As New Xml.XmlDocument
        xml.LoadXml(sPage)

        ' <ArrayOfTimeSeriesItem xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/JRC.REM.EURDEP.Web.App.Map.Public.DAL.Models">
        Dim oArray As Xml.XmlNode = xml.FirstChild

        Dim oItem As Xml.XmlNode = oArray.LastChild
        '<TimeSeriesItem>
        '<Code>YE999 =</Code>
        '<Date>2022-08-10T13:00:00Z</Date>
        '<Value>114080</Value>
        '</TimeSeriesItem>

        Dim oNew = FavTemplateLoad(SRC_POMIAR_SOURCE & "_" & sId, SRC_POMIAR_SOURCE)

        oNew.dCurrValue = oItem.Item("Value").InnerText / 1000 ' decode (dzielić przez 100, wtedy będzie poprawnie pokazywane
        oNew.sCurrValue = oNew.dCurrValue & " " & oNew.sUnit

        moListaPomiarow.Add(oNew)

        Return moListaPomiarow
    End Function


    Private Function decryptString(t As String, e As Integer) As String
        'Function(t, e)
        '       var n, i, r, o, a;
        ' For (a =[], i= r = 0, o=t.length; 0<=o?r<o:o<r; i=0<=o?++r:--r)
        '' teoretycznie:   for(a=[], r=0; r < t.length; r++)
        '           n = t.charCodeAt(r),
        'n ^= e,
        'a.push(String.fromCharCode(n));
        ' Return a.join("")

        Dim sOut As String = ""
        For Each znak As Char In t
            Dim iZnak As Integer = AscW(znak)
            iZnak = iZnak Xor e
            sOut &= ChrW(iZnak)
        Next

        Return sOut
    End Function

    Private Function decryptNumber(t As Double, e As Double, Optional n As Double = 0) As Double
        'decryptNumber:
        ' Function(t, e, n)
        ' {
        '	var i=2<arguments.length&&void 0!==n?n:0;
        '	Return h.a.round((t - i * e) / (1001 - e), 2)
        ' },
        Return Math.Round((t - n * e) / (1001 - e), 2)
    End Function
End Class
'Function(t)
'{
' t.exports = JSON.parse('{
'"urlRoot": "{root}/api",
'"stations":"/stations",
'"lastUpdate":"/stations/all/last-updated",
'"stationDetails":"/stations/{code}",
'"graph":"/graph",
'"stationsInArea":"timeseries/v1/stations/{startDate}/{endDate}/area",
'"stationsTimeseries":"timeseries/v1/stations/timeseries/{startDate}/{endDate}"
'}')
'},


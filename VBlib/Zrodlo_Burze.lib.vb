Imports System.Collections.ObjectModel
Imports System.Net.Http

Partial Public Class Source_Burze
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceBurze"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Burze.dzis"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "http://burze.dzis.net/soap.php"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "burze"
    ' Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://burze.dzis.net/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://burze.dzis.net/"

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub


    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await GetDataFromFavSensorAsync("", "", False, oPos)
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        moListaPomiarow = New Collection(Of JedenPomiar)()

        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        If SRC_MY_KEY.Length < 8 Then Return moListaPomiarow

        Await BurzaRequest(oPos)
        Await OstrzezeniaRequest(oPos)

        Return moListaPomiarow
    End Function


    ''' <summary>
    ''' Dopisuje do moListaPomiarow po jednym JedenPomiar o każdym ostrzeżeniu
    ''' </summary>
    ''' <param name="oPos"></param>
    ''' <returns></returns>
    Private Async Function OstrzezeniaRequest(oPos As MyBasicGeoposition) As Task
        DumpCurrMethod()

        Dim soapgr As String = "<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:soap=""http://burze.dzis.net/soap.php"">"
        soapgr = soapgr & "   <soapenv:Header/>"
        soapgr = soapgr & "   <soapenv:Body>"
        soapgr = soapgr & "      <soap:ostrzezenia_pogodowe soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">"
        soapgr = soapgr & "          <y xsi:type=""xsd:float"">" & oPos.Longitude & "</y>"
        soapgr = soapgr & "          <x xsi:type=""xsd:float"">" & oPos.Latitude & "</x>"
        soapgr = soapgr & "          <klucz xsi:type=""xsd:string"">" & SRC_MY_KEY & "</klucz>"
        soapgr = soapgr & "      </soap:ostrzezenia_pogodowe>"
        soapgr = soapgr & "   </soapenv:Body>"
        soapgr = soapgr & "</soapenv:Envelope>"

        Dim sResult As String = Await GetDataFromURL(soapgr)

        If sResult = "" Then Return

        Dim xmlRes = New Xml.XmlDocument
        Try
            xmlRes.LoadXml(sResult)
        Catch ex As Exception
            DumpMessage("Error parsing XML")
            Return
        End Try

        If xmlRes.ChildNodes.Count < 1 Then Return

        OstrzezenieTyp(xmlRes, "mroz")
        OstrzezenieTyp(xmlRes, "upal")
        OstrzezenieTyp(xmlRes, "wiatr")
        OstrzezenieTyp(xmlRes, "opad")
        OstrzezenieTyp(xmlRes, "burza")
        OstrzezenieTyp(xmlRes, "traba")

    End Function

    Private Sub OstrzezenieTyp(xmlRes As Xml.XmlDocument, sElementName As String)

        Dim iInteger As Integer = 0
        Dim sOdDnia As String = ""
        Dim sDoDnia As String = ""

        For Each oNode As Xml.XmlNode In xmlRes.ChildNodes
            If oNode.Name = sElementName Then
                iInteger = oNode.InnerText
            End If

            If oNode.Name = sElementName & "_od_dnia" Then
                sOdDnia = oNode.InnerText
            End If

            If oNode.Name = sElementName & "_do_dnia" Then
                sDoDnia = oNode.InnerText
            End If
        Next

        If sOdDnia = "" OrElse sDoDnia = "" Then Return

        Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)
        oNew.sPomiar = GetLangString("resPomiarBurza_" & sElementName)
        oNew.sAlert = "!"

        ' 2014-12-21 20:45:00

        oNew.sAddit = GetLangString("msgBurzeValidity") & ": " & sOdDnia & " - " & sDoDnia

        Dim oDateB, oDateE As Date
        If Date.TryParseExact(sDoDnia, "yyyy-MM-dd HH:mm:ss",
                Globalization.CultureInfo.CurrentCulture,
                Globalization.DateTimeStyles.AssumeUniversal And Globalization.DateTimeStyles.AllowWhiteSpaces,
                oDateE) Then
            If oDateE < Date.UtcNow Then Return ' już PO
        End If

        If Date.TryParseExact(sOdDnia, "yyyy-MM-dd HH:mm:ss",
                Globalization.CultureInfo.CurrentCulture,
                Globalization.DateTimeStyles.AssumeUniversal And Globalization.DateTimeStyles.AllowWhiteSpaces,
                oDateB) Then
            If oDateB < Date.UtcNow Then
                ' już trwa
                oNew.sCurrValue = "... - " & oDateE.ToLocalTime.ToString("yy.MM.dd HH:mm")
            Else
                ' dopiero będzie
                oNew.sCurrValue = oDateB.ToLocalTime.ToString("yy.MM.dd HH:mm") & " - ..."
            End If
        Else
            ' nie wiadomo - napisz ile wlezie tekstu (błąd rozpoznania daty)
            oNew.sCurrValue = sOdDnia & " - " & sDoDnia
        End If

        moListaPomiarow.Add(oNew)

    End Sub


    ' kopia z https://adminek.pl/automatyka/13-odleglosc-od-burzy
    ''' <summary>
    ''' Dopisuje do moListaPomiarow JedenPomiar o najbliższej burzy
    ''' </summary>
    ''' <param name="oPos"></param>
    ''' <returns></returns>
    Private Async Function BurzaRequest(oPos As MyBasicGeoposition) As Task
        DumpCurrMethod()

        Dim promien As Integer ' promien w kilometrach
        If GetSettingsBool("settingsLiveClock") Then
            promien = 100   ' 60 minut, i z GPS
        Else
            promien = 25    ' 30 minut, last position/fav
        End If

        Dim soapgr As String = "<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:soap=""http://burze.dzis.net/soap.php"">"
        soapgr = soapgr & "   <soapenv:Header/>"
        soapgr = soapgr & "   <soapenv:Body>"
        soapgr = soapgr & "      <soap:szukaj_burzy soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">"
        soapgr = soapgr & "          <y xsi:type=""xsd:float"">" & oPos.Longitude & "</y>"
        soapgr = soapgr & "          <x xsi:type=""xsd:float"">" & oPos.Latitude & "</x>"
        soapgr = soapgr & "          <promien xsi:type=""xsd:int"">" & promien & "</promien>"   ' default: 25 km
        soapgr = soapgr & "          <klucz xsi:type=""xsd:string"">" & SRC_MY_KEY & "</klucz>"
        soapgr = soapgr & "      </soap:szukaj_burzy>"
        soapgr = soapgr & "   </soapenv:Body>"
        soapgr = soapgr & "</soapenv:Envelope>"

        Dim sResult As String = Await GetDataFromURL(soapgr)

        If sResult = "" Then Return

        Dim xmlRes = New Xml.XmlDocument
        Try
            xmlRes.LoadXml(sResult)
        Catch ex As Exception
            DumpMessage("Error parsing XML")
            Return
        End Try

        ' Dim nodes = xmlRes.selectNodes("//*")

        If xmlRes.ChildNodes.Count < 1 Then Return

        Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)

        For Each oNode As Xml.XmlNode In xmlRes.ChildNodes

            oNew.sPomiar = GetLangString("resPomiarBurza")

            If oNode.Name = "liczba" Then   ' liczba wyładowań (int)
                oNew.dCurrValue = oNode.InnerText
                oNew.sCurrValue = oNew.dCurrValue
            End If

            If oNode.Name = "odleglosc" Then    ' od burzy (float)
                oNew.dOdl = oNode.InnerText
                oNew.sOdl = oNode.InnerText & " km"
            End If

            If oNode.Name = "kierunek" Then    ' (string) kierunek Kierunek do najblizszego wyladowania (E, NE, N, NW, W, SW, S, SE)
                oNew.sAdres = oNode.InnerText
            End If

            If oNode.Name = "okres" Then   ' integer okres - liczba minut, okres czasu obejmujacy dane (10, 15, 20 minut)
                oNew.sAddit = oNode.InnerText & " min"
            End If
        Next

        ' *TODO* zamiana dCurrValue na sAlert, liczba wykrzykników w zależności od liczby wyładowań

        moListaPomiarow.Add(oNew)
    End Function

    Private _oHttp As Net.Http.HttpClient = Nothing

    Private Async Function GetDataFromURL(strPostData As String) As Task(Of String)
        DumpCurrMethod()

        If _oHttp Is Nothing Then
            Dim oHandler As New Net.Http.HttpClientHandler With {.AllowAutoRedirect = True}
            _oHttp = New Net.Http.HttpClient(oHandler)
            _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd("http_requester/0.1")
        End If

        Dim content As New StringContent(strPostData, Text.Encoding.UTF8, "text/xml")

        Try
            Dim oResult = Await _oHttp.PostAsync(SRC_RESTURI_BASE, content)
            If oResult.IsSuccessStatusCode Then Return Await oResult.Content.ReadAsStringAsync()

            DumpMessage($"Burze:HttpError: {oResult.StatusCode}: {oResult.StatusCode.ToString}")
        Catch ex As Exception
            DumpMessage($"Burze:GetDataFromURL exception: {ex.Message}")
        End Try

        Return ""

    End Function

End Class

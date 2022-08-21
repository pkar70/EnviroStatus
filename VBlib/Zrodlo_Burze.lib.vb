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
    Public Overrides ReadOnly Property SRC_ZASIEG As Zasieg = Zasieg.Poland


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


    Private Function WspolrzDoDM(dWspolrzedna As Double) As String
        ' https://github.com/PiotrMachowski/Home-Assistant-custom-components-Burze.dzis.net/blob/master/custom_components/burze_dzis_net/binary_sensor.py
        ' return '{}.{:02}'.format(int(dmf), round(dmf % 1 * 60))
        ' czyli ma być degree/minute, a nie normalny float
        Dim oTSpan As TimeSpan = TimeSpan.FromMinutes(dWspolrzedna)
        Return (oTSpan.Minutes + oTSpan.Hours * 24).ToString & "." & oTSpan.Seconds.ToString("0#")
    End Function

    Private Function GetDmForSoap(oPos As MyBasicGeoposition) As String
        Return $"<y xsi:type=""xsd:float"">{WspolrzDoDM(oPos.Latitude)}</y>" &
               $"<x xsi:type=""xsd:float"">{WspolrzDoDM(oPos.Longitude)}</x>" ' dla Krakowa ma być x=19, czyli długość geograficzna
    End Function

    ''' <summary>
    ''' Dopisuje do moListaPomiarow po jednym JedenPomiar o każdym ostrzeżeniu
    ''' </summary>
    ''' <param name="oPos"></param>
    ''' <returns></returns>
    Private Async Function OstrzezeniaRequest(oPos As MyBasicGeoposition) As Task
        DumpCurrMethod()

        Try

            Dim soapgr As String = "<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:soap=""http://burze.dzis.net/soap.php"">"
            soapgr = soapgr & "   <soapenv:Header/>"
            soapgr = soapgr & "   <soapenv:Body>"
            soapgr = soapgr & "      <soap:ostrzezenia_pogodowe soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">"
            soapgr = soapgr & GetDmForSoap(oPos)
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

            '<?xml version="1.0" encoding="UTF-8"?>" & vbLf & 
            '<SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ns1="https://burze.dzis.net/soap.php" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:SOAP-ENC="http://schemas.xmlsoap.org/soap/encoding/" SOAP-ENV:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/">
            '<SOAP-ENV:Body>
            '<ns1:ostrzezenia_pogodoweResponse>
            '<return xsi:type="ns1:MyComplexTypeOstrzezenia">'
            '<od_dnia xsi:type="xsd:string"></od_dnia><do_dnia xsi:type="xsd:string"></do_dnia>
            '<mroz xsi:type="xsd:int">0</mroz><mroz_od_dnia xsi:type="xsd:string">0</mroz_od_dnia><mroz_do_dnia xsi:type="xsd:string">0</mroz_do_dnia>
            '<upal xsi:type="xsd:int">0</upal><upal_od_dnia xsi:type="xsd:string">0</upal_od_dnia><upal_do_dnia xsi:type="xsd:string">0</upal_do_dnia>
            '<wiatr xsi:type="xsd:int">0</wiatr><wiatr_od_dnia xsi:type="xsd:string">0</wiatr_od_dnia><wiatr_do_dnia xsi:type="xsd:string">0</wiatr_do_dnia>
            '<opad xsi:type="xsd:int">0</opad><opad_od_dnia xsi:type="xsd:string">0</opad_od_dnia><opad_do_dnia xsi:type="xsd:string">0</opad_do_dnia>
            '<burza xsi:type="xsd:int">0</burza><burza_od_dnia xsi:type="xsd:string">0</burza_od_dnia><burza_do_dnia xsi:type="xsd:string">0</burza_do_dnia>
            '<traba xsi:type="xsd:int">0</traba><traba_od_dnia xsi:type="xsd:string">0</traba_od_dnia><traba_do_dnia xsi:type="xsd:string">0</traba_do_dnia>
            '</return></ns1:ostrzezenia_pogodoweResponse></SOAP-ENV:Body></SOAP-ENV:Envelope>" & vbLf


            '                                        Envelope.  Body        ostrzezenia.return
            Dim oReturnNode As Xml.XmlNode = xmlRes?.LastChild?.FirstChild?.FirstChild?.FirstChild
            If oReturnNode Is Nothing Then Return

            OstrzezenieTyp(oReturnNode, "mroz")
            OstrzezenieTyp(oReturnNode, "upal")
            OstrzezenieTyp(oReturnNode, "wiatr")
            OstrzezenieTyp(oReturnNode, "opad")
            OstrzezenieTyp(oReturnNode, "burza")
            OstrzezenieTyp(oReturnNode, "traba")
        Catch ex As Exception

        End Try

    End Function

    Private Sub OstrzezenieTyp(xmlRes As Xml.XmlNode, sElementName As String)

        Dim iStopien As Integer = 0
        Dim sOdDnia As String = ""
        Dim sDoDnia As String = ""

        For Each oNode As Xml.XmlNode In xmlRes.ChildNodes
            If oNode.Name = sElementName Then
                iStopien = oNode.InnerText
            End If

            If oNode.Name = sElementName & "_od_dnia" Then
                sOdDnia = oNode.InnerText
            End If

            If oNode.Name = sElementName & "_do_dnia" Then
                sDoDnia = oNode.InnerText
            End If
        Next

        If iStopien = 0 OrElse sOdDnia = "" OrElse sDoDnia = "" Then Return

        Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)
        oNew.sPomiar = GetLangString("resPomiarBurza_" & sElementName)
        oNew.dCurrValue = iStopien

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

        ' znaczenie wykrzykników:
        ' https://github.com/PiotrMachowski/Home-Assistant-custom-components-Burze.dzis.net/blob/master/custom_components/burze_dzis_net/binary_sensor.py
        ' albo tu:
        ' https://burze.dzis.net/?page=mapa_ostrzezen
        oNew.sAlert = "!"
        If iStopien = 2 Then oNew.sAlert = "!!"
        If iStopien = 3 Then oNew.sAlert = "!!!"

        oNew.sLimity = "1: " & GetLangString("resLimitBurza_" & sElementName & "_1") & vbCrLf &
            "2: " & GetLangString("resLimitBurza_" & sElementName & "_2") & vbCrLf &
            "3: " & GetLangString("resLimitBurza_" & sElementName & "_3")

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

        Try

            Dim promien As Integer ' promien w kilometrach
            If GetSettingsBool("settingsLiveClock") Then
                promien = 100   ' 60 minut, i z GPS
            Else
                promien = 50    ' 30 minut, last position/fav
            End If

            Dim soapgr As String = "<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:soap=""http://burze.dzis.net/soap.php"">"
            soapgr = soapgr & "   <soapenv:Header/>"
            soapgr = soapgr & "   <soapenv:Body>"
            soapgr = soapgr & "      <soap:szukaj_burzy soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">"
            soapgr = soapgr & GetDmForSoap(oPos)
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

            '"<?xml version=""1.0"" encoding=""UTF-8""?>" & vbLf & "
            '<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ns1=""https://burze.dzis.net/soap.php"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:SOAP-ENC=""http://schemas.xmlsoap.org/soap/encoding/"" SOAP-ENV:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
            '<SOAP-ENV:Body>
            '<ns1:szukaj_burzyResponse>
            '<return xsi:type=""ns1:MyComplexTypeBurza"">
            '<liczba xsi:type=""xsd:int"">0</liczba>
            '<odleglosc xsi:type=""xsd:float"">0</odleglosc>
            '<kierunek xsi:type=""xsd:string""></kierunek>
            '<okres xsi:type=""xsd:int"">10</okres>
            '</return>
            '</ns1:szukaj_burzyResponse>
            '</SOAP-ENV:Body></SOAP-ENV:Envelope>" & vbLf


            '                                        Envelope.  Body        szukaj_burzy.return
            Dim oReturnNode As Xml.XmlNode = xmlRes?.LastChild?.FirstChild?.FirstChild?.FirstChild
            If oReturnNode Is Nothing Then Return

            Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)
            oNew.sPomiar = GetLangString("resPomiarBurza")

            For Each oNode As Xml.XmlNode In oReturnNode

                If oNode.Name = "liczba" Then   ' liczba wyładowań (int)
                    oNew.dCurrValue = oNode.InnerText
                    ' If oNew.dCurrValue = 0 Then Return ' nie ma powiadomienia, więc nie ma sensu tego pisać
                    oNew.sCurrValue = oNew.dCurrValue
                End If

                If oNode.Name = "odleglosc" Then    ' od burzy (float)
                    oNew.dOdl = oNode.InnerText
                    oNew.sOdl = oNode.InnerText & " km"
                End If

                If oNode.Name = "kierunek" Then    ' (string) kierunek Kierunek do najblizszego wyladowania (E, NE, N, NW, W, SW, S, SE)
                    If oNode.InnerText <> "" Then
                        oNew.sAdres = GetLangString("resBurzeDirection") & ": " & oNode.InnerText
                    End If
                End If

                    If oNode.Name = "okres" Then   ' integer okres - liczba minut, okres czasu obejmujacy dane (10, 15, 20 minut)
                    oNew.sAddit = GetLangString("resBurzaOkres") & ": " & oNode.InnerText & " min"
                End If
            Next

            ' *TODO* zamiana dCurrValue na sAlert, liczba wykrzykników w zależności od liczby wyładowań
            If oNew.dCurrValue > 0 Then oNew.sAlert = "!"

            moListaPomiarow.Add(oNew)
            '    End If
            'Next

        Catch ex As Exception

        End Try


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

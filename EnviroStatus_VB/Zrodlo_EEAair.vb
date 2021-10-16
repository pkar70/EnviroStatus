Imports EnviroStatus

Public Class Source_EEAair
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceEEAair"
    Protected Overrides Property SRC_SETTING_HEADER As String = "EEA air (test)"
    Protected Overrides Property SRC_RESTURI_BASE As String = "https://discomap.eea.europa.eu/Map/UTDViewer/dataService/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "EEAair"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Protected Overrides Property SRC_HAS_TEMPLATES As Boolean = True

    ' teoretycznie nie ma powodu robić template, bo aktualne dane sa w pliku z lista sensorow,
    '   ale plik z listą sensorów 62..260 kB, a plik z historią sensora tylko 41 kB

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

    Private Async Function GetPomiary(oTemplate As JedenPomiar, bInTimer As Boolean) As Task
        ' do moListaPomiarow dodaj wszystkie pomiary robione przez oTemplate.sId
        ' a kiedy: Alert, Limity, sCurrValue, dCurrValue, sPomiar

        If Not GetSettingsBool(SRC_SETTING_NAME, True) Then Return

        Dim sTmp As String
        sTmp = "SamplingPoint?spo=" & oTemplate.sId
        ' SAMPLINGPOINT_LOCALID,DATETIME_BEGIN,DATETIME_END,PROPERTY,VALUE_NUMERIC,UNIT,STATIONCLASSIFICATION,AREACLASSIFICATION,ALTITUDE,STATIONCODE,STATIONNAME,LONGITUDE,LATITUDE,MUNICIPALITY
        ' SPO.DE_DEST091_PM1_dataGroup1,20191215070000,20191215080000,PM10,8.55,Âµg/m3,traffic,urban,61.0000,DEST091,Dessau Albrechtsplatz,1363080.8934,6771350.7223,Dessau-RoÃŸlau
        ' SPO_PL0012A_5_001,20191215070000,20191215080000,PM10,27.5748,Âµg/m3,traffic,urban,207.0000,PL0012A,"KrakÃ³w, Aleja KrasiÅ„skiego",2218173.2129,6456270.6528,KrakÃ³w

        Dim sPage As String = Await GetREST(sTmp)

        Dim aLines As String() = sPage.Split(vbLf)

        Dim aFields As String() = aLines(0).Split(",")

        ' najpierw sprawdzamy kolumny (tak na wszelki wypadek, bo to i tak nic nie kosztuje)
        Dim iVal, iProp, iDate As Integer
        For i As Integer = 0 To aFields.GetUpperBound(0)
            If aFields(i).ToUpper = "VALUE_NUMERIC" Then iVal = i
            If aFields(i).ToUpper = "PROPERTY" Then iProp = i
            If aFields(i).ToUpper = "DATETIME_END" Then iDate = i
        Next

        ' interesuje nas ostatnia linijka
        sTmp = aLines(aLines.GetUpperBound(0))
        If sTmp.Length < 15 Then sTmp = aLines(aLines.GetUpperBound(0) - 1)
        aFields = sTmp.Split(",")
        Dim dVal As Double
        If Not Double.TryParse(aFields(iVal), dVal) Then Return ' nie liczba? error
        If dVal = -1 Then Return    ' "invalid"

        Dim oNew As JedenPomiar = New JedenPomiar With {
                .sSource = oTemplate.sSource,
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
        oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
        sTmp = aFields(iDate)
        If sTmp = 14 Then
            ' 20191215080000
            sTmp = sTmp.Substring(0, 4) & "." & sTmp.Substring(4, 2) & "." & sTmp.Substring(6, 2) & " " & sTmp.Substring(8, 2) & ":" & sTmp.Substring(10, 2) & ":" & sTmp.Substring(12, 2)
        End If
        oNew.sTimeStamp = sTmp
        moListaPomiarow.Add(oNew)


    End Function


    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        ' ma zwrocic pełną liste, save template pozniej zostanie wywolane

        Dim dMaxOdl As Double = 10

        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME, True) Then Return moListaPomiarow

        Dim sCmd As String
        Dim sData As String = DateTime.UtcNow.AddMinutes(-15).ToString("yyyyMMddHH") & "0000"
        For Each sPolu As String In {"PM10", "PM25", "NO2", "O3", "SO2", "CO"}
            sCmd = "Hourly?polu=" & sPolu & "&dt=" & sData
            ' SAMPLINGPOINT_LOCALID,DATETIME_BEGIN,DATETIME_END,PROPERTY,VALUE_NUMERIC,UNIT,STATIONCLASSIFICATION,AREACLASSIFICATION,ALTITUDE,STATIONCODE,STATIONNAME,LONGITUDE,LATITUDE,MUNICIPALITY
            ' SPO.DE_DEST091_PM1_dataGroup1,20191215070000,20191215080000,PM10,8.55,Âµg/m3,traffic,urban,61.0000,DEST091,Dessau Albrechtsplatz,1363080.8934,6771350.7223,Dessau-RoÃŸlau
            ' SPO_PL0012A_5_001,20191215070000,20191215080000,PM10,27.5748,Âµg/m3,traffic,urban,207.0000,PL0012A,"KrakÃ³w, Aleja KrasiÅ„skiego",2218173.2129,6456270.6528,KrakÃ³w

            Dim sPage As String = Await GetREST(sCmd)

            Dim aLines As String() = sPage.Split(vbLf)

            Dim aFields As String() = aLines(0).Split(",")

            ' najpierw sprawdzamy kolumny (tak na wszelki wypadek, bo to i tak nic nie kosztuje)
            Dim iId, iName, iLon, iLat, iAlt As Integer
            For i As Integer = 0 To aFields.GetUpperBound(0)
                If aFields(i).ToUpper = "SAMPLINGPOINT_LOCALID" Then iId = i
                If aFields(i).ToUpper = "STATIONNAME" Then iName = i
                If aFields(i).ToUpper = "LONGITUDE" Then iLon = i
                If aFields(i).ToUpper = "LATITUDE" Then iLat = i
                If aFields(i).ToUpper = "ALTITUDE" Then iAlt = i
            Next

            Dim iMax As Integer = Math.Max(iId, iName)
            iMax = Math.Max(iMax, iLon)
            iMax = Math.Max(iMax, iLat)
            iMax = Math.Max(iMax, iAlt)

            Dim sTmp, sName As String
            Dim iInd As Integer

            For i As Integer = 1 To aLines.GetUpperBound(0)
                sTmp = aLines(i)

                ' usuniemy jak są cudzysłowy
                sName = ""

                ' ominiecie bledu: """Kochla"""
                ' "SPO-EE0019A_00005_100,20191215110000,20191215120000,PM10,2.577,Âµg/m3,industrial,urban,60.0000,EE0019A,"" "" ""Kohtla-JÃ¤rve"""""",3036642.2738,8269476.1822,Kohtla-JÃ¤rve" & vbCr
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
                        ' SPO.IE.IE004APSample2_5,20191215110000,20191215120000,PM10,6,Âµg/m3,traffic,suburban,12.0000,IE004AP,"Dublin Ringsend Recycling Centre
                        ' ma zmianę linii w środku!
                        sName = sTmp.Substring(iInd + 1)
                        sTmp = sTmp.Substring(0, iInd) & "PKremovedPK"
                    End If
                End If

                aFields = sTmp.Split(",")
                ' jesli brakuje pól - pomijamy
                If aFields.GetUpperBound(0) < iMax Then Continue For

                Dim dLat, dLon As Double
                If Not Double.TryParse(aFields(iLat), dLat) Then Continue For
                If Not Double.TryParse(aFields(iLon), dLon) Then Continue For

                ' rekonfiguracja wedle leaflet

                Dim constR As Double = 6378137
                Dim constMAX_LATITUDE As Double = 85.0511287798
                Dim constD As Double = Math.PI / 180

                dLon = dLon / constR / constD
                dLat = Math.Asin((Math.Exp(2 * dLat / constR) - 1) / (Math.Exp(2 * dLat / constR) + 1)) / constD

                Dim oTemplate As JedenPomiar = New JedenPomiar
                oTemplate.sSource = SRC_POMIAR_SOURCE
                oTemplate.sId = aFields(iId)
                oTemplate.dLon = dLon
                oTemplate.dLat = dLat
                If Not Double.TryParse(aFields(iAlt), oTemplate.dWysok) Then oTemplate.dWysok = 0
                oTemplate.dOdl = GPSdistanceDwa(oPos.X, oPos.Y, oTemplate.dLat, oTemplate.dLon)
                oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl)
                oTemplate.sAdres = aFields(iName)
                If oTemplate.sAdres = "PKremovedPK" Then oTemplate.sAdres = sName

                ' depolit, bo plik ma niepoprawne kodowanie
                oTemplate.sAdres = oTemplate.sAdres.Replace("Ã³", "ó")
                oTemplate.sAdres = oTemplate.sAdres.Replace("Å", "ń")

                If oTemplate.dOdl < dMaxOdl * 1000 Then
                    Await GetPomiary(oTemplate, False)
                End If

            Next

        Next

        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        ' wywolywane dla kazdego z Template
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME, True) Then Return moListaPomiarow

        ' przeiteruj, tworząc JedenPomiar dla kazdego pomiaru i kazdej lokalizacji
        Dim oTemplate As JedenPomiar = New JedenPomiar

        ' wczytaj dane template dla danego favname
        Dim oFile As Windows.Storage.StorageFile =
        Await App.GetDataFile(False, SRC_POMIAR_SOURCE & "_" & sId & ".xml", False)
        If oFile IsNot Nothing Then
            Dim oSer As Xml.Serialization.XmlSerializer =
                New Xml.Serialization.XmlSerializer(GetType(JedenPomiar))
            Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
            oTemplate = TryCast(oSer.Deserialize(oStream), JedenPomiar)
            oStream.Dispose()   ' == fclose
        Else
            oTemplate = New JedenPomiar
        End If

        oTemplate.sSource = SRC_POMIAR_SOURCE  ' to tak na wszelki wypadek
        oTemplate.sId = sId

        Await GetPomiary(oTemplate, bInTimer)

        Return moListaPomiarow

    End Function
End Class

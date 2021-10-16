Imports EnviroStatus

Public Class Source_SeismicPortal
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceSeismicEU"
    Protected Overrides Property SRC_SETTING_HEADER As String = "SeismicPortal EU"
    Protected Overrides Property SRC_RESTURI_BASE As String = "https://www.seismicportal.eu/fdsnws/event/1/query?callback=angular.callbacks._1&format=jsonp&limit=50&offset=1&orderby=time"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "SeismicEU"


    Private Function EnergyKJoulesFromMag(dMag As Double) As Double
        ' 4.94065645841246544E-324 ... 1.79769313486231570E+308
        Dim dTmp As Double = Math.Pow(10, (2.24 + 1.44 * dMag))
        Return dTmp
    End Function

    Private Function PowerToPrefix(iPower As Integer) As String
        If iPower < -9 Then Return "p"
        If iPower < -6 Then Return "n"
        If iPower < -3 Then Return "μ"
        If iPower < 0 Then Return "m"

        If iPower < 3 Then Return ""
        If iPower < 6 Then Return "k"
        If iPower < 9 Then Return "M"
        If iPower < 12 Then Return "G"
        If iPower < 15 Then Return "T"
        If iPower < 18 Then Return "P"

        Return ""
    End Function
    Private Function BigNumPrefix(dValue As Double, iPower As Integer) As String

        If dValue < 1 Then
            Do
                dValue *= 1000
                iPower -= 3
                If dValue > 1 Then Return CInt(dValue).ToString & " " & PowerToPrefix(iPower)
                If iPower < -9 Then Return dValue & " p"
            Loop
        End If

        Do
            If dValue < 1000 Then Return CInt(dValue).ToString & " " & PowerToPrefix(iPower)
            If iPower > 17 Then Return dValue.ToString("###########################################0") & " P"
            dValue /= 1000
            iPower += 3
        Loop
    End Function

    Private Function MakeOpisFromKJoules(dKJoules As Double) As String
        Dim dMJoul As Double
        dMJoul = dKJoules / 1000

        Dim dMWh As Double = dMJoul / 3600 ' 1 W = 1 J/s ; 1Wh = 1J /s * 3600 s ; 1 MWh = 1MJ * 3600
        Dim dTonTNT As Double = dKJoules / (4.184 * 1000 * 1000) ' "ton of TNT" = 4.184 gigajoules
        Dim dHirosz As Double = dTonTNT / 15 * 1000     ' 15 kT
        Dim dAnnih As Double = dMJoul / 299792458.0 / 299792.458 ' E = mc², czyli m = E/c²; kropka przesunięta co robi z MJ zwykle J, czyli efekt w kg jest, co dzielimy przez 1000, by miec zwykle gramy
        Dim dWorldDayEnergy As Double = 365 * dMWh / (26614800.0 * 1000)  ' 26614800 GWh rocznie 2018

        Dim sTxt As String
        sTxt = "Released energy (about):" & vbCrLf
        sTxt = sTxt & "= " & BigNumPrefix(dMJoul, 6) & "J," & vbCrLf
        sTxt = sTxt & "= " & BigNumPrefix(dMWh, 6) & "Wh," & vbCrLf

        If dTonTNT < 10 Then
            dTonTNT *= 1000   ' na kg
            If dTonTNT > 1 Then
                sTxt = sTxt & "= " & BigNumPrefix(dTonTNT, 3) & "g TNT," & vbCrLf
            Else
                sTxt = sTxt & "= " & BigNumPrefix(dTonTNT, 1) & "g TNT," & vbCrLf
            End If
        Else
            sTxt = sTxt & "= " & BigNumPrefix(dTonTNT, 1) & "ton TNT," & vbCrLf
        End If

        If dHirosz > 1 Then
            sTxt = sTxt & "= " & BigNumPrefix(dHirosz, 1) & " Hiroshima bombs," & vbCrLf
        End If

        sTxt = sTxt & "= " & BigNumPrefix(dAnnih, 1) & "g (of matter)," & vbCrLf
        If dWorldDayEnergy > 1 Then sTxt = sTxt & "= " & BigNumPrefix(dWorldDayEnergy, 1) & " days of energy production"

        Return sTxt
        ' teragram of TNT	Tg	megaton of TNT	Mt	4.184×1015 J or 4.184 petajoules	1.162 TWh	mass loss 46.55 g

    End Function

    Private Function MakeOpisDokladnySingle(dMag As Double) As String
        Dim dKJoules As Double = EnergyKJoulesFromMag(dMag)
        Return MakeOpisFromKJoules(dKJoules)
    End Function
    Private Function MakeOpisDokladnySum(dValue As Double, iCount As Integer, sOldestTimestamp As String, sNewestTStamp As String) As String
        ' mamy już sumę energii w dValue, oraz licznik zdarzeń w iCount
        ' przeliczanie w Details na kT, Hirosima, Car, gram anihilacji, kJ, produkcja roczna energii na swiecie na 2017 rok (albo miesiac)
        Return "Total eartquakes: " & iCount & vbCrLf &
            "(" & sOldestTimestamp & " - " & sNewestTStamp & ")" & vbCrLf & vbCrLf &
            MakeOpisFromKJoules(dValue)

    End Function

    Private Async Function WczytujDane(dPosLat As Double, dPosLon As Double, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))

        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME, True) Then Return moListaPomiarow

        Dim sPage As String = Await GetREST(SRC_RESTURI_BASE)
        Dim iInd As Integer
        'iInd = sPage.IndexOf("{")
        'sPage = sPage.Substring(iInd)

        iInd = sPage.IndexOf("""features")
        sPage = "{" & sPage.Substring(iInd)
        iInd = sPage.LastIndexOf(")")
        sPage = sPage.Substring(0, iInd)


        ' {"arr": [{ "geometry" {

        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonValue = Nothing
        Try
            oJson = Windows.Data.Json.JsonValue.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            If Not bInTimer Then Await DialogBox("ERROR: JSON parsing error - " & SRC_SETTING_HEADER & ":global")
            Return moListaPomiarow
        End If

        Dim oJsonArr As Windows.Data.Json.JsonArray
        oJsonArr = oJson.GetObject.GetNamedArray("features")


        Dim oNewSum As JedenPomiar = New JedenPomiar With {
                .sSource = SRC_POMIAR_SOURCE,
                .sPomiar = "magΣ",
                .sUnit = "mag"
                }
        Dim oNearest As JedenPomiar = New JedenPomiar With {
                .sSource = SRC_POMIAR_SOURCE,
                .sPomiar = "mag",
                .sUnit = "mag"
                }

        Dim dMaxMag As Double = 0    ' najmocniejszy "skuteczny" w liscie
        Dim iZasieg As Integer = DistanceNum2Metry(GetSettingsInt(SRC_SETTING_NAME & "_distance")) ' liczone do SUMA

        For Each oVal As Windows.Data.Json.JsonValue In oJsonArr
            Dim oJsonProp As Windows.Data.Json.JsonValue = oVal.GetObject.GetNamedValue("properties")

            Dim dLat, dLon, dOdl, dMag As Double
            dLat = oJsonProp.GetObject.GetNamedNumber("lat", 0)
            dLon = oJsonProp.GetObject.GetNamedNumber("lon", 0)
            dOdl = GPSdistanceDwa(dPosLat, dPosLon, dLat, dLon)     '  na kilometry
            dMag = oJsonProp.GetObject.GetNamedNumber("mag", 0)

            ' suma tych w zadanym promieniu
            If dOdl / 1000 < iZasieg Then
                oNewSum.dLat += 1   ' licznik zdarzen
                oNewSum.dLon = oNewSum.dLon + (EnergyKJoulesFromMag(dMag) / 1000)
                oNewSum.dCurrValue = Math.Max(oNewSum.dCurrValue, dMag)   ' tu bedzie max
                oNewSum.sCurrValue = oJsonProp.GetObject.GetNamedString("time", "")
                If oNewSum.sTimeStamp = "" Then oNewSum.sTimeStamp = oNewSum.sCurrValue
            End If

            ' najsilniej odczuwane - zakladam malenie z kwadratem 
            Dim dOdlTmp As Double = Math.Max(0.5, dOdl / 1000) ' zeby nie poleciało do nieskonczonosci przy bliskich
            dOdlTmp = dOdlTmp * dOdlTmp
            If dMag / dOdlTmp > dMaxMag Then
                dMaxMag = dMag / dOdlTmp
                oNearest.dLat = dLat
                oNearest.dLon = dLon
                oNearest.dOdl = dOdl
                oNearest.dCurrValue = dMag
                oNearest.sTimeStamp = oJsonProp.GetObject.GetNamedString("time", "")
                oNearest.dWysok = oJsonProp.GetObject.GetNamedNumber("depth", 0)
                oNearest.sAdres = oJsonProp.GetObject.GetNamedString("flynn_region", "")


            End If

        Next

        oNearest.sCurrValue = oNearest.dCurrValue & " " & oNearest.sUnit
        oNearest.sOdl = Odleglosc2String(oNearest.dOdl)
        oNearest.sAddit = MakeOpisDokladnySingle(oNearest.dCurrValue)
        oNearest.sTimeStamp = oNearest.sTimeStamp.Replace("T", " ")

        oNewSum.sTimeStamp = oNewSum.sTimeStamp.Replace("T", " ")   ' timestamp najnowszego
        oNewSum.sCurrValue = oNewSum.sCurrValue.Replace("T", " ")   ' timestamp najstarszego
        oNewSum.sAddit = MakeOpisDokladnySum(oNewSum.dLon, oNewSum.dLat, oNewSum.sCurrValue, oNewSum.sTimeStamp)
        oNewSum.sCurrValue = oNewSum.dCurrValue & " " & oNewSum.sUnit
        oNewSum.dLat = 0
        oNewSum.dLon = 0


        If oNearest.dCurrValue > 0 Then moListaPomiarow.Add(oNearest)
        If oNewSum.dCurrValue > 0 Then moListaPomiarow.Add(oNewSum)
        Return moListaPomiarow

        'angular.callbacks._3(
        '   {
        '       "type": "FeatureCollection",
        '       "metadata": {"totalCount":836839},
        '       "features":[
        '           {
        '               "geometry": {
        '                           "type": "Point",
        '                           "coordinates": [     -65.96,     -22.29,      -304.0   ]
        '                           },
        '               "type": "Feature",
        '               "id": "20191215_0000129",
        '               "properties":
        '               {
        '                   "lastupdate": "2019-12-15T16:30:00.0Z",
        '                   "magtype": "m",
        '                   "evtype": "ke",
        '                   "lon": -65.96,
        '                   "auth": "NSNA",
        '                   "lat": -22.29,
        '                   "depth": 304.0,
        '                   "unid": "20191215_0000129",
        '                   "mag": 3.5,
        '                   "time": "2019-12-15T16:24:29.0Z",
        '                   "source_id": "812313",
        '                   "source_catalog": "EMSC-RTS",
        '                   "flynn_region": "JUJUY, ARGENTINA"
        '               }
        '           },
        '  }
        '}]})
    End Function

    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Return Await WczytujDane(oPos.X, oPos.Y, False)
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        Return Await WczytujDane(sId, sAddit, bInTimer)
    End Function


    Private Function DistanceNum2Metry(iDist As Integer) As Integer
        Select Case iDist
            Case 1
                Return 10
            Case 2
                Return 100
            Case 3
                Return 1000
            Case 4
                Return 10000
            Case 5
                Return 100000
            Case Else
                Return 0
        End Select

    End Function

    Private Function DistanceNum2Opis(iDist As Integer) As String

        Select Case iDist
            Case 1
                Return "10 km"
            Case 2
                Return "100 km"
            Case 3
                Return "1000 km"
            Case 4
                Return "10 000 km"
            Case 5
                Return GetLangString("resSeismicEU_DistAll")
            Case Else
                Return "???"
        End Select
    End Function

    Public Sub uiSettDistance_Changed(sender As Object, e As RangeBaseValueChangedEventArgs)
        Dim oSld As Slider
        oSld = TryCast(sender, Slider)
        If oSld IsNot Nothing Then
            Dim oGrid As Grid = TryCast(oSld.Parent, Grid)
            For Each oItem As UIElement In oGrid.Children
                Dim oTB As TextBlock
                oTB = TryCast(oItem, TextBlock)
                If oTB IsNot Nothing Then
                    If oTB.Name = "uiConfig_SeismicEU_Text" Then oTB.Text = DistanceNum2Opis(oSld.Value)
                End If
            Next
        End If

    End Sub


    Public Overrides Sub ConfigCreate(oStack As StackPanel)
        MyBase.ConfigCreate(oStack)

        Dim oSld As Slider = New Slider
        oSld.Name = "uiConfig_SeismicEU_Slider"
        oSld.Minimum = 1
        oSld.Maximum = 5
        oSld.Value = GetSettingsInt(SRC_SETTING_NAME & "_distance", 2)
        oSld.Header = GetLangString("resSeismicEU_SldHdr")
        oSld.HorizontalAlignment = HorizontalAlignment.Stretch
        AddHandler oSld.ValueChanged, AddressOf uiSettDistance_Changed

        Dim oTB As TextBlock = New TextBlock
        oTB.Name = "uiConfig_SeismicEU_Text"
        oTB.Text = DistanceNum2Opis(oSld.Value)

        Dim oCol1 As ColumnDefinition = New ColumnDefinition
        oCol1.Width = New GridLength(1, GridUnitType.Star)
        Dim oCol2 As ColumnDefinition = New ColumnDefinition
        oCol2.Width = New GridLength(0, GridUnitType.Auto)

        Dim oGrid As Grid = New Grid
        oGrid.ColumnDefinitions.Add(oCol1)
        oGrid.ColumnDefinitions.Add(oCol2)

        oGrid.Children.Add(oSld)
        oGrid.Children.Add(oTB)

        Grid.SetColumn(oSld, 0)
        Grid.SetColumn(oTB, 1)

        oStack.Children.Add(oGrid)

        'Dim oTS As ToggleSwitch = New ToggleSwitch
        'oTS.Name = "uiConfig_SeismicEU_MaxAll"
        'oTS.IsOn = GetSettingsBool(SRC_SETTING_NAME & "_MaxAll")
        'oTS.OnContent = GetLangString("resSeismicEU_All")
        'oTS.OffContent = GetLangString("resSeismicEU_Max")
        'oStack.Children.Add(oTS)
    End Sub

    Public Overrides Sub ConfigRead(oStack As StackPanel)
        MyBase.ConfigRead(oStack)

        For Each oItem As UIElement In oStack.Children
            'Dim oTS As ToggleSwitch
            'oTS = TryCast(oItem, ToggleSwitch)
            'If oTS IsNot Nothing Then
            '    If oTS.Name = "uiConfig_SeismicEU_MaxAll" Then SetSettingsBool(SRC_SETTING_NAME & "_MaxAll", oTS.IsOn)
            'End If

            Dim oGrid As Grid
            oGrid = TryCast(oItem, Grid)
            If oGrid IsNot Nothing Then
                For Each oChild In oGrid.Children
                    Dim oSld As Slider
                    oSld = TryCast(oChild, Slider)
                    If oSld IsNot Nothing Then
                        If oSld.Name = "uiConfig_SeismicEU_Slider" Then SetSettingsInt(SRC_SETTING_NAME & "_distance", oSld.Value)
                    End If
                Next
            End If
        Next
    End Sub
End Class

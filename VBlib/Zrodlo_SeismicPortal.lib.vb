
Imports System.Collections.ObjectModel

Public Class Source_SeismicPortal
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceSeismicEU"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "SeismicPortal EU"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://www.seismicportal.eu/fdsnws/event/1/query?callback=angular.callbacks._1&format=jsonp&limit=50&offset=1&orderby=time"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "SeismicEU"

    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://www.seismicportal.eu/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://www.seismicportal.eu/"

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Public Shared Function EnergyKJoulesFromMag(dMag As Double) As Double
        Dim dTmp As Double = Math.Pow(10, 2.24 + 1.44 * dMag)
        Return dTmp
    End Function

    Private Shared Function PowerToPrefix(iPower As Integer) As String
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

    Private Shared Function BigNumPrefix(dValue As Double, iPower As Integer) As String
        If dValue < 1 Then
            Do
                dValue *= 1000
                iPower -= 3
                If dValue > 1 Then Return CInt(dValue).ToString & " " & PowerToPrefix(iPower)
                If iPower < -9 Then Return dValue.ToString() & " p"
            Loop While True
        End If

        Do
            If dValue < 1000 Then Return (CInt((dValue))).ToString & " " & PowerToPrefix(iPower)
            If iPower > 17 Then Return dValue.ToString("###########################################0") & " P"
            dValue /= 1000
            iPower += 3
        Loop While True

        Return ""   ' nieosiągalny kod, ale kompilator tego nie widzi (w C# widzi!)
    End Function

    Private Function MakeOpisFromKJoules(dKJoules As Double) As String
        Dim dMJoul As Double
        dMJoul = dKJoules / 1000
        Dim dMWh As Double = dMJoul / 3600
        Dim dTonTNT As Double = dKJoules / (4.184 * 1000 * 1000)
        Dim dHirosz As Double = dTonTNT / 15000
        Dim dAnnih As Double = dMJoul / 299792458.0 / 299792.458
        Dim dWorldDayEnergy As Double = 365 * dMWh / (26614800.0 * 1000)
        Dim sTxt As String
        sTxt = "Released energy (about):" & vbCrLf
        sTxt = sTxt & $"= {BigNumPrefix(dMJoul, 6) }J, " & vbCrLf
        sTxt = sTxt & $"= {BigNumPrefix(dMWh, 6) }Wh, " & vbCrLf
        Dim kWh As Integer = GetSettingsInt(SRC_SETTING_NAME & "_homekWh", 0) ' default moje: 150
        If kWh > 0 Then sTxt = sTxt & $"= {CInt(dMWh * 1000 / kWh)} {GetLangString("resSeismicEU_HomekWh")}, " & vbCrLf
        kWh = GetSettingsInt(SRC_SETTING_NAME & "_krajTWh", 0) / 12 ' default moje: 170

        If kWh > 0 Then
            kWh = CInt((dMWh / 1000 / 1000 / kWh))
            If kWh > 0 Then sTxt = sTxt & $"= {kWh} {GetLangString("resSeismicEU_KrajTWh")}, " & vbCrLf
        End If

        If dTonTNT < 10 Then
            dTonTNT *= 1000   ' na kg

            If dTonTNT > 1 Then
                sTxt = sTxt & $"= {BigNumPrefix(dTonTNT, 3)}g TNT, " & vbCrLf
            Else
                sTxt = sTxt & $"= {BigNumPrefix(dTonTNT, 1)}g TNT, " & vbCrLf
            End If
        Else
            sTxt = sTxt & "= {BigNumPrefix(dTonTNT, 1)}ton TNT, " & vbCrLf
        End If

        If dHirosz > 0.1 Then sTxt = sTxt & $"= {BigNumPrefix(dHirosz, 1)} Hiroshima bombs, " & vbCrLf
        sTxt = sTxt & $"= {BigNumPrefix(dAnnih, 1)}g (of matter), " & vbCrLf
        If dWorldDayEnergy > 1 Then sTxt = sTxt & $"= {BigNumPrefix(dWorldDayEnergy, 1)} days of energy production"
        Return sTxt
    End Function

    Private Function MakeOpisDokladnySingle(dMag As Double) As String
        Dim dKJoules As Double = EnergyKJoulesFromMag(dMag)
        Return MakeOpisFromKJoules(dKJoules)
    End Function

    Private Function MakeOpisDokladnySum(dKJoules As Double, iCount As Integer, sOldestTimestamp As String, sNewestTStamp As String) As String
        Return $"Total eartquakes: {iCount}" & vbCrLf & $"({sOldestTimestamp} - {sNewestTStamp})" & vbCrLf & vbCrLf & MakeOpisFromKJoules(dKJoules)
    End Function

    Private Async Function WczytujDane(oPos As pkar.BasicGeopos, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        Dim moListaPomiarow As New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        Dim sPage As String = Await GetREST(SRC_RESTURI_BASE)
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim iInd As Integer
        iInd = sPage.IndexOf("""features")
        sPage = "{" & sPage.Substring(iInd)
        iInd = sPage.LastIndexOf(")")
        sPage = sPage.Substring(0, iInd)
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
            If Not bInTimer Then Await DialogBoxAsync("ERROR: JSON parsing error - getting sensor data (" & SRC_POMIAR_SOURCE & ")" & vbLf & " " & sErr)
            Return moListaPomiarow
        End If

        Dim oJsonArr As Newtonsoft.Json.Linq.JArray
        oJsonArr = oJson.GetObject().GetNamedArray("features")
        Dim oNewSum = New JedenPomiar(SRC_POMIAR_SOURCE) With {
            .sPomiar = "magΣ",
            .sUnit = "mag"
        }
        Dim oNearest = New JedenPomiar(SRC_POMIAR_SOURCE) With {
            .sPomiar = "mag",
            .sUnit = "mag"
        }
        Dim dMaxMag As Double = 0
        Dim iQuakesCounter = 0
        Dim dQuakesPowerSum = 0
        Dim iZasieg As Integer = DistanceNum2Metry(GetSettingsInt(SRC_SETTING_NAME & "_distance"))

        For Each oVal As Newtonsoft.Json.Linq.JToken In oJsonArr
            Dim oJsonProp As Newtonsoft.Json.Linq.JToken = oVal.GetObject().GetNamedToken("properties")
            Dim dOdl, dMag As Double

            Dim oEpicenter As New pkar.BasicGeopos(oJsonProp.GetObject().GetNamedNumber("lat", 0),
                                                   oJsonProp.GetObject().GetNamedNumber("lon", 0),
                                                   oJsonProp.GetObject().GetNamedNumber("depth", 0))

            dOdl = oPos.DistanceTo(oEpicenter)
            dMag = oJsonProp.GetObject().GetNamedNumber("mag", 0)

            If dOdl / 1000 < iZasieg Then
                iQuakesCounter += 1
                dQuakesPowerSum += EnergyKJoulesFromMag(dMag)
                oNewSum.dCurrValue = Math.Max(oNewSum.dCurrValue, dMag)
                oNewSum.sCurrValue = oJsonProp.GetObject().GetNamedString("time", "")
                If oNewSum.sTimeStamp = "" Then oNewSum.sTimeStamp = oNewSum.sCurrValue
            End If

            Dim dOdlTmp As Double = Math.Max(0.5, dOdl / 1000)
            dOdlTmp = dOdlTmp * dOdlTmp

            If dMag / dOdlTmp > dMaxMag Then
                dMaxMag = dMag / dOdlTmp
                oNearest.oGeo = oEpicenter
                oNearest.dOdl = dOdl
                oNearest.dCurrValue = dMag
                oNearest.sTimeStamp = oJsonProp.GetObject().GetNamedString("time", "")
                oNearest.sAdres = oJsonProp.GetObject().GetNamedString("flynn_region", "")
            End If
        Next

        oNearest.sCurrValue = oNearest.dCurrValue.ToString() & " " & oNearest.sUnit
        oNearest.sOdl = Odleglosc2String(oNearest.dOdl)
        oNearest.sAddit = MakeOpisDokladnySingle(oNearest.dCurrValue)
        oNearest.sTimeStamp = oNearest.sTimeStamp.Replace("T", " ")
        oNewSum.sTimeStamp = oNewSum.sTimeStamp.Replace("T", " ")
        oNewSum.sCurrValue = oNewSum.sCurrValue.Replace("T", " ")
        oNewSum.sAddit = MakeOpisDokladnySum(dQuakesPowerSum, iQuakesCounter, oNewSum.sCurrValue, oNewSum.sTimeStamp)
        oNewSum.sCurrValue = oNewSum.dCurrValue.ToString() & " " & oNewSum.sUnit
        If oNearest.dCurrValue > 0 Then moListaPomiarow.Add(oNearest)
        If oNewSum.dCurrValue > 0 Then moListaPomiarow.Add(oNewSum)
        Return moListaPomiarow
    End Function

    ' Public Overrides Async Function GetNearest(ByVal oPos As Windows.Devices.Geolocation.BasicGeoposition) As System.Threading.Tasks.Task(Of Collection(Of JedenPomiar))
    Public Overrides Async Function GetNearestAsync(oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await WczytujDane(oPos, False)
    End Function

    '    Public Overrides Async Function GetDataFromFavSensor(ByVal sId As String, ByVal sAddit As String, ByVal bInTimer As Boolean) As System.Threading.Tasks.Task(Of Collection(Of JedenPomiar))
    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, moGpsPoint As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        Dim oPos As New pkar.BasicGeopos(sId, sAddit)
        Return Await WczytujDane(oPos, bInTimer)
    End Function

    Private Shared Function DistanceNum2Metry(iDist As Integer) As Integer
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

    Public Shared Function DistanceNum2Opis(iDist As Integer) As String
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

End Class

Imports System.Collections.ObjectModel

' około 90 kB

Public Class Source_RadioPAA
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceRadioPAA"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Państwowa Agencja Atomistyki"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://mapa.paa.gov.pl/"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "PAA"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://www.gov.pl/web/paa/sytuacja-radiacyjna"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://www.gov.pl/web/paa/sytuacja-radiacyjna"
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

    'Private Function NormalizePomiarName(sPomiar As String) As String
    '    Return "Radiation"
    'End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        Return "μSv/h"
    End Function


    ''' <summary>
    ''' połączenie razem Nearest oraz Fav, bo i tak to samo robią
    ''' </summary>
    ''' <param name="oPos">GPS telefonu, do liczenia odległości</param>
    ''' <param name="dMaxOdl">double.Max gdy nie sprawdzamy</param>
    ''' <param name="sId">"" gdy każdy sensor</param>
    ''' <returns></returns>
    Private Async Function GetPomiaryAsync(oPos As MyBasicGeoposition, dMaxOdl As Double, sId As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        moListaPomiarow = New Collection(Of JedenPomiar)

        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        Dim sPage As String = Await GetREST("")
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim iInd As Integer
        iInd = sPage.IndexOf("data_object")
        If iInd < 0 Then Return moListaPomiarow
        iInd = sPage.IndexOf("{", iInd)
        sPage = sPage.Substring(iInd)

        iInd = sPage.IndexOf(";")
        sPage = sPage.Substring(0, iInd)

        ' i mamy już JSON
        Dim oJson As Newtonsoft.Json.Linq.JObject = Nothing
        Dim bError As Boolean = False
        Dim sErr = ""

        Try
            oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage)
        Catch ex As Exception
            bError = True
            sErr = ex.Message
        End Try

        If bError Then
            If Not bInTimer Then Await DialogBoxAsync("ERROR: JSON parsing error - getting measurements (Airly)" & vbCrLf & sErr)
            Return moListaPomiarow
        End If


        For Each oItem As Newtonsoft.Json.Linq.JToken In oJson.Children
            Dim oDetails As Newtonsoft.Json.Linq.JObject = oItem.First ' .Children(0)

            Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)

            oNew.sId = oDetails.GetNamedString("kod")
            If sId <> "" AndAlso oNew.sId <> sId Then Continue For
            oNew.sPomiar = "μSv/h"

            oNew.dLat = oDetails.GetNamedNumber("loc_x")
            oNew.dLon = oDetails.GetNamedNumber("loc_y")

            oNew.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oNew.dLat, oNew.dLon))
            If oNew.dOdl / 1000 > dMaxOdl Then Continue For

            oNew.sOdl = Odleglosc2String(oNew.dOdl)

            oNew.sAdres = oDetails.GetNamedString("nazwa")
            If oDetails.GetNamedString("notka").Length > 5 Then
                oNew.sAddit = oNew.sAddit & "Notka: " & oDetails.GetNamedString("notka") & vbCrLf
            End If
            If oDetails.GetNamedString("opis").Length > 5 Then
                oNew.sAddit = oNew.sAddit & "Opis: " & oDetails.GetNamedString("notka") & vbCrLf
            End If

            oNew.dCurrValue = oDetails.GetNamedNumber("wartosc") / 1000 ' jest  nSv, przeliczamy na μSv
            oNew.sCurrValue = oNew.dCurrValue & " " & Unit4Pomiar("")

            oNew.sTimeStamp = oDetails.GetNamedString("data") & " " & oDetails.GetNamedString("godzina")

            ' alertujemy
            Try
                ' podwojna srednia dzienna
                Dim oAvgMies = oDetails.GetNamedObject("srednie_miesieczne")
                If oAvgMies IsNot Nothing Then
                    Dim oFirstSrednia As Newtonsoft.Json.Linq.JObject = oAvgMies.Children(0)
                    Dim oSrednia As Newtonsoft.Json.Linq.JToken = oFirstSrednia.Children(1)
                    Dim dAvgVal As Double
                    If Double.TryParse(oSrednia.ToString, dAvgVal) Then
                        If oNew.dCurrValue > 2 * dAvgVal Then oNew.sAlert = "!"
                    End If
                End If
            Catch ex As Exception

            End Try

            ' Za: https://www.theguardian.com/news/datablog/2011/mar/15/radiation-exposure-levels-guide
            ' średnia roczna tła: 2 mSv/rocznie 
            ' szkodliwa na pewno (clearly evident): 100 mSv/rocznie
            If oNew.dCurrValue / 1000 > 2.0 / 365 / 24 Then oNew.sAlert = "!!"
            If oNew.dCurrValue / 1000 > 100.0 / 365 / 24 Then oNew.sAlert = "!!!"
            oNew.sLimity = "!!: 2 mSv/year" & vbCrLf & "!!!: 100 mSv/year"

            moListaPomiarow.Add(oNew)
        Next


        If moListaPomiarow.Count < 1 Then
            If Not bInTimer Then Await DialogBoxAsync($"ERROR: no station in range ({SRC_SETTING_NAME})")
            Return moListaPomiarow
        End If

        Return moListaPomiarow
    End Function


    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Dim dMaxOdl As Double = 50
        Return Await GetPomiaryAsync(oPos, dMaxOdl, "", False)
    End Function

    ''' <summary>
    ''' Wykorzystuje moGPSpoint!
    ''' </summary>
    ''' <param name="sId"></param>
    ''' <param name="sAddit"></param>
    ''' <param name="bInTimer"></param>
    ''' <param name="moGpsPoint"></param>
    ''' <returns></returns>
    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oGpsPoint As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await GetPomiaryAsync(oGpsPoint, Double.MaxValue, sId, bInTimer)
    End Function
End Class

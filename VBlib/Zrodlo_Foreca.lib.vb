Imports System.Linq
Imports System.Collections.ObjectModel

Public Class Source_Foreca
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceForeca"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Foreca record"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://www.foreca.pl/World"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE_PKAR As String = "https://www.foreca.pl/Poland/Lesser-Poland-Voivodeship/Krak%C3%B3w/10-day-forecast"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "Foreca"
    Public Overrides ReadOnly Property SRC_NO_COMPARE As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://www.foreca.pl"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://www.foreca.pl"

    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await GetDataFromFavSensorAsync("", "", False, Nothing)
    End Function

    Private Sub ExtractFirstItem(sPageFragment As String, sPomiar As String)
        Dim oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
        oNew.sPomiar = sPomiar
        oNew.sUnit = "°C"

        oNew.sAdres = sPageFragment.SubstringBetween("<a href=""/", """")

        For Each oItem As JedenPomiar In moListaPomiarow
            If oItem.sAdres = oNew.sAdres Then Return ' to jest to samo, ergo: błąd na stronie widoczny 2022.08
        Next

        oNew.sCurrValue = sPageFragment.SubstringBetween("temp_c", "<").Replace("+", "").Replace("&deg;", "")
        Dim iInd As Integer = oNew.sCurrValue.IndexOf(">")
        If iInd > 0 Then oNew.sCurrValue = oNew.sCurrValue.Substring(iInd + 1)

        Try
            oNew.dCurrValue = oNew.sCurrValue
        Catch
            oNew.dCurrValue = 0
        End Try

        oNew.sCurrValue = oNew.sCurrValue & " " & oNew.sUnit

        moListaPomiarow.Add(oNew)

    End Sub

    Public Overrides Async Function GetDataFromFavSensorAsync(ByVal sId As String, ByVal sAddit As String, ByVal bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        Dim sPage As String = Await GetREST("")
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim iInd As Integer

        iInd = sPage.IndexOf("trivia-warm-small.png")
        If iInd > 0 Then ExtractFirstItem(sPage.Substring(iInd), "Tmax")

        iInd = sPage.IndexOf("trivia-cold-small.png")
        If iInd > 0 Then ExtractFirstItem(sPage.Substring(iInd), "Tmin")

        If moListaPomiarow.Count < 1 Then
            If Not bInTimer Then Await DialogBoxAsync("ERROR: data parsing error Foreca\sPage")
            Return moListaPomiarow
        End If

        Return moListaPomiarow
    End Function
End Class

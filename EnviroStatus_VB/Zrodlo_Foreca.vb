Public Class Source_Foreca
    Inherits Source_Base

    ' ułatwienie dodawania następnych
    Protected Overrides Property SRC_SETTING_NAME As String = "sourceForeca"
    Protected Overrides Property SRC_SETTING_HEADER As String = "Foreca record"
    Protected Overrides Property SRC_RESTURI_BASE As String = "https://www.foreca.pl/World"
    Protected Overrides Property SRC_RESTURI_BASE_PKAR As String = "https://www.foreca.pl/Poland/Lesser-Poland-Voivodeship/Krak%C3%B3w/10-day-forecast"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "Foreca"

    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Return Await GetDataFromFavSensor("", "", False)    ' bo tak :) 
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME, True) Then Return moListaPomiarow

        Dim sPage As String = Await GetREST("")

        ' map.addOverlay(createMarker(new GLatLng(49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
        Dim iInd As Integer
        iInd = sPage.IndexOf("trivia-warm.png")
        If iInd > 0 Then
            Try
                Dim oNew As JedenPomiar = New JedenPomiar
                oNew.sSource = SRC_POMIAR_SOURCE
                oNew.sPomiar = "Tmax"
                oNew.sUnit = "°C"

                'Public Property sCurrValue As String = "" ' etap 2: wartosc
                'Public Property dCurrValue As Double = 0

                ' Public Property sJedn As String
                Dim sTmp As String
                sTmp = sPage.Substring(iInd)
                iInd = sTmp.IndexOf("<a href")
                sTmp = sTmp.Substring(iInd + 10)
                iInd = sTmp.IndexOf("""")
                oNew.sAdres = sTmp.Substring(0, iInd)

                iInd = sTmp.IndexOf("&deg")
                sTmp = sTmp.Substring(0, iInd).TrimEnd
                iInd = sTmp.LastIndexOf(">")
                sTmp = sTmp.Substring(iInd + 1).TrimStart

                sTmp = sTmp.Replace(",", ".")
                Double.TryParse(sTmp, oNew.dCurrValue)
                If oNew.dCurrValue = 0 Then     ' jak robi wedle PL, to moze trzeba przecinka? A zera raczej nie ma, bo jest plus/minus kilkadziesiat
                    sTmp = sTmp.Replace(".", ",")
                    Double.TryParse(sTmp, oNew.dCurrValue)
                End If
                oNew.sCurrValue = sTmp & " " & oNew.sUnit

                ' <a href="/Cameroon/North-Province/Garoua">Garoua</a></p>	<p class="obs warm u_metrickmh">+39,0 &deg;C
                moListaPomiarow.Add(oNew)

            Catch ex As Exception

            End Try

        End If

        iInd = sPage.IndexOf("trivia-cold.png")
        If iInd > 0 Then
            Try
                Dim oNew As JedenPomiar = New JedenPomiar
                oNew.sSource = SRC_POMIAR_SOURCE
                oNew.sPomiar = "Tmin"
                oNew.sUnit = "°C"

                'Public Property sCurrValue As String = "" ' etap 2: wartosc
                'Public Property dCurrValue As Double = 0

                ' Public Property sJedn As String
                Dim sTmp As String
                sTmp = sPage.Substring(iInd)
                iInd = sTmp.IndexOf("<a href")
                sTmp = sTmp.Substring(iInd + 10)
                iInd = sTmp.IndexOf("""")
                oNew.sAdres = sTmp.Substring(0, iInd)

                iInd = sTmp.IndexOf("&deg")
                sTmp = sTmp.Substring(0, iInd).TrimEnd
                iInd = sTmp.LastIndexOf(">")
                sTmp = sTmp.Substring(iInd + 1).TrimStart

                sTmp = sTmp.Replace(",", ".")
                Double.TryParse(sTmp, oNew.dCurrValue)
                oNew.sCurrValue = sTmp & " " & oNew.sUnit

                moListaPomiarow.Add(oNew)
                ' <a href="/Cameroon/North-Province/Garoua">Garoua</a></p>	<p class="obs warm u_metrickmh">+39,0 &deg;C
            Catch ex As Exception

            End Try

        End If
        If moListaPomiarow.Count < 1 Then
            If Not bInTimer Then Await DialogBox("ERROR: data parsing error ra@h\sPage")
            Return moListaPomiarow
        End If

        Return moListaPomiarow

    End Function






End Class


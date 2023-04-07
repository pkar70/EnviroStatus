Public Class MainPage

    Private Shared Function CalculateWilgAbs(dTemp As Double, dWilgWzgl As Double) As Double
        ' https://klimapoint.pl/kalkulator-wilgotnosci-bezwzglednej/
        Return 216.7 * (dWilgWzgl / 100 * 6.112 * Math.Exp(17.62 * dTemp / (243.12 + dTemp)) / (273.15 + dTemp))
    End Function

    Private Shared Function ConvertHumidity(dHigroExt As Double) As String
        Dim dKubatura = GetSettingsInt("higroKubatura") / 100.0
        If dKubatura = 0 Then Return ""
        Dim iIntTemp As Integer = GetSettingsInt("higroTemp")
        If iIntTemp = 0 Then Return ""
        Dim dExtTemp As Double = -1000

        For Each oItem As JedenPomiar In App.moPomiaryAll

            If oItem.sPomiar.ToLower.IndexOf("temp") > -1 AndAlso oItem.sSource.ToLower <> "noaawind" Then
                dExtTemp = oItem.dCurrValue
                Exit For
            End If
        Next

        If dExtTemp = -1000 Then Return ""      ' nie bylo temperatury!
        Dim dWilgAbs = CalculateWilgAbs(dExtTemp, dHigroExt)
        Dim dWilgInt As Double
        ' https://klimapoint.pl/kalkulator-wilgotnosci-bezwzglednej/

        dWilgInt = 100 * (dWilgAbs / 216.7) * (273.15 + iIntTemp) / (6.112 * Math.Exp(17.62 * iIntTemp / (243.12 + iIntTemp)))
        Dim dWoda40 = -(dWilgAbs - CalculateWilgAbs(iIntTemp, 40)) * dKubatura
        Dim dWoda60 = -(dWilgAbs - CalculateWilgAbs(iIntTemp, 60)) * dKubatura
        Return GetLangString("msgWilgInt") & ": " & dWilgInt.ToString("##0") & " %" & vbLf & "ΔH₂0 40 % = " & dWoda40.ToString("####0.00;-####0.00") & " g" & vbLf & "ΔH₂0 60 % = " & dWoda60.ToString("####0.00;-####0.00") & " g" & vbLf
    End Function

    ' cofamy się o minutę, by na start na pewno była możliwość pokazania
    Private Shared moLastDetails As Date = Date.Now.AddMinutes(-1)

    Public Shared Function ShowDetails(oItem As JedenPomiar) As String
        ' zabezpieczenie przed wielokrotnym okienkiem - telefon Barbara52 pokazywał kilka okienek, może to pomoże
        If moLastDetails.AddMilliseconds(250) > Date.Now Then Return ""

        moLastDetails = Date.Now
        Dim sMsg As String = ""


        If oItem.sSource = "me" Then
            sMsg = GetLangString("resCalculated") & vbCrLf
        Else

            If Not String.IsNullOrEmpty(oItem.sId) Then
                sMsg = "Sensor from " & oItem.sSource & " (id=" & oItem.sId
            Else
                sMsg = "Data from " & oItem.sSource
            End If

            If oItem.sSource = "gios" Then sMsg = sMsg & ", " & oItem.sAddit
            If sMsg.IndexOf("(") > 0 Then sMsg &= ")"
            sMsg = sMsg & vbCrLf
            If Not String.IsNullOrEmpty(oItem.sSensorDescr) Then sMsg = sMsg & oItem.sSensorDescr & vbCrLf
            If oItem.sAdres <> "" Then sMsg = sMsg & oItem.sAdres & vbCrLf

            If Not String.IsNullOrEmpty(oItem.sOdl) Then
                sMsg = sMsg & GetLangString("resOdleglosc") & " " & oItem.sOdl & vbCrLf

                If oItem.sSource <> "DarkSky" And oItem.sSource <> "burze" Then
                    sMsg &= oItem.oGeo.FormatLink("(lat: %lat, lon: %lon")
                    If oItem.oGeo.Altitude <> 0 Then
                        sMsg = sMsg & ", " & vbCrLf
                        If oItem.sSource <> "SeismicEU" Then
                            sMsg = sMsg & GetLangString("resWysokosc") & $": {oItem.oGeo.StringAlt} m"
                        Else
                            sMsg = sMsg & GetLangString("resGlebokosc") & $": {oItem.oGeo.StringAlt} km"
                        End If
                    End If

                    sMsg = sMsg & ")" & vbCrLf
                    End If
                End If
            End If

            sMsg = sMsg & vbCrLf
        If Not String.IsNullOrEmpty(oItem.sTimeStamp) Then sMsg = sMsg & "@" & oItem.sTimeStamp & vbCrLf
        sMsg = sMsg & oItem.sPomiar & ", "

        If oItem.sSource = "SeismicEU" Then
            sMsg &= "max value: "
        Else
            If oItem.sSource = "burze" AndAlso oItem.sPomiar = GetLangString("resPomiarBurza") Then
                sMsg = sMsg & GetLangString("resBurzeCount") & ": "
            Else
                sMsg &= "value: "
            End If
        End If

        If oItem.sSource = "VisCross" AndAlso oItem.sPomiar = GetLangString("resSlonce") Then
            sMsg = sMsg & oItem.sCurrValue
        Else
            sMsg = sMsg & oItem.dCurrValue & " " & oItem.sUnit
        End If

        ' przygotowane, na razie empty (bo nie ma oSrc.GetDetails - może kiedyś przeniosę tam te szczegóły)
        For Each oSrc As Source_Base In App.gaSrc
            If oItem.sSource = oSrc.SRC_POMIAR_SOURCE Then
                Dim sDet As String = oSrc.GetDetails(oItem)
                If sDet <> "" Then sMsg = sMsg & oSrc.GetDetails(oItem) & vbCrLf
            End If
        Next


        If Not String.IsNullOrEmpty(oItem.sAddit) AndAlso oItem.sSource <> "gios" Then sMsg = sMsg & vbCrLf & oItem.sAddit
        ' dla gios, sAddit to dodatkowy id, i pokazywany jest wczesniej

        sMsg &= vbLf
        If Not String.IsNullOrEmpty(oItem.sLimity) Then sMsg = sMsg & vbCrLf & oItem.sLimity

        If oItem.sPomiar = "Humidity" Then
            Dim sTmp As String
            sTmp = ConvertHumidity(oItem.dCurrValue)
            If Not String.IsNullOrEmpty(sTmp) Then sMsg = sMsg & vbLf & sTmp
        End If

        Return sMsg

    End Function

End Class

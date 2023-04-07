
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Linq

Public Class App

    ''' <summary>
    ''' gdy jest SettingsBool("settingsDataLog") to zapisze
    ''' </summary>
    ''' <returns>True: zapisane, False: nie trzeba było bądź error</returns>
    Public Shared Function TryDataLog() As Boolean
        DumpCurrMethod()
        If Not GetSettingsBool("settingsDataLog") Then Return False

        Try
            Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(moPomiaryAll, Newtonsoft.Json.Formatting.Indented)
            Dim logFile As String = msDataLog.GetLogFileDailyWithTime("", "json")
            File.WriteAllText(logFile, sTxt)
            Return True
        Catch
        End Try

        Return False
    End Function

    Public Shared Sub ZaznaczPowtorki()
        DumpCurrMethod()

        Dim i0 = 0, loopTo As Integer = moPomiaryAll.Count() - 1

        While i0 <= loopTo
            Dim oPomiar As JedenPomiar = moPomiaryAll(i0)
            Dim i1 = 0, loopTo1 As Integer = moPomiaryAll.Count() - 1

            While i1 <= loopTo1

                If i0 <> i1 Then
                    Dim oPomiar1 As JedenPomiar = moPomiaryAll(i1)
                    ' na pozniej >=, ale na razie trzeba wychwycic czemu rosnie
                    If oPomiar1.sSource = oPomiar.sSource AndAlso oPomiar1.sPomiar = oPomiar.sPomiar AndAlso oPomiar1.dOdl > oPomiar.dOdl Then oPomiar1.bDel = True
                End If

                i1 += 1
            End While

            i0 += 1
        End While
    End Sub

    Public Shared Sub ZrobToasty(bIsAndroid As Boolean)
        DumpCurrMethod()

        ' a teraz toasty
        Dim sToastSett As String = GetSettingsString("settingsAlerts")
        Dim iInd As Integer
        iInd = sToastSett.IndexOf("(!")
        If iInd < 0 Then Return
        sToastSett = sToastSett.Substring(iInd + 1).Replace(")", "")
        ' sToastSett = !|!!|!!!
        Dim sLastToast As String = GetSettingsString("lastToast")

        ' If sToastSett.IndexOf("!") > 0 Then iToastMode = 1
        ' If sToastSett.IndexOf("!!") > 0 Then iToastMode = 2
        ' If sToastSett.IndexOf("!!!") > 0 Then iToastMode = 3
        ' If iToastMode = 0 Then Exit Function

        Dim sToastMsg = ""
        Dim sToastMemory = ""
        DumpMessage("Poprzednie alerty: " & sLastToast)

        '        Dim aLastAlerts As String() = sLastToast.Replace(vbCrLf, vbCr).Trim.Split(vbCr)
        Dim aLastAlerts As String() = sLastToast.Replace(vbCrLf, vbCr).Trim().Split(vbCr)
        Dim bCleanAir = True

        For Each oItem As JedenPomiar In moPomiaryAll.Where(Function(x As JedenPomiar) Not x.bDel)
            'If oItem.bDel Then Continue For
            If Not oItem.bCleanAir Then bCleanAir = False    ' 2021.01.28
            Dim sAlertTmp As String = oItem.sAlert
            If sAlertTmp.Length < sToastSett.Length Then sAlertTmp = ""             ' !!
            Dim sOneParam As String = oItem.sPomiar & " (" + oItem.sSource & ")"    ' PM10 (Airly)

            If If(oItem.sSource, "") Is "NOAAalert" Then
                ' dla DarkSky toast, oraz NOAAalert, ma pokazac pelniejsze info
                ' toastMemory - nie zapisujemy, bo i tak nie odczyta drugi raz tego samego
                ' tylko do wyswietlenia podaje wiecej
                Dim sTmp As String
                sTmp = oItem.sAlert & " " & oItem.sCurrValue & " (" & oItem.sSource & ")" & vbCrLf
                sToastMemory &= sTmp ' 2021.09.26: jednak włączam ta linię (była zakomentowana)
                If Not sLastToast.Contains(oItem.sCurrValue) Then sToastMsg &= sTmp
            Else
                Dim sOneParamAlert = sAlertTmp & " " & sOneParam              ' !! PM10 (Airly)
                ' (a) dokladnie to samo bylo wczesniej

                DumpMessage(" analiza aktualnego: " & sOneParamAlert)
                Dim iPoprzedniStatus = 0

                For Each sPrevAlert In aLastAlerts
                    DumpMessage("  poprzedni wpis: " & sPrevAlert)

                    If sPrevAlert.Trim() = sOneParamAlert.Trim() Then
                        iPoprzedniStatus = 1
                        DumpMessage("- byl taki sam")
                        Exit For
                    ElseIf sPrevAlert.Contains(sOneParamAlert) Then
                        iPoprzedniStatus = 2
                        DumpMessage("- byl krotszy")
                        Exit For
                    ElseIf sPrevAlert.Contains(sOneParam) Then
                        iPoprzedniStatus = 3
                        DumpMessage("- byl dluzszy")
                        Exit For
                    End If
                Next

                Select Case iPoprzedniStatus
                    Case 0, 3  ' nie bylo, bądź było mniejsze

                        If Not String.IsNullOrEmpty(sAlertTmp) Then
                            If oItem.sPomiar.StartsWith("Alert") AndAlso oItem.sSource = "DarkSky" Then
                                sToastMsg = sToastMsg & oItem.sAlert & " " + oItem.sCurrValue & " (" + oItem.sSource & ")" & vbCrLf
                            Else
                                sToastMsg = sToastMsg & sOneParamAlert & vbCrLf
                            End If

                            sToastMemory = sToastMemory & sOneParamAlert & vbCrLf
                        End If

                    Case 1  ' bylo takie samo
                        ' If na wypadek gdy błąd
                        If Not String.IsNullOrEmpty(sAlertTmp) Then sToastMemory = sToastMemory & sOneParamAlert & vbCrLf
                    Case 2  ' bylo wieksze

                        If Not String.IsNullOrEmpty(sAlertTmp) Then
                            sToastMemory = sToastMemory & sOneParamAlert & vbCrLf
                        Else
                            sToastMsg = sToastMsg & "(ok) " & sOneParam & vbCrLf
                        End If
                End Select
            End If
        Next

        DumpMessage("nowy toastmemory" & sToastMemory)
        DumpMessage("toast string" & sToastMsg)
        SetSettingsString("lastToast", sToastMemory)

        If String.IsNullOrEmpty(sToastMemory) Then
            If String.IsNullOrEmpty(sLastToast) Then Return
        End If

        If bCleanAir AndAlso Not GetSettingsBool("cleanAir") Then
            If String.IsNullOrEmpty(sToastMemory) Then
                sToastMsg = GetLangString("msgAllOk")
            Else
                sToastMsg = sToastMsg & vbCrLf & GetLangString("msgAllOk")
            End If
        End If

        SetSettingsBool("cleanAir", bCleanAir)

        If Not String.IsNullOrEmpty(sToastMsg) Then
            Dim arraj = sToastMsg.Split(ChrW(10))

            If arraj.Length > 1 AndAlso bIsAndroid Then ' dla Android: trzeba przejsc do rozwijanego toastu, wiec dwa parametry potrzebne są
                MakeToast(GetLangString("resAndroidToastTitle"), sToastMsg)   ' jednolinijkowy, lub nie Android
            Else
                MakeToast(sToastMsg)
            End If
        End If
    End Sub

    Public Shared Sub KoncowkaPokazywaniaDanych()
        DumpCurrMethod()

        'return;
        ' reszta: odtworzona, bo próbowałem jak nie działało z samym return, najwyraźniej skasowalem potem :(

        DodajPrzekroczenia()
        ZaznaczPowtorki()

        'MakeToast("po UsunPowtorki")
        DodajTempOdczuwana()
        'MakeToast("po Tapp")

        ' Cache_Save()
        moLastPomiar = Date.Now
        TryDataLog()
        'MakeToast("po datalog")
        ' UpdateTile()
        'MakeToast("po tile")
        DumpMessage("app:KoncowkaPokazywaniaDanych() end")
    End Sub

    Private Shared Sub DodajPrzekroczenia()
        DumpCurrMethod()

        For Each oItem As JedenPomiar In moPomiaryAll
            oItem.DodajPrzekroczenia()
        Next
    End Sub


    Private Const CACHE_FILENAME As String = "data_cache.json"

    Public Shared Function Cache_Load(sLocalPath As String, sRoamPath As String) As DateTimeOffset

        If sLocalPath <> "" Then sLocalPath = IO.Path.Combine(sLocalPath, CACHE_FILENAME)
        If sRoamPath <> "" Then sRoamPath = IO.Path.Combine(sRoamPath, CACHE_FILENAME)

        ' nie ma nic - sygnalizacja nieudaności
        If Not IO.File.Exists(sLocalPath) AndAlso Not IO.File.Exists(sRoamPath) Then
            Return DateTime.Now.AddYears(-100)
        End If

        ' wczytujemy domyślnie lokalny plik, ale jeśli roam jest nowszy - to jego
        If IO.File.Exists(sLocalPath) AndAlso IO.File.Exists(sRoamPath) Then
            If IO.File.GetLastWriteTime(sRoamPath) > IO.File.GetLastWriteTime(sLocalPath) Then
                sLocalPath = sRoamPath
            End If
        End If

        Dim sContent As String = IO.File.ReadAllText(sLocalPath)
        Try
            moPomiaryAll = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Collection(Of JedenPomiar))(sContent)
        Catch
        End Try

        Return IO.File.GetLastWriteTime(sLocalPath)
    End Function

    Public Shared Function GetPomiaryAllAsString() As String
        Return Newtonsoft.Json.JsonConvert.SerializeObject(moPomiaryAll, Newtonsoft.Json.Formatting.Indented)
    End Function

    Public Shared Sub Cache_Save(sLocalPath As String, sRoamPath As String)

        DumpCurrMethod()

        Dim sContent As String = GetPomiaryAllAsString()

        ' local file
        If GetSettingsBool("settingsFileCache") Then
            Try
                IO.File.WriteAllText(IO.Path.Combine(sLocalPath, CACHE_FILENAME), sContent)
            Catch ex As Exception

            End Try
        End If

        ' roaming file
        If GetSettingsBool("settingsFileCacheRoam") Then
            Try
                IO.File.WriteAllText(IO.Path.Combine(sRoamPath, CACHE_FILENAME), sContent)
            Catch ex As Exception

            End Try
        End If

    End Sub

    Public Shared moPoint As pkar.BasicGeopos = Nothing
    Public Shared moGpsPoint As pkar.BasicGeopos
    Public Shared mbComparing As Boolean = False
    Public Shared Event ZmianaDanych As ZmianaDanychEventHandler
    Public Delegate Sub ZmianaDanychEventHandler()

    Public Shared Async Function GetFavDataAsync(sFavName As String, bInTimer As Boolean, sInTiles As String) As Task
        Dim sSensors As String = GetSettingsString("fav_" & sFavName)
        If String.IsNullOrEmpty(sSensors) Then Return
        Dim sPunkt As String = GetSettingsString("favgps_" & sFavName)
        If String.IsNullOrEmpty(sPunkt) Then Return
        Dim aPunkt As String() = sPunkt.Split("|")
        'Dim dTmpA, dTmpO As Double
        'Double.TryParse(aPunkt(0), dTmpA)
        'Double.TryParse(aPunkt(1), dTmpO)
        moGpsPoint = New pkar.BasicGeopos(aPunkt(0), aPunkt(1))

        If Not mbComparing Then moPomiaryAll = New Collection(Of VBlib.JedenPomiar)()
        Dim oPomiary As Collection(Of VBlib.JedenPomiar) = Nothing
        SetSettingsString("seenUri", "")

        Dim aSensory = sSensors.Split("|"c)

        For Each sSensor As String In aSensory
            Dim aData = sSensor.Split("#")
            If bInTimer AndAlso sInTiles.IndexOf(aData(0)) < 0 Then Continue For
            oPomiary = Nothing

            For Each oZrodlo As Source_Base In gaSrc.Where(Function(x As Source_Base) aData(0) = x.SRC_POMIAR_SOURCE)
                'If aData(0) = oZrodlo.SRC_POMIAR_SOURCE Then

                If Not App.mbComparing OrElse Not oZrodlo.SRC_NO_COMPARE Then

                        If aData(0) = "DarkSky" OrElse aData(0) = "SeismicEU" Then
                            oPomiary = Await oZrodlo.GetDataFromFavSensorAsync(moGpsPoint.Latitude, moGpsPoint.Longitude, bInTimer, Nothing)
                        Else
                            oPomiary = Await oZrodlo.GetDataFromFavSensorAsync(aData(1), aData(2), bInTimer, moGpsPoint)
                        End If
                    End If
                'End If
            Next

            If oPomiary IsNot Nothing Then

                For Each oPomiar As JedenPomiar In oPomiary

                    If mbComparing Then

                        For Each oOldPomiar As JedenPomiar In moPomiaryAll

                            If oOldPomiar.sSource = oPomiar.sSource AndAlso oOldPomiar.sPomiar = oPomiar.sPomiar Then

                                If oOldPomiar.dCurrValue = CInt(oOldPomiar.dCurrValue) Then
                                    oOldPomiar.sCurrValue = oOldPomiar.dCurrValue.ToString()
                                Else
                                    oOldPomiar.sCurrValue = oOldPomiar.dCurrValue.ToString("###0.00")
                                End If

                                If oOldPomiar.dCurrValue < oPomiar.dCurrValue Then
                                    oOldPomiar.sCurrValue += " < "
                                ElseIf oOldPomiar.dCurrValue = oPomiar.dCurrValue Then
                                    oOldPomiar.sCurrValue += " = "
                                Else
                                    oOldPomiar.sCurrValue += " > "
                                End If

                                If oPomiar.dCurrValue = CInt(oPomiar.dCurrValue) Then
                                    oOldPomiar.sCurrValue += oPomiar.dCurrValue.ToString()
                                Else
                                    oOldPomiar.sCurrValue += oPomiar.dCurrValue.ToString("###0.00")
                                End If

                                Exit For
                            End If
                        Next
                    Else
                        moPomiaryAll.Add(oPomiar)
                    End If
                Next

                If oPomiary.Count() > 0 AndAlso Not bInTimer Then
                    Try
                        RaiseEvent ZmianaDanych()
                    Catch ex As Exception
                    End Try
                End If
            End If
        Next

        If Not App.mbComparing Then

            If Not sSensors.Contains("NOAAalert") Then

                For Each oZrodlo As Source_Base In gaSrc

                    If oZrodlo.SRC_POMIAR_SOURCE = "NOAAalert" Then
                        oPomiary = Await oZrodlo.GetDataFromFavSensorAsync("", "", bInTimer, Nothing)
                        Exit For
                    End If
                Next

                If oPomiary IsNot Nothing Then

                    For Each oPomiar As JedenPomiar In oPomiary
                        moPomiaryAll.Add(oPomiar)
                    Next
                End If

                If oPomiary.Count() > 0 AndAlso Not bInTimer Then
                    ' ZmianaDanych?.Invoke()
                    Try
                        RaiseEvent ZmianaDanych()
                    Catch ex As Exception
                    End Try

                End If
            End If
        End If
    End Function


    Public Shared Sub DodajTempOdczuwana()
        DumpCurrMethod()
        Dim dTemp As Double = 1000
        Dim dWilg As Double = 1000

        ' MakeToast("before loop in Tapp")
        For Each oItem As JedenPomiar In moPomiaryAll.Where(Function(x As JedenPomiar) Not x.bDel AndAlso Not String.IsNullOrEmpty(x.sPomiar))
            ' MakeToast("source: " & oItem.sSource & ", pomiar " & oItem.sPomiar)
            ' If Not oItem.bDel AndAlso Not String.IsNullOrEmpty(oItem.sPomiar) Then
            ' MakeToast("value: " & oItem.dCurrValue)
            If oItem.sPomiar.ToLower = "humidity" Then dWilg = oItem.dCurrValue
            If oItem.sPomiar.ToLower.IndexOf("tempe") = 0 Then dTemp = oItem.dCurrValue ' airly tak, ale IMGW nie (bo tam jest temp)
            'End If
        Next

        ' MakeToast("Tapp, mam dane " & dTemp & ", " & dWilg)
        ' jesli ktorejs wartosci nie ma, to sie poddaj
        If dTemp = 1000 Then Return
        If dWilg = 1000 Then Return
        Dim oNew = New JedenPomiar("me") With {
            .dOdl = 0,
            .sPomiar = GetLangString("resTempOdczuwana"),
            .sUnit = " °C",
            .sTimeStamp = Date.Now.ToString(),
            .sSensorDescr = GetLangString("resTempOdczuwana"),
            .sOdl = ""
        }

        ' http://www.bom.gov.au/info/thermal_stress/#apparent
        ' czyli Source: Norms of apparent temperature in Australia, Aust. Met. Mag., 1994, Vol 43, 1-16
        Dim dWP As Double ' water pressure, hPa
        Dim dWind As Double = 0 ' wind speed, na wysok 10 m, w m/s
        dWP = dWilg / 100 * 6.105 * Math.Exp(17.27 * dTemp / (237.7 + dTemp))
        oNew.dCurrValue = Math.Round(dTemp + 0.33 * dWP - 0.7 * dWind - 4, 2)
        ' uwaga: dla wersji z naslonecznieniem jest inaczej
        oNew.sCurrValue = oNew.dCurrValue.ToString() & " °C"

        ' wersja z naslonecznieniem:
        ' oraz kalkulator: https://planetcalc.com/2089/
        ' var e = (H/100)*6.105*Math.exp( (17.27*Ta)/(237.7+Ta) );
        ' AT.SetValue(Ta + 0.348*e - 0.7*V - 4.25);

        moPomiaryAll.Add(oNew)
    End Sub

    Public Shared Function String2SentenceCase(sInput As String) As String
        ' założenie: wchodzi UPCASE
        Dim sOut As String = ""
        Dim bFirst = True

        For i As Integer = 0 To sInput.Length - 1
            Dim sChar As Char = sInput.ElementAt(i)
            If ("ABCDEFGHIJKLMNOPQRSTUVWXYZĄĆĘŁŃÓŚŻŹ").IndexOf(sChar) < 0 Then
                sOut &= sChar
                bFirst = True
                Continue For
            End If

            If bFirst Then
                bFirst = False
            Else
                sChar = sChar.ToString.ToLower
            End If

            sOut &= sChar
        Next

        Return sOut
    End Function

    Public Shared Function ShortPrevDate(sCurrDate As String, sPrevDate As String) As String

        ' 2022.08.11
        If sCurrDate.Length < 11 Then Return sPrevDate
        If sPrevDate.Length < 11 Then Return sPrevDate

        ' 2022.08.11 "odwieczny błąd", dwa takie same porównania 
        If sCurrDate.Substring(0, 10) = sPrevDate.Substring(0, 10) Then Return sPrevDate.Substring(11, 5)
        ' miesiac/dzien
        If sCurrDate.Substring(0, 4) = sPrevDate.Substring(0, 4) Then Return sPrevDate.Substring(5, 11)
        ' calosc ale bez sekund
        Return sPrevDate.Substring(0, 16)

    End Function


    Public Shared moPomiaryAll As New ObjectModel.Collection(Of JedenPomiar)()
    Public Shared moLastPomiar As New Date(1980, 1, 1)

    Public Shared gaSrc As New List(Of Source_Base)

    Public Shared Sub CreateSourceList(bMyNotPublic As Boolean, sTemplatePath As String)
        If gaSrc.Count > 0 Then Return

        gaSrc.Add(New Source_Foreca(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_DarkSky(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_VisualCrossing(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_NoaaKindex(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_NoaaWind(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_NoaaAlert(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_SeismicPortal(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_RadioAtHome(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_RadioPAA(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_Airly(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_EEAair(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_GIOS(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_IMGWhydro(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_IMGWmeteo(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_Burze(bMyNotPublic, sTemplatePath))
        gaSrc.Add(New Source_AlergenOBASNew(bMyNotPublic, sTemplatePath))
    End Sub



End Class

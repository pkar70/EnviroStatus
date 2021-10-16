
Public Class Source_IMGWhydro
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceImgwHydro"
    Protected Overrides Property SRC_SETTING_HEADER As String = "IMGW hydro"
    Protected Overrides Property SRC_RESTURI_BASE As String = "http://monitor.pogodynka.pl/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "IMGWhyd"
    Protected Overrides Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True

    Private Sub AddPomiar(oNew As JedenPomiar)
        For Each oItem As JedenPomiar In moListaPomiarow
            If oItem.sPomiar = oNew.sPomiar Then
                ' porownanie dat

                ' porownanie odleglosci
                If oItem.dOdl > oNew.dOdl Then
                    ' moListaPomiarow.Remove(oItem)
                    oItem.bDel = True
                    ' oNew zostanie dodany po zakonczeniu petli
                Else
                    Exit Sub    ' mamy nowszy pomiar, czyli oNew nas nie interesuje
                End If
            End If
        Next
        moListaPomiarow.Add(oNew)

    End Sub

    Private Function NormalizePomiarName(sPomiar As String) As String
        Return "Hydro"
    End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        Return "cm"
    End Function


    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Dim dMaxOdl As Double = 25


        Dim oListaPomiarow As Collection(Of JedenPomiar)
        oListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceImgwHydro", True) Then Return oListaPomiarow

        Dim sPage As String = Await GetREST("api/map/?category=hydro")
        '[ {"cd":"2019-02-25T10:40:00Z","cv":187,"i":"150190340","n":"KRAKÓW-BIELANY","a":1,"s":"normal","lo":19.843333333333334,"la":50.040277777777774} ...]

        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonArray = Nothing
        Try
            oJson = Windows.Data.Json.JsonArray.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await DialogBox("ERROR: JSON parsing error - IMGWhydro/sPage")
            Return oListaPomiarow
        End If

        If oJson.Count < 1 Then Return oListaPomiarow

        Dim dMinOdl As Double = 1000000
        Dim dMinOdlAdd As Double = 1000000

        For Each oJsonSensor As Windows.Data.Json.JsonValue In oJson
            ' {"cd":"2019-02-25T10:40:00Z","cv":187,"i":"150190340","n":"KRAKÓW-BIELANY","a":1,"s":"normal","lo":19.843333333333334,"la":50.040277777777774} ...]

            Dim oTemplate As JedenPomiar = New JedenPomiar
            oTemplate.sSource = SRC_POMIAR_SOURCE
            oTemplate.sPomiar = "Hydro"
            oTemplate.sId = oJsonSensor.GetObject().GetNamedString("i")
            oTemplate.dLon = oJsonSensor.GetObject.GetNamedNumber("lo")
            oTemplate.dLat = oJsonSensor.GetObject.GetNamedNumber("la")

            oTemplate.dOdl = GPSdistanceDwa(oPos.X, oPos.Y, oTemplate.dLat, oTemplate.dLon)
            dMinOdl = Math.Min(dMinOdl, oTemplate.dOdl)
            If oTemplate.dOdl > dMaxOdl * 1000 Then Continue For

            ' jesli do dalszego sensora, to nie chcemy go - potem i tak bedzie usuwanie dalszych
            'If oTemplate.dOdl > dMinOdlAdd Then Continue For
            'dMinOdlAdd = oTemplate.dOdl

            oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl)
            oTemplate.sAdres = App.String2SentenceCase(oJsonSensor.GetObject.GetNamedString("n"))

            'oTemplate.sTimeStamp = oJsonSensor.GetObject.GetNamedString("cd")

            'Try
            '    ' bo tam moze być NULL!
            '    oTemplate.dCurrValue = oJsonSensor.GetObject.GetNamedNumber("cv")
            'Catch ex As Exception
            '    oTemplate.dCurrValue = -1
            'End Try
            'If oTemplate.dCurrValue = -1 Then Continue For

            'oTemplate.sCurrValue = oTemplate.dCurrValue & " cm"

            '' niewykorzystane: a:
            'oTemplate.sAddit = "Status: " & oJsonSensor.GetObject.GetNamedString("s")
            'Select Case oJsonSensor.GetObject.GetNamedString("s")
            '    Case "low"
            '        oTemplate.sAlert = "!"
            '    Case "high"
            '        oTemplate.sAlert = "!"
            '    Case "warning"
            '        oTemplate.sAlert = "!!"
            '    Case "alarm"    ' choc nie wiem czy taki jest status
            '        oTemplate.sAlert = "!!!"
            '    Case "unknown"
            '    Case "normal"

            'End Select
            'oTemplate.sUnit = " cm"

            oListaPomiarow.Add(oTemplate)

        Next

        If oListaPomiarow.Count < 1 Then
            Await DialogBox("ERROR: data parsing error IMGWhydro\sPage")
            Return oListaPomiarow
        End If

        ' znajdz najblizszy, reszte zrob del
        ' dMinOdlAdd to najblizszy wstawiony, ale i tak policzymy sobie
        'For Each oItem As JedenPomiar In oListaPomiarow
        '    'If dMinOdlAdd > oItem.dOdl Then
        '    '    dMinOdlAdd = oItem.dOdl
        '    '    'sMinRzeka = 'ale tu jeszcze nie ma rzeki, niestety
        '    'End If
        '    dMinOdlAdd = Math.Min(dMinOdlAdd, oItem.dOdl)
        'Next
        '' a teraz usuwamy
        'For Each oItem As JedenPomiar In oListaPomiarow
        '    If oItem.dOdl > dMinOdlAdd Then oItem.bDel = True
        'Next

        ' dodaj pomiary
        moListaPomiarow = New Collection(Of JedenPomiar)

        For Each oItem As JedenPomiar In oListaPomiarow
            If Not oItem.bDel Then moListaPomiarow.Concat(Await GetDataFromSensor(oItem, False)) ' Return Await GetDataFromSensor(oItem, False)
        Next

        If Not GetSettingsBool("sourceImgwHydroAll") Then

            ' sprawdz co jest najblizej
            dMinOdlAdd = 100000
            Dim sMinRzeka = ""
            For Each oItem As JedenPomiar In moListaPomiarow
                If dMinOdlAdd > oItem.dOdl Then
                    dMinOdlAdd = oItem.dOdl
                    sMinRzeka = oItem.sPomiar
                End If
            Next

            ' usun inne rzeki
            Dim iInd As Integer
            iInd = sMinRzeka.IndexOf(" ")
            If iInd > 0 Then sMinRzeka = sMinRzeka.Substring(0, iInd)

            For Each oItem As JedenPomiar In moListaPomiarow
                If Not oItem.sPomiar.StartsWith(sMinRzeka) Then oItem.bDel = True
            Next

        End If

        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceImgwHydro", True) Then Return moListaPomiarow

        Dim oTemplate As JedenPomiar = New JedenPomiar

        ' wczytaj dane template dla danego favname
        Dim oFile As Windows.Storage.StorageFile =
        Await App.GetDataFile(False, "IMGWhyd_" & sId & ".xml", False)
        If oFile IsNot Nothing Then
            Dim oSer As Xml.Serialization.XmlSerializer =
                New Xml.Serialization.XmlSerializer(GetType(JedenPomiar))
            Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
            oTemplate = TryCast(oSer.Deserialize(oStream), JedenPomiar)
            oStream.Dispose()   ' == fclose
        Else
            oTemplate = New JedenPomiar
        End If

        Return Await GetDataFromSensor(oTemplate, bInTimer)
    End Function

    Private Async Function GetDataFromSensor(oTemplate As JedenPomiar, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        ' dodaj pomiary:
        ' RZEKA cm, z alertami
        ' RZEKA °C
        ' RZEKA m³

        Dim sPage As String = Await GetREST("api/station/hydro/?id=" & oTemplate.sId)

        Dim bError As Boolean = False
        Dim oJsonSensor As Windows.Data.Json.JsonValue = Nothing
        Try
            oJsonSensor = Windows.Data.Json.JsonValue.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            If Not bInTimer Then Await DialogBox("ERROR: JSON parsing error - IMGWhydro/Fav/sPage")
            Return moListaPomiarow
        End If

        oTemplate.sSource = "IMGWhyd"
        oTemplate.sId = oJsonSensor.GetObject().GetNamedString("id")    ' chociaż jest w template...
        'odczytane z template
        'oTemplate.dLon = oJsonSensor.GetObject.GetNamedNumber("lo")
        'oTemplate.dLat = oJsonSensor.GetObject.GetNamedNumber("la")

        'oTemplate.dOdl = App.GPSdistanceDwa(oPos.X, oPos.Y, oTemplate.dLat, oTemplate.dLon)
        'dMinOdl = Math.Min(dMinOdl, oTemplate.dOdl)
        'If oTemplate.dOdl > dMaxOdl * 1000 Then Continue For
        'oTemplate.sOdl = oTemplate.dOdl & " m"

        Dim oJsonValStatus As Windows.Data.Json.JsonValue
        oJsonValStatus = oJsonSensor.GetObject.GetNamedValue("status")

        ' sPomiar to nazwa rzeki
        Dim iInd As Integer
        oTemplate.sPomiar = oJsonValStatus.GetObject.GetNamedString("river")
        iInd = oTemplate.sPomiar.LastIndexOf("(")
        If iInd > 0 Then oTemplate.sPomiar = oTemplate.sPomiar.Substring(0, iInd).Trim

        oTemplate.sAdres = App.String2SentenceCase(oJsonSensor.GetObject.GetNamedString("name"))


        Try        ' dodajemy pomiar w cm
            Dim oNewCm As JedenPomiar = New JedenPomiar
            oNewCm.sSource = oTemplate.sSource ' = "IMGWhyd"
            oNewCm.sId = oTemplate.sId
            oNewCm.dLon = oTemplate.dLon
            oNewCm.dLat = oTemplate.dLat
            oNewCm.dOdl = oTemplate.dOdl
            oNewCm.sOdl = oTemplate.sOdl
            oNewCm.sPomiar = oTemplate.sPomiar & " cm"
            oNewCm.sUnit = " cm"
            oNewCm.sAdres = oTemplate.sAdres
            oNewCm.sTimeStamp = oJsonValStatus.GetObject.GetNamedString("currentDate").Replace("T", " ")

            ' tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
            oNewCm.dCurrValue = oJsonValStatus.GetObject.GetNamedNumber("currentValue")
            oNewCm.sCurrValue = oNewCm.dCurrValue & " cm"

            ' niewykorzystane: a:
            oNewCm.sAddit = "Status: " & oJsonSensor.GetObject.GetNamedString("state")
            Try
                '"currentDate":"2019-02-25T12:00:00Z",
                '"previousDate":"2019-02-25T11:50:00Z"
                oNewCm.sAddit = oNewCm.sAddit & vbCrLf & "Poprzednio " & oJsonValStatus.GetObject.GetNamedNumber("previousValue")
                'Dim iTmp As Integer
                Dim sPrevDate As String = oJsonValStatus.GetObject.GetNamedString("previousDate").Replace("T", " ")
                oNewCm.sAddit = oNewCm.sAddit & " @ " & App.ShortPrevDate(oNewCm.sTimeStamp, sPrevDate)
            Catch ex As Exception
                ' jakby było gdzies NULL
            End Try

            'Dim sTmp As String
            'Dim iInd As Integer
            'sTmp = oJsonValStatus.GetObject.GetNamedString("river")
            'iInd = sTmp.LastIndexOf("(")
            'If iInd > 0 Then sTmp = sTmp.Substring(0, iInd).Trim
            'oTemplate.sSensorDescr = sTmp & " (" & oJsonValStatus.GetObject.GetNamedNumber("riverCourseKm") & " km)"

            ' z Try, jakby ktores bylo null
            Dim dAlarm, dWarn, dHigh, dLow As Double

            Try
                dAlarm = oJsonValStatus.GetObject.GetNamedNumber("alarmValue", 0)
            Catch ex As Exception
                dAlarm = 0
            End Try
            Try
                dWarn = oJsonValStatus.GetObject.GetNamedNumber("warningValue", 0)
            Catch ex As Exception
                dWarn = 0
            End Try
            Try
                dHigh = oJsonValStatus.GetObject.GetNamedNumber("highValue", 0)
            Catch ex As Exception
                dHigh = 0
            End Try
            Try
                dLow = oJsonValStatus.GetObject.GetNamedNumber("lowValue", 0)
            Catch ex As Exception
                dLow = 0
            End Try

            Dim sLimity As String = ""
            If dAlarm > 0 Then sLimity = sLimity & "Alarm: " & dAlarm & " cm" & vbCrLf
            If dWarn > 0 Then sLimity = sLimity & "Warn: " & dWarn & " cm" & vbCrLf
            If dHigh > 0 Then sLimity = sLimity & "High: " & dHigh & " cm" & vbCrLf
            If dLow > 0 Then sLimity = sLimity & "Low: " & dLow & " cm" & vbCrLf

            oNewCm.sLimity = sLimity
            If oNewCm.dCurrValue <= dLow OrElse oNewCm.dCurrValue >= dHigh Then oNewCm.sAlert = "!"
            If oNewCm.dCurrValue >= dWarn Then oNewCm.sAlert = "!!"
            If oNewCm.dCurrValue >= dAlarm Then oNewCm.sAlert = "!!!"

            moListaPomiarow.Add(oNewCm)

        Catch ex As Exception

        End Try

        ' moze jest temperatura wody?
        Try        ' dodajemy pomiar w °C
            Dim oNew As JedenPomiar = New JedenPomiar
            oNew.sSource = oTemplate.sSource ' = "IMGWhyd"
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sUnit = " °C"
            oNew.sPomiar = oTemplate.sPomiar & oNew.sUnit
            oNew.sAdres = oTemplate.sAdres

            Dim oJsonArr As Windows.Data.Json.JsonArray
            oJsonArr = oJsonSensor.GetObject.GetNamedArray("waterTemperatureAutoRecords")

            ' dwa ostatnie Value:
            ' {"date":"2019-02-25T12:00:00Z","value":3.10,"dreId":1099,"operationId":"150190340_B00102A","parameterId":"B00102A","versionId":-1,"id":2603922134800}
            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Windows.Data.Json.JsonValue
                oJsonVal = oJsonArr.Item(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                ' tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                oNew.dCurrValue = oJsonVal.GetObject.GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue & " °C"

                Try
                    oJsonVal = oJsonArr.Item(oJsonArr.Count - 2)
                    oNew.sAddit = "Poprzednio " & oJsonVal.GetObject.GetNamedNumber("value") & oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch ex As Exception

                End Try
                moListaPomiarow.Add(oNew)
            End If

        Catch ex As Exception

        End Try

        ' moze jest przeplyw wody?
        Try        ' dodajemy pomiar w °C
            Dim oNew As JedenPomiar = New JedenPomiar
            oNew.sSource = oTemplate.sSource ' = "IMGWhyd"
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sUnit = " m³/s"
            oNew.sPomiar = oTemplate.sPomiar & oNew.sUnit
            oNew.sAdres = oTemplate.sAdres

            Dim oJsonArr As Windows.Data.Json.JsonArray
            oJsonArr = oJsonSensor.GetObject.GetNamedArray("dischargeRecords")

            ' dwa ostatnie Value:
            ' {"date":"2019-02-23T08:00:00Z","value":0.35,"dreId":1116,"operationId":"Przepływ operacyjny","parameterId":"B00050W","versionId":-1,"id":2601481083100}
            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Windows.Data.Json.JsonValue
                oJsonVal = oJsonArr.Item(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                ' tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                oNew.dCurrValue = oJsonVal.GetObject.GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit

                Try
                    oJsonVal = oJsonArr.Item(oJsonArr.Count - 2)
                    oNew.sAddit = "Poprzednio " & oJsonVal.GetObject.GetNamedNumber("value") & oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch ex As Exception

                End Try

                ' limity
                ' z Try, jakby ktores bylo null
                Dim dAlarmLow, dAlarmHigh, dHigh, dLow, dAvgRok As Double

                Try
                    dAlarmHigh = oJsonSensor.GetObject.GetNamedNumber("highestHighDischargeValue", 0)
                Catch ex As Exception
                    dAlarmHigh = 0
                End Try
                Try
                    dAlarmLow = oJsonSensor.GetObject.GetNamedNumber("lowestLowDischargeValue", 0)
                Catch ex As Exception
                    dAlarmLow = 0
                End Try

                Try
                    dAvgRok = oJsonSensor.GetObject.GetNamedNumber("mediumOfYearMediumsDischargeValue", 0)
                Catch ex As Exception
                    dAvgRok = 0
                End Try
                Try
                    dHigh = oJsonSensor.GetObject.GetNamedNumber("highDischargeValue", 0)
                Catch ex As Exception
                    dHigh = 0
                End Try
                Try
                    dLow = oJsonSensor.GetObject.GetNamedNumber("lowDischargeValue", 0)
                Catch ex As Exception
                    dLow = 0
                End Try

                Dim sLimity As String = ""
                If dAlarmHigh > 0 Then sLimity = sLimity & "Najwyższy: " & dAlarmHigh & " m³/s" & vbCrLf
                If dHigh > 0 Then sLimity = sLimity & "Wysoki: " & dHigh & " m³/s" & vbCrLf
                If dAvgRok > 0 Then sLimity = sLimity & "Średni roczny: " & dAvgRok & " m³/s" & vbCrLf
                If dLow > 0 Then sLimity = sLimity & "Niski: " & dLow & " m³/s" & vbCrLf
                If dAlarmLow > 0 Then sLimity = sLimity & "Najniższy: " & dAlarmLow & " m³/s" & vbCrLf


                oNew.sLimity = sLimity
                If dLow > 0 AndAlso oNew.dCurrValue <= dLow Then oNew.sAlert = "!"
                If dHigh > 0 AndAlso oNew.dCurrValue >= dHigh Then oNew.sAlert = "!"
                If dAlarmLow > 0 AndAlso oNew.dCurrValue <= dAlarmLow Then oNew.sAlert = "!!"
                If dAlarmHigh > 0 AndAlso oNew.dCurrValue >= dAlarmHigh Then oNew.sAlert = "!!"

                moListaPomiarow.Add(oNew)

            Else
                ' brak rekordów
            End If


        Catch ex As Exception

        End Try


        Return moListaPomiarow
    End Function

    Public Overrides Sub ConfigCreate(oStack As StackPanel)
        MyBase.ConfigCreate(oStack)

        Dim oTS As ToggleSwitch = New ToggleSwitch
        oTS.Name = "uiConfig_ImgwHydroAll"
        oTS.IsOn = GetSettingsBool("sourceImgwHydroAll")
        oTS.OnContent = GetLangString("resImgwHydroAllON")
        oTS.OffContent = GetLangString("resImgwHydroAllOFF")
        oStack.Children.Add(oTS)

    End Sub

    Public Overrides Sub ConfigRead(oStack As StackPanel)
        MyBase.ConfigRead(oStack)

        For Each oItem As UIElement In oStack.Children
            Dim oTS As ToggleSwitch
            oTS = TryCast(oItem, ToggleSwitch)
            If oTS IsNot Nothing Then
                If oTS.Name = "uiConfig_ImgwHydroAll" Then SetSettingsBool("sourceImgwHydroAll", oTS.IsOn)
            End If
        Next
    End Sub


End Class

Public Class Source_GIOS
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceGIOS"
    Protected Overrides Property SRC_SETTING_HEADER As String = "GIOŚ"
    Protected Overrides Property SRC_RESTURI_BASE As String = "http://api.gios.gov.pl/pjp-api/rest/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "gios"
    Protected Overrides Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True

    ' API: http://powietrze.gios.gov.pl/pjp/content/api
    ' LIMIT: 2x na godzinę

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

        Select Case sPomiar
            Case "C6H6"
                Return "C₆H₆"
            Case "SO2"
                Return "SO₂"
            Case "NO2"
                Return "NO₂"
            Case "O3"
                Return "O₃"
            Case "PM10"
                Return "PM₁₀"
            Case "PM1"
                Return "PM₁"
            Case "PM2.5"
                Return "PM₂₅"
        End Select
        Return sPomiar
    End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        Return " μg/m³"
    End Function


    Private Async Function GetPomiary(oTemplate As JedenPomiar, bInTimer As Boolean) As Task
        ' do moListaPomiarow dodaj wszystkie pomiary robione przez oTemplate.sId

        Dim sCmd As String
        sCmd = "station/sensors/" & oTemplate.sId
        Dim sPage As String = Await GetREST(sCmd)

        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonArray = Nothing
        Try
            oJson = Windows.Data.Json.JsonArray.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            If Not bInTimer Then Await DialogBox("ERROR: JSON parsing error - gios/GetPomiary")
            Exit Function
        End If

        Try

            For Each oJsonMeasurement As Windows.Data.Json.JsonValue In oJson

                Dim oNew As JedenPomiar = New JedenPomiar With {
                .sSource = oTemplate.sSource,
                .sId = oTemplate.sId,
                .dLon = oTemplate.dLon,
                .dLat = oTemplate.dLat,
                .dWysok = oTemplate.dWysok,
                .dOdl = oTemplate.dOdl,
                .sOdl = Odleglosc2String(oTemplate.dOdl),
                .sSensorDescr = oTemplate.sSensorDescr,
                .sAdres = oTemplate.sAdres
                }
                ' .sTimeStamp = oTemplate.sTimeStamp

                oNew.sAddit = oJsonMeasurement.GetObject().GetNamedNumber("id")

                Dim oJsonVal As Windows.Data.Json.JsonValue
                oJsonVal = oJsonMeasurement.GetObject().GetNamedValue("param")

                oNew.sPomiar = oJsonVal.GetObject().GetNamedString("paramCode")
                AddPomiar(oNew)
            Next
        Catch ex As Exception
            ' w razie bledu - nic
        End Try


        '  [{
        '    "id": 92,
        '    "stationId": 14,
        '    "param": {
        '        "paramName": "pył zawieszony PM10",
        '        "paramFormula": "PM10",
        '        "paramCode": "PM10",
        '        "idParam": 3
        '    }
        '}, ...

    End Function

    Private Async Function GetWartosci() As Task
        Try

            For Each oItem As JedenPomiar In moListaPomiarow
                If oItem.bDel Then Continue For

                Dim sCmd As String
                sCmd = "data/getData/" & oItem.sAddit
                Dim sPage As String = Await GetREST(sCmd)

                Dim bError As Boolean = False
                Dim oJson As Windows.Data.Json.JsonValue = Nothing
                Try
                    oJson = Windows.Data.Json.JsonValue.Parse(sPage)
                Catch ex As Exception
                    bError = True
                End Try
                If bError Then
                    Await DialogBox("ERROR: JSON parsing error - GIOS/GetWartosci")
                    Return
                End If

                Dim oJsonArr As Windows.Data.Json.JsonArray
                oJsonArr = oJson.GetObject().GetNamedArray("values")

                For Each oJsonMeasurement As Windows.Data.Json.JsonValue In oJsonArr
                    oItem.sTimeStamp = oJsonMeasurement.GetObject().GetNamedString("date")
                    oItem.sCurrValue = ""
                    Try
                        Dim oVal As Windows.Data.Json.JsonValue
                        oVal = oJsonMeasurement.GetObject().GetNamedValue("value")
                        If oVal.ValueType <> Windows.Data.Json.JsonValueType.Null Then
                            oItem.sCurrValue = oJsonMeasurement.GetObject().GetNamedNumber("value")
                        End If
                    Catch ex As Exception
                        ' bo moze byc NULL :)
                    End Try
                    If oItem.sCurrValue <> "" Then
                        oItem.dCurrValue = oItem.sCurrValue
                        oItem.sCurrValue = oItem.dCurrValue
                        oItem.sUnit = Unit4Pomiar(oItem.sPomiar)
                        If oItem.sCurrValue.Length > 5 Then oItem.sCurrValue = oItem.sCurrValue.Substring(0, 5)
                        oItem.sCurrValue = oItem.sCurrValue & oItem.sUnit
                        Exit For
                    End If
                Next
                If oItem.sCurrValue = "" Then
                    oItem.bDel = True
                    oItem.dCurrValue = 0
                Else
                    oItem.sPomiar = NormalizePomiarName(oItem.sPomiar)
                End If
            Next
        Catch ex As Exception
            'w razie bledu - nic. 
        End Try
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        ' ale w efekcie jest kilka GIOSów jednego parametru
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceGIOS", True) Then Return moListaPomiarow

        Dim oTemplate As JedenPomiar = New JedenPomiar

        ' wczytaj dane template dla danego favname
        Dim oFile As Windows.Storage.StorageFile =
        Await App.GetDataFile(False, "gios_" & sId & ".xml", False)
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
        'oTemplate.dLon = oJsonObj.GetNamedstring("gegrLon")
        'oTemplate.dLat = oJsonObj.GetNamedstring("gegrLat")
        'oTemplate.dWysok = 0    ' brak danych
        'oTemplate.dOdl = App.GPSdistanceDwa(oPos.X, oPos.Y,
        '                                    oTemplate.dLat, oTemplate.dLon)
        'oTemplate.sOdl = oTemplate.dOdl & " m"
        'oTemplate.sSensorDescr = oJsonObj.GetNamedString("stationName")
        'oTemplate.sAdres = oJsonObj.GetNamedString("addressStreet")


        Await GetPomiary(oTemplate, bInTimer)

        ' teraz odczytaj wartosci!
        Await GetWartosci()

        Return moListaPomiarow

    End Function

    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Dim dMaxOdl As Double = 10

        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceGIOS", True) Then Return moListaPomiarow

        Dim sCmd As String
        sCmd = "station/findAll"
        Dim sPage As String = Await GetREST(sCmd)


        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonArray = Nothing
        Try
            oJson = Windows.Data.Json.JsonArray.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await DialogBox("ERROR: JSON parsing error - GIOS\sPage")
            Return moListaPomiarow
        End If

        Try

            If oJson.Count = 0 Then Return moListaPomiarow     ' brak bliskich?


            ' przeiteruj, tworząc JedenPomiar dla kazdego pomiaru i kazdej lokalizacji

            For Each oJsonSensor As Windows.Data.Json.JsonValue In oJson
                Dim oJsonObj As Windows.Data.Json.JsonObject
                oJsonObj = oJsonSensor.GetObject()

                Dim oTemplate As JedenPomiar = New JedenPomiar
                oTemplate.sSource = SRC_POMIAR_SOURCE
                oTemplate.sId = oJsonObj.GetNamedNumber("id")

                oTemplate.dLon = oJsonObj.GetNamedString("gegrLon")
                oTemplate.dLat = oJsonObj.GetNamedString("gegrLat")

                oTemplate.dWysok = 0    ' brak danych

                oTemplate.dOdl = GPSdistanceDwa(oPos.X, oPos.Y,
                                                oTemplate.dLat, oTemplate.dLon)

                If oTemplate.dOdl / 1000 < dMaxOdl Then
                    ' teraz cos, co chce dodac

                    oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl)

                    oTemplate.sSensorDescr = oJsonObj.GetNamedString("stationName")

                    oTemplate.sAdres = oJsonObj.GetNamedString("addressStreet")
                    If oTemplate.sAdres = "" Then
                        Try
                            Dim oJsonAdres As Windows.Data.Json.JsonObject
                            oJsonAdres = oJsonObj.GetNamedValue("city").GetObject()
                            oTemplate.sAdres = oJsonAdres.GetNamedString("name")
                            Dim oJsonComm As Windows.Data.Json.JsonObject
                            oJsonComm = oJsonAdres.GetNamedValue("commune").GetObject()
                            oTemplate.sAdres = oTemplate.sAdres & vbCrLf & "("
                            If oJsonComm.GetNamedString("communeName") <> "" Then
                                oTemplate.sAdres = oTemplate.sAdres & "gmina " & oJsonComm.GetNamedString("communeName") & vbCrLf
                            End If
                            If oJsonComm.GetNamedString("districtName") <> "" Then
                                oTemplate.sAdres = oTemplate.sAdres & "powiat " & oJsonComm.GetNamedString("districtName") & vbCrLf
                            End If
                            If oJsonComm.GetNamedString("provinceName") <> "" Then
                                oTemplate.sAdres = oTemplate.sAdres & oJsonComm.GetNamedString("provinceName") & vbCrLf
                            End If
                        Catch ex As Exception
                        End Try


                    End If

                    Await GetPomiary(oTemplate, False)
                End If
            Next

            ' teraz odczytaj wartosci!
            Await GetWartosci()
        Catch ex As Exception

        End Try

        Return moListaPomiarow
        ' {""id"":14,""stationName"":""Działoszyn"",""gegrLat"":""50.972167"",""gegrLon"":""14.941319"",""city"":{""id"":192,""name"":""Działoszyn"",""commune"":{""communeName"":""Bogatynia"",""districtName"":""zgorzelecki"",""provinceName"":""DOLNOŚLĄSKIE""}},""addressStreet"":null},
        ' {""id"":400,""stationName"":""Kraków, Aleja Krasińskiego"",""gegrLat"":""50.057678"",""gegrLon"":""19.926189"",""city"":{""id"":415,""name"":""Kraków"",""commune"":{""communeName"":""Kraków"",""districtName"":""Kraków"",""provinceName"":""MAŁOPOLSKIE""}},""addressStreet"":""al. Krasińskiego""}
    End Function



End Class

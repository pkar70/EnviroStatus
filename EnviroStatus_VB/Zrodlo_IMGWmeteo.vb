'2019.10.30 uwzględniona inna postac oJsonSensor jako "null" 

Public Class Source_IMGWmeteo
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceImgwMeteo"
    Protected Overrides Property SRC_SETTING_HEADER As String = "IMGW meteo"
    Protected Overrides Property SRC_RESTURI_BASE As String = "http://monitor.pogodynka.pl/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "IMGWmet"

    Public Overrides Sub ReadResStrings()
        SetSettingsString("resPomiarWind", GetLangString("resPomiarWind"))
        SetSettingsString("resPomiarOpad", GetLangString("resPomiarOpad"))
    End Sub


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

    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Dim dMaxOdl As Double = 10

        Dim oListaPomiarow As Collection(Of JedenPomiar)
        oListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceImgwMeteo", True) Then Return oListaPomiarow

        Dim sPage As String = Await GetREST("api/map/?category=meteo")
        '[ {"pd":"2019-02-25T10:00:00Z","pv":0.0,"i":"250180590","n":"RYBNIK-STODOŁY","a":10,"s":"no-precip","lo":18.483055555555556,"la":50.154444444444444} ...]

        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonArray = Nothing
        Try
            oJson = Windows.Data.Json.JsonArray.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await DialogBox("ERROR: JSON parsing error - IMGWmeteo/sPage")
            Return oListaPomiarow
        End If

        If oJson.Count < 1 Then Return oListaPomiarow

        Dim dMinOdl As Double = 1000000
        Dim dMinOdlAdd As Double = 1000000

        For Each oJsonSensor As Windows.Data.Json.JsonValue In oJson
            ' {"cd":"2019-02-25T10:40:00Z","cv":187,"i":"150190340","n":"KRAKÓW-BIELANY","a":1,"s":"normal","lo":19.843333333333334,"la":50.040277777777774} ...]

            Dim oTemplate As JedenPomiar = New JedenPomiar
            oTemplate.sSource = "IMGWmet"
            oTemplate.sPomiar = "Meteo"
            oTemplate.sId = oJsonSensor.GetObject().GetNamedString("i")
            oTemplate.dLon = oJsonSensor.GetObject.GetNamedNumber("lo")
            oTemplate.dLat = oJsonSensor.GetObject.GetNamedNumber("la")

            oTemplate.dOdl = GPSdistanceDwa(oPos.X, oPos.Y, oTemplate.dLat, oTemplate.dLon)
            dMinOdl = Math.Min(dMinOdl, oTemplate.dOdl)
            If oTemplate.dOdl > dMaxOdl * 1000 Then Continue For

            ' jesli do dalszego sensora, to nie chcemy go - potem i tak bedzie usuwanie dalszych
            If oTemplate.dOdl > dMinOdlAdd Then Continue For
            dMinOdlAdd = oTemplate.dOdl

            oTemplate.sOdl = Odleglosc2String(oTemplate.dOdl)
            oTemplate.sAdres = App.String2SentenceCase(oJsonSensor.GetObject.GetNamedString("n"))

            oListaPomiarow.Add(oTemplate)

        Next

        If oListaPomiarow.Count < 1 Then
            Await DialogBox("ERROR: data parsing error IMGWmeteo\sPage\0")
            Return oListaPomiarow
        End If

        ' znajdz najblizszy, reszte zrob del
        ' dMinOdlAdd to najblizszy wstawiony, ale i tak policzymy sobie
        dMinOdlAdd = 100000
        For Each oItem As JedenPomiar In oListaPomiarow
            dMinOdlAdd = Math.Min(dMinOdlAdd, oItem.dOdl)
        Next
        ' a teraz usuwamy
        For Each oItem As JedenPomiar In oListaPomiarow
            If oItem.dOdl > dMinOdlAdd Then oItem.bDel = True
        Next

        ' dodaj pomiary
        moListaPomiarow = New Collection(Of JedenPomiar)
        For Each oItem As JedenPomiar In oListaPomiarow
            If Not oItem.bDel Then Return Await GetDataFromSensor(oItem, False)
        Next

        Return oListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceImgwMeteo", True) Then Return moListaPomiarow

        Dim oTemplate As JedenPomiar = New JedenPomiar

        ' wczytaj dane template dla danego favname
        Dim oFile As Windows.Storage.StorageFile =
        Await App.GetDataFile(False, "IMGWmeteo_" & sId & ".xml", False)
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
        ' opady / precip (tenMinutesPrecipRecords LUB hourlyPrecipRecords) ' {"date":"2019-02-25T10:00:00Z","value":0.0,"dreId":9234,"operationId":"SUM10MIN_OPAD_TELEMETRYCZNY","parameterId":"B00608A","versionId":-1,"id":2603646704200} , wedle sourceImgwMeteo10min
        ' temp (temperatureAutoRecords)         {"date":"2019-02-23T10:00:00Z","value":-1.36,"dreId":9234,"operationId":"250190470_B00302A","parameterId":"B00302A","versionId":-1,"id":2601427195700}
        ' szybk wiatru (windVelocityTelRecords) {"date":"2019-02-18T10:00:00Z","value":0.30}
        ' IF szybk>0 kier wiatru (windDirectionTelRecords) {"date":"2019-02-18T10:00:00Z","value":27.0}

        Dim sPage As String = Await GetREST("api/station/meteo/?id=" & oTemplate.sId)

        Dim bError As Boolean = False
        Dim oJsonSensor As Windows.Data.Json.JsonValue = Nothing
        Try
            oJsonSensor = Windows.Data.Json.JsonValue.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError OrElse oJsonSensor Is Nothing OrElse oJsonSensor.ToString = "null" Then
            If Not bInTimer Then Await DialogBox("ERROR: JSON parsing error - IMGWmeteo/Fav/sPage")
            Return moListaPomiarow
        End If


        oTemplate.sSource = "IMGWmet"
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

        oTemplate.sAdres = App.String2SentenceCase(oJsonSensor.GetObject.GetNamedString("name"))

        ' opady / precip (tenMinutesPrecipRecords LUB hourlyPrecipRecords) ' {"date":"2019-02-25T10:00:00Z","value":0.0,"dreId":9234,"operationId":"SUM10MIN_OPAD_TELEMETRYCZNY","parameterId":"B00608A","versionId":-1,"id":2603646704200} , wedle sourceImgwMeteo10min
        Try        ' dodajemy opad
            Dim oNew As JedenPomiar = New JedenPomiar
            oNew.sSource = oTemplate.sSource ' = "IMGWmet"
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sPomiar = GetSettingsString("resPomiarOpad") ' GetLangString("resPomiarOpad")
            oNew.sUnit = " cm"
            oNew.sAdres = oTemplate.sAdres

            Dim oJsonArr As Windows.Data.Json.JsonArray
            If GetSettingsBool("sourceImgwMeteo10min", True) Then
                oJsonArr = oJsonSensor.GetObject.GetNamedArray("tenMinutesPrecipRecords")
            Else
                oJsonArr = oJsonSensor.GetObject.GetNamedArray("hourlyPrecipRecords")
            End If

            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Windows.Data.Json.JsonValue
                oJsonVal = oJsonArr.Item(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                ' tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                oNew.dCurrValue = oJsonVal.GetObject.GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit
                If oNew.dCurrValue > 0 Then oNew.sAlert = "!"

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

        ' temp (temperatureAutoRecords)         {"date":"2019-02-23T10:00:00Z","value":-1.36,"dreId":9234,"operationId":"250190470_B00302A","parameterId":"B00302A","versionId":-1,"id":2601427195700}
        Try
            Dim oNew As JedenPomiar = New JedenPomiar
            oNew.sSource = oTemplate.sSource ' = "IMGWmet"
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sPomiar = "Temp"
            oNew.sUnit = " °C"
            oNew.sAdres = oTemplate.sAdres

            Dim oJsonArr As Windows.Data.Json.JsonArray
            oJsonArr = oJsonSensor.GetObject.GetNamedArray("temperatureAutoRecords")

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
                moListaPomiarow.Add(oNew)
            End If

        Catch ex As Exception

        End Try

        ' szybk wiatru (windVelocityTelRecords) {"date":"2019-02-18T10:00:00Z","value":0.30}
        Try        ' dodajemy opad
            Dim oNew As JedenPomiar = New JedenPomiar
            oNew.sSource = oTemplate.sSource ' = "IMGWmet"
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sPomiar = GetSettingsString("resPomiarWind") ' GetLangString("resPomiarWind")
            oNew.sUnit = " m/s"
            oNew.sAdres = oTemplate.sAdres

            Dim oJsonArr As Windows.Data.Json.JsonArray
            oJsonArr = oJsonSensor.GetObject.GetNamedArray("windVelocityTelRecords")

            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Windows.Data.Json.JsonValue
                oJsonVal = oJsonArr.Item(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                ' tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                oNew.dCurrValue = oJsonVal.GetObject.GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit

                oNew.sAddit = "= " & oNew.dCurrValue * 3.6 & " km/h"
                Try
                    oJsonVal = oJsonArr.Item(oJsonArr.Count - 2)
                    oNew.sAddit = oNew.sAddit & vbCrLf & "Poprzednio " & oJsonVal.GetObject.GetNamedNumber("value") & oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch ex As Exception

                End Try

                oJsonArr = oJsonSensor.GetObject.GetNamedArray("windDirectionTelRecords")
                If oJsonArr.Count > 2 Then
                    oJsonVal = oJsonArr.Item(oJsonArr.Count - 1)
                    oNew.sAddit = oNew.sAddit & vbCrLf & "Kierunek: " & oJsonVal.GetObject.GetNamedNumber("value") & "°"
                End If

                moListaPomiarow.Add(oNew)
            End If

        Catch ex As Exception

        End Try

        Try        ' dodajemy opad
            Dim oNew As JedenPomiar = New JedenPomiar
            oNew.sSource = oTemplate.sSource ' = "IMGWmet"
            oNew.sId = oTemplate.sId
            oNew.dLon = oTemplate.dLon
            oNew.dLat = oTemplate.dLat
            oNew.dOdl = oTemplate.dOdl
            oNew.sOdl = oTemplate.sOdl
            oNew.sPomiar = GetSettingsString("resPomiarWind") & " max" ' GetLangString("resPomiarWind")
            oNew.sUnit = " m/s"
            oNew.sAdres = oTemplate.sAdres

            Dim oJsonArr As Windows.Data.Json.JsonArray
            oJsonArr = oJsonSensor.GetObject.GetNamedArray("windMaxVelocityRecords")

            If oJsonArr.Count > 2 Then
                Dim oJsonVal As Windows.Data.Json.JsonValue
                oJsonVal = oJsonArr.Item(oJsonArr.Count - 1)
                oNew.sTimeStamp = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                ' tam moze być NULL! ale wtedy Try go obejmie, i pojdzie do nastepnego pomiaru
                oNew.dCurrValue = oJsonVal.GetObject.GetNamedNumber("value")
                oNew.sCurrValue = oNew.dCurrValue & oNew.sUnit

                oNew.sAddit = "= " & oNew.dCurrValue * 3.6 & " km/h"
                Try
                    oJsonVal = oJsonArr.Item(oJsonArr.Count - 2)
                    oNew.sAddit = oNew.sAddit & vbCrLf & "Poprzednio " & oJsonVal.GetObject.GetNamedNumber("value") & oNew.sUnit
                    Dim sPrevDate As String = oJsonVal.GetObject.GetNamedString("date").Replace("T", " ")
                    oNew.sAddit = oNew.sAddit & " @ " & App.ShortPrevDate(oNew.sTimeStamp, sPrevDate)
                Catch ex As Exception

                End Try

                'oJsonArr = oJsonSensor.GetObject.GetNamedArray("windDirectionTelRecords")
                'If oJsonArr.Count > 2 Then
                '    oJsonVal = oJsonArr.Item(oJsonArr.Count - 1)
                '    oNew.sAddit = oNew.sAddit & vbCrLf & "Kierunek: " & oJsonVal.GetObject.GetNamedNumber("value") & "°"
                'End If

                moListaPomiarow.Add(oNew)
            End If

        Catch ex As Exception

        End Try



        Return moListaPomiarow
    End Function




    Public Overrides Sub ConfigCreate(oStack As StackPanel)
        MyBase.ConfigCreate(oStack)

        Dim oTS As ToggleSwitch = New ToggleSwitch

        oTS = New ToggleSwitch
        oTS.Name = "uiConfig_ImgwMeteo10MIN"
        oTS.IsOn = GetSettingsBool("sourceImgwMeteo10min", True)
        oTS.OnContent = GetLangString("resImgwMeteo10minON")
        oTS.OffContent = GetLangString("resImgwMeteo10minOFF")
        oStack.Children.Add(oTS)

        'oTS = New ToggleSwitch
        'oTS.Name = "uiConfig_ImgwMeteoKier"
        'oTS.IsOn = GetSettingsBool("sourceImgwMeteoKier", True)
        'oTS.OnContent = GetLangString("uiImgwMeteo10minON")
        'oTS.OffContent = GetLangString("uiImgwMeteo10minOFF")
        'oStack.Children.Add(oTS)

    End Sub

    Public Overrides Sub ConfigRead(oStack As StackPanel)
        MyBase.ConfigRead(oStack)

        For Each oItem As UIElement In oStack.Children
            Dim oTS As ToggleSwitch
            oTS = TryCast(oItem, ToggleSwitch)
            If oTS IsNot Nothing Then
                ' If oTS.Name = "uiConfig_ImgwMeteo" Then SetSettingsBool("sourceImgwMeteo", oTS.IsOn)
                If oTS.Name = "uiConfig_ImgwMeteo10MIN" Then SetSettingsBool("sourceImgwMeteo10min", oTS.IsOn)
            End If
        Next
    End Sub

End Class

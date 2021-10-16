Public Class Source_Airly
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceAirly"
    Protected Overrides Property SRC_SETTING_HEADER As String = "Airly"
    Protected Overrides Property SRC_RESTURI_BASE As String = "https://airapi.airly.eu/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "airly"
    Protected Overrides Property SRC_HAS_TEMPLATES As Boolean = True
    Protected Overrides Property SRC_HAS_KEY As Boolean = True
    Protected Overrides Property SRC_KEY_LOGIN_LINK As String = "https://developer.airly.eu/login"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True

    ''https://airapi.airly.eu/v2/installations/nearest?lat=50.062006&lng=19.940984&maxDistanceKM=5&maxResults=3'

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
        If sPomiar = "PM10" Then Return "PM₁₀"
        If sPomiar = "PM1" Then Return "PM₁"
        If sPomiar = "PM25" Then Return "PM₂₅"
        If sPomiar.Substring(0, 2) = "PM" Then Return sPomiar   ' inny jakis PM :)

        Return sPomiar.Substring(0, 1) & sPomiar.Substring(1).ToLower
        '      { "name": "PM1",          "value": 12.73   },
        '      { "name": "PM25",         "value": 18.7    },
        '      { "name": "PM10",         "value": 35.53   },
        '      { "name": "PRESSURE",     "value": 1012.62 },
        '      { "name": "HUMIDITY",     "value": 66.44   },
        '      { "name": "TEMPERATURE",  "value": 24.71   },
    End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        If sPomiar.Substring(0, 2) = "PM" Then Return " μg/m³"

        Select Case sPomiar
            Case "PRESSURE"
                Return " hPa"
            Case "HUMIDITY"
                Return " %"
            Case "TEMPERATURE"
                Return " °C"
        End Select
        Return ""
    End Function


    Private Async Function GetPomiary(oTemplate As JedenPomiar, bInTimer As Boolean) As Task
        ' do moListaPomiarow dodaj wszystkie pomiary robione przez oTemplate.sId

        Dim sCmd As String
        sCmd = "v2/measurements/installation?installationId=" & oTemplate.sId
        Dim sPage As String = Await GetREST(sCmd)

        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonValue = Nothing
        Try
            oJson = Windows.Data.Json.JsonValue.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            If Not bInTimer Then Await DialogBox("ERROR: JSON parsing error - sPage (measurements)")
            Exit Function
        End If

        Dim oJsonCurrent As Windows.Data.Json.IJsonValue
        Try
            oJsonCurrent = oJson.GetObject().GetNamedValue("current")
            oTemplate.sTimeStamp = oJsonCurrent.GetObject().GetNamedString("fromDateTime")

            Dim oJsonValues As Windows.Data.Json.JsonArray

            oJsonValues = oJsonCurrent.GetObject().GetNamedArray("values")

            For Each oJsonMeasurement As Windows.Data.Json.JsonValue In oJsonValues

                Dim oNew As JedenPomiar = New JedenPomiar With {
            .sSource = oTemplate.sSource,
            .sId = oTemplate.sId,
            .dLon = oTemplate.dLon,
            .dLat = oTemplate.dLat,
            .dWysok = oTemplate.dWysok,
            .dOdl = oTemplate.dOdl,
            .sOdl = Odleglosc2String(oTemplate.dOdl),
            .sSensorDescr = oTemplate.sSensorDescr,
            .sAdres = oTemplate.sAdres,
            .sTimeStamp = oTemplate.sTimeStamp
            }

                oNew.sPomiar = oJsonMeasurement.GetObject().GetNamedString("name")
                oNew.dCurrValue = oJsonMeasurement.GetObject().GetNamedNumber("value")
                oNew.sUnit = Unit4Pomiar(oNew.sPomiar)
                If oNew.sPomiar = "HUMIDITY" OrElse oNew.sPomiar = "PRESSURE" Then   ' bez części dziesiętnych...
                    Dim iInt As Integer = oNew.dCurrValue
                    oNew.sCurrValue = iInt
                Else
                    oNew.sCurrValue = oNew.dCurrValue
                End If
                If oNew.sCurrValue.Length > 5 Then oNew.sCurrValue = oNew.sCurrValue.Substring(0, 5)
                oNew.sCurrValue = oNew.sCurrValue & oNew.sUnit

                oNew.sPomiar = NormalizePomiarName(oNew.sPomiar)
                AddPomiar(oNew)
            Next

        Catch ex As Exception
            Exit Function
        End Try


        '        {
        '  "current": {
        '    "fromDateTime": "2018-08-24T08:24:48.652Z",
        '    "tillDateTime": "2018-08-24T09:24:48.652Z",
        '    "values": [
        '      { "name": "PM1",          "value": 12.73   },
        '      { "name": "PM25",         "value": 18.7    },
        '      { "name": "PM10",         "value": 35.53   },
        '      { "name": "PRESSURE",     "value": 1012.62 },
        '      { "name": "HUMIDITY",     "value": 66.44   },
        '      { "name": "TEMPERATURE",  "value": 24.71   },
        '      ...
        '    ],
        '    "indexes": [
        '      {
        '        "name": "AIRLY_CAQI",
        '        "value": 35.53,
        '        "level": "LOW",
        '        "description": "Dobre powietrze.",
        '        "advice": "Możesz bez obaw wyjść na zewnątrz.",
        '        "color": "#D1CF1E"
        '      }
        '    ],
        '    "standards": [
        '      {
        '        "name": "WHO",
        '        "pollutant": "PM25",
        '        "limit": 25,
        '        "percent": 74.81
        '      },
        '      ...
        '    ]
        '  },
        '  "history": [ ... ],
        '  "forecast": [ ... ]
        '}


        '' oPomiar.sPomiar As String   ' jaki pomiar (np. PM10)
        '' oPomiar.sCurrValue As String ' etap 2: wartosc
        '' oPomiar.sTimeStamp As String ' etap 2: kiedy
        '' oPomiar.sLogoUri As String   ' logo, np. Airly etc., ktore warto pokazywac



    End Function

    ' bardzo podobnie powinna dzialac funkcja sprawdzania pomiarow z favourite, ale nie z GPS a z listy punktow? 
    ' Albo zawsze w ten sposob, wedle lokalizacji?
    ' tylko wtedy moze nie 5 stacji, tylko mniej?
    ' 1000 requests / day = 40 / hr
    ' 50 requests / min
    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Dim dMaxOdl As Double = 10

        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceAirly", True) Then Return moListaPomiarow

        If GetSettingsString("airly_apikey").Length < 8 Then Return moListaPomiarow

        Dim sCmd As String
        sCmd = "v2/installations/nearest?lat=" & oPos.X.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) &
                    "&lng=" & oPos.Y.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) &
                    "&maxDistanceKM=" & dMaxOdl.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) & "&maxResults=5"
        Dim sPage As String = Await GetREST(sCmd)


        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonArray = Nothing
        Try
            oJson = Windows.Data.Json.JsonArray.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await DialogBox("ERROR: JSON parsing error - Airly:sPage")
            Return moListaPomiarow
        End If



        Try
            If oJson.Count = 0 Then Return moListaPomiarow     ' brak bliskich?
        Catch ex As Exception
            Return moListaPomiarow  ' ale jesli cos jest nie tak z oJson, to tez wracaj pusto
        End Try


        ' przeiteruj, tworząc JedenPomiar dla kazdego pomiaru i kazdej lokalizacji
        Try

            For Each oJsonSensor As Windows.Data.Json.JsonValue In oJson

                ' tylko Airly nas interesuje, inne pomijamy (GIOS mamy po swojemu)
                If Not oJsonSensor.GetObject().GetNamedBoolean("airly") Then Continue For

                Dim oTemplate As JedenPomiar = New JedenPomiar
                oTemplate.sSource = "airly"
                oTemplate.sId = oJsonSensor.GetObject().GetNamedNumber("id")

                Dim oJsonPoint As Windows.Data.Json.JsonValue
                oJsonPoint = oJsonSensor.GetObject().GetNamedValue("location")
                oTemplate.dLon = oJsonPoint.GetObject().GetNamedNumber("longitude")
                oTemplate.dLat = oJsonPoint.GetObject().GetNamedNumber("latitude")

                oTemplate.dWysok = oJsonSensor.GetObject().GetNamedNumber("elevation", 0)

                oTemplate.dOdl = GPSdistanceDwa(oPos.X, oPos.Y,
                                                oTemplate.dLat, oTemplate.dLon)

                Dim oJsonSponsor As Windows.Data.Json.JsonValue
                oJsonSponsor = oJsonSensor.GetObject().GetNamedValue("sponsor")
                oTemplate.sSensorDescr = oJsonSponsor.GetObject().GetNamedString("name", "")

                Dim oJsonAdres As Windows.Data.Json.JsonValue
                oJsonAdres = oJsonSensor.GetObject().GetNamedValue("address")
                oTemplate.sAdres = oJsonAdres.GetObject().GetNamedString("city", "") & ", " &
                    oJsonAdres.GetObject().GetNamedString("street", "") & " " &
                    oJsonAdres.GetObject().GetNamedString("number", "")


                '' oPomiar.sPomiar As String   ' jaki pomiar (np. PM10)
                '' oPomiar.sCurrValue As String ' etap 2: wartosc
                '' oPomiar.sTimeStamp As String ' etap 2: kiedy
                '' oPomiar.sLogoUri As String   ' logo, np. Airly etc., ktore warto pokazywac

                Await GetPomiary(oTemplate, False)
            Next
        Catch ex As Exception

        End Try

        Return moListaPomiarow
        ' zwroc najblizsze
        ' {
        '  "id": 204,
        '  "location": {
        '    "latitude": 50.062006,
        '    "longitude": 19.940984
        '  },
        '  "address": {
        '    "country": "Poland",
        '    "city": "Kraków",
        '    "street": "Mikołajska",
        '    "number": "4B",
        '    "displayAddress1": "Kraków",
        '    "displayAddress2": "Mikołajska"
        '  },
        '  "elevation": 220.38,
        '  "airly": true,
        '  "sponsor": {
        '    "id": 7,       ' dopisek mój
        '    "name": "KrakówOddycha",
        '    "description": "Airly Sensor is part of action",
        '    "logo": "https://cdn.airly.eu/logo/KrakówOddycha.jpg",
        '    "link": "https://sponsor_home_address.pl"
        '  }
        '}

        ' rzeczywisty zwrot:
        '"[
        '{""id"":2395,""location"":{""latitude"":50.018006,""longitude"":19.983935},""address"":{""country"":""Poland"",""city"":""Kraków"",""street"":""Na Kozłówce"",""number"":""14"",""displayAddress1"":""Kraków"",""displayAddress2"":""Na Kozłówce""},""elevation"":238.73,""airly"":true,""sponsor"":{""id"":7,""name"":""KrakówOddycha"",""description"":""Airly Sensor is part of action"",""logo"":""https://cdn.airly.eu/logo/KrakowOddycha.jpg"",""link"":null}},
        '{""id"":857, ""location"":{""latitude"":50.03768, ""longitude"":19.990546},""address"":{""country"":""Poland"",""city"":""Kraków"",""street"":""Seweryna Goszczyńskiego"",""number"":""35"",""displayAddress1"":""Kraków"",""displayAddress2"":""Seweryna Goszczyńskiego""},""elevation"":200.37,""airly"":true,""sponsor"":{""id"":7,""name"":""KrakówOddycha"",""description"":""Airly Sensor is part of action"",""logo"":""https://cdn.airly.eu/logo/KrakowOddycha.jpg"",""link"":null}},
        '{""id"":18,  ""location"":{""latitude"":50.010575,""longitude"":19.949189},""address"":{""country"":""Poland"",""city"":""Kraków"",""street"":""Porucznika Halszki"",""number"":""16"",""displayAddress1"":""Kraków"",""displayAddress2"":""Porucznika Halszki""},""elevation"":224.34,""airly"":false,""sponsor"":{""id"":11,""name"":""State Environmental Monitoring Station"",""description"":"""",""logo"":""https://cdn.airly.eu/logo/GIOs.jpg"",""link"":null}},
        '{""id"":263, ""location"":{""latitude"":49.999615,""longitude"":19.966073},""address"":{""country"":""Poland"",""city"":""Kraków-Podgórze"",""street"":""Wyżynna"",""number"":""32"",""displayAddress1"":""Kraków"",""displayAddress2"":""Wyżynna""},""elevation"":235.34,""airly"":true,""sponsor"":{""id"":7,""name"":""KrakówOddycha"",""description"":""Airly Sensor is part of action"",""logo"":""https://cdn.airly.eu/logo/KrakowOddycha.jpg"",""link"":null}},
        '{""id"":2743,""location"":{""latitude"":50.04373, ""longitude"":19.987849},""address"":{""country"":""Poland"",""city"":""Kraków"",""street"":""Przewóz"",""number"":""14"",""displayAddress1"":""Kraków"",""displayAddress2"":""Przewóz""},""elevation"":199.35,""airly"":true,""sponsor"":{""id"":22,""name"":""Aviva"",""description"":""Airly Sensor's sponsor"",""logo"":""https://cdn.airly.eu/logo/Aviva_1538146740542_399306786.jpg"",""link"":""https://wiemczymoddycham.pl/""}}]"
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceAirly", True) Then Return moListaPomiarow

        If GetSettingsString("airly_apikey").Length < 8 Then Return moListaPomiarow


        ' przeiteruj, tworząc JedenPomiar dla kazdego pomiaru i kazdej lokalizacji
        Dim oTemplate As JedenPomiar = New JedenPomiar

        ' wczytaj dane template dla danego favname
        Dim oFile As Windows.Storage.StorageFile =
        Await App.GetDataFile(False, "airly_" & sId & ".xml", False)
        If oFile IsNot Nothing Then
            Dim oSer As Xml.Serialization.XmlSerializer =
                New Xml.Serialization.XmlSerializer(GetType(JedenPomiar))
            Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
            oTemplate = TryCast(oSer.Deserialize(oStream), JedenPomiar)
            oStream.Dispose()   ' == fclose
        Else
            oTemplate = New JedenPomiar
        End If

        oTemplate.sSource = "airly"  ' to tak na wszelki wypadek
        oTemplate.sId = sId

        Await GetPomiary(oTemplate, bInTimer)

        Return moListaPomiarow
    End Function




End Class

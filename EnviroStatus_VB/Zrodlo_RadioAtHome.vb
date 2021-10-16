
Public Class Source_RadioAtHome
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceRAH"
    Protected Overrides Property SRC_SETTING_HEADER As String = "Radioactive@Home"
    Protected Overrides Property SRC_RESTURI_BASE As String = "http://radioactiveathome.org/map/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "ra@h"

    Public Overrides Sub ReadResStrings()
        SetSettingsString("resRAHdailyAvg", GetLangString("resRAHdailyAvg"))
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

    Private Function NormalizePomiarName(sPomiar As String) As String
        Return "Radiation"
    End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        Return "μSv/h"
    End Function


    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Dim dMaxOdl As Double = 50

        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceRAH", True) Then Return moListaPomiarow

        Dim sPage As String = Await GetREST("")

        ' map.addOverlay(createMarker(new GLatLng(49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
        Dim iInd As Integer
        iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng")

        Try

            While iInd > 0
                Dim oNew As JedenPomiar = New JedenPomiar
                oNew.sSource = SRC_POMIAR_SOURCE

                sPage = sPage.Substring(iInd + "map.addOverlay(createMarker(new GLatLng".Length + 1)

                ' 49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
                iInd = sPage.IndexOf(",")
                oNew.dLat = sPage.Substring(0, iInd)
                sPage = sPage.Substring(iInd + 1)
                ' 18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                iInd = sPage.IndexOf(")")
                oNew.dLon = sPage.Substring(0, iInd)
                sPage = sPage.Substring(iInd + 2)
                ' 51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                oNew.dOdl = GPSdistanceDwa(oPos.X, oPos.Y, oNew.dLat, oNew.dLon)
                ' sprawdzamy odleglosc - czy w zakresie
                If oNew.dOdl / 1000 < dMaxOdl Then
                    ' teraz cos, co chce dodac

                    oNew.sOdl = Odleglosc2String(oNew.dOdl)

                    iInd = sPage.IndexOf(",")
                    oNew.sId = sPage.Substring(0, iInd)
                    sPage = sPage.Substring(iInd + 1)
                    ' 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                    oNew.dWysok = 0    ' brak danych

                    iInd = sPage.IndexOf(":")
                    sPage = sPage.Substring(iInd + 2)
                    iInd = sPage.IndexOf("<")
                    oNew.sCurrValue = sPage.Substring(0, iInd).Replace("uSv", "μSv").Trim
                    oNew.dCurrValue = oNew.sCurrValue.Replace("μSv/h", "")
                    If oNew.dCurrValue = 0 Then
                        ' jesli wyszlo zero, to moze trzeba zmienic kropke na przecinek?
                        oNew.dCurrValue = oNew.sCurrValue.Replace("μSv/h", "").Replace(".", ",")
                    End If
                    oNew.sUnit = Unit4Pomiar(oNew.sPomiar)

                    sPage = sPage.Substring(iInd + 1)
                    ' br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                    iInd = sPage.IndexOf(":")
                    sPage = sPage.Substring(iInd + 2)
                    iInd = sPage.IndexOf("<")
                    oNew.sTimeStamp = sPage.Substring(0, iInd)

                    sPage = sPage.Substring(iInd + 1)
                    ' br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                    iInd = sPage.IndexOf(":")
                    sPage = sPage.Substring(iInd + 2)
                    iInd = sPage.IndexOf("<")
                    oNew.sAddit = GetSettingsString("resRAHdailyAvg") & ": " & sPage.Substring(0, iInd).Replace("uSv", "μSv")


                    oNew.sSensorDescr = ""
                    Try
                        iInd = sPage.IndexOf("Team:")
                        sPage = sPage.Substring(iInd + 6)
                        iInd = sPage.IndexOf("<")
                        oNew.sSensorDescr = sPage.Substring(0, iInd)

                        iInd = sPage.IndexOf("Nick:")
                        sPage = sPage.Substring(iInd + 6)
                        iInd = sPage.IndexOf("'")
                        oNew.sSensorDescr = oNew.sSensorDescr & ", " & sPage.Substring(0, iInd)
                    Catch ex As Exception

                    End Try

                    oNew.sAdres = ""        ' brak danych
                    oNew.sPomiar = "μSv/h"

                    AddPomiar(oNew)

                End If
                iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng")
            End While

        Catch ex As Exception

        End Try

        If moListaPomiarow.Count < 1 Then
            Await DialogBox("ERROR: no station in range")
            Return moListaPomiarow
        End If

        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool("sourceRAH", True) Then Return moListaPomiarow

        Dim sPage As String = Await GetREST("")

        ' map.addOverlay(createMarker(new GLatLng(49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
        Dim iInd As Integer
        iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng")

        While iInd > 0
            Dim oNew As JedenPomiar = New JedenPomiar

            '' wczytaj dane template dla danego favname
            'Dim oFile As Windows.Storage.StorageFile =
            'Await App.GetDataFile(False, "rah_" & sFavName, False)
            'If oFile IsNot Nothing Then
            '    Dim oSer As Xml.Serialization.XmlSerializer =
            '        New Xml.Serialization.XmlSerializer(GetType(JedenPomiar))
            '    Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
            '    oNew = TryCast(oSer.Deserialize(oStream), JedenPomiar)
            '    oStream.Dispose()   ' == fclose
            'Else
            '    oNew = New JedenPomiar
            'End If

            oNew.sSource = SRC_POMIAR_SOURCE

            sPage = sPage.Substring(iInd + "map.addOverlay(createMarker(new GLatLng".Length + 1)

            ' 49.919102,18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
            iInd = sPage.IndexOf(",")
            oNew.dLat = sPage.Substring(0, iInd)
            sPage = sPage.Substring(iInd + 1)
            ' 18.998129),51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

            iInd = sPage.IndexOf(")")
            oNew.dLon = sPage.Substring(0, iInd)
            sPage = sPage.Substring(iInd + 2)
            ' 51, 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

            oNew.dOdl = GPSdistanceDwa(App.moGpsPoint.X, App.moGpsPoint.Y, oNew.dLat, oNew.dLon)
            ' sprawdzamy odleglosc - czy w zakresie

            oNew.sOdl = Odleglosc2String(oNew.dOdl)

            iInd = sPage.IndexOf(",")
            oNew.sId = sPage.Substring(0, iInd)
            sPage = sPage.Substring(iInd + 1)
            ' 'Last sample: 0.09 uSv/h<br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));
            If oNew.sId = sId Then
                ' teraz cos, co chce dodac

                oNew.dWysok = 0    ' brak danych

                iInd = sPage.IndexOf(":")
                sPage = sPage.Substring(iInd + 2)
                iInd = sPage.IndexOf("<")
                oNew.sCurrValue = sPage.Substring(0, iInd).Replace("uSv", "μSv")
                oNew.dCurrValue = oNew.sCurrValue.Replace("μSv/h", "").Trim

                sPage = sPage.Substring(iInd + 1)
                ' br />Last contact: 2019-02-07 16:19:45<br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                iInd = sPage.IndexOf(":")
                sPage = sPage.Substring(iInd + 2)
                iInd = sPage.IndexOf("<")
                oNew.sTimeStamp = sPage.Substring(0, iInd)

                sPage = sPage.Substring(iInd + 1)
                ' br/>24 hours average: 0.08 uSv/h<br /><a href=http://radioactiveathome.org/boinc/show_host_detail.php?hostid=51>Details sensor 51</a><br/><a href=http://radioactiveathome.org/boinc/results.php?hostid=51>Work Units</a><br/>Team: BOINC@Poland<br />Nick: AL ADIM',icon));

                iInd = sPage.IndexOf(":")
                sPage = sPage.Substring(iInd + 2)
                iInd = sPage.IndexOf("<")
                oNew.sAddit = "średnia dobowa: " & sPage.Substring(0, iInd).Replace("uSv", "μSv")


                oNew.sSensorDescr = ""
                Try
                    iInd = sPage.IndexOf("Team:")
                    sPage = sPage.Substring(iInd + 6)
                    iInd = sPage.IndexOf("<")
                    oNew.sSensorDescr = sPage.Substring(0, iInd)

                    iInd = sPage.IndexOf("Nick:")
                    sPage = sPage.Substring(iInd + 6)
                    iInd = sPage.IndexOf("'")
                    oNew.sSensorDescr = oNew.sSensorDescr & ", " & sPage.Substring(0, iInd)
                Catch ex As Exception

                End Try

                oNew.sAdres = ""        ' brak danych
                oNew.sPomiar = "μSv/h"

                moListaPomiarow.Add(oNew)

            End If
            iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng")
        End While

        If moListaPomiarow.Count < 1 Then
            If Not bInTimer Then Await DialogBox("ERROR: data parsing error - sPage")
        End If

        Return moListaPomiarow
    End Function



End Class


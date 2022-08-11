Imports System.Linq
Imports System.Collections.ObjectModel

Public Class Source_EUradiation
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceEUrad"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "EU Radioactivity Monitoring"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://remap.jrc.ec.europa.eu/api/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "EUremon"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "https://remap.jrc.ec.europa.eu/Help/Simple.aspx"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "https://remap.jrc.ec.europa.eu/Help/Simple.aspx"

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

    Private Function NormalizePomiarName(sPomiar As String) As String
        Return "Radiation"
    End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        Return "μSv/h"
    End Function

    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Dim dMaxOdl As Double = 50
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool("sourceRAH", SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        Dim sPage As String = Await GetREST("")
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim iInd As Integer
        iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng")

        Try

            While iInd > 0
                Dim oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
                sPage = sPage.Substring(iInd + "map.addOverlay(createMarker(new GLatLng".Length + 1)
                iInd = sPage.IndexOf(",")
                oNew.dLat = sPage.Substring(0, iInd)
                sPage = sPage.Substring(iInd + 1)
                iInd = sPage.IndexOf(")")
                oNew.dLon = sPage.Substring(0, iInd)
                sPage = sPage.Substring(iInd + 2)
                oNew.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oNew.dLat, oNew.dLon))

                If oNew.dOdl / 1000 < dMaxOdl Then
                    oNew.sOdl = Odleglosc2String(oNew.dOdl)
                    iInd = sPage.IndexOf(",")
                    oNew.sId = sPage.Substring(0, iInd)
                    sPage = sPage.Substring(iInd + 1)
                    oNew.dWysok = 0
                    iInd = sPage.IndexOf(":")
                    sPage = sPage.Substring(iInd + 2)
                    iInd = sPage.IndexOf("<")
                    oNew.sCurrValue = sPage.Substring(0, iInd).Replace("uSv", "μSv").Trim()
                    oNew.dCurrValue = oNew.sCurrValue.Replace("μSv/h", "")
                    If oNew.dCurrValue = 0 Then oNew.dCurrValue = Double.Parse(oNew.sCurrValue.Replace("μSv/h", "").Replace(".", ","))
                    oNew.sUnit = Unit4Pomiar(oNew.sPomiar)
                    sPage = sPage.Substring(iInd + 1)
                    iInd = sPage.IndexOf(":")
                    sPage = sPage.Substring(iInd + 2)
                    iInd = sPage.IndexOf("<")
                    oNew.sTimeStamp = sPage.Substring(0, iInd)
                    sPage = sPage.Substring(iInd + 1)
                    iInd = sPage.IndexOf(":")
                    sPage = sPage.Substring(iInd + 2)
                    iInd = sPage.IndexOf("<")
                    oNew.sAddit = GetLangString("resRAHdailyAvg") & ": " & sPage.Substring(0, iInd).Replace("uSv", "μSv")
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
                    Catch
                    End Try

                    oNew.sAdres = ""
                    oNew.sPomiar = "μSv/h"
                    AddPomiar(oNew)
                End If

                iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng")
            End While

        Catch
        End Try

        If moListaPomiarow.Count < 1 Then
            Await DialogBoxAsync("ERROR: no station in range")
            Return moListaPomiarow
        End If

        Return moListaPomiarow
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool("sourceRAH", SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        Dim sPage As String = Await GetREST("")
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim iInd As Integer
        iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng")

        While iInd > 0
            Dim oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
            sPage = sPage.Substring(iInd + "map.addOverlay(createMarker(new GLatLng".Length + 1)
            iInd = sPage.IndexOf(",")
            oNew.dLat = sPage.Substring(0, iInd)
            sPage = sPage.Substring(iInd + 1)
            iInd = sPage.IndexOf(")")
            oNew.dLon = sPage.Substring(0, iInd)
            sPage = sPage.Substring(iInd + 2)
            oNew.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oNew.dLat, oNew.dLon))
            oNew.sOdl = Odleglosc2String(oNew.dOdl)
            iInd = sPage.IndexOf(",")
            oNew.sId = sPage.Substring(0, iInd)
            sPage = sPage.Substring(iInd + 1)

            If (If(oNew.sId, "")) = (If(sId, "")) Then
                oNew.dWysok = 0
                iInd = sPage.IndexOf(":")
                sPage = sPage.Substring(iInd + 2)
                iInd = sPage.IndexOf("<")
                oNew.sCurrValue = sPage.Substring(0, iInd).Replace("uSv", "μSv")
                oNew.dCurrValue = Double.Parse(oNew.sCurrValue.Replace("μSv/h", "").Trim())
                sPage = sPage.Substring(iInd + 1)
                iInd = sPage.IndexOf(":")
                sPage = sPage.Substring(iInd + 2)
                iInd = sPage.IndexOf("<")
                oNew.sTimeStamp = sPage.Substring(0, iInd)
                sPage = sPage.Substring(iInd + 1)
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
                Catch
                End Try

                oNew.sAdres = ""
                oNew.sPomiar = "μSv/h"
                moListaPomiarow.Add(oNew)
            End If

            iInd = sPage.IndexOf("map.addOverlay(createMarker(new GLatLng")
        End While

        If moListaPomiarow.Count < 1 Then
            If Not bInTimer Then Await DialogBoxAsync("ERROR: data parsing error - sPage")
        End If

        Return moListaPomiarow
    End Function
End Class

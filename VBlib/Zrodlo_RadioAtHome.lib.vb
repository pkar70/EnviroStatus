Imports System.Collections.ObjectModel

Public Class Source_RadioAtHome
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceRAH"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Radioactive@Home"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "http://radioactiveathome.org/map/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "ra@h"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "http://radioactiveathome.org/en/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "http://radioactiveathome.org/pl/"

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

    'Private Function NormalizePomiarName(sPomiar As String) As String
    '    Return "Radiation"
    'End Function

    Private Function Unit4Pomiar(sPomiar As String) As String
        Return "μSv/h"
    End Function


    Private Const ITEM_PREFIX As String = "L.marker(["

    ''' <summary>
    ''' połączenie razem Nearest oraz Fav, bo i tak to samo robią
    ''' </summary>
    ''' <param name="oPos">GPS telefonu, do liczenia odległości</param>
    ''' <param name="dMaxOdl">double.Max gdy nie sprawdzamy</param>
    ''' <param name="sId">"" gdy każdy sensor</param>
    ''' <returns></returns>
    Private Async Function GetPomiaryAsync(oPos As MyBasicGeoposition, dMaxOdl As Double, sId As String) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        moListaPomiarow = New Collection(Of JedenPomiar)()
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow
        Dim sPage As String = Await GetREST("")
        If sPage.Length < 10 Then Return moListaPomiarow
        Dim iInd As Integer
        iInd = sPage.IndexOf(ITEM_PREFIX)

        ' 2022.08.11
        ' L.marker([59.92, 30.31], {icon: icon_green}).bindPopup('Last sample: 0.11 uSv/h<br />Last contact: 2022-08-11 13:32:37<br/>24 hours average: 0.12 uSv/h<br />Sensor 73<br/><a href=http://radioactiveathome.org/scripts/graph/drawweekdotted.php?hostid=73>7 days plot</a><br/>Team: hidden<br />Nick: hidden').addTo(map);
        Try

            While iInd > 0
                Dim oNew = New JedenPomiar(SRC_POMIAR_SOURCE)
                sPage = sPage.Substring(iInd + ITEM_PREFIX.Length)
                iInd = sPage.IndexOf(",")
                oNew.dLat = sPage.Substring(0, iInd)
                sPage = sPage.Substring(iInd + 1)
                iInd = sPage.IndexOf("]")
                oNew.dLon = sPage.Substring(0, iInd)
                sPage = sPage.Substring(iInd + 2)
                oNew.dOdl = oPos.DistanceTo(New MyBasicGeoposition(oNew.dLat, oNew.dLon))

                ' przy szukaniu wedle nazwy: zawsze prawdziwe, bo dMaxOdl = double.max
                If oNew.dOdl / 1000 < dMaxOdl Then
                    ' {icon: icon_green}).bindPopup('Last sample: 0.11 uSv/h<br />Last contact: 2022-08-11 13:32:37<br/>24 hours average: 0.12 uSv/h<br />Sensor 73<br/><a href=http://radioactiveathome.org/scripts/graph/drawweekdotted.php?hostid=73>7 days plot</a><br/>Team: hidden<br />Nick: hidden').addTo(map);

                    oNew.sOdl = Odleglosc2String(oNew.dOdl)
                    oNew.dWysok = 0

                    oNew.sId = sPage.SubstringBetween("hostid=", ">")

                    If sId = "" OrElse oNew.sId = sId Then

                        ' pomiar
                        oNew.sCurrValue = sPage.SubstringBetween("sample: ", "<").Replace("uSv", "μSv").Trim
                        oNew.dCurrValue = oNew.sCurrValue.Replace("μSv/h", "")
                        If oNew.dCurrValue = 0 Then oNew.dCurrValue = oNew.sCurrValue.Replace("μSv/h", "").Replace(".", ",")

                        oNew.sUnit = Unit4Pomiar(oNew.sPomiar)

                        oNew.sTimeStamp = sPage.SubstringBetween("contact: ", "<")

                        ' addit, tu: daily
                        oNew.sAddit = GetLangString("resRAHdailyAvg") & ": " &
                        sPage.SubstringBetween("average: ", "<").Replace("uSv", "μSv")

                        oNew.sSensorDescr = ""

                        Try
                            ' <br/>Team: hidden<br />Nick: hidden').
                            oNew.sSensorDescr = sPage.SubstringBetween("Team: ", "<") & ", " &
                                      sPage.SubstringBetween("Nick: ", "'")

                        Catch
                        End Try

                        oNew.sAdres = ""
                        oNew.sPomiar = "μSv/h"
                        AddPomiar(oNew)
                    End If

                End If

                iInd = sPage.IndexOf(ITEM_PREFIX)
            End While

        Catch
        End Try

        If moListaPomiarow.Count < 1 Then
            Await DialogBoxAsync($"ERROR: no station in range ({SRC_SETTING_NAME})")
            Return moListaPomiarow
        End If

        Return moListaPomiarow
    End Function


    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Dim dMaxOdl As Double = 50
        Return Await GetPomiaryAsync(oPos, dMaxOdl, "")
    End Function

    ''' <summary>
    ''' Wykorzystuje moGPSpoint!
    ''' </summary>
    ''' <param name="sId"></param>
    ''' <param name="sAddit"></param>
    ''' <param name="bInTimer"></param>
    ''' <param name="moGpsPoint"></param>
    ''' <returns></returns>
    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oGpsPoint As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await GetPomiaryAsync(oGpsPoint, Double.MaxValue, sId)
    End Function
End Class

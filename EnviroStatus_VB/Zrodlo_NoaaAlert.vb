
Public Class Source_NoaaAlert
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceNoaaAlert"
    Protected Overrides Property SRC_SETTING_HEADER As String = "NOAA alerts"
    Protected Overrides Property SRC_RESTURI_BASE As String = "https://services.swpc.noaa.gov/products/alerts.json"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "NOAAalert"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True

    Public Overrides Async Function GetNearest(oPos As Point) As Task(Of Collection(Of JedenPomiar))
        Return Await GetDataFromFavSensor("", "", False)
    End Function

    Public Overrides Async Function GetDataFromFavSensor(sId As String, sAddit As String, bInTimer As Boolean) As Task(Of Collection(Of JedenPomiar))
        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME) Then Return moListaPomiarow

        Dim sPage As String = Await GetREST("")

        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonValue = Nothing
        Try
            oJson = Windows.Data.Json.JsonValue.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            If Not bInTimer Then Await DialogBox("ERROR: JSON parsing error - sPage " & SRC_POMIAR_SOURCE)
            Return moListaPomiarow
        End If

        ' [
        ' {"product_id":"EF3A","issue_datetime":"2019-05-31 08:59:39.047","message":"Space Weather Message Code: ALTEF3\r\nSerial Number: 2945\r\nIssue Time: 2019 May 31 0859 UTC\r\n\r\nCONTINUED ALERT: Electron 2MeV Integral Flux exceeded 1000pfu\r\nContinuation of Serial Number: 2944\r\nBegin Time: 2019 May 30 1535 UTC\r\nYesterday Maximum 2MeV Flux: 1766 pfu\r\n\r\nNOAA Space Weather Scale descriptions can be found at\r\nwww.swpc.noaa.gov\/noaa-scales-explanation\r\n\r\nPotential Impacts: Satellite systems may experience significant charging resulting in increased risk to satellite systems."},
        ' czasem ma: Valid Until:
        ' Valid To: 2019 May 29 0900 UTC
        ' Valid Until: 2019 May 29 1500 UTC

        ']
        'robimy inaczej - pokazuje tylko nowsze niz widziany poprzednio

        Dim oJsonArray As Windows.Data.Json.JsonArray
        oJsonArray = oJson.GetArray

        ' Dim sPrevLastTime As String = GetSettingsString("NOAAalertTLastimestamp")
        Dim iGuard As Integer = 0 ' limit ostrzezen (potrzebne przy pierwszym uruchomieniu)
        'Dim sLastTime As String = "1999-01-01"
        For Each oJsonVal As Windows.Data.Json.JsonValue In oJsonArray
            Dim sThisTime As String = oJsonVal.GetObject.GetNamedString("issue_datetime")
            'If sThisTime > sPrevLastTime Then
            'If sLastTime < sThisTime Then sLastTime = sThisTime
            iGuard += 1
                If iGuard > 5 Then Exit For

                Dim oNew As JedenPomiar = New JedenPomiar
                oNew.sSource = SRC_POMIAR_SOURCE
            oNew.sTimeStamp = sThisTime

            Dim iInd As Integer
            iInd = sThisTime.IndexOf(".")
            If iInd > 0 Then sThisTime = sThisTime.Substring(0, iInd)

            Dim oDate As Date
            If Date.TryParseExact(sThisTime, "yyyy-MM-dd HH:mm:ss", Nothing, Globalization.DateTimeStyles.AssumeUniversal, oDate) Then
                oNew.sTimeStamp = oDate.ToLocalTime.ToString("yyyy-MM-dd HH:mm:ss")
                If oDate.AddDays(1) < Date.Now Then Continue For
            Else
                oNew.sTimeStamp = sThisTime & " UTC"
            End If

            If iGuard > 1 Then
                    oNew.sPomiar = "Alert" & iGuard
                Else
                    oNew.sPomiar = "Alert"
                End If

                Dim sMsg As String = oJsonVal.GetObject.GetNamedString("message")
                sMsg = sMsg.Replace("NOAA Space Weather Scale descriptions can be found at", "")
                sMsg = sMsg.Replace("www.swpc.noaa.gov/noaa-scales-explanation", "")
                sMsg = sMsg.Replace("\r\n\r\n", "\r\n")
                sMsg = sMsg.Replace("\r\n", vbCrLf)
                Dim aTmp As String() = sMsg.Split(vbCrLf)
                For Each sLine As String In aTmp
                iInd = sLine.IndexOf(":")

                If sLine.Contains("Space Weather Message Code") Then
                        oNew.sPomiar = "N." & sLine.Substring(iInd + 1).Trim
                    End If
                ' *TODO* ewentualnie nie wedle Alert/Watch/Warning, ale zapisanej w środku skali: G1- Minor etc.
                If sLine.Contains("ALERT:") Then
                        oNew.sCurrValue = sLine.Substring(iInd + 1).Trim
                        oNew.sAlert = "!!!"
                    End If

                    If sLine.Contains("WATCH:") Then
                        oNew.sCurrValue = sLine.Substring(iInd + 1).Trim
                        oNew.sAlert = "!!"
                    End If

                    If sLine.Contains("WARNING:") Then
                        oNew.sCurrValue = sLine.Substring(iInd + 1).Trim
                        oNew.sAlert = "!"
                    End If
                Next ' linie w 'message'

                oNew.sAddit = sMsg
                moListaPomiarow.Add(oNew)

            'End If
        Next

        'If sLastTime > sPrevLastTime Then SetSettingsString("NOAAalertTLastimestamp", sLastTime)

        Return moListaPomiarow

    End Function
End Class

'Partial Public Class App
'    Public Shared moSrc_NoaaKind As Source_NoaaKindex = New Source_NoaaKindex
'End Class

Public Class Source_NoaaKindex
    Inherits Source_Base

    Protected Overrides Property SRC_SETTING_NAME As String = "sourceNoaaKind"
    Protected Overrides Property SRC_SETTING_HEADER As String = "NOAA planetary K-index (magnetic activity)"
    Protected Overrides Property SRC_RESTURI_BASE As String = "https://services.swpc.noaa.gov/products/noaa-planetary-k-index-forecast.json"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "NOAAkind"
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True

    Public Overrides Sub ReadResStrings()
        SetSettingsString("resPomiarNoaaKindexPredicted", GetLangString("resPomiarNoaaKindexPredicted"))
    End Sub

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
        ' ["time_tag","kp","observed","noaa_scale"],
        ' ["2019-05-31 06:00:00","2","observed",null],
        ' ["2019-05-31 12:00:00","1","estimated",null],
        ' ["2019-06-01 00:00:00","3","predicted",null]
        ']
        ' G0 (Kp<5), G1 (Kp=5), G2 (Kp=6), G3 (Kp=7), G4 (Kp=8), G5 (Kp>=9)

        Dim oJsonArray As Windows.Data.Json.JsonArray
        oJsonArray = oJson.GetArray

        Dim oNew As JedenPomiar = New JedenPomiar
        oNew.sSource = SRC_POMIAR_SOURCE
        oNew.sPomiar = "Kp index"
        Dim sNowyPomiar As String = ""
        Dim sNowyTime As String = ""

        For Each oJsonOdczyt As Windows.Data.Json.JsonValue In oJsonArray
            If oJsonOdczyt.GetArray.Item(2).GetString = "estimated" Then
                sNowyPomiar = oJsonOdczyt.GetArray.Item(1).GetString
                sNowyTime = oJsonOdczyt.GetArray.Item(0).GetString
                Exit For
            End If
            If oJsonOdczyt.GetArray.Item(2).GetString <> "observed" Then Exit For
            oNew.sTimeStamp = oJsonOdczyt.GetArray.Item(0).GetString
            oNew.sCurrValue = oJsonOdczyt.GetArray.Item(1).GetString
        Next

        If oNew.sTimeStamp = "" Then Return moListaPomiarow

        Dim oDate As Date
        Dim sTime As String = oNew.sTimeStamp
        If Date.TryParseExact(sTime, "yyyy-MM-dd HH:mm:ss", Nothing, Globalization.DateTimeStyles.AssumeUniversal, oDate) Then
            sTime = oDate.ToLocalTime.ToString("yyyy-MM-dd HH:mm:ss")
        Else
            sTime = sTime & " UTC"
        End If
        oNew.sTimeStamp = sTime
        Double.TryParse(oNew.sCurrValue, oNew.dCurrValue)

        If Date.TryParseExact(sNowyTime, "yyyy-MM-dd HH:mm:ss", Nothing, Globalization.DateTimeStyles.AssumeUniversal, oDate) Then
            sNowyTime = oDate.ToLocalTime.ToString("yyyy-MM-dd HH:mm:ss")
        Else
            sNowyTime = sNowyTime & " UTC"
        End If

        oNew.sAddit = GetSettingsString("resPomiarNoaaKindexPredicted") & " " & sNowyTime & ": " & sNowyPomiar

        If oNew.dCurrValue >= 7 Then oNew.sAlert = "!"
        If oNew.dCurrValue >= 8 Then oNew.sAlert = "!!"
        If oNew.dCurrValue >= 9 Then oNew.sAlert = "!!!"

        moListaPomiarow.Add(oNew)


        Return moListaPomiarow

    End Function
End Class

Imports System.Collections.ObjectModel
Imports System.Linq.Expressions
Imports HtmlAgilityPack

Partial Public Class Source_AlergenOBASNew
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceAlergenNew"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Ośrodek Badania Alergenów Środowiskowych"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://www.momesternasal.pl/handlers/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "OBAŚ"
    ' Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "http://obas.pl/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "http://obas.pl/"
    Public Overrides ReadOnly Property SRC_ZASIEG As Zasieg = Zasieg.Prywatne


    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub


    Public Overrides Async Function GetNearestAsync(oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        Return Await GetDataFromFavSensorAsync("", "", False, pkar.BasicGeopos.Empty)
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As pkar.BasicGeopos) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        Await OstrzezeniaPylkowe(sId)

        Return moListaPomiarow
    End Function


    ''' <summary>
    ''' Dopisuje do moListaPomiarow po jednym JedenPomiar o każdym ostrzeżeniu
    ''' </summary>
    ''' <param name="oPos"></param>
    ''' <returns></returns>
    Private Async Function OstrzezeniaPylkowe(sId As String) As Task
        DumpCurrMethod()

        Dim sPage As String = Await GetREST($"map.php?region=Ma%C5%82opolska%20i%20Ziemia%20Lubelska")
        If sPage = "" Then Return
        If sPage.Length < 10 Then Return ' "null" daje?

        Dim oHtml As New HtmlAgilityPack.HtmlDocument()
        oHtml.LoadHtml(sPage)

        '<li class="map__info__item">
        '                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="30" viewBox="0 0 20 30">
        '                                <path id="Polygon_4" data-name="Polygon 4" d="M11,5.333a5,5,0,0,1,8,0L24,12a5,5,0,0,1-4,8H10a5,5,0,0,1-4-8Z" transform="translate(20) rotate(90)" fill="#389c51"/>
        '                            </svg>
        '
        '                            
        '                            <span class="map__legend__sign map__legend__sign--down"> </span>                   <span class="pollen_name"> olsza </span>
        '                </li>
        ' to jest tylko wnętrze (fragment strony)
        For Each oNode In oHtml.DocumentNode.SelectNodes("//li")
            Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)

            oNew.sPomiar = oNode.SelectSingleNode(".//span[@class='pollen_name']")?.InnerText.Trim
            oNew.sAdres = "Małopolska i ziemia lubelska"
            oNew.bCleanAir = True

            oNew.sCurrValue = ExtractValue(oNode.SelectSingleNode(".//path")?.Attributes.Item("data-name").Value)

            oNew.sAddit = "Trend: " & ExtractTrend(oNode.InnerHtml.ToLowerInvariant)

            oNew.sAlert = LevelToAlert(oNew.sCurrValue)
            If oNew.sAlert <> "" Then oNew.bCleanAir = False

            moListaPomiarow.Add(oNew)

        Next

    End Function

    Private Function ExtractValue(attrValue As String) As String
        If attrValue Is Nothing Then Return "?null"

        Select Case attrValue
            Case "Polygon 6"
                Return "brak"
            Case "Polygon 5"
                Return "bardzo niskie"
            Case "Polygon 4"
                Return "niskie"
            Case "Polygon 3"
                Return "średnie"
            Case "Polygon 2"
                Return "wysokie"
            Case "Polygon 7"
                Return "bardzo wysokie"
        End Select

        Return "?"
    End Function


    Private Function ExtractTrend(innerHtml As String) As String
        If innerHtml.Contains("&#215") Then Return "koniec sezonu"
        If innerHtml.Contains("&#8595") Then Return "silny spadek"
        If innerHtml.Contains("map__legend__sign--down") Then Return "spadek"
        If innerHtml.Contains("map__info__item__sign") Then Return "bez zmian"
        If innerHtml.Contains("map__legend__sign--up") Then Return "wzrost"
        If innerHtml.Contains("&#8593") Then Return "silny wzrost"

        Return ""
    End Function

    Private Function LevelToAlert(sLevel As String) As String
        If sLevel.ToUpperInvariant.Contains("WYSOKIE") Then Return "!!!"
        If sLevel.ToUpperInvariant = "ŚREDNIE" Then Return "!!"
        If sLevel.ToUpperInvariant.Contains("NISKIE") Then Return "!"

        Return ""
    End Function


End Class


Imports System.Collections.ObjectModel

' *TODO* może być ewentualne w Setup do źródła żeby nie pokazywać zer


Partial Public Class Source_AlergenOBAS
    Inherits Source_Base

    Public Overrides ReadOnly Property SRC_SETTING_NAME As String = "sourceAlergen"
    Public Overrides ReadOnly Property SRC_SETTING_HEADER As String = "Ośrodek Badania Alergenów Środowiskowych"
    Protected Overrides ReadOnly Property SRC_RESTURI_BASE As String = "https://api.zadnegoale.pl/"
    Public Overrides ReadOnly Property SRC_POMIAR_SOURCE As String = "OBAŚ"
    ' Protected Overrides ReadOnly Property SRC_HAS_TEMPLATES As Boolean = True
    Public Overrides ReadOnly Property SRC_IN_TIMER As Boolean = True
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_EN As String = "http://obas.pl/"
    Protected Overrides ReadOnly Property SRC_URI_ABOUT_PL As String = "http://obas.pl/"
    Public Overrides ReadOnly Property SRC_ZASIEG As Zasieg = Zasieg.Poland


    Public Sub New(bMyNotPublic As Boolean, sTemplatePath As String)
        MyBase.New(bMyNotPublic, sTemplatePath)
    End Sub


    Public Overrides Async Function GetNearestAsync(oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
        DumpCurrMethod()

        moListaPomiarow = New Collection(Of JedenPomiar)
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return moListaPomiarow

        ' zamiana współrzędnych na rejon
        Dim sPage As String = Await GetREST($"regions/public/{oPos.Latitude}/{oPos.Longitude}")
        If sPage = "" Then
            DumpMessage("ERROR w pyłkach: nie potrafi zamienić GeoLoc na kod regionu?")
            Return moListaPomiarow
        End If

        Return Await GetDataFromFavSensorAsync(sPage + 1, "", False, Nothing)
    End Function

    Public Overrides Async Function GetDataFromFavSensorAsync(sId As String, sAddit As String, bInTimer As Boolean, oPos As MyBasicGeoposition) As Task(Of Collection(Of JedenPomiar))
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

        Dim sPage As String = Await GetREST($"dusts/public/date/{Date.Now.ToString("dd-MM-yyyy")}/region/{sId}")
        If sPage = "" Then Return

        Dim listaPylkow As List(Of AllergenOBAS_JedenPylek)
        listaPylkow = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(List(Of AllergenOBAS_JedenPylek)))

        ' *TODO* ale jak zrobić że robi OK i jednocześnie nie pokazywać danego pyłku?

        Dim bLangEN As Boolean = (GetLangString("_lang").ToUpperInvariant = "EN")

        For Each oPylek As AllergenOBAS_JedenPylek In listaPylkow
            Dim oNew As New JedenPomiar(SRC_POMIAR_SOURCE)

            oNew.dCurrValue = oPylek.value
            oNew.sAdres = oPylek.region.name
            oNew.sId = sId

            oNew.sAlert = LevelToAlert(oPylek.level)

            oNew.bCleanAir = True
            If oNew.sAlert <> "" Then oNew.bCleanAir = False


            If bLangEN Then
                ' tłumaczymy
                oNew.sPomiar = DictPylki(oPylek.allergen.name.ToLowerInvariant)
                oNew.sCurrValue = oNew.dCurrValue & " (" & DictLevel(oPylek.level.ToLowerInvariant) & ")"
                oNew.sAddit = "Trend: " & DictTrend(oPylek.trend.ToLowerInvariant)
                If oNew.sPomiar.ToLowerInvariant <> oPylek.allergen.name.ToLowerInvariant Then
                    oNew.sAddit = oNew.sAddit & vbCrLf & "In Polish: " & oPylek.allergen.name
                End If

            Else
                oNew.sPomiar = oPylek.allergen.name
                oNew.sCurrValue = oNew.dCurrValue & " (" & oPylek.level & ")"
                oNew.sAddit = "Trend: " & oPylek.trend
            End If

            moListaPomiarow.Add(oNew)

        Next

    End Function

    Private Function LevelToAlert(sLevel As String) As String
        If sLevel.ToUpperInvariant.Contains("WYSOKIE") Then Return "!!!"
        If sLevel.ToUpperInvariant = "ŚREDNIE" Then Return "!!"
        If sLevel.ToUpperInvariant.Contains("NISKIE") Then Return "!"

        Return ""
    End Function



#Region "słowniki (translacja)"
    Private DictPylki As New Dictionary(Of String, String) From
        {
    {"alternaria", "alternaria"},
    {"ambrozja", "ragweed"},
    {"babka", "plantain"},
    {"brzoza", "birch_tree"},
    {"buk", "beech"},
    {"bylica", "mugwort"},
    {"cis", "yew"},
    {"cladosporium", "cladosporium"},
    {"dąb", "oak"},
    {"grab", "hornbeam"},
    {"jesion", "ash_tree"},
    {"klon", "maple"},
    {"komosa", "pigweed"},
    {"leszczyna", "hazel"},
    {"nawłoć", "goldenrod"},
    {"olsza", "alder"},
    {"platan", "plane_tree"},
    {"pokrzywa", "nettle"},
    {"sosna", "pine"},
    {"szczaw", "sorrel"},
    {"topola", "poplar"},
    {"trawy", "grass"},
    {"wierzba", "willow"},
    {"wiąz", "elm"}
    }

    Private DictLevel As New Dictionary(Of String, String) From
        {
    {"bardzo wysokie", "very high"},
    {"wysokie", "high"},
    {"średnie", "medium"},
    {"niskie", "low"},
    {"bardzo niskie", "very low"},
    {"brak", "lack"}
    }

    Private DictTrend As New Dictionary(Of String, String) From
        {
        {"bez zmian", "no change"},
    {"wzrost", "rising"},
    {"silny wzrost", "strong rising"},
    {"spadek", "falling"},
    {"silny spadek", "strong falling"},
    {"koniec sezonu", "end of season"}
        }
#End Region

#Region "klasy dla JSON"

    Public Class AllergenOBAS_Allergen
        'Public Property id As Integer
        Public Property name As String
    End Class

    Public Class AllergenOBAS_Data
        Public Property [date] As Date
        'Public Property timezone_type As Integer
        'Public Property timezone As String
    End Class

    Public Class AllergenOBAS_Region
        Public Property id As Integer
        Public Property name As String ' ": "Warmia, Mazury i Podlasie"
    End Class

    Public Class AllergenOBAS_JedenPylek

        ' "id": 1930,
        ' text z opisem, ale wtedy nie ma trend, level, value ani allergen
        Public Property startDate As AllergenOBAS_Data
        Public Property endDate As AllergenOBAS_Data
        Public Property trend As String ' "Wzrost"
        Public Property level As String ' "Bardzo wysokie"
        Public Property value As Double '  1200
        Public Property region As AllergenOBAS_Region   ' wykorzystywany do oNew.sAdres
        Public Property allergen As AllergenOBAS_Allergen
    End Class
#End Region

End Class


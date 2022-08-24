

' JedenSensor - czyli pomiar danego parametru, zeby wiadomo bylo skad go brac
' lista sensorow najblizszych: Source_XXX.GetNearest
' to potem do posortowania (i znalezienia najblizszych dla kazdego pomiaru)


Public Class JedenPomiar
        Public Property sSource As String = ""  ' np. airly
    Public Property sId As String = ""     ' interpretowane przez klasę airly, używane przy Fav do ściągania (które ma być używany)
    Public Property dLon As Double = 0    ' lokalizacja sensora
        Public Property dLat As Double = 0
        Public Property dWysok As Double = 0
        Public Property dOdl As Double = 0    ' odleglosc - wazne przy sprawdzaniu ktory najblizszy
        Public Property sPomiar As String = "" ' jaki pomiar (np. PM10)
        Public Property sCurrValue As String = "" ' etap 2: wartosc
        Public Property dCurrValue As Double = 0
        Public Property sUnit As String = ""
        Public Property sTimeStamp As String = "" ' etap 2: kiedy
        Public Property sLogoUri As String = "" ' logo, np. Airly etc., ktore warto pokazywac
        Public Property sSensorDescr As String = "" ' opis (np. krakówoddycha)
        Public Property sAdres As String = ""  ' adres (postal address)
        ' Public Property sJedn As String
        Public Property sOdl As String = ""
        Public Property sAddit As String = ""
        Public Property bDel As Boolean = False
        Public Property sAlert As String = ""
        Public Property bCleanAir As Boolean = True ' 2021.01.28
        Public Property sLimity As String = ""

    Public Sub New(sSourceName As String)
        sSource = sSourceName
    End Sub

    ''' <summary>
    ''' potrzebny do XML serializer na pewno - bez tego nie ruszy; JSON chyba też :)
    ''' </summary>
    Public Sub New()

    End Sub
    Private Shared Function Wykrzyknikuj(dCurrent As Double, dJeden As Double, dDwa As Double, dTrzy As Double) As String
            If dCurrent < dJeden Then Return ""
            If dCurrent < dDwa Then Return "!"
            If dCurrent < dTrzy Then Return "!!"
            Return "!!!"
        End Function

        Private Shared Function PoziomDopuszczalnyPL(sPomiar As String) As String
        ' http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
        DumpCurrMethod()
        Select Case sPomiar
            Case "PM₂₅"
                Return "Poziom dopuszalny (średnia roczna): 25 μg/m³ od 2015, 20 μg/m³ od 2020" & vbLf
            Case "PM₁₀"
                Return "Poziom dopuszalny (od 2005): średnia roczna 40 μg/m³, dobowa 50 μg/m³" & vbLf
            Case "C₆H₆"
                Return "Poziom dopuszalny (średnia roczna): 5 μg/m³, od 2010" & vbLf
            Case "NO₂"
                Return "Poziom dopuszalny (od 2010): 40 μg/m³ średnia roczna, 200 μg/m³ dobowa" & vbLf
            Case "NOx"
                Return "Poziom dopuszalny (średnia roczna): 30 μg/m³ od 2003" & vbLf
            Case "SO₂"
                Return "Poziom dopuszalny: 125 μg/m³ (średnia dobowa), 350 μg/m³ (godzinna), od 2005" & vbLf
            Case "Pb"
                Return "Poziom dopuszalny (średnia roczna): 0.5 μg/m³, od 2005" & vbLf
            Case "CO"
                Return "Poziom dopuszalny (średnia 8 godzinna): 10 000 μg/m³, od 2005" & vbLf
        End Select

        Return ""
        End Function

        Private Shared Function PoziomDocelowyPL(sPomiar As String) As String
        ' http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
        DumpCurrMethod()
        Select Case sPomiar
            Case "As"
                Return "Poziom docelowy (do 2013): 6 ng/m³ (średnia roczna)" & vbLf
            Case "benzoapiren"
                Return "Poziom docelowy (do 2013): 1 ng/m³ (średnia roczna)" & vbLf
            Case "Cd"
                Return "Poziom docelowy (do 2013): 5 ng/m³ (średnia roczna)" & vbLf
            Case "Ni"
                Return "Poziom docelowy (do 2013): 20 ng/m³ (średnia roczna)" & vbLf
            Case "O₃"
                Return "Poziom docelowy (do 2010): 120 μg/m³ (średnia 8 godzinna), okres wegetacji (1 V - 31 VII): 18 000" & "Poziom długoterminowy (do 2020): 120/6000" & vbLf
            Case "PM₂₅"
                Return "Poziom docelowy (do 2010): 25 μg/m³ (średnia roczna)" & vbLf
        End Select

        Return ""
        End Function

        Private Shared Function PoziomAlarmuPL(sPomiar As String) As String
        ' http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
        DumpCurrMethod()
        Select Case sPomiar
            Case "NO₂"
                Return "Poziom alarmowania: 400 μg/m³ średnia godzinna" & vbLf
            Case "SO₂"
                Return "Poziom alarmowania: 500 μg/m³ średnia godzinna" & vbLf
            Case "O₃"
                Return "Poziom alarmowania: 240 μg/m³ średnia godzinna" & vbLf
            Case "PM₁₀"
                Return "Poziom alarmowania: 400 μg/m³ średnia dobowa" & vbLf
        End Select

        Return ""
        End Function

        Private Shared Function PoziomInformowaniaPL(sPomiar As String) As String
        ' http://prawo.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20120001031
        DumpCurrMethod()

        Select Case sPomiar
            Case "O₃"
                Return "Poziom informowania: 180 μg/m³ średnia godzinna" & vbLf
            Case "PM₁₀"
                Return "Poziom informowania: 200 μg/m³ średnia dobowa" & vbLf
        End Select

        Return ""
        End Function

    Private Shared Function PoziomyWHO(sPomiar As String) As String
        ' https://www.who.int/news-room/fact-sheets/detail/ambient-(outdoor)-air-quality-and-health

        DumpCurrMethod()

        Select Case sPomiar
            Case "PM₂₅"
                Return "Limit WHO 2005: 10 μg/m³ (średnia roczna), 25 μg/m³ (średnia dobowa)" & vbLf & "Limit WHO 2021: 5 μg/m³ (średnia roczna 2006), 15 μg/m³ (średnia dobowa)" & vbLf
            Case "PM₁₀"
                Return "Limit WHO 2005: 20 μg/m³ (średnia roczna), 50 μg/m³ (średnia dobowa)" & vbLf & "Limit WHO 2021: 15 μg/m³ (średnia roczna), 45 μg/m³ (średnia dobowa)" & vbLf
            Case "O₃"
                Return "Limit WHO: 100 μg/m³ (średnia 8-godzinna)" & vbLf
            Case "NO₂"
                Return "Limit WHO 2005: 40 μg/m³ (średnia roczna), 200 μg/m³ (średnia godzinna)" & vbLf & "Limit WHO 2021: 20 μg/m³ (średnia roczna), 25 μg/m³ (średnia dobowa)" & vbLf
            Case "SO₂"
                Return "Limit WHO 2005: 20 μg/m³ (średnia dobowa), 500 μg/m³ (średnia 10-minutowa)" & vbLf & "Limit WHO 2021: 40 μg/m³ (średnia dobowa)" & vbLf
            Case "CO"
                Return "Limit WHO 2021: 4 mg/m³ (średnia dobowa)" & vbLf
        End Select

        Return ""
    End Function

    Private Sub DodajPrzekroczeniaEU()
        DumpCurrMethod()

        Select Case sPomiar
            Case "PM₁"
            Case "PM₂₅"
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(25), CDbl(1000), CDbl(2000))
            Case "PM₁₀"
                ' 20 μg/m³ średnia roczna, 50 μg/m³ średnia godzinna
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(40), CDbl(200), CDbl(400))
            Case "μSv/h"
            Case "C₆H₆"
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(5), CDbl(1000), CDbl(2000))
            Case "SO₂"
                ' 20 μg/m³ średnia dobowa, 500 μg/m³ średnia 10 minutowa
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(125), CDbl(125), CDbl(500))
            Case "NO₂"
                ' 40 μg/m³ średnia roczna, 200 μg/m³ średnia godzinna
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(40), CDbl(40), CDbl(400))
            Case "O₃"
                ' 100 μg/m³ średnia 8 h
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(120), CDbl(180), CDbl(240))
            Case Else
                sAlert = ""
        End Select

    End Sub

    Private Sub DodajPrzekroczeniaWHO()
        DumpCurrMethod()

        Select Case sPomiar
            Case "PM₁"
            Case "PM₂₅"
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(10), CDbl(25), CDbl(50))
            Case "PM₁₀"
                ' 20 μg/m³ średnia roczna, 50 μg/m³ średnia godzinna
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(20), CDbl(50), CDbl(100))
            Case "μSv/h"
            Case "C₆H₆" ' to jest nie WHO, bo WHO nie ma!
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(5), CDbl(1000), CDbl(1000))
            Case "SO₂"
                ' 20 μg/m³ średnia dobowa, 500 μg/m³ średnia 10 minutowa
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(20), CDbl(500), CDbl(1000))
            Case "NO₂"
                ' 40 μg/m³ średnia roczna, 200 μg/m³ średnia godzinna
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(40), CDbl(200), CDbl(400))
            Case "O₃"
                ' 100 μg/m³ średnia 8 h
                sAlert = Wykrzyknikuj(dCurrValue, CDbl(100), CDbl(100), CDbl(200))
            Case Else
                sAlert = ""
        End Select

    End Sub

    Private Sub DodajPrzekroczeniaWHO2021()
        DumpCurrMethod()

        Select Case sPomiar
                Case "PM₂₅"
                    sAlert = Wykrzyknikuj(dCurrValue, 5, 15, 50)
                Case "PM₁₀"
                    sAlert = Wykrzyknikuj(dCurrValue, 15, 45, 100)
                Case "C₆H₆" ' to jest nie WHO, bo WHO nie ma!
                    sAlert = Wykrzyknikuj(dCurrValue, 5, 1000, 1000)
                Case "SO₂"
                    sAlert = Wykrzyknikuj(dCurrValue, 40, 500, 1000)
                Case "NO₂"
                    sAlert = Wykrzyknikuj(dCurrValue, 10, 25, 400)
                Case "O₃"
                    sAlert = Wykrzyknikuj(dCurrValue, 100, 100, 200)
                Case "CO"
                    sAlert = Wykrzyknikuj(dCurrValue, 4, 40, 100)
                Case Else
                    sAlert = ""
            End Select

        End Sub

    Public Sub DodajPrzekroczenia()
        ' p.k.GetSettingsInt("uiLimitWgCombo", 0)
        DumpCurrMethod()

        ' http://ec.europa.eu/environment/air/quality/standards.htm

        If sSource <> "gios" AndAlso sSource <> "airly" AndAlso sSource <> "EEAair" Then Return
        sLimity = PoziomyWHO(sPomiar) & PoziomDocelowyPL(sPomiar) & PoziomDopuszczalnyPL(sPomiar) & PoziomInformowaniaPL(sPomiar) & PoziomAlarmuPL(sPomiar)

        Select Case GetSettingsInt("uiLimitWgCombo") ' numery: index w uiLimitWgCombo w Settings
            Case 0 ' EU
                DodajPrzekroczeniaEU()
            Case 1 ' WHO
                DodajPrzekroczeniaWHO()
            Case 2 'WHO 2021
                DodajPrzekroczeniaWHO2021()
        End Select

        If sAlert.Contains("!") Then bCleanAir = False    ' 2021.01.28
    End Sub


End Class


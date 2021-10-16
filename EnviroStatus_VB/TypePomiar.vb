

' JedenSensor - czyli pomiar danego parametru, zeby wiadomo bylo skad go brac
' lista sensorow najblizszych: Source_XXX.GetNearest
' to potem do posortowania (i znalezienia najblizszych dla kazdego pomiaru)
Public Class JedenPomiar
    Public Property sSource As String =""  ' np. airly
    Public Property sId As String = ""     ' interpretowane przez klasę airly
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
    Public Property sLimity As String = ""
End Class

'Public Class JedenPomiar
'    Public Property sName As String
'    Public Property sCurrValue As String
'    Public Property sSource As String
'    Public Property dLon As Double
'    Public Property dLat As Double
'    Public Property sTimeStamp As String
'    Public Property dOdl As Double
'End Class


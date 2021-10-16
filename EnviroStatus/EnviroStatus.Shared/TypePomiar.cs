

// JedenSensor - czyli pomiar danego parametru, zeby wiadomo bylo skad go brac
// lista sensorow najblizszych: Source_XXX.GetNearest
// to potem do posortowania (i znalezienia najblizszych dla kazdego pomiaru)
namespace EnviroStatus
{
    public class JedenPomiar
    {
        public string sSource { get; set; } = "";  // np. airly
        public string sId { get; set; } = "";     // interpretowane przez klasę airly
        public double dLon { get; set; } = 0;    // lokalizacja sensora
        public double dLat { get; set; } = 0;
        public double dWysok { get; set; } = 0;
        public double dOdl { get; set; } = 0;    // odleglosc - wazne przy sprawdzaniu ktory najblizszy
        public string sPomiar { get; set; } = ""; // jaki pomiar (np. PM10)
        public string sCurrValue { get; set; } = ""; // etap 2: wartosc
        public double dCurrValue { get; set; } = 0;
        public string sUnit { get; set; } = "";
        public string sTimeStamp { get; set; } = ""; // etap 2: kiedy
        public string sLogoUri { get; set; } = ""; // logo, np. Airly etc., ktore warto pokazywac
        public string sSensorDescr { get; set; } = ""; // opis (np. krakówoddycha)
        public string sAdres { get; set; } = "";  // adres (postal address)
                                                  // Public Property sJedn As String
        public string sOdl { get; set; } = "";
        public string sAddit { get; set; } = "";
        public bool bDel { get; set; } = false;
        public string sAlert { get; set; } = "";
        public bool bCleanAir { get; set; } = true; // 2021.01.28
        public string sLimity { get; set; } = "";
    }
}

// Public Class JedenPomiar
// Public Property sName As String
// Public Property sCurrValue As String
// Public Property sSource As String
// Public Property dLon As Double
// Public Property dLat As Double
// Public Property sTimeStamp As String
// Public Property dOdl As Double
// End Class



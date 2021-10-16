// dodając:
// App.xaml.vb końcówka: dopisac do listy w tabelce
// App.xaml.vb DodajPrzekroczenia - jesli to ma byc w App, a nie podczas odczytywania

// juz zrobione (przez zrobienie gaSrc):
// App.xaml.vb końcówka, ReadResStrings: odczytywanie ewentualnych zmiennych tekstów
// App.xaml.vb GetFavData - odwolanie do source przy wczytywaniu
// App.xaml.vb SourcesUsedInTimer - jesli to jest uzywane w timer, to dodac
// MainPage.xaml.vb uiStore_Click - dodawanie template (na wszelki wypadek, jakby potem sie dodawało template)
// MainPage.xaml.vb uiGPS_Click - odczytywanie danych
// Zrodelka.xaml.vb


using Windows.Foundation;
//using System.Threading.Tasks;
using Windows.UI.Xaml;
using System.IO;
using System.Collections.ObjectModel;
using System;
using Windows.UI.Xaml.Controls;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public abstract partial class Source_Base
    {
        // ułatwienie dodawania następnych
        protected abstract string SRC_SETTING_NAME { get;  }
        protected abstract string SRC_SETTING_HEADER { get;  }
        protected abstract string SRC_RESTURI_BASE { get;  }
        protected abstract string SRC_URI_ABOUT_EN { get; }
        protected abstract string SRC_URI_ABOUT_PL { get; }
        protected virtual string SRC_RESTURI_BASE_PKAR { get; } = "";
        public abstract string SRC_POMIAR_SOURCE { get;  }
        protected virtual bool SRC_DEFAULT_ENABLE { get; } = false;
        protected virtual bool SRC_HAS_KEY { get; set; } = false;
        protected virtual string SRC_KEY_LOGIN_LINK { get; } = "";
        protected virtual bool SRC_HAS_TEMPLATES { get; } = false;
        public virtual bool SRC_IN_TIMER { get;  } = false;
        protected virtual string SRC_MY_KEY { get; } = "";
        public virtual bool SRC_NO_COMPARE { get; } = false;

        protected virtual async System.Threading.Tasks.Task<string> GetREST(string sCommand)
        {
            // przeciez jest rownolegle wiele serwerow odpytywanych, wiec sie przeplataja
            string sCurrUri = sCommand;
            if (sCurrUri.Length > 20)
                sCurrUri = sCurrUri.Substring(0, 20);
            string mSeenUri = p.k.GetSettingsString("seenUri");

            if (mSeenUri.Contains("|" + sCurrUri + "|"))
                await System.Threading.Tasks.Task.Delay(100);   // nie wolno zasypywac serwera
            else
                p.k.SetSettingsString("seenUri", mSeenUri + "|" + sCurrUri + "|");

            // Windows.Web.Http.HttpClient oHttp = new Windows.Web.Http.HttpClient();
            System.Net.Http.HttpClient oHttp = new System.Net.Http.HttpClient();

            if (SRC_HAS_KEY)
            {
                string sKey;
                sKey = p.k.GetSettingsString(SRC_SETTING_NAME + "_apikey");
                if (sKey.ToLower() == PrivateSwitch && p.k.IsThisMoje())
                {
                    sKey = SRC_MY_KEY;
                    p.k.SetSettingsString(SRC_SETTING_NAME + "_apikey", sKey);
                }

                if (SRC_POMIAR_SOURCE == "DarkSky")
                {
                    sCommand = sKey + "/" + sCommand;
                }
                
                if( SRC_POMIAR_SOURCE == "airly")
                {
                    oHttp.DefaultRequestHeaders.Add("Accept", "application/json");
                    oHttp.DefaultRequestHeaders.Add("apikey", sKey);
                }
            }

            Uri oUri;
            if (p.k.IsThisMoje() && !string.IsNullOrEmpty(SRC_RESTURI_BASE_PKAR))
                oUri = new Uri(SRC_RESTURI_BASE_PKAR + sCommand);
            else
                oUri = new Uri(SRC_RESTURI_BASE + sCommand);

            oHttp.Timeout = TimeSpan.FromSeconds(10);   // domyslnie jest 100 sekund

            string oRes = "";
            try
            {
                oRes = await oHttp.GetStringAsync(oUri);
            }
            catch 
            {
                oRes = "";
            }

            return oRes;
        }

        protected Collection<EnviroStatus.JedenPomiar> moListaPomiarow = null;

        // Public MustOverride Async Function GetNearest(oPos As Point, dMaxOdl As Double) As Task(Of Collection(Of JedenPomiar))

        public abstract  System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetNearest(Windows.Devices.Geolocation.BasicGeoposition oPos);

        public abstract  System.Threading.Tasks.Task<Collection<EnviroStatus.JedenPomiar>> GetDataFromFavSensor(string sId, string sAddit, bool bInTimer);

        public virtual void ReadResStrings()
        {
        }

        //protected bool InsidePoland(Windows.Devices.Geolocation.BasicGeoposition oPos)
        //{// https://pl.wikipedia.org/wiki/Geometryczny_%C5%9Brodek_Polski

        //    double dOdl;
            
        //    dOdl = p.k.GPSdistanceDwa(oPos.X, oPos.Y, 52.2159333, 19.1344222);
        //    if (dOdl / 1000 > 500) return false;

        //    return true;    // ale to nie jest pewne, tylko: "możliwe"
        //}

        public virtual async System.Threading.Tasks.Task SaveFavTemplate()
        {
            if (!SRC_HAS_TEMPLATES)
                return;

            // zapisz dane template sensorów
            string sNums = "";
            foreach (EnviroStatus.JedenPomiar oItem in EnviroStatus.App.moPomiaryAll)
            {
                if (!oItem.bDel && (oItem.sSource ?? "") == (SRC_POMIAR_SOURCE ?? ""))
                {
                    if (sNums.IndexOf(oItem.sId + "|") < 0)
                    {
                        string sFileName = SRC_POMIAR_SOURCE + "_" + oItem.sId + ".xml";
                        if ((SRC_POMIAR_SOURCE ?? "") == "IMGWmet")
                            sFileName = "IMGWmeteo" + "_" + oItem.sId + ".xml";
                        else
                            sFileName = SRC_POMIAR_SOURCE + "_" + oItem.sId + ".xml";
                        var oFile = await EnviroStatus.App.GetDataFile(false, SRC_POMIAR_SOURCE + "_" + oItem.sId + ".xml", true);
                        if (oFile == null)
                            return ;

                        var oSer = new System.Xml.Serialization.XmlSerializer(typeof(JedenPomiar));
                        var oStream = await oFile.OpenStreamForWriteAsync();
                        oSer.Serialize(oStream, oItem);
                        oStream.Dispose();   // == fclose

                        sNums = sNums + oItem.sId + "|";
                    }
                }
            }


        }

        public virtual void ConfigCreate(StackPanel oStack)
        {
            // potrzebne, gdy nie ma Headerow w ToggleSwitchach
            //if (!p.k.GetPlatform("uwp"))
            //{
            //    var oTH = new TextBlock();
            //    oTH.Text = SRC_SETTING_HEADER;
            //    oStack.Children.Add(oTH);
            //}

            var oTS = new ToggleSwitch();
            oTS.Header = SRC_SETTING_HEADER;
            oTS.Name = "uiConfig_" + SRC_SETTING_NAME;
            oTS.IsOn = p.k.GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE);
            oStack.Children.Add(oTS);

            if (!SRC_HAS_KEY)
                return;

            Windows.UI.Xaml.Data.Binding oBind = new Windows.UI.Xaml.Data.Binding();
            oBind.ElementName = oTS.Name;
            oBind.Path = new PropertyPath("IsOn");

            var oTBox = new TextBox();
            oTBox.Header = SRC_SETTING_HEADER + " API key";
            oTBox.Name = "uiConfig_" + SRC_SETTING_NAME + "_Key";
            oTBox.Text = p.k.GetSettingsString(SRC_SETTING_NAME + "_apikey");
            oTBox.SetBinding(TextBox.IsEnabledProperty, oBind);

            oStack.Children.Add(oTBox);
            var oLink = new HyperlinkButton();
            oLink.Content = p.k.GetLangString("msgForAPIkey"); // "Aby uzyskać API key, zarejestruj się"
            oLink.NavigateUri = new Uri(SRC_KEY_LOGIN_LINK);
            oStack.Children.Add(oLink);
        }

        public virtual string ConfigDataOk(StackPanel oStack)
        {
            // jesli nie ma Key, to na pewno poprawne
            if (!SRC_HAS_KEY)
                return "";

            // jesli nie jest wlaczone, to tez jest poprawnie
            foreach (UIElement oItem in oStack.Children)
            {
                ToggleSwitch oTS;
                oTS = oItem as ToggleSwitch;
                if (oTS != null)
                {
                    if ((oTS.Name ?? "") == ("uiConfig_" + SRC_SETTING_NAME ?? ""))
                    {
                        if (!oTS.IsOn)
                            return "";
                    }
                }
            }

            foreach (UIElement oItem in oStack.Children)
            {
                TextBox oTB;
                oTB = oItem as TextBox;
                if (oTB != null)
                {
                    if ((oTB.Name ?? "") == ("uiConfig_" + SRC_SETTING_NAME + "_Key" ?? ""))
                    {
                        if (oTB.Text.Length > 8)
                            return "";
                        return "Too short API key";
                    }
                }
            }

            return "UIError - no API key";
        }

        public virtual void ConfigRead(StackPanel oStack)
        {
            foreach (UIElement oItem in oStack.Children)
            {
                ToggleSwitch oTS;
                oTS = oItem as ToggleSwitch;
                if (oTS != null)
                {
                    if ((oTS.Name ?? "") == ("uiConfig_" + SRC_SETTING_NAME ?? ""))
                    {
                        p.k.SetSettingsBool(SRC_SETTING_NAME, oTS.IsOn);
                        break;
                    }
                }
            }

            if (!SRC_HAS_KEY)
                return;

            // tylko gdy jest wlaczony
            foreach (UIElement oItem in oStack.Children)
            {
                TextBox oTB;
                oTB = oItem as TextBox;
                if (oTB != null)
                {
                    if ((oTB.Name ?? "") == ("uiConfig_" + SRC_SETTING_NAME + "_Key" ?? ""))
                    {
                        p.k.SetSettingsString(SRC_SETTING_NAME + "_apikey", oTB.Text, true);
                        break;
                    }
                }
            }
        }

        public string Odleglosc2String(double dOdl)
        {
            if (dOdl < 10000)
                return dOdl.ToString() + " m";
            return (dOdl / 1000).ToString() + " km";
        }
    }
}

static partial class Extensions
{
    // z JVALUE

    public static string GetNamedString(this Newtonsoft.Json.Linq.JValue jVal, string sName, string sDefault="")
    {
        string sTmp;
        try
        {
            sTmp = jVal[sName].ToString();
        }
        catch
        {
            sTmp = sDefault;
        }
        return sTmp;
    }
    public static double GetNamedNumber(this Newtonsoft.Json.Linq.JValue jVal, string sName, double sDefault = 0)
    {
        double sTmp;
        try
        {
            sTmp = (double)jVal[sName];
        }
        catch
        {
            sTmp = sDefault;
        }
        return sTmp;
    }

    public static Newtonsoft.Json.Linq.JValue GetNamedValue(this Newtonsoft.Json.Linq.JValue jVal, string sName)
    {
        return jVal[sName] as Newtonsoft.Json.Linq.JValue;
    }

    public static Newtonsoft.Json.Linq.JArray GetNamedArray(this Newtonsoft.Json.Linq.JValue jVal, string sName)
    {
        return jVal[sName] as Newtonsoft.Json.Linq.JArray;
    }

    public static Newtonsoft.Json.Linq.JValue GetObject(this Newtonsoft.Json.Linq.JValue jVal)
    {
        return jVal;
    }

    //  z JTOKEN
    public static string GetNamedString(this Newtonsoft.Json.Linq.JToken jVal, string sName, string sDefault = "")
    {
        string sTmp;
        try
        {
            sTmp = jVal[sName].ToString();
        }
        catch
        {
            sTmp = sDefault;
        }
        return sTmp;
    }
    public static double GetNamedNumber(this Newtonsoft.Json.Linq.JToken jVal, string sName, double sDefault = 0)
    {
        
        double sTmp;
        try
        {
            if (jVal[sName] == null) return sDefault;
            sTmp = (double)jVal[sName];
        }
        catch
        {
            sTmp = sDefault;
        }
        return sTmp;
    }

    public static Newtonsoft.Json.Linq.JValue GetNamedValue(this Newtonsoft.Json.Linq.JToken jVal, string sName)
    {
        return jVal[sName] as Newtonsoft.Json.Linq.JValue;
    }

    public static Newtonsoft.Json.Linq.JArray GetNamedArray(this Newtonsoft.Json.Linq.JToken jVal, string sName)
    {
        return jVal[sName] as Newtonsoft.Json.Linq.JArray;
    }

    public static Newtonsoft.Json.Linq.JToken GetNamedToken(this Newtonsoft.Json.Linq.JToken jVal, string sName, string sDefault = "")
    {
        return jVal[sName];
    }

    public static Newtonsoft.Json.Linq.JToken GetObject(this Newtonsoft.Json.Linq.JToken jVal)
    {
        return jVal;
    }



    // z JOBJECT

    public static string GetNamedString(this Newtonsoft.Json.Linq.JObject jVal, string sName, string sDefault = "")
    {
        string sTmp;
        try
        {
            sTmp = jVal[sName].ToString();
        }
        catch
        {
            sTmp = sDefault;
        }
        return sTmp;
    }
    public static Newtonsoft.Json.Linq.JToken GetNamedToken(this Newtonsoft.Json.Linq.JObject jVal, string sName, string sDefault = "")
    {
        return jVal[sName];
    }
    public static double GetNamedNumber(this Newtonsoft.Json.Linq.JObject jVal, string sName, double sDefault = 0)
    {
        double sTmp;
        try
        {
            sTmp = (double)jVal[sName];
        }
        catch
        {
            sTmp = sDefault;
        }
        return sTmp;
    }

    public static Newtonsoft.Json.Linq.JValue GetNamedValue(this Newtonsoft.Json.Linq.JObject jVal, string sName)
    {
        return jVal[sName] as Newtonsoft.Json.Linq.JValue;
    }
    public static Newtonsoft.Json.Linq.JObject GetNamedObject(this Newtonsoft.Json.Linq.JObject jVal, string sName)
    {
        return jVal[sName] as Newtonsoft.Json.Linq.JObject;
    }

    public static Newtonsoft.Json.Linq.JArray GetNamedArray(this Newtonsoft.Json.Linq.JObject jVal, string sName)
    {
        return jVal[sName] as Newtonsoft.Json.Linq.JArray;
    }
    public static bool GetNamedBool(this Newtonsoft.Json.Linq.JObject jVal, string sName)
    {
        return (bool)jVal[sName];
    }

    public static Newtonsoft.Json.Linq.JObject GetObject(this Newtonsoft.Json.Linq.JObject jVal)
    {
        return jVal;
    }

    // e, nie działa, bo musi być this, czyli dopiero dla zmiennej double może zadziałać?
    public static double ParseDefault(String sStr, double dDefault)
    {
        double dDouble;
        if (!double.TryParse(sStr, out dDouble))
            dDouble = dDefault;
        return dDouble;
    }

}


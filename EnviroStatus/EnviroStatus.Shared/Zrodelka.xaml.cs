// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

/// <summary>

/// An empty page that can be used on its own or navigated to within a Frame.

/// </summary>
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;
using VBlib;

//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public sealed partial class Zrodelka : Page
    {
        public Zrodelka()
        {
            this.InitializeComponent();
        }

        private ComboBoxItem GetZasiegComboItem(int zasiegSensora, int currentZasieg)
        {
            var oNew = new ComboBoxItem();
            oNew.Content = vb14.GetLangString("uiZasieg" + zasiegSensora.ToString());
            oNew.DataContext = zasiegSensora;
            if (currentZasieg == zasiegSensora) oNew.IsSelected = true;
            return oNew;
        }

        private bool bInInitPage = true;

        private void InitZasiegCombo(int currentZasieg)
        {
            uiZasieg.Items.Clear();
            uiZasieg.Items.Add(GetZasiegComboItem((int)Zasieg.World, currentZasieg));
            uiZasieg.Items.Add(GetZasiegComboItem((int)Zasieg.Europe, currentZasieg));
            uiZasieg.Items.Add(GetZasiegComboItem((int)Zasieg.Poland, currentZasieg));
            if(p.k.IsThisMoje())
                uiZasieg.Items.Add(GetZasiegComboItem((int)Zasieg.Prywatne, currentZasieg));
        }

        private void uiZasieg_Changed(object sender, RoutedEventArgs e)
        {
            if (bInInitPage) return;

            if (uiZasieg.SelectedItem is null) return;
            var oFE = uiZasieg.SelectedItem as FrameworkElement;
            if (oFE is null) return;
            if (oFE.DataContext is null) return;

            int currentZasieg = (int)oFE.DataContext;
            vb14.SetSettingsInt("zasieg", currentZasieg);

            PokazUstawienia(currentZasieg);
        }


        private void PokazUstawienia(int currentZasieg)
        {
            uiStackConfig.Children.Clear();

            foreach (VBlib.Source_Base oZrodlo in VBlib.App.gaSrc)
                if ((int)oZrodlo.SRC_ZASIEG <= currentZasieg)
                    FromSrc_ConfigCreate(uiStackConfig, oZrodlo);

            var oButton = new Button();
            oButton.Content = vb14.GetLangString("uiSettingsSave.Content");
            if ((oButton.Content.ToString() == "uiSettingsSave.Content")
                || (oButton.Content.ToString() == ""))
                oButton.Content = "Save!";
            oButton.HorizontalAlignment = HorizontalAlignment.Center;
            oButton.Click += uiSave_ClickEvent;
            // oButton.AddHandler(Button.cl)
            uiStackConfig.Children.Add(oButton);
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bInInitPage = true;

            int currentZasieg = vb14.GetSettingsInt("zasieg");
            InitZasiegCombo(currentZasieg);

            PokazUstawienia(currentZasieg);

            bInInitPage = false;
        }

        private void uiSave_ClickEvent(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { uiSave_Click(); });
#pragma warning restore CS4014 
        }

        private string VerifyDataOK()
        {
            string sMsg = "";
            // ewentualnie sprawdzanie zasiegu - ale właściwie to po co?
            foreach (VBlib.Source_Base oZrodlo in VBlib.App.gaSrc)
            {
                sMsg = FromSrc_ConfigDataOk(uiStackConfig, oZrodlo);
                if (!string.IsNullOrEmpty(sMsg))
                    return sMsg;
            }

            return "";
        }

        private async System.Threading.Tasks.Task WylaczPozaZasiegiemAsync(StackPanel oStack)
        {
            bool isEnabled = false;
            int currentZasieg = vb14.GetSettingsInt("zasieg");

            foreach (VBlib.Source_Base oZrodlo in VBlib.App.gaSrc)
            {
                if ((int)oZrodlo.SRC_ZASIEG > currentZasieg)
                {
                    foreach (UIElement oItem in oStack.Children)
                    {
                        ToggleSwitch oTS;
                        oTS = oItem as ToggleSwitch;
                        if (oTS != null)
                        {
                            if (oTS.Name == "uiConfig_" + oZrodlo.SRC_SETTING_NAME)
                            {
                                isEnabled = true;
                                break;
                            }
                        }
                    }
                }
                if (isEnabled) break;
            }

            if (!isEnabled) return;

            if(!await vb14.DialogBoxResYNAsync("msgWylaczycPozaZasiegiem")) return;

            foreach (VBlib.Source_Base oZrodlo in VBlib.App.gaSrc)
            {
                foreach (UIElement oItem in oStack.Children)
                {
                    ToggleSwitch oTS;
                    oTS = oItem as ToggleSwitch;
                    if (oTS != null)
                    {
                        if (oTS.Name == "uiConfig_" + oZrodlo.SRC_SETTING_NAME && (int)oZrodlo.SRC_ZASIEG > currentZasieg)
                        {
                            oTS.IsOn = false;
                        }
                    }
                }

            }


        }

        private  async void uiSave_Click(object sender = null, RoutedEventArgs e = null)
        {
            string sMsg = VerifyDataOK();
            if (!string.IsNullOrEmpty(sMsg))
            {
                vb14.DialogBox(sMsg);
                return;
            }

            // jeśli coś poza zasięgiem jest włączone, to zapytaj czy wyłączyć
            await WylaczPozaZasiegiemAsync(uiStackConfig);

            foreach (VBlib.Source_Base oZrodlo in VBlib.App.gaSrc)
                FromSrc_ConfigRead(uiStackConfig, oZrodlo);

            vb14.SetSettingsBool("wasSetup",true);

            Frame.GoBack();
        }


        private string FromSrc_ConfigDataOk(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            // wszystkie były takie same

            // jesli nie ma Key, to na pewno poprawne
            if (!oZrodlo.SRC_HAS_KEY)
                return "";

            // jesli nie jest wlaczone, to tez jest poprawnie
            foreach (UIElement oItem in oStack.Children)
            {
                ToggleSwitch oTS;
                oTS = oItem as ToggleSwitch;
                if (oTS != null)
                {
                    if (oTS.Name == "uiConfig_" + oZrodlo.SRC_SETTING_NAME)
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
                    if (oTB.Name == "uiConfig_" + oZrodlo.SRC_SETTING_NAME + "_Key" )
                    {
                        if (oTB.Text.Length > 8)
                            return "";
                        return "Too short API key";
                    }
                }
            }

            return "UIError - no API key";
        }

        public void FromSrc_ConfigCreate(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            // przełącznik wedle zrδdła - czyli dla kopii z klas, lub default: to co w Source_Base było
            switch (oZrodlo.SRC_POMIAR_SOURCE)
            {
                case "SeismicEU":
                    FromSrcSeismic_ConfigCreate(oStack, oZrodlo);
                    break;
                case "IMGWmet":
                    FromSrcIMGWmet_ConfigCreate(oStack, oZrodlo);
                    break;
                case "IMGWhyd":
                    FromSrcIMGWhyd_ConfigCreate(oStack, oZrodlo);
                    break;
                default:
                    FromSrcBase_ConfigCreate(oStack, oZrodlo);
                    break;
            }
        }

        public void FromSrc_ConfigRead(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            // przełącznik wedle zrδdła - czyli dla kopii z klas, lub default: to co w Source_Base było
            switch (oZrodlo.SRC_POMIAR_SOURCE)
            {
                case "SeismicEU":
                    FromSrcSeismic_ConfigRead(oStack, oZrodlo);
                    break;
                case "IMGWmet":
                    FromSrcIMGWmet_ConfigRead(oStack, oZrodlo);
                    break;
                case "IMGWhyd":
                    FromSrcIMGWhyd_ConfigRead(oStack, oZrodlo);
                    break;

                default:
                    FromSrcBase_ConfigRead(oStack, oZrodlo);
                    break;
            }
        }


        public void FromSrcBase_ConfigCreate(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            // to co było w Source_Base (domyślne)

            // potrzebne, gdy nie ma Headerow w ToggleSwitchach
            //if (!p.k.GetPlatform("uwp"))
            //{
            //    var oTH = new TextBlock();
            //    oTH.Text = SRC_SETTING_HEADER;
            //    oStack.Children.Add(oTH);
            //}

            var oTB = new TextBlock();
            oTB.Margin = new Thickness(1, 15, 1, 0);   // odstęp
            oTB.Text = oZrodlo.SRC_SETTING_HEADER;
            oTB.FontSize = 16;
            oTB.FontWeight = Windows.UI.Text.FontWeights.Bold;
            oStack.Children.Add(oTB);

            var oLnk = new HyperlinkButton();
            oLnk.Content = vb14.GetLangString("msgAboutLink");
            oLnk.Margin = new Thickness(1, 0, 1, 2);   // odstęp
            oLnk.NavigateUri = oZrodlo.GetAboutUri();
            oStack.Children.Add(oLnk);

            var oTS = new ToggleSwitch();
            // oTS.Header = oZrodlo.SRC_SETTING_HEADER;
            oTS.Name = "uiConfig_" + oZrodlo.SRC_SETTING_NAME;
            oTS.IsOn = vb14.GetSettingsBool(oZrodlo.SRC_SETTING_NAME, oZrodlo.SRC_DEFAULT_ENABLE);
            oStack.Children.Add(oTS);

            if (!oZrodlo.SRC_HAS_KEY)
                return;

            Windows.UI.Xaml.Data.Binding oBind = new Windows.UI.Xaml.Data.Binding();
            oBind.ElementName = oTS.Name;
            oBind.Path = new PropertyPath("IsOn");

            var oTBox = new TextBox();
            oTBox.Header = oZrodlo.SRC_SETTING_HEADER + " API key";
            oTBox.Name = "uiConfig_" + oZrodlo.SRC_SETTING_NAME + "_Key";
            oTBox.Text = vb14.GetSettingsString(oZrodlo.SRC_SETTING_NAME + "_apikey");
            oTBox.SetBinding(TextBox.IsEnabledProperty, oBind);

            oStack.Children.Add(oTBox);
            var oLink = new HyperlinkButton();
            oLink.Content = vb14.GetLangString("msgForAPIkey"); // "Aby uzyskać API key, zarejestruj się"
            oLink.NavigateUri = new Uri(oZrodlo.SRC_KEY_LOGIN_LINK);
            oStack.Children.Add(oLink);
        }


        public void FromSrcBase_ConfigRead(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            // to co było w Source_Base (domyślne)

            foreach (UIElement oItem in oStack.Children)
            {
                ToggleSwitch oTS;
                oTS = oItem as ToggleSwitch;
                if (oTS != null)
                {
                    if (oTS.Name == "uiConfig_" + oZrodlo.SRC_SETTING_NAME )
                    {
                        vb14.SetSettingsBool(oZrodlo.SRC_SETTING_NAME, oTS.IsOn);
                        break;
                    }
                }
            }

            if (!oZrodlo.SRC_HAS_KEY)
                return;

            // tylko gdy jest wlaczony
            foreach (UIElement oItem in oStack.Children)
            {
                TextBox oTB;
                oTB = oItem as TextBox;
                if (oTB != null)
                {
                    if ((oTB.Name ?? "") == ("uiConfig_" + oZrodlo.SRC_SETTING_NAME + "_Key" ?? ""))
                    {
                        vb14.SetSettingsString(oZrodlo.SRC_SETTING_NAME + "_apikey", oTB.Text, true);
                        break;
                    }
                }
            }
        }

        #region SeismicPortal
        public void FromSrcSeismic_uiSettDistance_Changed(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Windows.UI.Xaml.Controls.Slider oSld;
            oSld = sender as Windows.UI.Xaml.Controls.Slider;
            if (oSld != null)
            {
                var oGrid = oSld.Parent as Windows.UI.Xaml.Controls.Grid;
                foreach (Windows.UI.Xaml.UIElement oItem in oGrid.Children)
                {
                    Windows.UI.Xaml.Controls.TextBlock oTB;
                    oTB = oItem as Windows.UI.Xaml.Controls.TextBlock;
                    if (oTB != null)
                    {
                        if (oTB.Name == "uiConfig_SeismicEU_Text")
                            oTB.Text = VBlib.Source_SeismicPortal.DistanceNum2Opis((int)oSld.Value);
                    }
                }
            }
        }
        public void FromSrcSeismic_ConfigCreate(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            FromSrcBase_ConfigCreate(oStack,oZrodlo);

            Windows.UI.Xaml.Data.Binding oBind = new Windows.UI.Xaml.Data.Binding();
            oBind.ElementName = "uiConfig_" + oZrodlo.SRC_SETTING_NAME;
            oBind.Path = new Windows.UI.Xaml.PropertyPath("IsOn");

            var oSld = new Windows.UI.Xaml.Controls.Slider();
            oSld.Name = "uiConfig_SeismicEU_Slider";
            oSld.Minimum = 1;
            oSld.Maximum = 5;
            oSld.Value = vb14.GetSettingsInt(oZrodlo.SRC_SETTING_NAME + "_distance", 2);
            oSld.Header = vb14.GetLangString("resSeismicEU_SldHdr");
            oSld.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            oSld.ValueChanged += FromSrcSeismic_uiSettDistance_Changed;
            oSld.SetBinding(Windows.UI.Xaml.Controls.Slider.IsEnabledProperty, oBind);

            var oTB = new Windows.UI.Xaml.Controls.TextBlock();
            oTB.Name = "uiConfig_SeismicEU_Text";
            oTB.Text = VBlib.Source_SeismicPortal.DistanceNum2Opis((int)oSld.Value);

            var oCol1 = new Windows.UI.Xaml.Controls.ColumnDefinition();
            oCol1.Width = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Star);
            var oCol2 = new Windows.UI.Xaml.Controls.ColumnDefinition();
            oCol2.Width = new Windows.UI.Xaml.GridLength(0, Windows.UI.Xaml.GridUnitType.Auto);

            var oGrid = new Windows.UI.Xaml.Controls.Grid();
            oGrid.ColumnDefinitions.Add(oCol1);
            oGrid.ColumnDefinitions.Add(oCol2);

            oGrid.Children.Add(oSld);
            oGrid.Children.Add(oTB);

            Windows.UI.Xaml.Controls.Grid.SetColumn(oSld, 0);
            Windows.UI.Xaml.Controls.Grid.SetColumn(oTB, 1);

            oStack.Children.Add(oGrid);

#if _PK_NUMBOX_

            var oHomekWh = new Microsoft.UI.Xaml.Controls.NumberBox();
            oHomekWh.Name = "uiConfig_SeismicEU_HomekWh";
            oHomekWh.Value = vb14.GetSettingsInt(oZrodlo.SRC_SETTING_NAME + "_homekWh", p.k.IsThisMoje() ? 150 : 0);
            oHomekWh.Header = vb14.GetLangString("resSeismicEU_HomekWh_Hdr") + " (kWh)";
            oHomekWh.Minimum = 0;
            oHomekWh.SpinButtonPlacementMode = Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Compact;
            oHomekWh.SetBinding(Microsoft.UI.Xaml.Controls.NumberBox.IsEnabledProperty, oBind);
            oStack.Children.Add(oHomekWh);

            var oKrajTWh = new Microsoft.UI.Xaml.Controls.NumberBox();
            oKrajTWh.Name = "uiConfig_SeismicEU_KrajTWh";
            oKrajTWh.Value = vb14.GetSettingsInt(oZrodlo.SRC_SETTING_NAME + "_krajTWh", p.k.IsThisMoje() ? 170 : 0);
            oKrajTWh.Header = vb14.GetLangString("resSeismicEU_KrajTWh_Hdr") + " (TWh)";
            oKrajTWh.SpinButtonPlacementMode = Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Compact;
            oKrajTWh.Minimum = 0;
            oKrajTWh.SetBinding(Microsoft.UI.Xaml.Controls.NumberBox.IsEnabledProperty, oBind);
            oStack.Children.Add(oKrajTWh);

#else
            Windows.UI.Xaml.Input.InputScope oInpScope = new Windows.UI.Xaml.Input.InputScope();
            Windows.UI.Xaml.Input.InputScopeName scopeName = new Windows.UI.Xaml.Input.InputScopeName();
            scopeName.NameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Number;
            oInpScope.Names.Add(scopeName);

            var oHomekWh = new Windows.UI.Xaml.Controls.TextBox();
            oHomekWh.Name = "uiConfig_SeismicEU_HomekWh";
            oHomekWh.Text = p.k.GetSettingsInt(SRC_SETTING_NAME + "_homekWh", p.k.IsThisMoje() ? 150 : 0).ToString();
            oHomekWh.Header = p.k.GetLangString("resSeismicEU_HomekWh_Hdr") + " (kWh)";
            oHomekWh.InputScope = oInpScope;
            oStack.Children.Add(oHomekWh);


            oInpScope = new Windows.UI.Xaml.Input.InputScope();
            scopeName = new Windows.UI.Xaml.Input.InputScopeName();
            scopeName.NameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Number;
            oInpScope.Names.Add(scopeName);

            var oKrajTWh = new Windows.UI.Xaml.Controls.TextBox();
            oKrajTWh.Name = "uiConfig_SeismicEU_KrajTWh";
            oKrajTWh.Text = p.k.GetSettingsInt(SRC_SETTING_NAME + "_krajTWh", p.k.IsThisMoje() ? 170 : 0).ToString();
            oKrajTWh.Header = p.k.GetLangString("resSeismicEU_KrajTWh_Hdr") + " (TWh)";
            oKrajTWh.InputScope = oInpScope;
            oStack.Children.Add(oKrajTWh);

#endif


        }

        public void FromSrcSeismic_ConfigRead(Windows.UI.Xaml.Controls.StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            FromSrcBase_ConfigRead(oStack,oZrodlo);

            foreach (Windows.UI.Xaml.UIElement oItem in oStack.Children)
            {
                // Dim oTS As ToggleSwitch
                // oTS = TryCast(oItem, ToggleSwitch)
                // If oTS IsNot Nothing Then
                // If oTS.Name = "uiConfig_SeismicEU_MaxAll" Then SetSettingsBool(SRC_SETTING_NAME & "_MaxAll", oTS.IsOn)
                // End If

                Windows.UI.Xaml.Controls.Grid oGrid;
                oGrid = oItem as Windows.UI.Xaml.Controls.Grid;
                if (oGrid != null)
                {
                    foreach (var oChild in oGrid.Children)
                    {
                        Windows.UI.Xaml.Controls.Slider oSld;
                        oSld = oChild as Windows.UI.Xaml.Controls.Slider;
                        if (oSld != null)
                        {
                            if (oSld.Name == "uiConfig_SeismicEU_Slider")
                                vb14.SetSettingsInt(oZrodlo.SRC_SETTING_NAME + "_distance", (int)oSld.Value);
                        }
                    }
                }

#if _PK_NUMBOX_
                Microsoft.UI.Xaml.Controls.NumberBox oTBox;
                oTBox = oItem as Microsoft.UI.Xaml.Controls.NumberBox;
                if (oTBox != null)
                {
                    if (oTBox.Name == "uiConfig_SeismicEU_HomekWh")
                        vb14.SetSettingsInt(oZrodlo.SRC_SETTING_NAME + "_homekWh", (int)oTBox.Value);
                    if (oTBox.Name == "uiConfig_SeismicEU_KrajTWh")
                        vb14.SetSettingsInt(oZrodlo.SRC_SETTING_NAME + "_krajTWh", (int)oTBox.Value);
                }

#else
                Windows.UI.Xaml.Controls.TextBox oTBox;
                oTBox = oItem as Windows.UI.Xaml.Controls.TextBox;

                if (oTBox != null)
                {
                    if(oTBox.Name == "uiConfig_SeismicEU_HomekWh")
                        p.k.SetSettingsInt(SRC_SETTING_NAME + "_homekWh", int.Parse(oTBox.Text));
                    if (oTBox.Name == "uiConfig_SeismicEU_KrajTWh")
                        p.k.SetSettingsInt(SRC_SETTING_NAME + "_krajTWh", int.Parse(oTBox.Text));
                }
#endif

            }
        }

        #endregion

        #region IMGW meteo

        public void FromSrcIMGWmet_ConfigCreate(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            FromSrcBase_ConfigCreate(oStack, oZrodlo);

            Windows.UI.Xaml.Data.Binding oBind = new Windows.UI.Xaml.Data.Binding();
            oBind.ElementName = "uiConfig_" + oZrodlo.SRC_SETTING_NAME;
            oBind.Path = new Windows.UI.Xaml.PropertyPath("IsOn");

            var oTS = new ToggleSwitch();

            oTS = new ToggleSwitch();
            oTS.Name = "uiConfig_ImgwMeteo10MIN";
            oTS.IsOn = vb14.GetSettingsBool("sourceImgwMeteo10min", true);
            oTS.OnContent = vb14.GetLangString("resImgwMeteo10minON");
            oTS.OffContent = vb14.GetLangString("resImgwMeteo10minOFF");
            oTS.SetBinding(Windows.UI.Xaml.Controls.ToggleSwitch.IsEnabledProperty, oBind);

            oStack.Children.Add(oTS);
        }

        public  void FromSrcIMGWmet_ConfigRead(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            FromSrcBase_ConfigRead(oStack, oZrodlo);

            foreach (UIElement oItem in oStack.Children)
            {
                ToggleSwitch oTS;
                oTS = oItem as ToggleSwitch;
                if (oTS != null)
                {
                    // If oTS.Name = "uiConfig_ImgwMeteo" Then SetSettingsBool("sourceImgwMeteo", oTS.IsOn)
                    if ((oTS.Name ?? "") == "uiConfig_ImgwMeteo10MIN")
                        oTS.SetSettingsBool("sourceImgwMeteo10min");
                }
            }
        }


        #endregion

        #region IMGW hydro
        public  void FromSrcIMGWhyd_ConfigCreate(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            FromSrcBase_ConfigCreate(oStack, oZrodlo);

            Windows.UI.Xaml.Data.Binding oBind = new Windows.UI.Xaml.Data.Binding();
            oBind.ElementName = "uiConfig_" + oZrodlo.SRC_SETTING_NAME;
            oBind.Path = new Windows.UI.Xaml.PropertyPath("IsOn");

            var oTS = new ToggleSwitch();
            oTS.Name = "uiConfig_ImgwHydroAll";
            oTS.IsOn = vb14.GetSettingsBool("sourceImgwHydroAll");
            oTS.OnContent = vb14.GetLangString("resImgwHydroAllON");
            oTS.OffContent = vb14.GetLangString("resImgwHydroAllOFF");
            oTS.SetBinding(ToggleSwitch.IsEnabledProperty, oBind);

            oStack.Children.Add(oTS);
        }

        public  void FromSrcIMGWhyd_ConfigRead(StackPanel oStack, VBlib.Source_Base oZrodlo)
        {
            FromSrcBase_ConfigRead(oStack, oZrodlo);

            foreach (UIElement oItem in oStack.Children)
            {
                ToggleSwitch oTS;
                oTS = oItem as ToggleSwitch;
                if (oTS != null)
                {
                    if ((oTS.Name ?? "") == "uiConfig_ImgwHydroAll")
                        oTS.SetSettingsBool("sourceImgwHydroAll");
                }
            }
        }
        #endregion
    }
}

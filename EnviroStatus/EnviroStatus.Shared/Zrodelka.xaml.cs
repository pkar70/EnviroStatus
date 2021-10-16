// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

/// <summary>

/// An empty page that can be used on its own or navigated to within a Frame.

/// </summary>
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
//using Microsoft.VisualBasic.CompilerServices;

namespace EnviroStatus
{
    public sealed partial class Zrodelka : Page
    {
        public Zrodelka()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (EnviroStatus.Source_Base oZrodlo in EnviroStatus.App.gaSrc)
                oZrodlo.ConfigCreate(uiStackConfig);

            var oButton = new Button();
            oButton.Content = p.k.GetLangString("uiSettingsSave.Content");
            if ( (oButton.Content.ToString() == "uiSettingsSave.Content")
                || (oButton.Content.ToString() == ""))
                    oButton.Content = "Save!";
            oButton.HorizontalAlignment = HorizontalAlignment.Center;
            oButton.Click += uiSave_ClickEvent;
            // oButton.AddHandler(Button.cl)
            uiStackConfig.Children.Add(oButton);
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

            foreach (EnviroStatus.Source_Base oZrodlo in EnviroStatus.App.gaSrc)
            {
                sMsg = oZrodlo.ConfigDataOk(uiStackConfig);
                if (!string.IsNullOrEmpty(sMsg))
                    return sMsg;
            }

            return "";
        }

        private async void uiSave_Click(object sender = null, RoutedEventArgs e = null)
        {
            string sMsg = VerifyDataOK();
            if (!string.IsNullOrEmpty(sMsg))
            {
                await p.k.DialogBoxAsync(sMsg);
                return;
            }

            foreach (EnviroStatus.Source_Base oZrodlo in EnviroStatus.App.gaSrc)
                oZrodlo.ConfigRead(uiStackConfig);

            p.k.SetSettingsBool("wasSetup",true);

            Frame.GoBack();
        }
    }
}

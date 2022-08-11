
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using System.IO;
using Windows.UI.Xaml.Controls;
//using Microsoft.VisualBasic.CompilerServices;
using static p.Extensions;

namespace EnviroStatus
{
    public sealed partial class Info : Page
    {
        public Info()
        {
            this.InitializeComponent();
        }

        private void uiOk_Click(object sender, RoutedEventArgs e)
        {
            this.GoBack();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            uiVers.ShowAppVers();

            uiWeb.NavigateToString(VBlib.Info.GetHtmlHelpPage(uiTitle.RequestedTheme == ElementTheme.Dark));
        }

        private void uiMail_Click(object sender, RoutedEventArgs e)
        {
            var oMsg = new Windows.ApplicationModel.Email.EmailMessage();
            oMsg.Subject = "Smogometr - feedback";
            oMsg.To.Add(new Windows.ApplicationModel.Email.EmailRecipient("pkar.apps@outlook.com"));
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(oMsg);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

        private void UiWeb_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri == null)
                return;

            args.Cancel = true;
            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Windows.System.Launcher.LaunchUriAsync(args.Uri);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }


    }
}

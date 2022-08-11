
// i tak tu nie wchodzi jak nie jest na Windows

//using VBlib;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static p.Extensions;

namespace EnviroStatus
{
    public sealed partial class WedleMapy : Page
    {
        public WedleMapy()
        {
            this.InitializeComponent();
        }

        private void uiMapka_Loaded(object sender, RoutedEventArgs e)
        {
            VBlib.MyBasicGeoposition oPosition;
            oPosition = new VBlib.MyBasicGeoposition(50.061389, 19.938333) ; // // współrzędne wedle Wiki

            uiMapka.Center = new Windows.Devices.Geolocation.Geopoint(oPosition.ToWinGeopos());
            uiMapka.ZoomLevel = 5;
            uiMapka.MapServiceToken = GetMapKey();

            uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
        }


        private void uiMapka_Holding(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            VBlib.App.moPoint = args.Location.Position.ToMyGeopos();
            Frame.GoBack();
        }
    }
}

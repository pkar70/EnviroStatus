
// i tak tu nie wchodzi jak nie jest na Windows

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            Windows.Devices.Geolocation.BasicGeoposition oPosition;
            oPosition = p.k.NewGeoPos(50.061389, 19.938333) ; // // współrzędne wedle Wiki

            uiMapka.Center = new Windows.Devices.Geolocation.Geopoint(oPosition);
            uiMapka.ZoomLevel = 5;
            if (p.k.IsThisMoje())
                uiMapka.MapServiceToken = "rDu5Hj5dykMMBblRgIaq~AalqbgIUph7UvMnI1WrB8A~AvZXaT3i_qD-UiyF61F4sbXe5ptSp3Wq0JdPF0dcOiAs0ZpAJ7W1QjQ28P5HCXSG";
            else
                uiMapka.MapServiceToken = "oaQmZvvDqQ39JcwdXSjK~TCuV7-3VaLPbJINptVo9gw~AuExUGkiHbYbqMIEVyx3RaKMprPZShlsQEpjGceEQIQM4HY9nYeWD0D19-Yb8OhY";

            uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
        }


        private void uiMapka_Holding(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            EnviroStatus.App.moPoint = args.Location.Position;
            Frame.GoBack();
        }
    }
}

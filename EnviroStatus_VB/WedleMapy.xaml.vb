' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class WedleMapy
    Inherits Page

    Private Sub uiMapka_Loaded(sender As Object, e As RoutedEventArgs)
        Dim oPosition As Windows.Devices.Geolocation.BasicGeoposition
        oPosition = New Windows.Devices.Geolocation.BasicGeoposition()
        oPosition.Latitude = 50.061389 '  // współrzędne wedle Wiki
        oPosition.Longitude = 19.938333

        uiMapka.Center = New Windows.Devices.Geolocation.Geopoint(oPosition)
        uiMapka.ZoomLevel = 5
        If IsThisMoje() Then
            uiMapka.MapServiceToken = "rDu5Hj5dykMMBblRgIaq~AalqbgIUph7UvMnI1WrB8A~AvZXaT3i_qD-UiyF61F4sbXe5ptSp3Wq0JdPF0dcOiAs0ZpAJ7W1QjQ28P5HCXSG"
        Else
            uiMapka.MapServiceToken = "oaQmZvvDqQ39JcwdXSjK~TCuV7-3VaLPbJINptVo9gw~AuExUGkiHbYbqMIEVyx3RaKMprPZShlsQEpjGceEQIQM4HY9nYeWD0D19-Yb8OhY"
        End If

        uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road
    End Sub


    Private Sub uiMapka_Holding(sender As Maps.MapControl, args As Maps.MapInputEventArgs)
        App.moPoint = New Point
        App.moPoint.X = args.Location.Position.Latitude
        App.moPoint.Y = args.Location.Position.Longitude
        Me.Frame.GoBack()
    End Sub


End Class

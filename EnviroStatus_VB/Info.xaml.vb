' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class Info
    Inherits Page

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.GoBack()
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiVers.Text = "v" & Package.Current.Id.Version.Major & "." &
            Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build

        Dim oFile As StreamReader
        Dim sTxt As String
        sTxt = "Assets\Guide-" & Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName & ".htm"
        If Not File.Exists(sTxt) Then sTxt = "Assets\Guide-En.htm"

        oFile = File.OpenText(sTxt)
        sTxt = ""
        While Not oFile.EndOfStream
            sTxt = sTxt & oFile.ReadLine
        End While
        oFile.Dispose()

        If uiTitle.RequestedTheme = ElementTheme.Dark Then
            sTxt = sTxt.Replace("<body>", "<body bgcolor='#000000' style='color:#eeeeee'>")
        End If

        uiWeb.NavigateToString(sTxt)

    End Sub

    Private Sub uiMail_Click(sender As Object, e As RoutedEventArgs)
        Dim oMsg As Email.EmailMessage = New Windows.ApplicationModel.Email.EmailMessage()
        oMsg.Subject = "Smogometr - feedback"
        oMsg.To.Add(New Email.EmailRecipient("pkar.apps@outlook.com"))
        Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(oMsg)
    End Sub

    Private Sub UiWeb_NavigationStarting(sender As WebView, args As WebViewNavigationStartingEventArgs) Handles uiWeb.NavigationStarting
        If args.Uri Is Nothing Then Exit Sub

        args.Cancel = True
        Windows.System.Launcher.LaunchUriAsync(args.Uri)

    End Sub
End Class

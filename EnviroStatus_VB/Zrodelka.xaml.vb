' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class Zrodelka
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        For Each oZrodlo As Source_Base In App.gaSrc
            oZrodlo.ConfigCreate(uiStackConfig)
        Next

        Dim oButton As Button = New Button
        oButton.Content = GetLangString("uiSettingsSave.Content")
        If oButton.Content = "uiSettingsSave.Content" OrElse oButton.Content = "" Then
            oButton.Content = "Save!"
        End If
        oButton.HorizontalAlignment = HorizontalAlignment.Center
        AddHandler oButton.Click, AddressOf uiSave_ClickEvnt
        ' oButton.AddHandler(Button.cl)
        uiStackConfig.Children.Add(oButton)

        ' <Button Content="Save!" HorizontalAlignment="Center" Click="uiSave_Click" x:Uid="uiSettingsSave"/>

    End Sub

    Private Sub uiSave_ClickEvnt(sender As Object, e As RoutedEventArgs)
        Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, AddressOf uiSave_Click)
    End Sub

    Private Function VerifyDataOK() As String
        Dim sMsg As String = ""

        For Each oZrodlo As Source_Base In App.gaSrc
            sMsg = oZrodlo.ConfigDataOk(uiStackConfig)
            If sMsg <> "" Then Return sMsg
        Next

        Return ""
    End Function

    Private Async Sub uiSave_Click(Optional sender As Object = Nothing, Optional e As RoutedEventArgs = Nothing)
        Dim sMsg As String = VerifyDataOK()
        If sMsg <> "" Then
            Await DialogBox(sMsg)
            Exit Sub
        End If

        For Each oZrodlo As Source_Base In App.gaSrc
            oZrodlo.ConfigRead(uiStackConfig)
        Next

        Me.Frame.GoBack()
    End Sub
End Class

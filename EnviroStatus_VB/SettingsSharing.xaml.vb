' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SettingsSharing
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        GetSettingsBool(uiFileCache, "settingsFileCache")
        GetSettingsBool(uiFileCacheRoam, "settingsFileCacheRoam")
        GetSettingsBool(uiRemSysAllowData, "settingsRemSysData")
        GetSettingsBool(uiRemSysAllowAPIKey, "settingsRemSysAPI")

        uiRemSysAllowAPIKey.IsEnabled = GetSettingsBool("sourceDarkSky")
        'GetSettingsString("airly_apikey")
    End Sub

    Private Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        SetSettingsBool(uiFileCache, "settingsFileCache")
        SetSettingsBool(uiFileCacheRoam, "settingsFileCacheRoam")
        SetSettingsBool(uiRemSysAllowData, "settingsRemSysData")
        SetSettingsBool(uiRemSysAllowAPIKey, "settingsRemSysAPI")
        Me.Frame.GoBack()
    End Sub

    Private Sub uiFileCache_Toggled(sender As Object, e As RoutedEventArgs) Handles uiFileCache.Toggled
        uiFileCacheRoam.IsEnabled = uiFileCache.IsOn
    End Sub
End Class

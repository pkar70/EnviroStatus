
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EnviroStatus
{
    public sealed partial class SettingsSharing : Page
    {
        public SettingsSharing()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            p.k.GetSettingsBool(uiFileCache, "settingsFileCache");
            p.k.GetSettingsBool(uiFileCacheRoam, "settingsFileCacheRoam");
            p.k.GetSettingsBool(uiRemSysAllowData, "settingsRemSysData");
            p.k.GetSettingsBool(uiRemSysAllowAPIKey, "settingsRemSysAPI");

            uiRemSysAllowAPIKey.IsEnabled = p.k.GetSettingsBool("sourceDarkSky");
        }

        private void uiSave_Click(object sender, RoutedEventArgs e)
        {
            p.k.SetSettingsBool(uiFileCache, "settingsFileCache");
            p.k.SetSettingsBool(uiFileCacheRoam, "settingsFileCacheRoam");
            p.k.SetSettingsBool(uiRemSysAllowData, "settingsRemSysData");
            p.k.SetSettingsBool(uiRemSysAllowAPIKey, "settingsRemSysAPI");
            Frame.GoBack();
        }

        private void uiFileCache_Toggled(object sender, RoutedEventArgs e)
        {
            uiFileCacheRoam.IsEnabled = uiFileCache.IsOn;
        }
    }
}

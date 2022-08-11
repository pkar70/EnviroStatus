
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;

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
            uiFileCache.GetSettingsBool("settingsFileCache");
            uiFileCacheRoam.GetSettingsBool("settingsFileCacheRoam");
            uiRemSysAllowData.GetSettingsBool("settingsRemSysData");
            uiRemSysAllowAPIKey.GetSettingsBool("settingsRemSysAPI");

            uiRemSysAllowAPIKey.IsEnabled = vb14.GetSettingsBool("sourceDarkSky");
        }

        private void uiSave_Click(object sender, RoutedEventArgs e)
        {
            uiFileCache.SetSettingsBool("settingsFileCache");
            uiFileCacheRoam.SetSettingsBool("settingsFileCacheRoam");
            uiRemSysAllowData.SetSettingsBool("settingsRemSysData");
            uiRemSysAllowAPIKey.SetSettingsBool("settingsRemSysAPI");
            Frame.GoBack();
        }

        private void uiFileCache_Toggled(object sender, RoutedEventArgs e)
        {
            uiFileCacheRoam.IsEnabled = uiFileCache.IsOn;
        }
    }
}

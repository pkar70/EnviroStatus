' ponieważ NIE DZIAŁA pod Uno.Android wczytywanie pliku (apk nie jest rozpakowany?),
' to w ten sposób przekazywanie zawartości pliku INI
' wychodzi na to samo, edycja pliku defaults.ini albo defsIni.lib.vb

Public Class IniLikeDefaults

    Public Const sIniContent As String = "
[main]
uiLimitWgCombo=0    #0: EU, 1: WHO, 2:WHO2021
higroTemp=22
# remark
' remark
; remark
// remark

[debug]
key=value # remark

[app]
; lista z app (bez ustawiania)
simulateGPS=false
settingsLiveTile=''
lastTimer=''
testTimera=false
settingsAlerts=''
settingStartPage=''
currentFav=''
sourceDarkSky_apikey=''
settingsRemSysData=false
wasSetup=false
favNames=''
gpsEmulationLat=''
gpsEmulationLon=''
settingsLiveClock=false
settingsDataLog=false
settingsRemSysAPI=false
settingsRemSysData=false


[libs]
; lista z pkarmodule
remoteSystemDisabled=false
appFailData=
offline=false
lastPolnocnyTry=
lastPolnocnyOk=
seenUri=    # ostatni link (opóźnienie jeśli kolejny HttpGet jest z tego samego serwera
# SRC_SETTING_NAME & ""_apikey""  dla każdego
settingsDataLog=false
settingsAlerts=""
lastToast=""
cleanAir=false
settingsFileCache=false
settingsFileCacheRoam=false
'fav_' & sFavName
'favgps_' & sFavName
higroKubatura=0
SRC_SETTING_NAME & '_apikey'
seenUri=''
sourceAirly=false
sourceAirly_apikey=''
SRC_SETTING_NAME=SRC_DEFAULT_ENABLE
sourceRAH=false
sourceGIOS=false
sourceImgwHydro=false
sourceImgwHydroAll=false
sourceImgwHydro=false
sourceImgwMeteo=false
sourceImgwMeteo10min=false
SRC_SETTING_NAME & '_homekWh'
SRC_SETTING_NAME & '_krajTWh'
SRC_SETTING_NAME & '_distance'
"

End Class

﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.42000
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System
Imports System.Reflection

Namespace My.Resources
    
    'This class was auto-generated by the StronglyTypedResourceBuilder
    'class via a tool like ResGen or Visual Studio.
    'To add or remove a member, edit your .ResX file then rerun ResGen
    'with the /str option, or rebuild your VS project.
    '''<summary>
    '''  A strongly-typed resource class, for looking up localized strings, etc.
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0"),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
    Friend Class Resource_EN
        
        Private Shared resourceMan As Global.System.Resources.ResourceManager
        
        Private Shared resourceCulture As Global.System.Globalization.CultureInfo
        
        <Global.System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")>  _
        Friend Sub New()
            MyBase.New
        End Sub
        
        '''<summary>
        '''  Returns the cached ResourceManager instance used by this class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Shared ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
            Get
                If Object.ReferenceEquals(resourceMan, Nothing) Then
                    Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("VBlib.Resource_EN", GetType(Resource_EN).GetTypeInfo.Assembly)
                    resourceMan = temp
                End If
                Return resourceMan
            End Get
        End Property
        
        '''<summary>
        '''  Overrides the current thread's CurrentUICulture property for all
        '''  resource lookups using this strongly typed resource class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Shared Property Culture() As Global.System.Globalization.CultureInfo
            Get
                Return resourceCulture
            End Get
            Set
                resourceCulture = value
            End Set
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to EN.
        '''</summary>
        Friend Shared ReadOnly Property _lang() As String
            Get
                Return ResourceManager.GetString("_lang", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to ERROR.
        '''</summary>
        Friend Shared ReadOnly Property errAnyError() As String
            Get
                Return ResourceManager.GetString("errAnyError", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Network is required.
        '''</summary>
        Friend Shared ReadOnly Property errNoNet() As String
            Get
                Return ResourceManager.GetString("errNoNet", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Cannot get data folder.
        '''</summary>
        Friend Shared ReadOnly Property errNoRoamFolder() As String
            Get
                Return ResourceManager.GetString("errNoRoamFolder", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to This functionality is not present on XBox version of Windows.
        '''</summary>
        Friend Shared ReadOnly Property errXBoxNot() As String
            Get
                Return ResourceManager.GetString("errXBoxNot", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to SmogMeter.
        '''</summary>
        Friend Shared ReadOnly Property manifestAppName() As String
            Get
                Return ResourceManager.GetString("manifestAppName", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Clean air :).
        '''</summary>
        Friend Shared ReadOnly Property msgAllOk() As String
            Get
                Return ResourceManager.GetString("msgAllOk", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Environment status changes:.
        '''</summary>
        Friend Shared ReadOnly Property msgAndroidToastTitle() As String
            Get
                Return ResourceManager.GetString("msgAndroidToastTitle", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Copy.
        '''</summary>
        Friend Shared ReadOnly Property msgCopyDetails() As String
            Get
                Return ResourceManager.GetString("msgCopyDetails", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to As this is first run, you have to enable some data sources first..
        '''</summary>
        Friend Shared ReadOnly Property msgFirstRunGoSetup() As String
            Get
                Return ResourceManager.GetString("msgFirstRunGoSetup", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to For API key, please register.
        '''</summary>
        Friend Shared ReadOnly Property msgForAPIkey() As String
            Get
                Return ResourceManager.GetString("msgForAPIkey", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to version.
        '''</summary>
        Friend Shared ReadOnly Property msgVersion() As String
            Get
                Return ResourceManager.GetString("msgVersion", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Humidity inside.
        '''</summary>
        Friend Shared ReadOnly Property msgWilgInt() As String
            Get
                Return ResourceManager.GetString("msgWilgInt", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Yes.
        '''</summary>
        Friend Shared ReadOnly Property msgYes() As String
            Get
                Return ResourceManager.GetString("msgYes", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to calculated value.
        '''</summary>
        Friend Shared ReadOnly Property resCalculated() As String
            Get
                Return ResourceManager.GetString("resCalculated", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to (no live tile).
        '''</summary>
        Friend Shared ReadOnly Property resDeadTile() As String
            Get
                Return ResourceManager.GetString("resDeadTile", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Cancel.
        '''</summary>
        Friend Shared ReadOnly Property resDlgCancel() As String
            Get
                Return ResourceManager.GetString("resDlgCancel", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Continue.
        '''</summary>
        Friend Shared ReadOnly Property resDlgContinue() As String
            Get
                Return ResourceManager.GetString("resDlgContinue", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to No.
        '''</summary>
        Friend Shared ReadOnly Property resDlgNo() As String
            Get
                Return ResourceManager.GetString("resDlgNo", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Yes.
        '''</summary>
        Friend Shared ReadOnly Property resDlgYes() As String
            Get
                Return ResourceManager.GetString("resDlgYes", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Cannot get location (in timely fashion).
        '''</summary>
        Friend Shared ReadOnly Property resErrorGettingPos() As String
            Get
                Return ResourceManager.GetString("resErrorGettingPos", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Location service is not available. Do I have a permission?.
        '''</summary>
        Friend Shared ReadOnly Property resErrorNoGPSAllowed() As String
            Get
                Return ResourceManager.GetString("resErrorNoGPSAllowed", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to depth.
        '''</summary>
        Friend Shared ReadOnly Property resGlebokosc() As String
            Get
                Return ResourceManager.GetString("resGlebokosc", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Nearest river.
        '''</summary>
        Friend Shared ReadOnly Property resImgwHydroAllOFF() As String
            Get
                Return ResourceManager.GetString("resImgwHydroAllOFF", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to All rivers in range.
        '''</summary>
        Friend Shared ReadOnly Property resImgwHydroAllON() As String
            Get
                Return ResourceManager.GetString("resImgwHydroAllON", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Precip 60 min.
        '''</summary>
        Friend Shared ReadOnly Property resImgwMeteo10minOFF() As String
            Get
                Return ResourceManager.GetString("resImgwMeteo10minOFF", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Precip 10 min.
        '''</summary>
        Friend Shared ReadOnly Property resImgwMeteo10minON() As String
            Get
                Return ResourceManager.GetString("resImgwMeteo10minON", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Name of place.
        '''</summary>
        Friend Shared ReadOnly Property resNazwa() As String
            Get
                Return ResourceManager.GetString("resNazwa", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to (no autostart).
        '''</summary>
        Friend Shared ReadOnly Property resNoAutostart() As String
            Get
                Return ResourceManager.GetString("resNoAutostart", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to No sensor in range.
        '''</summary>
        Friend Shared ReadOnly Property resNoSensorInRange() As String
            Get
                Return ResourceManager.GetString("resNoSensorInRange", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Solar wind density.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarAdditSolarWindDensity() As String
            Get
                Return ResourceManager.GetString("resPomiarAdditSolarWindDensity", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Solar wind speed.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarAdditSolarWindSpeed() As String
            Get
                Return ResourceManager.GetString("resPomiarAdditSolarWindSpeed", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Solar wind temperature.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarAdditSolarWindTemp() As String
            Get
                Return ResourceManager.GetString("resPomiarAdditSolarWindTemp", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Prediction for.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarNoaaKindexPredicted() As String
            Get
                Return ResourceManager.GetString("resPomiarNoaaKindexPredicted", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Precip.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarOpad() As String
            Get
                Return ResourceManager.GetString("resPomiarOpad", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Dew point.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarRosa() As String
            Get
                Return ResourceManager.GetString("resPomiarRosa", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to SolWind density.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarSolarWindDensity() As String
            Get
                Return ResourceManager.GetString("resPomiarSolarWindDensity", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to SolWind speed.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarSolarWindSpeed() As String
            Get
                Return ResourceManager.GetString("resPomiarSolarWindSpeed", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to SolWind temp.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarSolarWindTemp() As String
            Get
                Return ResourceManager.GetString("resPomiarSolarWindTemp", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Visibility.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarWidocz() As String
            Get
                Return ResourceManager.GetString("resPomiarWidocz", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Wind.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarWind() As String
            Get
                Return ResourceManager.GetString("resPomiarWind", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Cloud cover.
        '''</summary>
        Friend Shared ReadOnly Property resPomiarZachm() As String
            Get
                Return ResourceManager.GetString("resPomiarZachm", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to 24 hours average.
        '''</summary>
        Friend Shared ReadOnly Property resRAHdailyAvg() As String
            Get
                Return ResourceManager.GetString("resRAHdailyAvg", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Save.
        '''</summary>
        Friend Shared ReadOnly Property resSaveFav() As String
            Get
                Return ResourceManager.GetString("resSaveFav", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Sum of all.
        '''</summary>
        Friend Shared ReadOnly Property resSeismicEU_All() As String
            Get
                Return ResourceManager.GetString("resSeismicEU_All", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to All.
        '''</summary>
        Friend Shared ReadOnly Property resSeismicEU_DistAll() As String
            Get
                Return ResourceManager.GetString("resSeismicEU_DistAll", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to monthly home consumpion.
        '''</summary>
        Friend Shared ReadOnly Property resSeismicEU_HomekWh() As String
            Get
                Return ResourceManager.GetString("resSeismicEU_HomekWh", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Your home monthly consumption.
        '''</summary>
        Friend Shared ReadOnly Property resSeismicEU_HomekWh_Hdr() As String
            Get
                Return ResourceManager.GetString("resSeismicEU_HomekWh_Hdr", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to months of country production.
        '''</summary>
        Friend Shared ReadOnly Property resSeismicEU_KrajTWh() As String
            Get
                Return ResourceManager.GetString("resSeismicEU_KrajTWh", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Your country yearly production.
        '''</summary>
        Friend Shared ReadOnly Property resSeismicEU_KrajTWh_Hdr() As String
            Get
                Return ResourceManager.GetString("resSeismicEU_KrajTWh_Hdr", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Only one quake (max).
        '''</summary>
        Friend Shared ReadOnly Property resSeismicEU_Max() As String
            Get
                Return ResourceManager.GetString("resSeismicEU_Max", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Quake range.
        '''</summary>
        Friend Shared ReadOnly Property resSeismicEU_SldHdr() As String
            Get
                Return ResourceManager.GetString("resSeismicEU_SldHdr", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to T Αpparent.
        '''</summary>
        Friend Shared ReadOnly Property resTempOdczuwana() As String
            Get
                Return ResourceManager.GetString("resTempOdczuwana", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to altitude.
        '''</summary>
        Friend Shared ReadOnly Property resWysokosc() As String
            Get
                Return ResourceManager.GetString("resWysokosc", resourceCulture)
            End Get
        End Property
    End Class
End Namespace

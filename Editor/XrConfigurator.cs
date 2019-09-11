
using System.IO;
#if UNITY_EDITOR
#if XR_SDK
using UnityEditor.XR.Management;
using System;
#endif
using UnityEditor;
#endif
using UnityEngine;
#if XR_SDK
using UnityEngine.XR.Management;
#endif
#if OCULUS_SDK
using Unity.XR.Oculus;
#endif

namespace com.unity.cliconfigmanager
{
    public class XrConfigurator
    {
        private readonly PlatformSettings platformSettings;

        public XrConfigurator(PlatformSettings platformSettings)
        {
            this.platformSettings = platformSettings;
        }
#if UNITY_EDITOR
        public void ConfigureXr()
        {

#if XR_SDK
            string testXrGeneralSettingsPath = "Assets/XR/Settings/Test Settings.asset";
            PlayerSettings.virtualRealitySupported = false;

            // Create our own test version of xr general settings.
            var xrGeneralSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            var buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();

            xrGeneralSettings.Manager = managerSettings;
#if OCULUS_SDK
            ConfigureOculusXrSdk(xrGeneralSettings, buildTargetSettings, testXrGeneralSettingsPath, managerSettings);
#endif
#endif

#if !XR_SDK
            ConfigureLegacyVr();
#endif
        }

#if  !XR_SDK
        private void ConfigureLegacyVr()
        {
            PlayerSettings.virtualRealitySupported = true;
            PlayerSettings.stereoRenderingPath = platformSettings.StereoRenderingPath;
            UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(platformSettings.BuildTargetGroup,
                new string[] {platformSettings.XrTarget});
        }
#endif

#if OCULUS_SDK
        private void ConfigureOculusXrSdk(XRGeneralSettings xrGeneralSettings,
            XRGeneralSettingsPerBuildTarget buildTargetSettings, string testXrGeneralSettingsPath,
            XRManagerSettings managerSettings)
        {
            EnsureArgumentNotNull(xrGeneralSettings);
            EnsureArgumentNotNull(buildTargetSettings);
            EnsureArgumentNotNull(managerSettings);

            var xrSdkSettingsName = "Unity.XR.Oculus.Settings";
            var settings = ScriptableObject.CreateInstance<OculusSettings>();

            if (settings == null)
            {
                throw new ArgumentNullException(
                    $"Tried to instantiate an instance of {typeof(OculusSettings).Name} but it is null.");
            }

            var loader = ScriptableObject.CreateInstance<OculusLoader>();

            if (loader == null)
            {
                throw new ArgumentNullException(
                    $"Tried to instantiate an instance of {typeof(OculusLoader).Name}, but it is null.");
            }

            xrGeneralSettings.Manager.loaders.Add(loader);

            buildTargetSettings.SetSettingsForBuildTarget(EditorUserBuildSettings.selectedBuildTargetGroup,
                xrGeneralSettings);

            EnsureXrGeneralSettingsPathExists(testXrGeneralSettingsPath);
            AssetDatabase.CreateAsset(buildTargetSettings, testXrGeneralSettingsPath);
            AssetDatabase.AddObjectToAsset(xrGeneralSettings, testXrGeneralSettingsPath);
            AssetDatabase.AddObjectToAsset(managerSettings, testXrGeneralSettingsPath);
            AssetDatabase.AddObjectToAsset(settings, testXrGeneralSettingsPath);
            AssetDatabase.AddObjectToAsset(loader, testXrGeneralSettingsPath);

            if (platformSettings.BuildTarget == BuildTarget.Android)
            {
                settings.m_StereoRenderingModeAndroid = platformSettings.StereoRenderingModeAndroid;
            }
            else
            {
                settings.m_StereoRenderingModeDesktop = platformSettings.StereoRenderingModeDesktop;
            }

            AssetDatabase.SaveAssets();
            EditorBuildSettings.AddConfigObject(xrSdkSettingsName, settings, true);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
        }

        private static void EnsureXrGeneralSettingsPathExists(string testXrGeneralSettingsPath)
        {
            var settingsPath = Path.GetDirectoryName(testXrGeneralSettingsPath);
            if (!Directory.Exists(settingsPath))
            {
                Directory.CreateDirectory(testXrGeneralSettingsPath);
            }
        }

        private static void EnsureArgumentNotNull(object arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }
        }
#endif
#endif
    }

}
#if UNITY_EDITOR && XR_SDK
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;

#if OCULUS_SDK
using Unity.XR.Oculus;
#endif

namespace com.unity.cliconfigmanager
{
    public static class XrSdkConfigurator
    {
        private static readonly string TestXrGeneralSettingsPath = "Assets/XR/Settings/Test Settings.asset";

        public static void SetupXrSdk()
        {
            // Create our own test version of xr general settings.
            var xrGeneralSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            var buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();

            xrGeneralSettings.Manager = managerSettings;
            string xrSdkSettingsName;
#if OCULUS_SDK
            xrSdkSettingsName = "Unity.XR.Oculus.Settings";
            var settings = ScriptableObject.CreateInstance<OculusSettings>();

            if (settings == null)
            {
                throw new ArgumentNullException($"{typeof(OculusSettings).Name} is null, but shouldn't be.");
            }

            var loader = ScriptableObject.CreateInstance<OculusLoader>();

            if (loader == null)
            {
                throw new ArgumentNullException($"{typeof(OculusLoader).Name} is null, but shouldn't be.");
            }

            xrGeneralSettings.Manager.loaders.Add(loader);

            buildTargetSettings.SetSettingsForBuildTarget(EditorUserBuildSettings.selectedBuildTargetGroup,
                xrGeneralSettings);

            AssetDatabase.CreateAsset(buildTargetSettings, TestXrGeneralSettingsPath);
            AssetDatabase.AddObjectToAsset(xrGeneralSettings, TestXrGeneralSettingsPath);
            AssetDatabase.AddObjectToAsset(managerSettings, TestXrGeneralSettingsPath);
            AssetDatabase.AddObjectToAsset(settings, TestXrGeneralSettingsPath);
            AssetDatabase.AddObjectToAsset(loader, TestXrGeneralSettingsPath);

            if (PlatformSettings.BuildTarget == BuildTarget.Android)
            {
                settings.StereoRenderingModeAndroid = PlatformSettings.StereoRenderingModeAndroid;
            }
            else
            {
                settings.StereoRenderingModeDesktop = PlatformSettings.StereoRenderingModeDesktop;
            }

            AssetDatabase.SaveAssets();
            EditorBuildSettings.AddConfigObject(xrSdkSettingsName, settings, true);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
#endif
        }
    }
}
#endif

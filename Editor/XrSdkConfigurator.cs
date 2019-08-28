using System;
#if UNITY_EDITOR
#if XR_SDK
using UnityEditor.XR.Management;
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
    public static class XrSdkConfigurator
    {
        private static readonly string TestXrGeneralSettingsPath = "Assets/XR/Settings/Test Settings.asset";

        public static void SetupXrSdk()
        {
#if UNITY_EDITOR && XR_SDK
            // Create our own test version of xr general settings.
            var xrGeneralSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            var buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();

            xrGeneralSettings.Manager = managerSettings;
#if OCULUS_SDK
            var xrSdkSettingsName = "Unity.XR.Oculus.Settings";
            var settings = ScriptableObject.CreateInstance<OculusSettings>();

            if (settings == null)
            {
                throw new ArgumentNullException($"Tried to instantiate an instance of {typeof(OculusSettings).Name} but it is null.");
            }

            var loader = ScriptableObject.CreateInstance<OculusLoader>();

            if (loader == null)
            {
                throw new ArgumentNullException($"Tried to instantiate an instance of {typeof(OculusLoader).Name}, but it is null.");
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
                settings.m_StereoRenderingModeAndroid = PlatformSettings.StereoRenderingModeAndroid;
            }
            else
            {
                settings.m_StereoRenderingModeDesktop = PlatformSettings.StereoRenderingModeDesktop;
            }

            AssetDatabase.SaveAssets();
            EditorBuildSettings.AddConfigObject(xrSdkSettingsName, settings, true);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
#endif
#endif
        }
    }
}
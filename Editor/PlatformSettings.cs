#if OCULUS_SDK
using Unity.XR.Oculus;
#endif

using com.unity.xr.test.runtimesettings;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_VR
using UnityEngine.XR;
#endif

namespace com.unity.cliconfigmanager
{
    public static class PlatformSettings
    {
#if UNITY_EDITOR
        public static BuildTargetGroup BuildTargetGroup => EditorUserBuildSettings.selectedBuildTargetGroup;
        public static BuildTarget BuildTarget => EditorUserBuildSettings.activeBuildTarget;

        public static string XrTarget;
        public static GraphicsDeviceType PlayerGraphicsApi;

        public static StereoRenderingPath StereoRenderingPath;
#if OCULUS_SDK
        public static OculusSettings.StereoRenderingMode StereoRenderingModeDesktop;
        public static OculusSettings.StereoRenderingMode StereoRenderingModeAndroid;
#endif
        public static bool MtRendering = true;
        public static bool GraphicsJobs;
        public static AndroidSdkVersions MinimumAndroidSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        public static AndroidSdkVersions TargetAndroidSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        public static ScriptingImplementation ScriptingImplementation = ScriptingImplementation.IL2CPP;
        public static string AppleDeveloperTeamId;
        public static string IOsProvisioningProfileId;

        public static string SimulationMode;
        private static readonly string ResourceDir = "Assets/Resources";

        public static void SerializeToAsset()
        {
            var settingsAsset = ScriptableObject.CreateInstance<CurrentSettings>();

            settingsAsset.EnabledXrTarget = XrTarget;

            settingsAsset.PlayerGraphicsApi = PlayerGraphicsApi.ToString();
            settingsAsset.StereoRenderingMode = GetXrStereoRenderingPathMapping(StereoRenderingPath).ToString();
#if OCULUS_SDK
            settingsAsset.StereoRenderingModeDesktop = StereoRenderingModeDesktop.ToString();
            settingsAsset.StereoRenderingModeAndroid = StereoRenderingModeAndroid.ToString();
#endif
            settingsAsset.MtRendering = MtRendering;
            settingsAsset.GraphicsJobs = GraphicsJobs;

            CreateAndSaveCurrentSettingsAsset(settingsAsset);
        }

        private static XRSettings.StereoRenderingMode GetXrStereoRenderingPathMapping(StereoRenderingPath stereoRenderingPath)
        {
            switch (stereoRenderingPath)
            {
                case StereoRenderingPath.SinglePass:
                    return XRSettings.StereoRenderingMode.SinglePass;
                case StereoRenderingPath.MultiPass:
                    return XRSettings.StereoRenderingMode.MultiPass;
                case StereoRenderingPath.Instancing:
                    return XRSettings.StereoRenderingMode.SinglePassInstanced;
                default:
                    return XRSettings.StereoRenderingMode.SinglePassMultiview;
            }
        }

        private static void CreateAndSaveCurrentSettingsAsset(CurrentSettings settingsAsset)
        {
            if (!System.IO.Directory.Exists(ResourceDir))
            {
                System.IO.Directory.CreateDirectory(ResourceDir);
            }

            AssetDatabase.CreateAsset(settingsAsset, ResourceDir + "/settings.asset");
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
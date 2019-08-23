#if UNITY_EDITOR
using UnityEditor;
#endif
#if OCULUS_SDK
using Unity.XR.Oculus;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace com.unity.cliconfigmanager
{
    public class OculusSettingsConfigurator
    {
#if UNITY_EDITOR
        public void ConfigureXr()
        {
            XrSdkConfigurator.SetupXrSdk();
        }


        public void TrySetOculusXrSdkStereoRenderingPath(string stereoRenderingPath)
        {
#if OCULUS_SDK
            // This allows us to be "backward compatible".
            PlayerSettings.virtualRealitySupported = false;
            var srp = stereoRenderingPath.Equals("SinglePass") || stereoRenderingPath.Contains("Instancing") ? "SinglePassInstanced" : stereoRenderingPath;
            if (PlatformSettings.BuildTarget == BuildTarget.Android)
            {
                PlatformSettings.StereoRenderingModeAndroid =
 PrebuildSettingsConfigurator.TryParse<OculusSettings.StereoRenderingMode>(srp);
            }
            else
            {
                PlatformSettings.StereoRenderingModeDesktop =
 PrebuildSettingsConfigurator.TryParse<OculusSettings.StereoRenderingMode>(srp);
            }
#endif
        }
#endif
    }
}

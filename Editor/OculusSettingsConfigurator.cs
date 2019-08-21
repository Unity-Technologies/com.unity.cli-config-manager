#if UNITY_EDITOR
using UnityEditor;
#if OCULUS_SDK
using Unity.XR.Oculus;
using UnityEngine;
using UnityEngine.Rendering;
#endif

namespace com.unity.cliconfigmanager
{
    public class OculusSettingsConfigurator
    {
        public void ConfigureXr()
        {
            XrSdkConfigurator.SetupXrSdk();
        }


        public void TrySetOculusXrSdkStereoRenderingPath(string stereoRenderingPath)
        {
#if OCULUS_SDK
            // This allows us to be "backward compatible".
            PlayerSettings.virtualRealitySupported = false;
            var srp = stereoRenderingPath.Equals("SinglePass") ? "SinglePassInstanced" : stereoRenderingPath;
            if (PlatformSettings.BuildTarget == BuildTarget.Android)
            {
                PlatformSettings.StereoRenderingModeAndroid = PrebuildSettingsConfigurator.TryParse<OculusSettings.StereoRenderingMode>(srp);
            }
            else
            {
                PlatformSettings.StereoRenderingModeDesktop = PrebuildSettingsConfigurator.TryParse<OculusSettings.StereoRenderingMode>(srp);
            }
#endif
        }
    }
}
#endif
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;

namespace com.unity.cliconfigmanager
{
    public class XrSdkPackageHandler
    {
        private readonly List<string> xrSdkPackages = new List<string>
        {
            "com.unity.xr.management",
            "com.unity.xr.oculus",
            "com.unity.xr.windowsmr"
        };

        public void RemoveXrPackages()
        {
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted)
            {
            }

            var installedXrSdkPackages = listRequest.Result.Select(r => r.name).ToList().Intersect(xrSdkPackages).ToList();

            if (installedXrSdkPackages.Any())
            {
                foreach (var packageId in installedXrSdkPackages)
                {
                    var removePackageCallback = Client.Remove(packageId);

                    while (!removePackageCallback.IsCompleted || EditorApplication.isCompiling)
                    {
                    }

                    if (removePackageCallback.Error != null)
                    {
                        throw new Exception(string.Format(
                            "Error removing package {0}:\r\n{1}", packageId, removePackageCallback.Error.message));
                    }
                }

            }
        }

        public void RemovePackage(string packageName)
        {
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted)
            {
            }

            if (listRequest.Result.Any(p => p.name.Equals(packageName)))
            {
                var removePackageCallback = Client.Remove(packageName);

                while (!removePackageCallback.IsCompleted)
                {
                }

                if (removePackageCallback.Error != null)
                {
                    throw new Exception(string.Format(
                        "Error removing package {0}:\r\n{1}",
                        packageName,
                        removePackageCallback.Error.message));
                }
            }
        }

        public void AddPackage(string packageIdentifier)
        {
            var listRequest = Client.List();
            while (!listRequest.IsCompleted)
            {
            }
            if (!listRequest.Result.Any(p => p.name.Equals(packageIdentifier)))
            {
                var addPackageCallback = Client.Add(packageIdentifier);

                while (!addPackageCallback.IsCompleted)
                {
                }

                if (addPackageCallback.Error != null)
                {
                    throw new Exception(string.Format(
                        "Error installing package {0}:\r\n{1}",
                        packageIdentifier,
                        addPackageCallback.Error.message));
                }
            }
        }
    }
}
#endif

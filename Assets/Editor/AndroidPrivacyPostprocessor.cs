#if UNITY_ANDROID
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

/// <summary>
/// Removes network and advertising permissions from the generated Android player.
/// The game is offline, so denying these permissions is a final safeguard against
/// accidental telemetry or advertising SDK additions.
/// </summary>
internal sealed class AndroidPrivacyPostprocessor : IPostGenerateGradleAndroidProject
{
    private static readonly HashSet<string> ForbiddenPermissions = new HashSet<string>
    {
        "android.permission.INTERNET",
        "android.permission.ACCESS_NETWORK_STATE",
        "com.google.android.gms.permission.AD_ID"
    };

    public int callbackOrder => int.MaxValue;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
            return;

        var document = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
        if (document.Root == null)
            return;

        XNamespace android = "http://schemas.android.com/apk/res/android";
        var permissions = document.Root
            .Elements()
            .Where(element =>
                (element.Name.LocalName == "uses-permission" ||
                 element.Name.LocalName == "uses-permission-sdk-23") &&
                ForbiddenPermissions.Contains((string)element.Attribute(android + "name")));

        foreach (var permission in permissions.ToArray())
            permission.Remove();

        document.Save(manifestPath, SaveOptions.DisableFormatting);
    }
}
#endif

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

// Unity 2022's manifest generation forces Unity activities to exported=true.
// Restore the internal-only game entry after generation, for every Android build.
public sealed class AndroidPrivacyGradleGuard : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 10000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string file = Path.Combine(path, "src/main/AndroidManifest.xml");
        XDocument document = XDocument.Load(file);
        XNamespace android = "http://schemas.android.com/apk/res/android";
        XElement application = document.Root.Element("application");
        XElement gate = application.Elements("activity").Single(element =>
            (string)element.Attribute(android + "name") == "com.celldefense.privacy.PrivacyActivity");
        XElement game = application.Elements("activity").Single(element =>
            (string)element.Attribute(android + "name") == "com.celldefense.privacy.ConsentUnityActivity");
        gate.SetAttributeValue(android + "exported", "true");
        game.SetAttributeValue(android + "exported", "false");
        foreach (XElement intent in game.Elements("intent-filter").ToArray()) intent.Remove();
        if (application.Elements("activity").Any(element =>
            (string)element.Attribute(android + "name") == "com.unity3d.player.UnityPlayerActivity"))
            throw new InvalidOperationException("Unexpected default Unity activity bypasses the privacy launcher.");
        document.Save(file);
    }
}

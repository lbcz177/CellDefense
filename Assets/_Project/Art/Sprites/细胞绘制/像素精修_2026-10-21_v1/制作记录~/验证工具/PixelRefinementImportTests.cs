using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Validation-only fixture in the isolated project; no runtime code is added to the game.
public class PixelRefinementImportTests
{
    [Serializable] private class Job { public string source; public string destination; public string kind; }
    [Serializable] private class Plan { public Job[] jobs; }

    [Test]
    public void RefinedTexturesImportWithOriginalSpriteGeometryAndCrispSettings()
    {
        var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("E:/unity/CellDefense/Logs/ArtOptimization20261021/plan.json"));
        Assert.That(plan.jobs.Length,Is.EqualTo(75));
        foreach(var job in plan.jobs)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(job.destination);
            Assert.That(importer,Is.Not.Null,job.destination);
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point),job.destination);
            Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed),job.destination);
            Assert.That(importer.mipmapEnabled,Is.False,job.destination);
            var before=AssetDatabase.LoadAllAssetsAtPath(job.source).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            var after=AssetDatabase.LoadAllAssetsAtPath(job.destination).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            Assert.That(after.Length,Is.EqualTo(before.Length),job.destination);
            if(job.kind=="animation") Assert.That(after.Length,Is.EqualTo(10),job.destination);
            for(int i=0;i<before.Length;i++)
            {
                Assert.That(after[i].rect,Is.EqualTo(before[i].rect),job.destination);
                Assert.That(after[i].pivot,Is.EqualTo(before[i].pivot),job.destination);
                Assert.That(after[i].pixelsPerUnit,Is.EqualTo(before[i].pixelsPerUnit),job.destination);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(before[i],out string oldGuid,out long oldId);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(after[i],out string newGuid,out long newId);
                Assert.That(newId,Is.EqualTo(oldId),job.destination);
            }
        }
        string folder="Assets/_Project/Art/Sprites/细胞绘制/像素精修_2026-10-21_v1/图标";
        var icons=AssetDatabase.FindAssets("t:Texture2D",new[]{folder});
        Assert.That(icons.Length,Is.EqualTo(6));
        foreach(string guid in icons)
        {
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(icon,Is.Not.Null);
            Assert.That(icon.rect.size,Is.EqualTo(new Vector2(128,128)));
        }
    }
}

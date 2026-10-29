using System;
using UnityEngine;

public enum CellImpactKind { Neutrophil, Antibody, Phagocyte, Fibrin, Acid, Neural, Repair, Complement }

[Serializable]
public sealed class CellImpactStyle
{
    public CellImpactKind kind;
    public Sprite[] frames;
    public Sprite projectile;
    public float worldSize = .8f;
    [NonSerialized] private Sprite[] trail;
    public Sprite[] Trail => projectile == null ? null : trail ?? (trail = new[] { projectile });
}

[CreateAssetMenu(menuName = "CellDefense/Cell Impact Artwork")]
public sealed class CellImpactArtwork : ScriptableObject
{
    public Material spriteMaterial;
    public CellImpactStyle[] styles;
    public CellImpactStyle Find(CellImpactKind kind)
    {
        if (styles != null) foreach (CellImpactStyle style in styles) if (style.kind == kind) return style;
        return null;
    }
}

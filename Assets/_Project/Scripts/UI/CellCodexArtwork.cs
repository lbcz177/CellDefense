using System;
using UnityEngine;

[Serializable]
public sealed class CellCodexEntry
{
    public string id, displayName;
    public int category;
    public Sprite portrait, page, caption;
}

public sealed class CellCodexArtwork : ScriptableObject
{
    public Sprite frame, title, defenders, invaders, home, index, previous, next;
    public CellCodexEntry[] entries;
}

using UnityEngine;

// A summon is born on its parent's current segment, with its own speed and life.
public readonly struct EnemySpawnRequest
{
    public readonly EnemyController Parent;
    public readonly uint ParentVersion;
    public readonly EnemyDefinition Definition;
    public readonly WaypointPath Path;
    public readonly Vector3 Position;
    public readonly int NextPointIndex;
    public readonly int Count;
    public readonly AntigenId? Antigen;
    public EnemySpawnRequest(EnemyController parent, EnemyDefinition definition, int count, bool inheritAntigen)
    {
        Parent = parent;
        ParentVersion = parent.SpawnVersion;
        Definition = definition;
        PathFollower follower = parent.GetComponent<PathFollower>();
        Path = follower.CurrentPath;
        Position = parent.transform.position;
        NextPointIndex = follower.NextPointIndex;
        Count = count;
        Antigen = inheritAntigen ? (AntigenId?)parent.CurrentAntigenId : null;
    }
}

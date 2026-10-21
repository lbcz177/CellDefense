using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BuildPlacementKind
{
    TowerSite = 0,
    RoadSite = 1
}

public class BuildSlot : MonoBehaviour
{
    [SerializeField]
    private BuildPlacementKind placementKind = BuildPlacementKind.TowerSite;
    [SerializeField]
    private TowerController currentTower;
    [SerializeField]
    private BuildSlot[] signalNeighbors = Array.Empty<BuildSlot>();
    [SerializeField]
    private AntigenSignalFeedback signalFeedback;
    public BuildPlacementKind PlacementKind => placementKind;
    public TowerController CurrentTower => currentTower;

    public event Action<BuildSlot> Clicked;

    private void Awake()
    {
        if (signalFeedback == null)
        {
            signalFeedback = GetComponent<AntigenSignalFeedback>();
        }
    }

    public bool CanBuild()
    {
        if(CurrentTower == null)
        {
            return true;
        }
        return false;
    }

    public void Occupy(TowerController tower)
    {
        if(tower == null)
        {
            throw new ArgumentNullException(nameof(tower));
        }
        if(CanBuild() == false)
        {
            throw new InvalidOperationException("Cannot occupy a build slot that is already occupied.");
        }
        currentTower = tower;
    }

    public void Release()
    {
        currentTower = null;
    }

    public void ConfigureSignalNeighbors(BuildSlot[] neighbors)
    {
        signalNeighbors = neighbors ?? Array.Empty<BuildSlot>();
    }

    public void ShareAntigen(AntigenId antigenId, float duration)
    {
        if (duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if (signalNeighbors == null)
        {
            return;
        }
        if (currentTower == null || !currentTower.isActiveAndEnabled) return;

        foreach (BuildSlot neighbor in signalNeighbors)
        {
            if (neighbor == null || neighbor == this || neighbor.CurrentTower == null ||
                !neighbor.CurrentTower.isActiveAndEnabled)
            {
                continue;
            }

            AntibodyAttackBehaviour antibody = neighbor.CurrentTower.GetComponent<AntibodyAttackBehaviour>();
            if (antibody != null && antibody.isActiveAndEnabled)
            {
                antibody.RememberAntigen(antigenId, duration);
                // Open-field build slots are created at runtime and have no scene-wired feedback.
                if (signalFeedback == null) signalFeedback = gameObject.AddComponent<AntigenSignalFeedback>();
                signalFeedback.Show(currentTower.transform, neighbor.CurrentTower.transform);
            }
        }
    }

    void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        Clicked?.Invoke(this);
    }
}

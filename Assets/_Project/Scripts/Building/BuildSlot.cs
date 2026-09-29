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
    public BuildPlacementKind PlacementKind => placementKind;
    public TowerController CurrentTower => currentTower;

    public event Action<BuildSlot> Clicked;
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

    void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        Clicked?.Invoke(this);
    }
}

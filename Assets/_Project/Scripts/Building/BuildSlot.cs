using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildSlot : MonoBehaviour
{
    [SerializeField]
    private TowerController currentTower;
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

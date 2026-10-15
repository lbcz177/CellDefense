using UnityEngine;
using UnityEngine.EventSystems;

public class TowerBuildChoiceHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private HudController hud;
    private TowerDefinition definition;
    private System.Action entered;
    private System.Action exited;

    public void Initialize(HudController owner, TowerDefinition towerDefinition)
    {
        hud = owner;
        definition = towerDefinition;
        entered = null;
        exited = null;
    }

    public void Initialize(System.Action onEnter, System.Action onExit)
    {
        hud = null;
        definition = null;
        entered = onEnter;
        exited = onExit;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        entered?.Invoke();
        if (hud != null)
        {
            hud.ShowBuildPreview(definition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        exited?.Invoke();
        if (hud != null)
        {
            hud.HideBuildPreview(definition);
        }
    }

    private void OnDisable()
    {
        exited?.Invoke();
        if (hud != null)
        {
            hud.HideBuildPreview(definition);
        }
    }
}

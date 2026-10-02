using UnityEngine;
using UnityEngine.EventSystems;

public class TowerBuildChoiceHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private HudController hud;
    private TowerDefinition definition;

    public void Initialize(HudController owner, TowerDefinition towerDefinition)
    {
        hud = owner;
        definition = towerDefinition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hud != null)
        {
            hud.ShowBuildPreview(definition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hud != null)
        {
            hud.HideBuildPreview(definition);
        }
    }

    private void OnDisable()
    {
        if (hud != null)
        {
            hud.HideBuildPreview(definition);
        }
    }
}

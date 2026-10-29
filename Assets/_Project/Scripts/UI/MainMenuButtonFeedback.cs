using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The menu labels are baked into the background. Draw feedback over their hit areas
// instead of tinting a transparent Button image or scaling unrelated artwork.
public sealed class MainMenuButtonFeedback : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private MainMenuController owner;
    private Button button;
    private MainMenuFocusGraphic frame;
    private bool hovered, pressed;
    public bool IsHighlighted => frame != null && frame.enabled;
    public bool IsPressed => pressed && IsHighlighted;
    public void Initialize(MainMenuController menu, Button entry)
    {
        owner = menu;button = entry;
        var obj = new GameObject("Button focus feedback", typeof(RectTransform), typeof(CanvasRenderer), typeof(MainMenuFocusGraphic));
        obj.layer = gameObject.layer;obj.transform.SetParent(transform, false);
        frame = obj.GetComponent<MainMenuFocusGraphic>();frame.raycastTarget = false;
        frame.rectTransform.anchorMin = Vector2.zero;frame.rectTransform.anchorMax = Vector2.one;
        frame.rectTransform.offsetMin = new Vector2(4,4);frame.rectTransform.offsetMax = new Vector2(-4,-4);
        RefreshVisual();
    }
    public void ResetPointer() { hovered = pressed = false;RefreshVisual(); }
    public void RefreshVisual()
    {
        if (frame == null) return;
        frame.enabled = owner != null && owner.IsMainMenuInteractive && button.IsInteractable() &&
            (hovered || (owner.MainMenuKeyboardFocus && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject));
        frame.SetPressed(pressed);
    }
    public void OnPointerEnter(PointerEventData data)
    {
        if (!owner.IsMainMenuInteractive || !button.IsInteractable() || data.pointerId >= 0) return;
        owner.UseMainMenuPointer();hovered = true;RefreshVisual();
    }
    public void OnPointerExit(PointerEventData data) { hovered = pressed = false;RefreshVisual(); }
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || !owner.IsMainMenuInteractive || !button.IsInteractable()) return;
        owner.UseMainMenuPointer();hovered = pressed = true;RefreshVisual();
    }
    public void OnPointerUp(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        pressed = false;if (data.pointerId >= 0) hovered = false;RefreshVisual();
    }
    public void OnSelect(BaseEventData data) => RefreshVisual();
    public void OnDeselect(BaseEventData data) => RefreshVisual();
    private void LateUpdate() => RefreshVisual();
    private void OnDisable() { hovered = pressed = false;if (frame != null) frame.enabled = false; }
}

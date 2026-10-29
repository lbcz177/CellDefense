using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Adapts Level02's select -> stats/range -> sell -> confirm flow to open-field cells.
public sealed class OpenFieldTowerMenu : MonoBehaviour
{
    public RectTransform PanelRect => IsVisible ? panelRect : null;
    public RectTransform UpgradeButtonRect => IsVisible && !IsConfirming
        ? upgradeButton.GetComponent<RectTransform>() : null;
    private OpenFieldPrototypeController owner;
    private Camera gameplayCamera;
    private GameObject canvasObject;
    private RectTransform canvasRect, panelRect;
    private GameObject confirmation;
    private TowerController shownTower, pendingSellTower;
    private PixelImageText info, sellLabel, upgradeLabel, confirmationText;
    private Button upgradeButton;
    private int lastATP = -1, lastRefund = -1;
    private float lastHealth = -1f;
    private bool lastUpgraded;
    private float lastRepairBudget = -1f;
    public bool IsVisible => canvasObject != null && canvasObject.activeSelf;
    public bool IsConfirming => IsVisible && confirmation != null && confirmation.activeSelf;

    public void Initialize(OpenFieldPrototypeController controller, TMP_FontAsset font, Camera camera)
    {
        if (controller == null || font == null || camera == null)
            throw new ArgumentException("Tower menu needs its controller, font and gameplay camera.");
        owner = controller;
        gameplayCamera = camera;
        canvasObject = new GameObject("Tower Actions Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = .5f;
        MobileDisplay.ConfigureCanvas(canvas);
        canvasRect = canvasObject.GetComponent<RectTransform>();
        Image panel = Image("Tower Details", canvasRect, new Color(.1f, .14f, .21f, .97f));
        PromptImageResources.StylePanel(panel);
        panelRect = panel.rectTransform;
        Rect(panelRect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(280f, MobileDisplay.IsActive ? 378f : 334f));
        info = Label("Tower Stats", panelRect, font, 17f);
        info.alignment = TextAlignmentOptions.TopLeft;
        Rect(info.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(252f, 200f));
        Button close = Button("Close Tower", panelRect, font, "×", new Color(.25f, .32f, .4f), owner.ClearSelection);
        Rect(close.GetComponent<RectTransform>(), Vector2.one, Vector2.one, new Vector2(-4f, -4f), new Vector2(MobileDisplay.IsActive ? 48f : 28f, MobileDisplay.IsActive ? 48f : 28f));
        Button sell = Button("Sell Tower", panelRect, font, "出售", new Color(.55f, .18f, .2f), RequestSell);
        Rect(sell.GetComponent<RectTransform>(), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 12f), new Vector2(252f, MobileDisplay.IsActive ? 56f : 38f));
        sellLabel = sell.GetComponentInChildren<PixelImageText>();
        upgradeButton = Button("Upgrade Tower", panelRect, font, "升级", new Color(.18f, .4f, .52f), UpgradeTower);
        Rect(upgradeButton.GetComponent<RectTransform>(), new Vector2(.5f, 0f), new Vector2(.5f, 0f),
            new Vector2(0f, MobileDisplay.IsActive ? 78f : 58f), new Vector2(252f, MobileDisplay.IsActive ? 56f : 38f));
        upgradeLabel = upgradeButton.GetComponentInChildren<PixelImageText>();

        Image backdrop = Image("Sell Confirmation", canvasRect, new Color(0f, 0f, 0f, .55f));
        confirmation = backdrop.gameObject;
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
        Button cancelBackdrop = backdrop.gameObject.AddComponent<Button>();
        cancelBackdrop.targetGraphic = backdrop;
        cancelBackdrop.transition = Selectable.Transition.None;
        cancelBackdrop.onClick.AddListener(CancelSell);
        Image dialog = Image("Dialog", backdrop.transform, new Color(.1f, .14f, .21f, 1f));
        PromptImageResources.StylePanel(dialog);
        Button dialogBlocker = dialog.gameObject.AddComponent<Button>();
        dialogBlocker.targetGraphic = dialog;
        dialogBlocker.transition = Selectable.Transition.None;
        Rect(dialog.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(380f, 190f));
        confirmationText = Label("Refund Amount", dialog.transform, font, 23f);
        Rect(confirmationText.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -16f), new Vector2(350f, 104f));
        Button confirm = Button("Confirm Sell", dialog.transform, font, "确认出售", new Color(.55f, .18f, .2f), ConfirmSell);
        Rect(confirm.GetComponent<RectTransform>(), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(-88f, 18f), new Vector2(156f, MobileDisplay.IsActive ? 56f : 38f));
        Button cancel = Button("Cancel Sell", dialog.transform, font, "取消", new Color(.25f, .32f, .4f), CancelSell);
        Rect(cancel.GetComponent<RectTransform>(), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(88f, 18f), new Vector2(156f, MobileDisplay.IsActive ? 56f : 38f));
        Hide();
    }

    public void Show(TowerController tower)
    {
        shownTower = tower;
        lastATP = lastRefund = -1;
        lastHealth = -1f;
        CancelSell();
        canvasObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Refresh();
    }

    public void RequestSell()
    {
        if (!IsVisible || !owner.CanBuild || shownTower == null || owner.SelectedTower != shownTower) return;
        pendingSellTower = shownTower;
        confirmation.SetActive(true);
        Refresh();
    }

    public void UpgradeTower()
    {
        if (!IsVisible || IsConfirming || shownTower == null || owner.SelectedTower != shownTower ||
            !owner.SelectedTowerCell.HasValue) return;
        owner.TryUpgradeAt(owner.SelectedTowerCell.Value);
        Refresh();
    }

    public void ConfirmSell()
    {
        TowerController promisedTower = pendingSellTower;
        Vector2Int? cell = owner.SelectedTowerCell;
        CancelSell();
        if (promisedTower != null && owner.SelectedTower == promisedTower && cell.HasValue)
            owner.TrySellAt(cell.Value);
    }

    public void CancelSell()
    {
        pendingSellTower = null;
        if (confirmation != null) confirmation.SetActive(false);
    }

    public void Hide()
    {
        CancelSell();
        shownTower = null;
        if (canvasObject != null) canvasObject.SetActive(false);
    }

    public bool ContainsPointer(Vector2 screenPosition) => IsVisible && (IsConfirming ||
        RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, null));

    public void Refresh()
    {
        if (!IsVisible) return;
        if (!owner.CanBuild || shownTower == null || owner.SelectedTower != shownTower)
        {
            Hide();
            return;
        }
        TowerCombatState state = shownTower.GetComponent<TowerCombatState>();
        int refund = owner.GetSellRefund(owner.SelectedTowerCell.Value);
        int upgradeCost = owner.GetUpgradeCost(owner.SelectedTowerCell.Value);
        upgradeButton.interactable = !IsConfirming && owner.CanUpgradeAt(owner.SelectedTowerCell.Value);
        upgradeButton.image.color = upgradeButton.interactable ? Color.white : new Color(1f, 1f, 1f, .45f);
        if (lastATP != owner.CurrentATP || lastHealth != state.CurrentHealth ||
            lastRefund != refund || lastUpgraded != shownTower.IsUpgraded || lastRepairBudget != state.RepairBudgetRemaining)
        {
            lastATP = owner.CurrentATP;
            lastHealth = state.CurrentHealth;
            lastRefund = refund;
            lastUpgraded = shownTower.IsUpgraded;
            lastRepairBudget = state.RepairBudgetRemaining;
            info.text = $"{shownTower.Definition.DisplayName}  Lv.{(shownTower.IsUpgraded ? 2 : 1)}\n" +
                $"生命  {state.CurrentHealth:0}/{state.MaxHealth:0}" +
                (state.InterceptsAllEnemies ? $" · 可修补 {state.RepairBudgetRemaining:0}\n" : "\n") +
                (state.InterceptsAllEnemies ? "拦截  全部经过的敌人\n" : $"拦截容量  {state.Capacity}\n") +
                $"射程  {shownTower.Definition.AttackRange:0.#}\n" +
                $"伤害  {shownTower.CurrentDamage:0.#}\n" +
                $"攻击间隔  {shownTower.Definition.AttackInterval:0.##} 秒\n" +
                (shownTower.IsUpgraded ? "升级  已满级\n" : !shownTower.SupportsDamageUpgrade ? "升级  暂未开放\n" :
                    $"升级后伤害  {shownTower.CurrentDamage * (1f + TowerController.UpgradeDamageBonusPercent / 100f):0.#}" +
                    (owner.CurrentATP < upgradeCost ? "（ATP不足）\n" : "\n")) +
                $"出售返还  {refund} ATP";
            sellLabel.text = $"出售 +{refund} ATP";
            upgradeLabel.text = shownTower.IsUpgraded ? "已满级" : !shownTower.SupportsDamageUpgrade
                ? "暂未开放升级" : $"升级 {upgradeCost} ATP";
        }
        if (IsConfirming)
        {
            if (pendingSellTower != shownTower) CancelSell();
            else confirmationText.text = $"出售 {shownTower.Definition.DisplayName}？\n返还 {refund} ATP";
        }
        PositionPanel();
    }

    private void PositionPanel()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
            gameplayCamera.WorldToScreenPoint(shownTower.transform.position), null, out Vector2 point);
        Rect safe = MobileDisplay.SafeArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safe.min, null, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safe.max, null, out Vector2 max);
        Vector2 half = panelRect.sizeDelta * .5f;
        float x = point.x + half.x + 48f;
        if (x + half.x > max.x - 8f) x = point.x - half.x - 48f;
        float yMax = max.y - half.y - 8f;
        UnityEngine.Rect hud = owner.BattleHudScreenRect;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, hud.min, null, out Vector2 hudMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, hud.max, null, out Vector2 hudMax);
        x = Mathf.Clamp(x, min.x + half.x + 8f, max.x - half.x - 8f);
        if (hud.width > 0f && x + half.x > hudMin.x && x - half.x < hudMax.x)
            yMax = Mathf.Min(yMax, hudMin.y - half.y - 8f);
        float yMin = min.y + half.y + 8f;
        panelRect.anchoredPosition = new Vector2(x, yMin <= yMax ? Mathf.Clamp(point.y, yMin, yMax) : (min.y + max.y) * .5f);
    }

    private void LateUpdate() => Refresh();
    private void OnDisable() => Hide();
    private static Image Image(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }
    private static PixelImageText Label(string name, Transform parent, TMP_FontAsset font, float size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(PixelImageText));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        PixelImageText text = obj.GetComponent<PixelImageText>();

        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
    private static Button Button(string name, Transform parent, TMP_FontAsset font, string caption, Color color, UnityEngine.Events.UnityAction action)
    {
        Image background = Image(name, parent, color);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        PromptImageResources.StyleButton(button);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() =>
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            action();
        });
        PixelImageText text = Label("Label", background.transform, font, 18f);
        text.text = caption;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
    private static void Rect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}

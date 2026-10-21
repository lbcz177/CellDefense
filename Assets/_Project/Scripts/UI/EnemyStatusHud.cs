using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One canvas per level; each pooled enemy keeps and reuses its existing row.
public sealed class EnemyStatusHud : MonoBehaviour
{
    private HashSet<EnemyController> enemies;
    private Camera camera;
    private TMP_FontAsset font;
    private IEnumerable<TowerCombatState> defenders;
    private readonly Dictionary<TowerCombatState, Row> defenderRows = new Dictionary<TowerCombatState, Row>();
    private readonly List<TowerCombatState> expiredDefenders = new List<TowerCombatState>();
    private RectTransform canvasRect;
    private readonly Dictionary<EnemyController, Row> rows = new Dictionary<EnemyController, Row>();
    private readonly HashSet<EnemyDefinition> announced = new HashSet<EnemyDefinition>();
    private readonly Queue<string> hints = new Queue<string>();
    private TextMeshProUGUI hint;
    private float hintUntil;
    private float allowEmptyHintUntil;

    public void Initialize(HashSet<EnemyController> activeEnemies, Camera gameplayCamera, TMP_FontAsset textFont,
        IEnumerable<TowerCombatState> cells = null)
    {
        enemies = activeEnemies;
        camera = gameplayCamera;
        font = textFont;
        defenders = cells;
        GameObject obj = new GameObject("Enemy Status Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        obj.transform.SetParent(transform, false);
        obj.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        MobileDisplay.ConfigureCanvas(canvas);
        canvasRect = obj.GetComponent<RectTransform>();
        GameObject notice = new GameObject("Enemy Introduction", typeof(RectTransform), typeof(Image));
        notice.transform.SetParent(canvasRect, false);
        notice.layer = obj.layer;
        RectTransform noticeRect = notice.GetComponent<RectTransform>();
        noticeRect.anchorMin = noticeRect.anchorMax = noticeRect.pivot = Vector2.one;
        noticeRect.anchoredPosition = new Vector2(-12f, -12f);
        noticeRect.sizeDelta = new Vector2(320f, 90f);
        notice.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.21f, 0.94f);
        notice.GetComponent<Image>().raycastTarget = false;
        GameObject text = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(noticeRect, false);
        text.layer = obj.layer;
        hint = text.GetComponent<TextMeshProUGUI>();
        hint.font = font;
        hint.fontSize = 18f;
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        hint.raycastTarget = false;
        hint.rectTransform.anchorMin = Vector2.zero;
        hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.offsetMin = new Vector2(10f, 6f);
        hint.rectTransform.offsetMax = new Vector2(-10f, -6f);
        notice.SetActive(false);
    }

    public void Announce(EnemyDefinition definition)
    {
        if (!announced.Add(definition)) return;
        if (definition.IsBoss)
        {
            hints.Clear();
            hint.transform.parent.gameObject.SetActive(false);
            allowEmptyHintUntil = Time.time + 4f;
        }
        string detail;
        if (definition.IsBoss)
            detail = definition.AntigenId == AntigenId.Fungus ? "受伤时播散孢子，留出清扫火力。" :
                definition.AntigenId == AntigenId.Biofilm ? "为附近同伴补盾和治疗，兼顾两路压力。" :
                "复制小单位，变异后需要重新采样。";
        else if (definition.ArmorReduction > 0f) detail = "外壳减伤，标记配合吞噬；酸化能破壳。";
        else if (definition.AntigenId == AntigenId.Flagellate) detail = "移动很快，接力火力与减速更有效。";
        else if (definition.KillReward == 0) detail = "召唤单位也会伤害核心，需要及时清扫。";
        else detail = "成群进入，注意覆盖不同入口。";
        hints.Enqueue(definition.DisplayName + (definition.IsBoss ? " 即将进入" : "") + "\n" + detail);
    }

    public void ClearHints()
    {
        hints.Clear();
        allowEmptyHintUntil = 0f;
        if (hint != null) hint.transform.parent.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (enemies == null || camera == null) return;
        if (hint.transform.parent.gameObject.activeSelf && Time.time >= hintUntil)
            hint.transform.parent.gameObject.SetActive(false);
        if (!hint.transform.parent.gameObject.activeSelf && hints.Count > 0 &&
            (enemies.Count > 0 || Time.time < allowEmptyHintUntil))
        {
            hint.text = hints.Dequeue();
            hintUntil = Time.time + 4f;
            hint.transform.parent.gameObject.SetActive(true);
        }
        if (enemies.Count == 0 && Time.time >= allowEmptyHintUntil)
        {
            hints.Clear();
            hint.transform.parent.gameObject.SetActive(false);
        }
        if (MobileDisplay.IsActive)
        {
            Rect safe = MobileDisplay.LocalSafeArea(canvasRect);
            RectTransform notice = (RectTransform)hint.transform.parent;
            notice.anchoredPosition = new Vector2(safe.xMax - canvasRect.rect.xMax - 12f,
                safe.yMax - canvasRect.rect.yMax - 92f);
        }
        Vector3 screenMouse = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
        screenMouse.z = -camera.transform.position.z;
        Vector3 worldMouse = camera.ScreenToWorldPoint(screenMouse);
        int frame = Time.frameCount;
        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.CanBeTargeted ||
                enemy.Definition == null) continue;
            bool hovered = (!MobileDisplay.IsActive || Input.touchCount > 0) && enemy.ContainsVisualPoint(worldMouse);
            bool boss = enemy.Definition.IsBoss;
            if (!boss && !hovered && !enemy.HasShield && enemy.CurrentHealth >= enemy.MaxHealth) continue;
            if (!rows.TryGetValue(enemy, out Row row))
            {
                row = CreateRow(boss);
                rows.Add(enemy, row);
            }
            row.SeenAt = frame;
            row.Root.gameObject.SetActive(true);
            Vector3 position = enemy.transform.position + Vector3.up * (boss ? 1.15f : 0.62f);
            Vector2 screen = camera.WorldToScreenPoint(position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            row.Root.anchoredPosition = local;
            row.Health.anchorMax = new Vector2(Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth), 1f);
            row.ShieldRoot.gameObject.SetActive(enemy.HasShield);
            if (enemy.HasShield)
                row.Shield.anchorMax = new Vector2(Mathf.Clamp01(enemy.CurrentShield /
                    Mathf.Max(1f, enemy.ShieldCapacity)), 1f);

            string phase = boss ? enemy.BossPhaseLabel : string.Empty;
            string warning = boss ? enemy.AbilityWarning : string.Empty;
            bool showText = boss || hovered;
            row.Label.gameObject.SetActive(showText);
            if (showText && (row.LastHealth != enemy.CurrentHealth || row.LastShield != enemy.CurrentShield ||
                row.LastPhase != phase || row.LastWarning != warning || row.LastAntigen != enemy.CurrentAntigenId ||
                row.LastBroken != enemy.IsArmorBroken))
            {
                row.LastHealth = enemy.CurrentHealth;
                row.LastShield = enemy.CurrentShield;
                row.LastPhase = phase;
                row.LastWarning = warning;
                row.LastAntigen = enemy.CurrentAntigenId;
                row.LastBroken = enemy.IsArmorBroken;
                string detail = boss ? phase : enemy.IsArmorBroken ? "外壳已破" : string.Empty;
                if (enemy.CurrentAntigenId == AntigenId.VirusA) detail += "  抗原 A";
                if (enemy.CurrentAntigenId == AntigenId.VirusB) detail += "  抗原 B";
                row.Label.text = boss
                    ? $"{enemy.Definition.DisplayName}  {enemy.CurrentHealth:0}/{enemy.MaxHealth:0}\n{detail}\n{warning}"
                    : enemy.Definition.DisplayName + (string.IsNullOrEmpty(detail) ? "" : "\n" + detail);
            }
        }
        foreach (Row row in rows.Values)
            if (row.SeenAt != frame && row.Root.gameObject.activeSelf) row.Root.gameObject.SetActive(false);
        UpdateDefenders(worldMouse);
    }

    private void UpdateDefenders(Vector3 worldMouse)
    {
        if (defenders == null) return;
        foreach (TowerCombatState defender in defenders)
        {
            if (defender == null || !defender.IsAlive) continue;
            bool hovered = (!MobileDisplay.IsActive || Input.touchCount > 0) &&
                (defender.transform.position - worldMouse).sqrMagnitude < 0.3f;
            bool visible = hovered || defender.ContactCount > 0 || defender.CurrentHealth < defender.MaxHealth;
            if (!defenderRows.TryGetValue(defender, out Row row))
            {
                if (!visible) continue;
                row = CreateRow(false);
                defenderRows.Add(defender, row);
                row.Health.GetComponent<Image>().color = new Color(0.3f, 0.85f, 1f);
            }
            row.Root.gameObject.SetActive(visible);
            if (!visible) continue;
            Vector2 screen = camera.WorldToScreenPoint(defender.transform.position + Vector3.down * 0.55f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            row.Root.anchoredPosition = local;
            row.Health.anchorMax = new Vector2(Mathf.Clamp01(defender.CurrentHealth / defender.MaxHealth), 1f);
            row.ShieldRoot.gameObject.SetActive(false);
            row.Label.gameObject.SetActive(hovered || defender.ContactCount > 0);
            if (row.LastHealth != defender.CurrentHealth || row.LastShield != defender.ContactCount)
            {
                row.LastHealth = defender.CurrentHealth;
                row.LastShield = defender.ContactCount;
                string role = defender.InterceptsAllEnemies ? $"拦截 {defender.ContactCount} · 无上限" :
                    $"接敌 {defender.ContactCount}/{defender.Capacity}";
                row.Label.text = $"{defender.CurrentHealth:0}/{defender.MaxHealth:0}  {role}";
            }
        }
        expiredDefenders.Clear();
        foreach (KeyValuePair<TowerCombatState, Row> pair in defenderRows)
            if (pair.Key == null || !pair.Key.IsAlive)
            {
                Destroy(pair.Value.Root.gameObject);
                expiredDefenders.Add(pair.Key);
            }
        foreach (TowerCombatState defender in expiredDefenders) defenderRows.Remove(defender);
    }

    private Row CreateRow(bool boss)
    {
        GameObject obj = new GameObject("Enemy Status", typeof(RectTransform));
        obj.transform.SetParent(canvasRect, false);
        obj.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(boss ? 190f : 66f, 12f);
        RectTransform hpRoot = CreateBar(rect, "Health", 0f, new Color(0.3f, 0.95f, 0.45f), out RectTransform hp);
        RectTransform shieldRoot = CreateBar(rect, "Shield", 7f, new Color(0.25f, 0.65f, 1f), out RectTransform shield);
        GameObject labelObj = new GameObject("Name and Warning", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.layer = obj.layer;
        labelObj.transform.SetParent(rect, false);
        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = boss ? 16f : 13f;
        label.alignment = TextAlignmentOptions.Bottom;
        label.color = Color.white;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 7f);
        label.rectTransform.sizeDelta = new Vector2(boss ? 270f : 140f, boss ? 65f : 36f);
        return new Row { Root = rect, Health = hp, ShieldRoot = shieldRoot, Shield = shield, Label = label };
    }

    private static RectTransform CreateBar(RectTransform parent, string name, float y,
        Color color, out RectTransform fill)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 5f);
        rect.anchoredPosition = new Vector2(0f, y);
        Image background = obj.GetComponent<Image>();
        background.color = new Color(0.05f, 0.08f, 0.1f, 0.85f);
        background.raycastTarget = false;
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.layer = obj.layer;
        fillObj.transform.SetParent(rect, false);
        fill = fillObj.GetComponent<RectTransform>();
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fillObj.GetComponent<Image>().color = color;
        fillObj.GetComponent<Image>().raycastTarget = false;
        return rect;
    }

    private sealed class Row
    {
        public RectTransform Root, Health, ShieldRoot, Shield;
        public TextMeshProUGUI Label;
        public int SeenAt = -1;
        public float LastHealth = -1f, LastShield = -1f;
        public string LastPhase, LastWarning;
        public AntigenId LastAntigen;
        public bool LastBroken;
    }

    private void OnDisable()
    {
        ClearHints();
        foreach (Row row in rows.Values)
            if (row.Root != null) row.Root.gameObject.SetActive(false);
        foreach (Row row in defenderRows.Values)
            if (row.Root != null) row.Root.gameObject.SetActive(false);
    }
}

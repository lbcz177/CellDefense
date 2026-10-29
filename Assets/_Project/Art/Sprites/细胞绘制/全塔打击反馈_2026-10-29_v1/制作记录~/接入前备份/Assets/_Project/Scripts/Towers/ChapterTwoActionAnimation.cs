using UnityEngine;

// Support actions use their real repair/assembly clocks, rather than TowerController's projectile trigger.
[DisallowMultipleComponent, DefaultExecutionOrder(50)]
public sealed class ChapterTwoActionAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] actionFrames = new Sprite[0];
    [SerializeField] private Sprite[] contactFrames = new Sprite[0];
    [SerializeField] private bool lysis;
    private static readonly float[] ReleaseEnds = { .04f, .105f, .145f, .20f, .27f, .37f };
    private static readonly float[] ContactEnds = { .03f, .07f, .115f, .17f, .235f, .31f };
    private SpriteRenderer body, contact;
    private Sprite rest;
    private OpenFieldPrototypeController owner;
    private float release = -1f, impact = -1f;
    private int startedFrame = -1;
    private Vector3 contactPosition;

    private void Awake()
    {
        body = GetComponent<SpriteRenderer>();rest = body != null ? body.sprite : null;
        owner = FindObjectOfType<OpenFieldPrototypeController>();
    }

    public void Configure(Sprite[] actions, Sprite[] contacts, bool isLysis)
    {
        actionFrames = actions;contactFrames = contacts;lysis = isLysis;
    }

    public void SetCharge(float fraction)
    {
        if (release >= 0f || body == null || actionFrames.Length != 10) return;
        float value = Mathf.Clamp01(fraction);
        body.sprite = value <= 0f ? rest : actionFrames[Mathf.Min(3, Mathf.FloorToInt(value * 4f))];
    }

    public void ClearCharge()
    {
        if (release < 0f && body != null) body.sprite = rest;
    }

    // Called only after a successful repair or when the assembly has completed.
    public void PlayContact(Vector3 position)
    {
        if (!isActiveAndEnabled || body == null || actionFrames.Length != 10 || contactFrames.Length != 6) return;
        release = impact = 0f;startedFrame = Time.frameCount;contactPosition = position;body.sprite = actionFrames[4];
        if (contact == null)
        {
            var obj = new GameObject(lysis ? "Lysis contact pixels" : "Repair contact pixels");
            obj.transform.SetParent(transform, false);contact = obj.AddComponent<SpriteRenderer>();
            contact.sharedMaterial = body.sharedMaterial;contact.sortingLayerID = body.sortingLayerID;
            contact.sortingOrder = body.sortingOrder + 16;
        }
        contact.sprite = contactFrames[0];contact.enabled = true;PlaceContact();
    }

    private void PlaceContact()
    {
        contact.transform.position = contactPosition;contact.transform.rotation = Quaternion.identity;
        float size = lysis ? 1.15f : .90f;
        Vector3 parentScale = transform.lossyScale;
        contact.transform.localScale = new Vector3(size / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
            size / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), 1f);
    }

    private void LateUpdate()
    {
        if (owner == null || !owner.HasActiveRun) { Stop();return; }
        if (owner.IsPaused || Time.deltaTime <= 0f) return;
        // Always render the sharp first contact pose, including on a slow frame or at 2x speed.
        if (Time.frameCount == startedFrame) return;
        if (release >= 0f)
        {
            release += Time.deltaTime;int frame = FrameAt(release, ReleaseEnds);
            if (frame >= ReleaseEnds.Length) { release = -1f;body.sprite = rest; }
            else body.sprite = actionFrames[4 + frame];
        }
        if (impact >= 0f)
        {
            impact += Time.deltaTime;int frame = FrameAt(impact, ContactEnds);
            if (frame >= ContactEnds.Length) { impact = -1f;contact.enabled = false; }
            else { contact.sprite = contactFrames[frame];PlaceContact(); }
        }
    }

    private static int FrameAt(float seconds, float[] ends)
    {
        for (int i = 0; i < ends.Length; i++) if (seconds < ends[i]) return i;
        return ends.Length;
    }

    public void Stop()
    {
        release = impact = -1f;
        if (body != null) body.sprite = rest;
        if (contact != null) contact.enabled = false;
    }
    private void OnDisable() => Stop();
}

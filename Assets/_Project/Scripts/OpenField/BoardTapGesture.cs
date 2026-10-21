using UnityEngine;

// One completed tap selects a board cell. UI gestures, dragging and multiple fingers cancel it.
public sealed class BoardTapGesture
{
    private int finger = -1;
    private Vector2 origin;
    private bool cancelled, multiple;
    public void Reset()
    {
        finger = -1;
        cancelled = multiple = false;
    }
    public bool Process(int touchCount, int fingerId, TouchPhase phase, Vector2 position,
        bool blocked, float dragThreshold)
    {
        if (touchCount == 0) { Reset(); return false; }
        if (touchCount > 1) { multiple = true; finger = -1; return false; }
        if (multiple) return false;
        if (phase == TouchPhase.Began)
        {
            finger = fingerId;
            origin = position;
            cancelled = blocked;
            return false;
        }
        if (finger != fingerId) return false;
        if (blocked || (position - origin).sqrMagnitude > dragThreshold * dragThreshold) cancelled = true;
        if (phase == TouchPhase.Canceled) { Reset(); return false; }
        if (phase != TouchPhase.Ended) return false;
        bool accepted = !cancelled;
        Reset();
        return accepted;
    }
}

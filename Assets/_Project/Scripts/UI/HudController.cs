using UnityEngine;

public class HudController : MonoBehaviour
{
    public void Initialize(GameFlowController flow, EconomyService economy)
    {
        // TODO: Store the read-only sources displayed by the HUD.
    }

    private void OnEnable()
    {
        // TODO: Subscribe to display events while the HUD is enabled.
    }

    private void OnDisable()
    {
        // TODO: Unsubscribe from every event subscribed in OnEnable.
    }

    private void RefreshATP(int currentATP)
    {
        // TODO: Update only the ATP presentation.
    }
}

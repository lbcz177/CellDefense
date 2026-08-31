using UnityEngine;
using TMPro;
using System;

public class HudController : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI atpText;
    private EconomyService economyService;
    private bool isSubscribed;
    public void Initialize(EconomyService economy)
    {
        if(economy == null)
        {
            throw new ArgumentNullException(nameof(economy));
        }
        if(atpText == null)
        {
            throw new InvalidOperationException("ATP Text reference is not set in the inspector.");
        }
        economyService = economy;
        RefreshATP(economyService.CurrentATP);
        if(isActiveAndEnabled)
        {
            Subscribe();
        }
        
    }

    private void OnEnable()
    {
        Subscribe();

        if(economyService != null)
        {
            RefreshATP(economyService.CurrentATP);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void RefreshATP(int currentATP)
    {
        if(atpText == null)
        {
            throw new InvalidOperationException("ATP Text reference is not set in the inspector.");
        }
        atpText.text = $"ATP: {currentATP}";
    }

    private void Subscribe()
    {
        if(economyService != null && !isSubscribed)
        {
            economyService.ATPChanged += RefreshATP;
            isSubscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if(economyService != null && isSubscribed)
        {
            economyService.ATPChanged -= RefreshATP;
            isSubscribed = false;
        }
    }
}


using UnityEngine;
using System;

public class GameplayCompositionRoot : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int initialATP = 100;
    [SerializeField]
    private BuildController buildController;
    private EconomyService economyService;
    [SerializeField]
    private HudController hudController;
    
    private void Awake()
    {
        if(buildController == null)
        {
            throw new InvalidOperationException("BuildController reference is not set in the inspector.");
        }
        if(hudController == null)
        {
            throw new InvalidOperationException("HudController reference is not set in the inspector.");
        }
        economyService = new EconomyService();
        economyService.Initialize(initialATP);

        buildController.Initialize(economyService);
        hudController.Initialize(economyService);
    }


}

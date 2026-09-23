using System;
using UnityEngine;

public class GameplayCompositionRoot : MonoBehaviour
{
    [SerializeField, Min(0)] private int initialATP = 100;
    [SerializeField, Min(1)] private int initialLife = 10;
    [SerializeField] private BuildController buildController;
    [SerializeField] private HudController hudController;
    [SerializeField] private WaveController waveController;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ProjectilePool projectilePool;
    private EconomyService economyService;
    private LifeService lifeService;

    private void Awake()
    {
        if (buildController == null)
        {
            throw new InvalidOperationException("BuildController reference is not set in the inspector.");
        }
        if (hudController == null)
        {
            throw new InvalidOperationException("HudController reference is not set in the inspector.");
        }
        if (waveController == null)
        {
            throw new InvalidOperationException("WaveController reference is not set in the inspector.");
        }
        if (gameFlowController == null)
        {
            throw new InvalidOperationException("GameFlowController reference is not set in the inspector.");
        }
        if (projectilePool == null)
        {
            throw new InvalidOperationException("ProjectilePool reference is not set in the inspector.");
        }

        economyService = new EconomyService();
        economyService.Initialize(initialATP);
        lifeService = new LifeService();
        lifeService.Initialize(initialLife);
        waveController.Initialize(economyService, lifeService);
        gameFlowController.Initialize(lifeService, waveController);
        projectilePool.Initialize();
        buildController.Initialize(economyService, gameFlowController, projectilePool);
        hudController.Initialize(economyService, lifeService, gameFlowController, waveController, buildController);
    }

    private void Start()
    {
        gameFlowController.StartRun();
    }
}

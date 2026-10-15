using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FungalBossBehaviour : BossAbilityBehaviour
{
    [SerializeField] private EnemyDefinition sporeDefinition;
    private bool firstIssued;
    private bool secondIssued;
    public int SummonsIssued { get; private set; }
    public override void Initialize(EnemyDefinition definition)
    {
        if (sporeDefinition == null || sporeDefinition.EnemyPrefab == null)
            throw new ArgumentException("Assign a spore definition and prefab.");
        base.Initialize(definition);
    }
    public override void ResetState()
    {
        firstIssued = secondIssued = false;
        SummonsIssued = 0;
    }
    public override void ExecutePhase(int index)
    {
        if (!IsCurrent) return;
        if (index == 1 && !firstIssued)
        {
            firstIssued = true;
            SummonsIssued += 3;
            Enemy.RequestSummon(sporeDefinition, 3, true);
        }
        else if (index == 2 && !secondIssued)
        {
            secondIssued = true;
            SummonsIssued += 3;
            Enemy.SetAbilitySpeedMultiplier(1.2f);
            Enemy.RequestSummon(sporeDefinition, 3, true);
        }
    }
}

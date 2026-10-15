using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VirusBossBehaviour : BossAbilityBehaviour
{
    [SerializeField] private EnemyDefinition cloneDefinition;
    private bool firstIssued;
    private bool secondIssued;
    private bool mutated;
    public int SummonsIssued { get; private set; }
    public override void Initialize(EnemyDefinition definition)
    {
        if (cloneDefinition == null || cloneDefinition.EnemyPrefab == null)
            throw new ArgumentException("Assign a clone definition and prefab.");
        base.Initialize(definition);
    }
    public override void ResetState()
    {
        firstIssued = secondIssued = mutated = false;
        SummonsIssued = 0;
    }
    public override void ExecutePhase(int index)
    {
        if (!IsCurrent) return;
        if (index == 0 && !firstIssued)
        {
            firstIssued = true;
            SummonsIssued += 4;
            Enemy.RequestSummon(cloneDefinition, 4, true);
        }
        else if (index == 1 && !mutated)
        {
            mutated = true;
            Enemy.ChangeAntigen(AntigenId.VirusB);
        }
        else if (index == 2 && !secondIssued)
        {
            secondIssued = true;
            if (!mutated)
            {
                mutated = true;
                Enemy.ChangeAntigen(AntigenId.VirusB);
            }
            SummonsIssued += 4;
            Enemy.SetAbilitySpeedMultiplier(1.2f);
            Enemy.RequestSummon(cloneDefinition, 4, true);
        }
    }
}

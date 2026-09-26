using System;
using NUnit.Framework;

[TestFixture]
public class EconomyServiceTests
{
    private EconomyService economyService;

    [SetUp]
    public void SetUp()
    {
        economyService = new EconomyService();
    }

    [Test]
    public void TrySpend_WhenBalanceIsSufficient_DeductsATPAndReturnsTrue()
    {
        const int initialATP = 100;
        const int amount = 25;

        economyService.Initialize(initialATP);
        bool spendSucceeded = economyService.TrySpend(amount);

        Assert.That(spendSucceeded, Is.True);
        Assert.That(economyService.CurrentATP, Is.EqualTo(initialATP - amount));
    }

    [Test]
    public void TrySpend_WhenBalanceIsInsufficient_DoesNotDeductATPAndReturnsFalse()
    {
        const int initialATP = 20;
        const int amount = 25;

        economyService.Initialize(initialATP);
        bool spendSucceeded = economyService.TrySpend(amount);

        Assert.That(spendSucceeded, Is.False);
        Assert.That(economyService.CurrentATP, Is.EqualTo(initialATP));
    }

    [Test]
    public void TrySpend_WhenBalanceIsInsufficient_DoesNotRaiseATPChanged()
    {
        economyService.Initialize(20);
        int notificationCount = 0;
        economyService.ATPChanged += _ => notificationCount++;

        economyService.TrySpend(25);

        Assert.That(notificationCount, Is.Zero);
    }

    [Test]
    public void TrySpend_WhenSuccessful_RaisesATPChangedOnceWithRemainingATP()
    {
        const int initialATP = 100;
        const int amount = 25;
        int notificationCount = 0;
        int observedATP = -1;


        economyService.Initialize(initialATP);

        economyService.ATPChanged += currentATP =>
        {
            notificationCount++;
            observedATP = currentATP;
        };

        economyService.TrySpend(amount);

        Assert.That(notificationCount, Is.EqualTo(1));
        Assert.That(observedATP, Is.EqualTo(initialATP - amount));
    }

    [Test]
    public void TrySpend_WhenAmountIsZero_ThrowsArgumentOutOfRangeException()
    {
        const int initialATP = 20;

        economyService.Initialize(initialATP);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => economyService.TrySpend(0));
    }
}

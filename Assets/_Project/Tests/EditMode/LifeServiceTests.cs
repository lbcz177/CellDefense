using NUnit.Framework;

[TestFixture]
public class LifeServiceTests
{
    [Test]
    public void TryLoseLife_AfterLifeReachesZero_DoesNotSettleAgain()
    {
        var lifeService = new LifeService();
        lifeService.Initialize(1);

        int notificationCount = 0;
        int observedLife = -1;
        lifeService.LifeChanged += currentLife =>
        {
            notificationCount++;
            observedLife = currentLife;
        };

        bool firstLossSucceeded = lifeService.TryLoseLife(2);
        bool secondLossSucceeded = lifeService.TryLoseLife(1);

        Assert.That(firstLossSucceeded, Is.True);
        Assert.That(secondLossSucceeded, Is.False);
        Assert.That(lifeService.CurrentLife, Is.Zero);
        Assert.That(lifeService.IsDepleted, Is.True);
        Assert.That(notificationCount, Is.EqualTo(1));
        Assert.That(observedLife, Is.Zero);
    }
}

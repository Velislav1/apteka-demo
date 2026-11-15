using Apteka.SharedKernel;

namespace Apteka.SharedKernel.Tests;

public class ResultAndEntityTests
{
    private sealed class Thing(int id) : Entity<int>(id);

    private sealed record Happened : IDomainEvent
    {
        public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
    }

    private sealed class Raiser() : Entity<int>(1)
    {
        public void Do() => Raise(new Happened());
    }

    [Fact]
    public void Failure_carries_error_and_hides_value()
    {
        var result = Result.Failure<int>(new Error("x", "boom"));

        Assert.True(result.IsFailure);
        Assert.Equal("x", result.Error.Code);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Success_exposes_value()
    {
        Assert.Equal(5, Result.Success(5).Value);
        Assert.True(Result.Success().IsSuccess);
    }

    [Fact]
    public void Entities_with_same_type_and_id_are_equal()
    {
        Assert.Equal(new Thing(1), new Thing(1));
        Assert.NotEqual(new Thing(1), new Thing(2));
    }

    [Fact]
    public void Domain_events_are_collected_and_cleared()
    {
        var raiser = new Raiser();
        raiser.Do();

        Assert.Single(raiser.DomainEvents);

        raiser.ClearDomainEvents();

        Assert.Empty(raiser.DomainEvents);
    }
}
namespace Apteka.SharedKernel;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
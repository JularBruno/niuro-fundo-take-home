namespace LoanApp.Application.Abstractions;

/// <summary>
/// Queues an event as part of the current unit of work. Nothing is sent until
/// <see cref="IUnitOfWork.SaveChangesAsync"/> commits, so a failed save publishes nothing.
/// </summary>
public interface IEventPublisher
{
    void Publish<TEvent>(TEvent @event) where TEvent : class;
}

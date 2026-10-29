using System.Text.Json;
using LoanApp.Application.Abstractions;

namespace LoanApp.Infrastructure.Persistence.Outbox;

/// <summary>Publishes by adding an outbox row to the current DbContext; it is committed by the unit of work.</summary>
public sealed class OutboxEventPublisher(AppDbContext db) : IEventPublisher
{
    public void Publish<TEvent>(TEvent @event) where TEvent : class =>
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(@event, JsonSerializerOptions.Web),
        });
}

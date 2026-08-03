namespace ProjectNamePlaceholder.Application.Integration;

// Marker for a fact that already happened, published for other parts of the app to react to --
// as opposed to a command (an instruction to do something) or a query (a request for data),
// neither of which exist yet since nothing in this codebase currently needs them. Add
// ICommand/ICommandBus and IQuery/IQueryBus only when a real caller needs one, not speculatively
// ahead of it -- see TASKS.md "Integration Hub".
public interface IIntegrationEvent
{
}

public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}

// In-process today (handlers resolved from the same DI container, invoked synchronously and
// awaited inline -- so a publish still participates in the caller's ambient transaction/SaveChanges
// the way TenantService.ApproveRegistrationAsync relies on). A future out-of-process implementation
// (publish to a real broker) would satisfy this same interface without callers changing.
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent;
}

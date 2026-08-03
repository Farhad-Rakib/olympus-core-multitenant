namespace ProjectNamePlaceholder.Application.Integration;

// Deliberately not routed through a command bus/dispatcher -- resolved directly via
// [FromServices] at the controller action, same as every other service in this codebase. A real
// ICommandBus (reflection-based routing, pipeline behaviors, etc.) is scaffolding nobody needs
// yet; add one only when a real caller needs to send a command without knowing the concrete
// handler type (e.g. dispatching across a module boundary). See TASKS.md "Integration Hub".
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

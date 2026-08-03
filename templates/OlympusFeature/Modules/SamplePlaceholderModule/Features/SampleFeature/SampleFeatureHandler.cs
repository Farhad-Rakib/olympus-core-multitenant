using ProjectNamePlaceholder.Application.Integration;

namespace ProjectNamePlaceholder.BusinessModules.SamplePlaceholderModule.Features.SampleFeature;

// TODO: inject whatever repository/service this feature actually needs, and replace the body
// below with real persistence. Resolved directly via DI at the controller action (no command
// bus) -- see ICommandHandler's doc comment for why.
public sealed class SampleFeatureHandler : ICommandHandler<SampleFeatureCommand, SampleFeatureResponse>
{
    public Task<SampleFeatureResponse> HandleAsync(SampleFeatureCommand command, CancellationToken cancellationToken = default)
    {
        var response = new SampleFeatureResponse(Id: 0, Name: command.Name);
        return Task.FromResult(response);
    }
}

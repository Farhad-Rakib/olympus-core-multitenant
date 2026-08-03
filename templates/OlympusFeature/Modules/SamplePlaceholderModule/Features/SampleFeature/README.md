# SampleFeature

Scaffolded by `dotnet new olympus-feature --module SamplePlaceholderModule --feature SampleFeature`.

Generated: `SampleFeatureRequest`, `SampleFeatureResponse`, `SampleFeatureCommand`,
`SampleFeatureRequestValidator`, `SampleFeatureHandler` (`ICommandHandler<SampleFeatureCommand,
SampleFeatureResponse>`, resolved via DI -- no command bus, see `ICommandHandler`'s doc comment).

## Wiring (manual, one step)

1. Register the handler in `Application/DependencyInjection.cs`:

   ```csharp
   services.AddScoped<ICommandHandler<SampleFeatureCommand, SampleFeatureResponse>, SampleFeatureHandler>();
   ```

2. Add an action to this module's controller (in the `Api` project):

   ```csharp
   [HttpPost("sample-feature")]
   [Authorize(Policy = "samplemodule.create")]
   public async Task<IActionResult> SampleFeature(
       [FromBody] SampleFeatureRequest request,
       [FromServices] IValidator<SampleFeatureRequest> validator,
       [FromServices] ICommandHandler<SampleFeatureCommand, SampleFeatureResponse> handler,
       CancellationToken cancellationToken)
   {
       await validator.ValidateAndThrowAsync(request, cancellationToken);
       var result = await handler.HandleAsync(new SampleFeatureCommand(request.Name), cancellationToken);
       return Ok(ApiResponse<SampleFeatureResponse>.SuccessResponse(result, "SampleFeature completed successfully"));
   }
   ```

Replace the permission policy, route, and request/response shape with what this feature actually
needs, then delete this README.

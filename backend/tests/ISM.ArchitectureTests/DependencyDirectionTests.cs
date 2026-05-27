using FluentAssertions;
using ISM.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ISM.ArchitectureTests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Controllers_ShouldNotDependOnInfrastructureImplementations()
    {
        var controllerTypes = typeof(FornecedorController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));

        var invalidDependencies = controllerTypes
            .SelectMany(type => type.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters()
                    .Where(parameter => parameter.ParameterType.Namespace?.StartsWith("ISM.Infrastructure", StringComparison.Ordinal) == true)
                    .Select(parameter => $"{type.Name} -> {parameter.ParameterType.Name}")))
            .ToArray();

        invalidDependencies.Should().BeEmpty();
    }
}

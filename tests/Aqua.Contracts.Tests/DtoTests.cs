using Aqua.Contracts.Dto;
using FluentAssertions;
using Xunit;

namespace Aqua.Contracts.Tests;

public class DtoTests
{
    [Fact]
    public void IDto_IsMarkerOnly()
    {
        // Marker interface: no methods. Compile-time check that it can be applied to records.
        IDto dto = new SampleDto("hello");
        dto.Should().NotBeNull();
    }

    private sealed record SampleDto(string Value) : IDto;
}

using System.Net;
using System.Net.Http.Json;
using PropertyShowingBookingApi.Contracts;
using Xunit;

namespace PropertyShowingBookingApi.Tests;

public sealed class SlotEndpointTests
{
    [Fact]
    public async Task PostSlots_ReturnsSlotWithFullAvailability()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var request = new CreateSlotRequest(
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), 30, 4);

        var response = await client.PostAsJsonAsync("/slots", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slot = await response.Content.ReadFromJsonAsync<SlotResponse>();
        Assert.NotNull(slot);
        Assert.Equal(4, slot.Capacity);
        Assert.Equal(4, slot.SeatsRemaining);
    }
}

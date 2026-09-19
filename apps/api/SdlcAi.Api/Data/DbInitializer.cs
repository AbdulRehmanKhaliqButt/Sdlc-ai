using Microsoft.EntityFrameworkCore;

namespace SdlcAi.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SdlcAiDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}

using Microsoft.EntityFrameworkCore;

namespace SdlcAi.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SdlcAiDbContext>();
        await db.Database.EnsureCreatedAsync();

        // EnsureCreated does not evolve an existing demo database. Keep this lightweight
        // compatibility patch until the project switches fully to EF migrations.
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE IF EXISTS delivery_runs ADD COLUMN IF NOT EXISTS validation_json jsonb NULL;");
    }
}

using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class E2ePlanningService(SdlcAiDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<E2eProposal?> GenerateAsync(Guid projectId, Guid testPlanId, CancellationToken ct)
    {
        var entity = await db.TestPlans.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == testPlanId && x.ProjectId == projectId, ct);
        if (entity is null || entity.Status != "Approved") return null;

        var cases = JsonSerializer.Deserialize<List<TestCase>>(entity.TestCasesJson, Json) ?? new();
        var traceability = cases.SelectMany(x => x.AcceptanceCriteria)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("import { test, expect } from \"@playwright/test\";");
        sb.AppendLine();
        sb.AppendLine("test.describe(\"Generated acceptance coverage\", () => {");
        foreach (var testCase in cases)
        {
            sb.AppendLine($"  test(\"{Escape(testCase.Id + " - " + testCase.Title)}\", async ({{ page }}) => {{");
            sb.AppendLine("    await page.goto(\"/\");");
            foreach (var step in testCase.Steps)
            {
                sb.AppendLine($"    await test.step(\"{Escape(step)}\", async () => {{");
                sb.AppendLine("      // Bind this step to product-specific selectors/actions during QA review.");
                sb.AppendLine("      await expect(page).toHaveURL(/.*/);");
                sb.AppendLine("    });");
            }
            sb.AppendLine("  });");
        }
        sb.AppendLine("});");

        return new E2eProposal(
            projectId,
            testPlanId,
            $"apps/e2e/tests/generated-{testPlanId.ToString("N")[..8]}.spec.ts",
            sb.ToString(),
            traceability);
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
}

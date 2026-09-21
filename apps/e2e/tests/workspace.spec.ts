import { test, expect } from "@playwright/test";

test("workspace exposes requirements review", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByText(/SDLC/i).first()).toBeVisible();
  await expect(page.getByRole("button", { name: /analyze/i })).toBeVisible();
});

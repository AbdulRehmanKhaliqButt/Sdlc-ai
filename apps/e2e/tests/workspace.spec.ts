import { test, expect } from "@playwright/test";

test("browser workspace drives the requirements approval flow", async ({ page }) => {
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "SDLC AI" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Start with a project" })).toBeVisible();

  await page.getByLabel("New project name").fill("Playwright Demo");
  await page.getByLabel("Description").fill("Created by the browser E2E test");
  await page.getByRole("button", { name: "Create project" }).click();

  await expect(page.getByRole("heading", { name: "Grooming → structured requirements" })).toBeVisible();
  await page.getByRole("button", { name: "Analyze requirements" }).click();

  await expect(page.getByText("PendingReview")).toBeVisible();
  await expect(page.getByRole("button", { name: "Approve requirements" })).toBeVisible();

  await page.getByRole("button", { name: "Approve requirements" }).click();
  await expect(page.getByText("Approved").first()).toBeVisible();
  await expect(page.getByRole("button", { name: "Generate QA plan" })).toBeEnabled();
});

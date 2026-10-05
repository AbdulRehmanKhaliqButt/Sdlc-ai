import test from "node:test";
import assert from "node:assert/strict";
import { health } from "./health.js";

test("health contains status and service name", () => {
  assert.deepEqual(health("catalog"), { status: "ok", service: "catalog" });
});

#!/usr/bin/env node
import { readFile } from "node:fs/promises";
import { Agent } from "@cursor/sdk";
import { buildAgentOptions } from "./agent-config.js";
import { resolveProjectRoot } from "./paths.js";

async function loadApiKey(): Promise<string> {
  const root = resolveProjectRoot();
  try {
    const env = await readFile(`${root}/.env`, "utf-8");
    for (const line of env.split("\n")) {
      const m = line.match(/^CURSOR_API_KEY=(.+)$/);
      if (m) return m[1]!.trim().replace(/^["']|["']$/g, "");
    }
  } catch {
    /* ignore */
  }
  const key = process.env.CURSOR_API_KEY?.trim();
  if (!key) throw new Error("CURSOR_API_KEY missing");
  return key;
}

async function main() {
  const root = resolveProjectRoot();
  const apiKey = await loadApiKey();
  const mode = process.argv[2] ?? "project";
  console.log("[debug] mode=", mode);

  if (mode === "auth") {
    const { Cursor } = await import("@cursor/sdk");
    const cursor = new Cursor({ apiKey });
    const models = await cursor.models.list();
    console.log("[debug] models:", models.items?.slice(0, 3).map((m) => m.id));
    return;
  }

  const opts: import("@cursor/sdk").AgentOptions = { apiKey, model: { id: "composer-2.5" } };
  if (mode === "cloud") {
    opts.cloud = { workOnCurrentBranch: true };
  } else if (mode === "minimal") {
    /* api key + model only */
  } else if (mode === "no-sources") {
    opts.local = { cwd: root };
  } else {
    Object.assign(opts, buildAgentOptions(root, apiKey));
  }

  console.log("[debug] opts:", JSON.stringify({ ...opts, apiKey: "***" }));
  await using agent = await Agent.create(opts);
  const run = await agent.send("Reply with exactly: OK");
  console.log("[debug] run=", run.id);

  for await (const event of run.stream()) {
    console.log("[debug] event:", JSON.stringify(event));
  }

  const result = await run.wait();
  console.log("[debug] result:", JSON.stringify(result, null, 2));
}

main().catch((err) => {
  console.error("[debug] fatal:", err);
  process.exit(1);
});

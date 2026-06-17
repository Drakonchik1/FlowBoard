#!/usr/bin/env node
import { readFile } from "node:fs/promises";
import { projectPaths, resolveProjectRoot } from "./paths.js";
import { findNextTask, loadQueue, queueSummary } from "./queue.js";
import { isCouncilTask } from "./types.js";
import { loadState } from "./state.js";

async function main() {
  const rootFlag = process.argv.find((a) => a.startsWith("--root="));
  const projectRoot = rootFlag ? rootFlag.split("=")[1]! : resolveProjectRoot();
  const paths = projectPaths(projectRoot);

  const queue = await loadQueue(paths.queue);
  const next = findNextTask(queue) ?? findNextTask(queue, { retryFailed: true });
  const state = await loadState(paths.state);

  console.log(`[agent-runner] ${queueSummary(queue)}\n`);

  console.log("Tasks:");
  for (const t of queue.tasks) {
    const mark =
      t.status === "pending" || t.status === "in_progress" || t.status === "failed" ? "→" : " ";
    const tag = isCouncilTask(t) ? " [council]" : "";
    console.log(`  ${mark} [${t.status.padEnd(11)}] ${t.id}: ${t.title}${tag}`);
  }

  if (next) {
    console.log(`\nNext pending: ${next.id} — ${next.title}`);
  } else {
    console.log("\nNo pending tasks.");
  }

  if (state.lastTaskId) {
    console.log(
      `\nLast run: ${state.lastTaskId} (agent=${state.lastAgentId ?? "?"}, status=${state.lastStatus ?? "?"})`
    );
  }

  try {
    const handoff = await readFile(paths.handoff, "utf-8");
    const nextLine = handoff.match(/next pending: \*\*([^*]+)\*\*/i);
    if (nextLine) console.log(`HANDOFF next: ${nextLine[1]}`);
  } catch {
    /* optional */
  }
}

main().catch((err) => {
  console.error("[agent-runner] Fatal:", err);
  process.exit(1);
});

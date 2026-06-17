#!/usr/bin/env node
import { spawn } from "node:child_process";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { loadQueue, queueSummary, findNextTask } from "./queue.js";
import { projectPaths, resolveProjectRoot } from "./paths.js";

function parseArgs(argv: string[]) {
  const max = Number(argv.find((a) => a.startsWith("--max="))?.split("=")[1] ?? "3");
  const dryRun = argv.includes("--dry-run");
  const rootFlag = argv.find((a) => a.startsWith("--root="));
  const projectRoot = rootFlag ? rootFlag.split("=")[1]! : resolveProjectRoot();
  return { max: Math.max(1, max), dryRun, projectRoot };
}

function runNext(dryRun: boolean): Promise<number> {
  const scriptDir = dirname(fileURLToPath(import.meta.url));
  const runNextScript = join(scriptDir, "run-next.ts");
  const args = ["tsx", runNextScript];
  if (dryRun) args.push("--dry-run");

  return new Promise((resolve) => {
    const child = spawn("npx", args, {
      stdio: "inherit",
      shell: true,
      cwd: join(scriptDir, ".."),
    });
    child.on("close", (code) => resolve(code ?? 1));
  });
}

async function main() {
  const { max, dryRun, projectRoot } = parseArgs(process.argv.slice(2));
  const paths = projectPaths(projectRoot);

  console.log(`[agent-runner] Loop: up to ${max} task(s)${dryRun ? " (dry-run)" : ""}`);

  for (let i = 0; i < max; i++) {
    const queue = await loadQueue(paths.queue);
    const next = findNextTask(queue);
    if (!next) {
      console.log(`[agent-runner] ${queueSummary(queue)} — stopping.`);
      break;
    }

    console.log(`\n[agent-runner] === iteration ${i + 1}/${max}: ${next.id} ===\n`);
    const code = await runNext(dryRun);
    if (code !== 0 && !dryRun) {
      console.error(`[agent-runner] Stopping loop after exit code ${code}`);
      process.exit(code);
    }
    if (dryRun) break;
  }

  console.log("[agent-runner] Loop finished.");
}

main().catch((err) => {
  console.error("[agent-runner] Fatal:", err);
  process.exit(1);
});

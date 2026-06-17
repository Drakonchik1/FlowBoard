import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = dirname(fileURLToPath(import.meta.url));

/** FlowBoard repo root (scripts/agent-runner/src → ../../../) */
export function resolveProjectRoot(explicit?: string): string {
  if (explicit) return resolve(explicit);
  return resolve(scriptDir, "..", "..", "..");
}

export function projectPaths(root: string) {
  return {
    root,
    queue: join(root, "tasks", "queue.json"),
    handoff: join(root, "HANDOFF.md"),
    sprint: join(root, "SPRINT.md"),
    state: join(root, "tasks", ".runner-state.json"),
  };
}

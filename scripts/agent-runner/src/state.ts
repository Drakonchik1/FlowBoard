import { readFile, writeFile } from "node:fs/promises";
import type { RunnerState } from "./types.js";

export async function loadState(statePath: string): Promise<RunnerState> {
  try {
    return JSON.parse(await readFile(statePath, "utf-8")) as RunnerState;
  } catch {
    return {};
  }
}

export async function saveState(statePath: string, state: RunnerState): Promise<void> {
  await writeFile(statePath, `${JSON.stringify({ ...state, updatedAt: new Date().toISOString() }, null, 2)}\n`, "utf-8");
}

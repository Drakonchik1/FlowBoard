import { readFile, writeFile } from "node:fs/promises";
import type { Task, TaskQueue, TaskStatus } from "./types.js";

export async function loadQueue(queuePath: string): Promise<TaskQueue> {
  const raw = await readFile(queuePath, "utf-8");
  return JSON.parse(raw) as TaskQueue;
}

export async function saveQueue(queuePath: string, queue: TaskQueue): Promise<void> {
  await writeFile(queuePath, `${JSON.stringify(queue, null, 2)}\n`, "utf-8");
}

/** Council verify tasks (e.g. s6-council-verify) run only after matching *-council-fixes is done. */
export function taskDependenciesMet(queue: TaskQueue, task: Task): boolean {
  const verifyMatch = task.id.match(/^(.+)-council-verify$/);
  if (!verifyMatch) return true;

  const fixesId = `${verifyMatch[1]}-council-fixes`;
  const fixes = findTask(queue, fixesId);
  return !fixes || fixes.status === "done";
}

export function findNextTask(queue: TaskQueue, options?: { retryFailed?: boolean }): Task | undefined {
  // Resume interrupted run before picking new pending work.
  const inProgress = queue.tasks.find((t) => t.status === "in_progress");
  if (inProgress) return inProgress;

  const pending = queue.tasks.find((t) => t.status === "pending" && taskDependenciesMet(queue, t));
  if (pending) return pending;

  if (options?.retryFailed) {
    return queue.tasks.find((t) => t.status === "failed" && taskDependenciesMet(queue, t));
  }

  return undefined;
}

export function findTask(queue: TaskQueue, id: string): Task | undefined {
  return queue.tasks.find((t) => t.id === id);
}

export function setTaskStatus(queue: TaskQueue, id: string, status: TaskStatus): void {
  const task = findTask(queue, id);
  if (!task) throw new Error(`Task not found: ${id}`);
  task.status = status;
}

export function queueSummary(queue: TaskQueue): string {
  const counts = { pending: 0, in_progress: 0, done: 0, failed: 0, skipped: 0 };
  for (const t of queue.tasks) counts[t.status]++;
  return `Sprint ${queue.sprint}: ${counts.done}/${queue.tasks.length} done, ${counts.pending} pending, ${counts.in_progress} in progress, ${counts.failed} failed`;
}

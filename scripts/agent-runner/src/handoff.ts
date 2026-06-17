import { readFile, writeFile } from "node:fs/promises";

export async function readHandoff(handoffPath: string): Promise<string> {
  try {
    return await readFile(handoffPath, "utf-8");
  } catch {
    return "";
  }
}

export async function updateHandoff(
  handoffPath: string,
  update: {
    taskId: string;
    title: string;
    result: string;
    tests: string;
    git?: string;
    nextTaskId: string | null;
    filesTouched?: string[];
  }
): Promise<void> {
  const now = new Date().toISOString().slice(0, 10);
  const files = update.filesTouched?.length
    ? update.filesTouched.map((f) => `- \`${f}\``).join("\n")
    : "_(see git diff)_";

  const nextSection = update.nextTaskId
    ? `Queue: \`tasks/queue.json\` — next pending: **${update.nextTaskId}**`
    : "Queue complete for current sprint — add tasks to `tasks/queue.json`.";

  const gitRow = update.git ? `| **Git** | ${update.git} |\n` : "";

  const content = `# FlowBoard — session handoff

> **AI:** Read this file at the start of every automated or manual task chat.
> The agent-runner script updates this after each completed task.

## Project goal

FlowBoard — ASP.NET Core 10 Kanban API (Clean Architecture, JWT, RBAC, SignalR, Redis).

## Last session

| Field | Value |
|-------|-------|
| **Date** | ${now} |
| **Task ID** | ${update.taskId} |
| **Result** | ${update.result} |
| **Tests** | ${update.tests} |
${gitRow}
## Decisions made (carry forward)

- See \`SPRINT.md\` → Architecture decisions
- One task per agent session — do not batch unrelated work

## What was done

**${update.taskId}:** ${update.title}

${update.result}

## Next task

${nextSection}

## Blockers / open questions

_(agent: update if any)_

## Files touched last run

${files}
`;

  await writeFile(handoffPath, content, "utf-8");
}

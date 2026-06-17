import type { Task, TaskQueue } from "./types.js";
import { buildCommitSubject, shouldPublishToGit } from "./git-publish.js";

export function buildTaskPrompt(
  task: Task,
  queue: TaskQueue,
  handoff: string,
  sprint: string
): string {
  const fileRefs = task.files.map((f) => `@${f}`).join("\n");

  return `You are working on FlowBoard — one task only, then stop.

## Task

**ID:** ${task.id}
**Title:** ${task.title}

## Context (read first)

@SPRINT.md
@HANDOFF.md
@tasks/queue.json

### Sprint excerpt
${truncate(sprint, 4000)}

### Previous handoff
${truncate(handoff, 2000)}

## Files to focus on

${fileRefs}

## Done when

${task.doneWhen}

## Do NOT touch

${task.dontTouch.map((d) => `- ${d}`).join("\n")}

${task.notes ? `## Notes\n\n${task.notes}\n` : ""}

${gitFinishInstructions(task)}

## Project goal

${queue.projectGoal}
`;
}

function gitFinishInstructions(task: Task): string {
  const lines = [
    "## Required before you finish",
    "",
    "1. Implement only this task — minimal diff, match existing conventions (.cursor/rules/flowboard.mdc).",
    "2. Run `dotnet test` and fix failures in scope.",
    `3. Update \`tasks/queue.json\`: set task \`${task.id}\` status to \`"done"\` (or \`"failed"\` with reason in notes if blocked).`,
    '4. Update `SPRINT.md`: checklist, session log row, "Last updated" date.',
    "5. Update `HANDOFF.md`: what was done, test result, next task id.",
  ];

  if (shouldPublishToGit(task)) {
    lines.push(
      `6. Do **not** run \`git commit\` or \`git push\` — agent-runner publishes after this council-fix task (\`${buildCommitSubject(task)}\`).`
    );
  } else {
    lines.push(
      "6. Do **not** run `git commit` or `git push` — changes stay local until the sprint council-fix publish task (`close-docs`, `sN-council-fixes`, or `gitPublish: true` in queue)."
    );
  }

  lines.push("7. Do NOT start the next queued task in this session.");
  return lines.join("\n");
}

function truncate(text: string, max: number): string {
  if (text.length <= max) return text;
  return `${text.slice(0, max)}\n\n…(truncated)…`;
}

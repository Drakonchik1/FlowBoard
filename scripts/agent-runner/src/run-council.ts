import { access } from "node:fs/promises";
import { readFile } from "node:fs/promises";
import { runAgentSession } from "./agent-session.js";
import {
  buildCouncilMemberPrompt,
  buildCouncilSynthesizerPrompt,
  councilWorkFile,
} from "./council-prompt.js";
import type { Task, TaskQueue } from "./types.js";

export interface CouncilRunResult {
  ok: boolean;
  memberResults: { id: string; ok: boolean; status: string }[];
  synthesizerOk: boolean;
  outputFile?: string;
}

export function resolveCouncilPaths(task: Task, queue: TaskQueue) {
  if (!task.council) {
    throw new Error(`Council task ${task.id} missing council config`);
  }
  const workDir = task.council.workDir ?? "docs/council/.work";
  const outputFile = task.council.outputFile ?? `docs/council/sprint-${queue.sprint}-report.md`;
  return { workDir, outputFile, members: task.council.members };
}

export function buildCouncilDryRunPrompts(
  task: Task,
  queue: TaskQueue,
  sprint: string
): { label: string; prompt: string }[] {
  const { workDir, outputFile, members } = resolveCouncilPaths(task, queue);
  const prompts: { label: string; prompt: string }[] = [];

  for (const member of members) {
    const workFile = councilWorkFile(workDir, queue.sprint, member.id);
    prompts.push({
      label: `member:${member.id}`,
      prompt: buildCouncilMemberPrompt(member, task, queue, sprint, workFile),
    });
  }

  const workFiles = members.map((m) => councilWorkFile(workDir, queue.sprint, m.id));
  prompts.push({
    label: "synthesizer",
    prompt: buildCouncilSynthesizerPrompt(task, queue, workFiles, outputFile),
  });

  return prompts;
}

export async function runCouncilTask(options: {
  task: Task;
  queue: TaskQueue;
  sprint: string;
  projectRoot: string;
  apiKey: string;
}): Promise<CouncilRunResult> {
  const { task, queue, sprint, projectRoot, apiKey } = options;
  const { workDir, outputFile, members } = resolveCouncilPaths(task, queue);

  console.log(
    `[agent-runner] Live Council: ${members.length} reviewers in parallel, then synthesizer → ${outputFile}`
  );

  const memberRuns = await Promise.all(
    members.map(async (member) => {
      const workFile = councilWorkFile(workDir, queue.sprint, member.id);
      const prompt = buildCouncilMemberPrompt(member, task, queue, sprint, workFile);
      const result = await runAgentSession({
        projectRoot,
        apiKey,
        prompt,
        label: `council/${member.id}`,
        model: member.model,
      });
      return { id: member.id, ok: result.ok, status: result.status };
    })
  );

  const failedMembers = memberRuns.filter((r) => !r.ok);
  if (failedMembers.length > 0) {
    console.error(
      `[agent-runner] Council members failed: ${failedMembers.map((m) => m.id).join(", ")}`
    );
    return { ok: false, memberResults: memberRuns, synthesizerOk: false, outputFile };
  }

  const workFiles = members.map((m) => councilWorkFile(workDir, queue.sprint, m.id));
  const synthPrompt = buildCouncilSynthesizerPrompt(task, queue, workFiles, outputFile);
  const synth = await runAgentSession({
    projectRoot,
    apiKey,
    prompt: synthPrompt,
    label: "council/synthesizer",
    model: task.council?.synthesizer?.model,
  });

  let reportExists = false;
  try {
    await access(`${projectRoot}/${outputFile}`);
    reportExists = true;
  } catch {
    reportExists = false;
  }

  const ok = synth.ok && reportExists;
  if (!reportExists) {
    console.error(`[agent-runner] Council report missing: ${outputFile}`);
  }

  return {
    ok,
    memberResults: memberRuns,
    synthesizerOk: synth.ok,
    outputFile,
  };
}

export async function readSprintExcerpt(sprintPath: string): Promise<string> {
  return readFile(sprintPath, "utf-8").catch(() => "");
}

#!/usr/bin/env node

import { readFile } from "node:fs/promises";

import { streamRun } from "./agent-session.js";

import { buildAgentOptions } from "./agent-config.js";

import { updateHandoff } from "./handoff.js";

import { projectPaths, resolveProjectRoot } from "./paths.js";

import { buildTaskPrompt } from "./prompt.js";

import { checkConnectNode, checkNodeVersion } from "./preflight.js";

import {

  findNextTask,

  findTask,

  loadQueue,

  queueSummary,

  saveQueue,

  setTaskStatus,

} from "./queue.js";

import { buildCouncilDryRunPrompts, runCouncilTask } from "./run-council.js";

import { loadState, saveState } from "./state.js";

import { isCouncilTask } from "./types.js";

import { publishTaskToGitHub, shouldPublishToGit } from "./git-publish.js";

import { runDotnetTest } from "./verify.js";

import { Agent, CursorAgentError } from "@cursor/sdk";



function parseArgs(argv: string[]) {

  const dryRun = argv.includes("--dry-run") || argv.includes("-n");

  const verifyTests = !argv.includes("--skip-tests");

  const skipGit = argv.includes("--skip-git");

  const forceGit = argv.includes("--force-git");

  const retryFailed = argv.includes("--retry-failed") || argv.includes("--retry");

  const rootFlag = argv.find((a) => a.startsWith("--root="));

  const projectRoot = rootFlag ? rootFlag.split("=")[1]! : resolveProjectRoot();

  return { dryRun, verifyTests, skipGit, forceGit, retryFailed, projectRoot };

}



function resolveApiKey(): string | undefined {

  const key = process.env.CURSOR_API_KEY?.trim();

  return key || undefined;

}



async function main() {

  checkNodeVersion();



  const args = parseArgs(process.argv.slice(2));

  const paths = projectPaths(args.projectRoot);



  const queue = await loadQueue(paths.queue);

  const task = findNextTask(queue, { retryFailed: args.retryFailed });



  if (!task) {

    const summary = queueSummary(queue);

    console.log(`[agent-runner] No runnable tasks. ${summary}`);

    if (queue.tasks.some((t) => t.status === "failed")) {

      console.log("[agent-runner] Tip: retry failed with --retry-failed");

    }

    process.exit(0);

  }



  if (task.status === "in_progress") {

    console.log(`[agent-runner] Resuming stale in_progress task: ${task.id}`);

  }

  if (task.status === "failed" && args.retryFailed) {

    console.log(`[agent-runner] Retrying failed task: ${task.id}`);

  }



  const handoff = await readFile(paths.handoff, "utf-8").catch(() => "");

  const sprint = await readFile(paths.sprint, "utf-8").catch(() => "");

  const council = isCouncilTask(task);



  console.log(`[agent-runner] Next task: ${task.id} — ${task.title}${council ? " [Live Council]" : ""}`);

  console.log(`[agent-runner] ${queueSummary(queue)}`);



  if (args.dryRun) {

    console.log("\n--- DRY RUN: paste into a new Cursor chat ---\n");

    if (council) {

      const prompts = buildCouncilDryRunPrompts(task, queue, sprint);

      for (const p of prompts) {

        console.log(`\n### ${p.label}\n`);

        console.log(p.prompt);

        console.log("\n---\n");

      }

    } else {

      console.log(buildTaskPrompt(task, queue, handoff, sprint));

    }

    console.log("\n--- end prompt ---\n");

    console.log("[agent-runner] Dry run only — queue unchanged.");

    process.exit(0);

  }



  const apiKey = resolveApiKey();

  if (!apiKey) {

    console.error(

      "[agent-runner] CURSOR_API_KEY is not set.\n" +

        "  Add to FlowBoard/.env (git-ignored) — see .env.example\n" +

        "  Mint a key: Cursor Dashboard → API Keys\n" +

        "  Or use: powershell scripts/run-next-task.ps1 -DryRun"

    );

    process.exit(1);

  }



  await checkConnectNode();



  setTaskStatus(queue, task.id, "in_progress");

  await saveQueue(paths.queue, queue);



  let resultStatus = "unknown";

  let agentId: string | undefined;

  let runId: string | undefined;

  let councilOk = true;



  if (council) {

    const councilResult = await runCouncilTask({

      task,

      queue,

      sprint,

      projectRoot: paths.root,

      apiKey,

    });

    councilOk = councilResult.ok;

    resultStatus = councilOk ? "finished" : "error";

  } else {

    console.log(`[agent-runner] Starting fresh local agent (composer-2.5, cwd=${paths.root})`);



    await using agent = await Agent.create(buildAgentOptions(paths.root, apiKey));



    try {

      agentId = agent.agentId;

      const prompt = buildTaskPrompt(task, queue, handoff, sprint);

      const run = await agent.send(prompt);

      runId = run.id;



      console.log(`[agent-runner] agent=${agentId} run=${runId}`);



      await streamRun(run);



      const result = await run.wait();

      resultStatus = result.status;



      if (result.status === "error") {

        console.error(`[agent-runner] Run failed: ${result.id}`);

        process.exit(2);

      }

      if (result.status === "cancelled") {

        console.error(`[agent-runner] Run cancelled: ${result.id}`);

        process.exit(2);

      }

      if (result.status !== "finished") {

        console.error(`[agent-runner] Run ended with status: ${result.status}`);

      }

    } catch (err) {

      if (err instanceof CursorAgentError) {

        console.error(`[agent-runner] SDK error: ${err.message} (retryable=${err.isRetryable})`);

        setTaskStatus(queue, task.id, "failed");

        await saveQueue(paths.queue, queue);

        process.exit(err.isRetryable ? 75 : 1);

      }

      throw err;

    }

  }



  const queueAfter = await loadQueue(paths.queue);

  const taskAfter = findTask(queueAfter, task.id);

  const agentMarkedDone = taskAfter?.status === "done";



  let tests = council ? "skipped (council — read-only review)" : "skipped (--skip-tests)";

  if (!council && args.verifyTests) {

    console.log("[agent-runner] Running dotnet test…");

    const testResult = await runDotnetTest(paths.root);

    tests = testResult.summary;

    console.log(`[agent-runner] ${tests}`);



    if (!testResult.ok && taskAfter?.status === "done") {

      taskAfter.status = "failed";

      taskAfter.notes = `Tests failed after agent run. ${taskAfter.notes ?? ""}`.trim();

      await saveQueue(paths.queue, queueAfter);

    }

  }



  const finalQueue = await loadQueue(paths.queue);

  const finalTask = findTask(finalQueue, task.id);



  if (council && !agentMarkedDone) {

    if (councilOk) {

      setTaskStatus(finalQueue, task.id, "done");

      await saveQueue(paths.queue, finalQueue);

      console.log("[agent-runner] Council marked done (report produced).");

    } else {

      setTaskStatus(finalQueue, task.id, "failed");

      await saveQueue(paths.queue, finalQueue);

      console.log("[agent-runner] Council marked failed — check member drafts and logs.");

    }

  } else if (!agentMarkedDone && finalTask?.status === "in_progress") {

    if (resultStatus === "finished" && tests.includes("passed")) {

      setTaskStatus(finalQueue, task.id, "done");

      await saveQueue(paths.queue, finalQueue);

      console.log("[agent-runner] Marked done (tests passed, agent did not update queue).");

    } else {

      setTaskStatus(finalQueue, task.id, "failed");

      await saveQueue(paths.queue, finalQueue);

      console.log("[agent-runner] Marked failed — review HANDOFF.md and git diff.");

    }

  }



  const refreshed = await loadQueue(paths.queue);

  const next = findNextTask(refreshed);

  const finalStatus = findTask(refreshed, task.id)?.status;

  const outcome =

    finalStatus === "done"

      ? council

        ? `Live Council completed — see ${task.council?.outputFile ?? "docs/council/"}`

        : "Completed successfully"

      : `Ended with status ${finalStatus ?? resultStatus}`;



  const publishGit =
    finalStatus === "done" &&
    !args.skipGit &&
    (args.forceGit || shouldPublishToGit(task));

  let gitSummary = args.skipGit
    ? "skipped (--skip-git)"
    : finalStatus !== "done"
      ? "skipped (task not done)"
      : "skipped (local only — push after council verify: close-council or sN-council-verify)";

  if (publishGit) {

    console.log("[agent-runner] Council verify passed — publishing remediation to GitHub…");

    const gitResult = await publishTaskToGitHub(paths.root, task);

    gitSummary = gitResult.summary;

    console.log(`[agent-runner] Git: ${gitSummary}`);

    if (!gitResult.ok) {

      await updateHandoff(paths.handoff, {

        taskId: task.id,

        title: task.title,

        result: `${outcome} — git publish failed: ${gitSummary}`,

        tests,

        nextTaskId: next?.id ?? null,

        filesTouched: council

          ? [task.council?.outputFile ?? "docs/council/", "SPRINT.md", "HANDOFF.md"]

          : task.files,

      });

      process.exit(2);

    }

  }



  await updateHandoff(paths.handoff, {

    taskId: task.id,

    title: task.title,

    result: outcome,

    tests,

    git: gitSummary,

    nextTaskId: next?.id ?? null,

    filesTouched: council

      ? [task.council?.outputFile ?? "docs/council/", "SPRINT.md", "HANDOFF.md"]

      : task.files,

  });



  await saveState(paths.state, {

    lastTaskId: task.id,

    lastRunId: runId,

    lastAgentId: agentId,

    lastStatus: resultStatus,

  });



  console.log(`[agent-runner] ${queueSummary(refreshed)}`);

  if (next) {

    console.log(`[agent-runner] Next: ${next.id} — run again to continue.`);

  } else {

    console.log("[agent-runner] Queue empty.");

  }



  process.exit(finalStatus === "done" ? 0 : 2);

}



main().catch((err) => {

  console.error("[agent-runner] Fatal:", err);

  process.exit(1);

});


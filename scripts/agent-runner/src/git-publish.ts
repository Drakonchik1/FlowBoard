import { spawn } from "node:child_process";

import type { Task } from "./types.js";
import { isCouncilTask } from "./types.js";

export interface GitPublishResult {
  ok: boolean;
  committed: boolean;
  pushed: boolean;
  summary: string;
}

const FORBIDDEN_PATHS = [
  /^\.env$/,
  /^\.env\.[^/]+$/,
  /[/\\]secrets\.json$/,
  /[/\\]appsettings\.Development\.json$/,
];

function runGit(
  cwd: string,
  args: string[]
): Promise<{ code: number; stdout: string; stderr: string }> {
  return new Promise((resolve, reject) => {
    const child = spawn("git", args, {
      cwd,
      shell: true,
      stdio: ["ignore", "pipe", "pipe"],
    });

    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (d) => (stdout += d.toString()));
    child.stderr.on("data", (d) => (stderr += d.toString()));

    child.on("close", (code) => resolve({ code: code ?? 1, stdout, stderr }));
    child.on("error", reject);
  });
}

function parseStatusPath(line: string): string {
  const trimmed = line.trimEnd();
  if (trimmed.length < 4) return trimmed;
  return trimmed.slice(3).trim();
}

/** Re-verification council after fixes — the only automatic git-publish trigger. */
export function isCouncilVerifyTask(task: Task): boolean {
  if (task.id === "close-council") return true;
  return /^s\d+-council-verify$/.test(task.id);
}

/**
 * Push to GitHub only after council re-verifies that all vulnerability fixes landed.
 * Flow: council review → fix tasks (local) → council verify → publish.
 */
export function shouldPublishToGit(task: Task): boolean {
  if (task.gitPublish === false) return false;
  if (task.gitPublish === true) return true;
  return isCouncilVerifyTask(task);
}

function inferCommitType(task: Task): string {
  if (isCouncilVerifyTask(task)) return "fix";
  if (isCouncilTask(task)) return "docs";
  if (task.id.endsWith("-docs")) return "docs";
  if (task.id.startsWith("close-")) return "fix";
  return "feat";
}

export function buildCommitSubject(task: Task): string {
  const type = inferCommitType(task);
  if (task.id === "close-council") {
    return `${type}(closeout): Council verified — Sprints 1–5 remediation published`;
  }
  const sprintVerify = task.id.match(/^s(\d+)-council-verify$/);
  if (sprintVerify) {
    return `${type}(s${sprintVerify[1]}): Council verified — Sprint ${sprintVerify[1]} remediation published`;
  }
  return `${type}(${task.id}): ${task.title}`;
}

/** Stage all tracked changes, commit with a queue-task message, and push to origin. */
export async function publishTaskToGitHub(
  projectRoot: string,
  task: Task,
  options: { push?: boolean } = {}
): Promise<GitPublishResult> {
  const push = options.push !== false;

  const status = await runGit(projectRoot, ["status", "--porcelain"]);
  if (status.code !== 0) {
    return {
      ok: false,
      committed: false,
      pushed: false,
      summary: `git status failed: ${status.stderr.trim()}`,
    };
  }

  const lines = status.stdout
    .split("\n")
    .map((l) => l.trimEnd())
    .filter(Boolean);

  if (lines.length === 0) {
    return {
      ok: true,
      committed: false,
      pushed: false,
      summary: "No changes to commit — skipped",
    };
  }

  for (const line of lines) {
    const file = parseStatusPath(line);
    if (FORBIDDEN_PATHS.some((p) => p.test(file))) {
      return {
        ok: false,
        committed: false,
        pushed: false,
        summary: `Refusing to commit sensitive file: ${file}`,
      };
    }
  }

  const add = await runGit(projectRoot, ["add", "-A"]);
  if (add.code !== 0) {
    return {
      ok: false,
      committed: false,
      pushed: false,
      summary: `git add failed: ${add.stderr.trim()}`,
    };
  }

  const subject = buildCommitSubject(task);
  const commit = await runGit(projectRoot, [
    "commit",
    "-m",
    subject,
    "-m",
    `Task queue: ${task.id} — council verify passed`,
  ]);

  if (commit.code !== 0) {
    const err = `${commit.stderr}\n${commit.stdout}`.trim();
    if (err.toLowerCase().includes("nothing to commit")) {
      return {
        ok: true,
        committed: false,
        pushed: false,
        summary: "Nothing to commit after staging",
      };
    }
    return {
      ok: false,
      committed: false,
      pushed: false,
      summary: `git commit failed: ${err}`,
    };
  }

  const sha = (await runGit(projectRoot, ["rev-parse", "--short", "HEAD"])).stdout.trim();

  if (!push) {
    return {
      ok: true,
      committed: true,
      pushed: false,
      summary: `Committed ${sha} (push skipped)`,
    };
  }

  const branch = (await runGit(projectRoot, ["rev-parse", "--abbrev-ref", "HEAD"])).stdout.trim();
  const pushResult = await runGit(projectRoot, ["push", "origin", branch]);

  if (pushResult.code !== 0) {
    return {
      ok: false,
      committed: true,
      pushed: false,
      summary: `Committed ${sha} but push failed: ${pushResult.stderr.trim()}`,
    };
  }

  return {
    ok: true,
    committed: true,
    pushed: true,
    summary: `Committed ${sha} and pushed to origin/${branch}`,
  };
}

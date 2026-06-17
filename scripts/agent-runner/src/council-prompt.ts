import type { CouncilMember, CouncilScope, Task, TaskQueue } from "./types.js";

const SECURITY_STANDARDS = `
## Security standards (apply rigorously)

- **OWASP API Security Top 10 (2023)** — broken auth, excessive data exposure, mass assignment, resource consumption, etc.
- **OWASP ASVS 4.0** — Level 2 controls for authentication, session management, access control, validation.
- **CWE Top 25** — especially injection, XSS (if any), auth bypass, sensitive data exposure.
- **ASP.NET Core security** — JWT validation, HTTPS, CORS policy, rate limiting, secrets via user-secrets/env (never in repo).
- **Project rules** — non-members get **404** (not 403); refresh token rotation; no secrets in git.
- Read \`SECURITY.md\` and flag any credential hygiene violations.
`.trim();

const PROJECT_SECURITY_SCOPE = `
## Full-project security scope (mandatory)

Audit the **entire FlowBoard codebase** — not only Sprint changes. Systematically review:

- **API surface** — \`src/FlowBoard.API/\` (controllers, \`Program.cs\`, middleware, CORS, rate limits, Swagger exposure).
- **Auth** — JWT issuance/validation, refresh rotation, \`?access_token=\` on SignalR, token lifetime, clock skew.
- **Authorization** — workspace RBAC, 404 vs 403 semantics, IDOR on boards/cards/projects, hub group join checks.
- **Application layer** — mass assignment, validation bypass, sensitive fields in DTOs/commands.
- **Infrastructure** — EF queries (SQL injection via raw SQL if any), Dapper parameterization, connection strings, migrations.
- **Real-time** — \`BoardHub\`, group membership, unauthorized subscribe/publish.
- **Redis / SignalR backplane** — connection string handling, network exposure in docker-compose.
- **Docker / deploy** — \`docker-compose.yml\`, \`.env.example\`, default passwords, published ports, secrets in env.
- **Dependencies** — known-vulnerable packages in \`.csproj\` files (flag for review; do not upgrade in this task).
- **Tests** — security-sensitive paths lacking coverage (auth, RBAC, refresh, hub auth).

Use \`git log\` / search if needed — do **not** limit findings to the latest sprint diff.
`.trim();

const BUG_REVIEW_FOCUS = `
## Bug-hunt focus

- Logic errors, off-by-one, race conditions, null/empty edge cases.
- MediatR handler validation gaps vs FluentValidation.
- EF vs Dapper read/write consistency (stale reads, soft-delete leaks).
- SignalR group membership and auth edge cases.
- Test gaps: missing unit cases, flaky or skipped integration tests.
- \`dotnet test\` failures or silent skips (e.g. Docker unavailable).
`.trim();

const ARCHITECTURE_FOCUS = `
## Architecture focus

- Clean Architecture layer violations (Domain → no deps, thin controllers).
- Scope creep vs sprint goals in \`SPRINT.md\` / \`tasks/queue.json\`.
- Naming and convention drift vs \`.cursor/rules/flowboard.mdc\`.
- Unnecessary dependencies or drive-by refactors.
`.trim();

function memberScope(member: CouncilMember): CouncilScope {
  return member.scope ?? "sprint";
}

function memberFocusBlock(member: CouncilMember): string {
  const blocks = [member.focus];
  if (member.id.includes("security")) blocks.push(SECURITY_STANDARDS);
  if (member.id.includes("bug")) blocks.push(BUG_REVIEW_FOCUS);
  if (member.id.includes("arch")) blocks.push(ARCHITECTURE_FOCUS);
  if (member.id.includes("security") && memberScope(member) === "project") {
    blocks.push(PROJECT_SECURITY_SCOPE);
  }
  return blocks.join("\n\n");
}

function buildScopeSection(member: CouncilMember, queue: TaskQueue): string {
  if (memberScope(member) === "project") {
    return `## Scope — **full project**

Security baseline across the **entire repository** (Sprints 1–${queue.sprint}). Include legacy auth, RBAC, boards/cards, SignalR, Redis, Docker — not just Sprint ${queue.sprint} changes.

Read:
@SPRINT.md
@SECURITY.md
@tasks/queue.json
@HANDOFF.md
@README.md
@.env.example`;
  }

  return `## Scope — Sprint ${queue.sprint}

Review code changed or touched during Sprint ${queue.sprint}. Read:
@SPRINT.md
@tasks/queue.json
@HANDOFF.md`;
}

export function buildCouncilMemberPrompt(
  member: CouncilMember,
  task: Task,
  queue: TaskQueue,
  sprint: string,
  workFile: string
): string {
  const fileRefs = task.files.map((f) => `@${f}`).join("\n");
  const focus = memberFocusBlock(member);

  return `You are **${member.role}** on the FlowBoard Live Council — Sprint ${queue.sprint}.

## Your mandate

${member.focus}

${focus}

${buildScopeSection(member, queue)}

### Files in scope
${fileRefs}

### Sprint excerpt
${truncate(sprint, 3000)}

## Rules

1. **Read-only** — do NOT fix code, commit, or change application logic. Report only.
2. Search the codebase; cite **file:line** for every finding.
3. Classify each finding: **Critical | High | Medium | Low | Info**.
4. Write your full report to: \`${workFile}\`

## Output format (\`${workFile}\`)

\`\`\`markdown
# Council — ${member.role} (Sprint ${queue.sprint}${memberScope(member) === "project" ? ", full-project scope" : ""})

## Summary
(2–4 sentences; for full-project security: state overall posture, not only sprint delta)

## Findings

| Sev | Area | Location | Issue | Recommendation |
|-----|------|----------|-------|----------------|
| ... | ... | \`path:line\` | ... | ... |

## Positive notes
(optional — what looks solid)
\`\`\`

5. Create parent directories if needed. Overwrite the work file completely.
6. Stop after writing the work file — do not start implementation tasks.
`;
}

export function buildCouncilSynthesizerPrompt(
  task: Task,
  queue: TaskQueue,
  workFiles: string[],
  outputFile: string
): string {
  const synth = task.council?.synthesizer;
  const role = synth?.role ?? "Council chair";
  const focus =
    synth?.focus ??
    "Merge member reports, deduplicate, prioritize by severity and exploitability.";

  const workRefs = workFiles.map((f) => `@${f}`).join("\n");

  return `You are the **${role}** for FlowBoard Live Council — Sprint ${queue.sprint}.

## Mandate

${focus}

## Member reports (read all)

${workRefs}

@SPRINT.md
@SECURITY.md

## Rules

1. Read every member report above. Deduplicate overlapping findings.
2. Produce the **final council report** at: \`${outputFile}\`
3. Do NOT fix application code — report only. You may delete \`docs/council/.work/\` member drafts after merging.
4. Update \`SPRINT.md\` session log with one row: "Sprint ${queue.sprint} Live Council — report at ${outputFile}".
5. Update \`tasks/queue.json\`: set task \`${task.id}\` status to \`"done"\`.
6. Update \`HANDOFF.md\` with council summary and link to the report.

## Final report format (\`${outputFile}\`)

\`\`\`markdown
# Live Council Report — Sprint ${queue.sprint}

**Date:** (today)
**Task:** ${task.id} — ${task.title}

## Executive summary
(3–6 sentences: overall health, top risks)

## Findings (prioritized)

| # | Sev | Category | Location | Issue | Action |
|---|-----|----------|----------|-------|--------|
| 1 | Critical/... | Security/Bug/Arch | \`path:line\` | ... | ... |

## Security posture
(Full-project baseline vs OWASP API Top 10 / ASVS — not limited to latest sprint)

## Test & quality gaps

## Recommended follow-up tasks
(Numbered list for next sprint queue)

## Sign-off
- Bug hunter: (summary)
- Security: (summary)
- Architecture: (summary)
\`\`\`

## Done when

${task.doneWhen}

Stop after completing the report and queue updates.
`;
}

export function councilWorkFile(workDir: string, sprint: number, memberId: string): string {
  return `${workDir}/sprint-${sprint}-${memberId}.md`;
}

function truncate(text: string, max: number): string {
  if (text.length <= max) return text;
  return `${text.slice(0, max)}\n\n…(truncated)…`;
}

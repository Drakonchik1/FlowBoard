import { Agent, CursorAgentError } from "@cursor/sdk";
import { buildAgentOptions } from "./agent-config.js";

export interface AgentSessionResult {
  ok: boolean;
  status: string;
  agentId?: string;
  runId?: string;
  retryable?: boolean;
}

export async function streamRun(
  run: Awaited<ReturnType<Awaited<ReturnType<typeof Agent.create>>["send"]>>
): Promise<void> {
  if (!run.supports("stream")) {
    console.log(`[agent-runner] stream unsupported: ${run.unsupportedReason("stream") ?? "unknown"}`);
    return;
  }

  for await (const event of run.stream()) {
    switch (event.type) {
      case "status":
        console.log(`[agent-runner] status: ${event.status}`);
        break;
      case "tool_call":
        if (event.status === "completed" || event.status === "error") {
          console.log(`[agent-runner] tool: ${event.name} → ${event.status}`);
        }
        break;
      default:
        break;
    }
  }
}

export async function runAgentSession(options: {
  projectRoot: string;
  apiKey: string;
  prompt: string;
  label: string;
  model?: string;
}): Promise<AgentSessionResult> {
  const agentOpts = buildAgentOptions(options.projectRoot, options.apiKey);
  if (options.model) {
    agentOpts.model = { id: options.model };
  }

  console.log(`[agent-runner] [${options.label}] starting agent (model=${options.model ?? "composer-2.5"})`);

  await using agent = await Agent.create(agentOpts);

  try {
    const run = await agent.send(options.prompt);
    console.log(`[agent-runner] [${options.label}] agent=${agent.agentId} run=${run.id}`);

    await streamRun(run);

    const result = await run.wait();
    const ok = result.status === "finished";

    if (!ok) {
      console.error(`[agent-runner] [${options.label}] ended with status: ${result.status}`);
    }

    return {
      ok,
      status: result.status,
      agentId: agent.agentId,
      runId: run.id,
    };
  } catch (err) {
    if (err instanceof CursorAgentError) {
      console.error(
        `[agent-runner] [${options.label}] SDK error: ${err.message} (retryable=${err.isRetryable})`
      );
      return { ok: false, status: "error", retryable: err.isRetryable };
    }
    throw err;
  }
}

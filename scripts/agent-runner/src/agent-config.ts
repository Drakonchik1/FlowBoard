import type { AgentOptions } from "@cursor/sdk";

/** FlowBoard local agent options — aligned with cursor.com/docs/api/sdk/typescript */
export function buildAgentOptions(projectRoot: string, apiKey: string): AgentOptions {
  return {
    apiKey: apiKey.trim(),
    model: { id: "composer-2.5" },
    local: {
      cwd: projectRoot,
      // Load .cursor/rules, mcp.json, agents from the FlowBoard repo (not user-global noise).
      settingSources: ["project"],
    },
  };
}

const MIN_NODE = [22, 13] as const;

export function checkNodeVersion(): void {
  const [major, minor] = process.versions.node.split(".").map(Number);
  const [minMajor, minMinor] = MIN_NODE;
  if (major < minMajor || (major === minMajor && minor < minMinor)) {
    console.error(
      `[agent-runner] Node ${process.versions.node} is too old. @cursor/sdk requires Node >= ${minMajor}.${minMinor}.`
    );
    process.exit(1);
  }
}

/** @cursor/sdk dynamically imports this on Node; not bundled in @cursor/sdk dependencies. */
export async function checkConnectNode(): Promise<void> {
  try {
    await import("@connectrpc/connect-node");
  } catch {
    console.error(
      "[agent-runner] Missing @connectrpc/connect-node.\n" +
        "  Run: cd scripts/agent-runner && npm install"
    );
    process.exit(1);
  }
}

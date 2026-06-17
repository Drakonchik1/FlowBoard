import { spawn } from "node:child_process";

export interface TestResult {
  ok: boolean;
  summary: string;
}

export function runDotnetTest(projectRoot: string): Promise<TestResult> {
  return new Promise((resolve) => {
    const child = spawn("dotnet", ["test", "--no-build"], {
      cwd: projectRoot,
      shell: true,
      stdio: ["ignore", "pipe", "pipe"],
    });

    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (d) => (stdout += d.toString()));
    child.stderr.on("data", (d) => (stderr += d.toString()));

    child.on("close", (code) => {
      const output = `${stdout}\n${stderr}`.trim();
      const lastLines = output.split("\n").slice(-5).join(" ");
      resolve({
        ok: code === 0,
        summary: code === 0 ? `dotnet test passed — ${lastLines}` : `dotnet test failed (exit ${code}) — ${lastLines}`,
      });
    });

    child.on("error", (err) => {
      resolve({ ok: false, summary: `Could not run dotnet test: ${err.message}` });
    });
  });
}

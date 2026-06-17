export type TaskStatus = "pending" | "in_progress" | "done" | "failed" | "skipped";

export type TaskKind = "implement" | "council";

/** sprint = changes from current sprint; project = full codebase baseline (e.g. milestone security audit). */
export type CouncilScope = "sprint" | "project";

export interface CouncilMember {
  id: string;
  role: string;
  focus: string;
  /** Default "sprint". Use "project" for full-repo reviews (e.g. security after Sprint 5). */
  scope?: CouncilScope;
  /** Optional model override (e.g. thinking model for security). */
  model?: string;
}

export interface CouncilConfig {
  outputFile: string;
  workDir?: string;
  members: CouncilMember[];
  synthesizer?: {
    role: string;
    focus: string;
    model?: string;
  };
}

export interface Task {
  id: string;
  title: string;
  status: TaskStatus;
  kind?: TaskKind;
  files: string[];
  doneWhen: string;
  dontTouch: string[];
  notes?: string;
  council?: CouncilConfig;
}

export interface TaskQueue {
  version: number;
  projectGoal: string;
  sprint: number;
  tasks: Task[];
}

export interface RunnerState {
  lastTaskId?: string;
  lastRunId?: string;
  lastAgentId?: string;
  lastStatus?: string;
  updatedAt?: string;
}

export interface RunOptions {
  dryRun: boolean;
  verifyTests: boolean;
  projectRoot: string;
}

export function isCouncilTask(task: Task): boolean {
  return task.kind === "council" || Boolean(task.council);
}

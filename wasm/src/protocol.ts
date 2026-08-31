export type Workflow = "loading" | "idle" | "ready" | "running" | "paused" | "finished" | "error";

export interface WarriorSource {
  name: string;
  source: string;
}

export interface PlayerView {
  name: string;
  regs: string;
  code: string;
}

export interface AppState {
  workflow: Workflow;
  message: string;
  player1: PlayerView;
  player2: PlayerView;
  memory: string[];
  console: string;
  status: string;
  scores: string;
  completedCombats: number;
  totalCombats: number;
  activePlayer: number;
  historyPosition: number;
  historyCount: number;
  canBrowseEarlier: boolean;
  canBrowseLater: boolean;
  report?: string;
}

export type MainToWorker =
  | { type: "init"; wasmUrl: string }
  | { type: "load"; warriors: WarriorSource[] }
  | { type: "command"; command: string };

export type WorkerToMain =
  | { type: "ready" }
  | { type: "state"; state: AppState }
  | { type: "error"; message: string };

export const emptyMemory = (): string[] => Array.from({ length: 1024 }, () => "");

export const emptyState = (): AppState => ({
  workflow: "loading",
  message: "Loading radare2 WebAssembly…",
  player1: { name: "Player 1", regs: "", code: "" },
  player2: { name: "Player 2", regs: "", code: "" },
  memory: emptyMemory(),
  console: "[r2wars wasm]",
  status: "Loading",
  scores: "No tournament loaded.",
  completedCombats: 0,
  totalCombats: 0,
  activePlayer: -1,
  historyPosition: 0,
  historyCount: 0,
  canBrowseEarlier: false,
  canBrowseLater: false,
});

import { architectureFromFilename, displayName } from "./architecture";
import { CombatEngine, type EngineSnapshot } from "./engine";
import { emptyMemory, emptyState, type AppState, type WarriorSource, type Workflow } from "./protocol";
import { RadareRuntime } from "./radare";

const MAX_CYCLES = 2000;
const MAX_HISTORY = 2048;
const TICKS_PER_BATCH = 8;

interface ActiveCombat {
  pairing: [number, number];
  round: number;
  roundWins: [number, number];
  cycles: number;
}

export class TournamentController {
  private engine: CombatEngine;
  private warriors: WarriorSource[] = [];
  private pairings: Array<[number, number]> = [];
  private scores: number[] = [];
  private completedCombats = 0;
  private pairingIndex = 0;
  private active: ActiveCombat | null = null;
  private autoRun = false;
  private loopScheduled = false;
  private workflow: Workflow = "idle";
  private message = "Choose warrior files to begin.";
  private consoleText = "[r2wars wasm]\nAll execution stays in this browser.";
  private combatLog = "";
  private fullLog = "";
  private history: EngineSnapshot[] = [];
  private historyIndex = -1;
  private report: string | undefined;

  constructor(runtime: RadareRuntime, private emit: (state: AppState) => void) {
    this.engine = new CombatEngine(runtime);
  }

  load(warriors: WarriorSource[], emit = true): void {
    this.stopLoop();
    this.engine.dispose();
    const normalized = warriors
      .filter((warrior) => warrior.name.toLowerCase().endsWith(".asm"))
      .sort((left, right) => left.name.localeCompare(right.name, undefined, { sensitivity: "base" }));
    const names = new Set<string>();
    for (const warrior of normalized) {
      architectureFromFilename(warrior.name);
      const key = warrior.name.toLowerCase();
      if (names.has(key)) throw new Error(`Duplicate warrior filename: ${warrior.name}`);
      names.add(key);
      if (!warrior.source.trim()) throw new Error(`${warrior.name}: source is empty`);
    }

    this.warriors = normalized;
    this.pairings = [];
    for (let first = 0; first < normalized.length; first++) {
      for (let second = first + 1; second < normalized.length; second++) {
        this.pairings.push([first, second]);
      }
    }
    this.scores = normalized.map(() => 0);
    this.completedCombats = 0;
    this.pairingIndex = 0;
    this.active = null;
    this.history = [];
    this.historyIndex = -1;
    this.fullLog = "";
    this.combatLog = "";
    this.report = undefined;
    const list = normalized.map((warrior) => `  • ${warrior.name}`).join("\n");
    this.consoleText = `Tournament engine: radare2 6.2.0 WASI\nLoaded warriors (${normalized.length}):\n${list}`;

    if (normalized.length < 2) {
      this.workflow = "idle";
      this.message = "At least two .asm warriors are required.";
    } else {
      this.workflow = "ready";
      this.message = `${normalized.length} warriors loaded for ${this.pairings.length} battles. Start when ready.`;
    }
    if (emit) this.emitState();
  }

  start(warriors: WarriorSource[]): void {
    this.load(warriors, false);
    if (this.workflow === "ready") this.run();
    else this.emitState();
  }

  command(command: string): void {
    switch (command) {
      case "cmd_state":
        this.emitState();
        break;
      case "cmd_run":
        this.run();
        break;
      case "cmd_stop":
        this.pause();
        break;
      case "cmd_step":
      case "cmd_next":
        this.step();
        break;
      case "cmd_reset":
        this.load(this.warriors);
        break;
      case "cmd_prevlog":
        this.browseHistory(-1);
        break;
      case "cmd_nextlog":
        this.browseHistory(1);
        break;
    }
  }

  fail(error: unknown): void {
    this.stopLoop();
    this.workflow = "error";
    this.message = error instanceof Error ? error.message : String(error);
    this.consoleText += `\n\nERROR\n${this.message}`;
    this.emitState();
  }

  private run(): void {
    if (this.workflow !== "ready" && this.workflow !== "paused") return;
    this.autoRun = true;
    this.workflow = "running";
    this.message = "Tournament running. Pause at any time to inspect or step.";
    this.engine.setStatus("Running");
    this.emitState();
    this.scheduleLoop();
  }

  private pause(): void {
    if (this.workflow !== "running") return;
    this.autoRun = false;
    this.workflow = "paused";
    this.message = "Paused. Resume continuous play or step exactly one cycle.";
    this.engine.setStatus("Paused");
    this.emitState();
  }

  private step(): void {
    if (this.workflow !== "paused") return;
    this.autoRun = false;
    this.advanceOneTick();
    if (this.completedCombats < this.pairings.length) {
      this.workflow = "paused";
      this.message = "Paused after one cycle. Step again or resume continuous play.";
      this.engine.setStatus("Paused — advanced 1 cycle");
      this.emitState();
    }
  }

  private scheduleLoop(): void {
    if (this.loopScheduled || !this.autoRun) return;
    this.loopScheduled = true;
    setTimeout(() => {
      this.loopScheduled = false;
      if (!this.autoRun) return;
      try {
        for (let tick = 0; tick < TICKS_PER_BATCH && this.autoRun; tick++) {
          this.advanceOneTick(false);
          if (this.workflow === "finished" || this.workflow === "error") break;
        }
        this.emitState();
        this.scheduleLoop();
      } catch (error) {
        this.fail(error);
      }
    }, 16);
  }

  private advanceOneTick(emit = true): void {
    if (!this.active) {
      if (!this.prepareCombat()) return;
    }
    const result = this.engine.tick();
    this.active!.cycles++;
    this.recordHistory(result.snapshot);

    if (result.deadPlayer !== null) {
      const winner = 1 - result.deadPlayer;
      const winnerGlobal = this.active!.pairing[winner];
      const loserGlobal = this.active!.pairing[result.deadPlayer];
      this.active!.roundWins[winner]++;
      this.scores[winnerGlobal]++;
      const roundLine = `Round ${this.active!.round + 1}: ${displayName(this.warriors[winnerGlobal].name)} wins (${this.active!.cycles} cycles)\n` +
        `  Defeated: ${displayName(this.warriors[loserGlobal].name)}\n` +
        `  Reason: ${result.deathReason}\n` +
        `  Instruction: ${result.deathInstruction}\n`;
      this.combatLog += roundLine;
      this.fullLog += roundLine;
      if (this.active!.roundWins[winner] >= 2 || this.active!.round >= 2) {
        this.finishCombat(winner);
      } else {
        this.active!.round++;
        this.active!.cycles = 0;
        this.engine.resetRound();
        this.clearHistory(this.engine.snapshot());
      }
    } else if (this.active!.cycles >= MAX_CYCLES) {
      const timeout = `Round ${this.active!.round + 1}: timeout (${this.active!.cycles} cycles)\n`;
      this.combatLog += timeout;
      this.fullLog += timeout;
      if (this.active!.round >= 2) {
        this.finishCombat(null);
      } else {
        this.active!.round++;
        this.active!.cycles = 0;
        this.engine.resetRound();
        this.clearHistory(this.engine.snapshot());
      }
    }

    if (emit) this.emitState();
  }

  private prepareCombat(): boolean {
    if (this.pairingIndex >= this.pairings.length) {
      this.finishTournament();
      return false;
    }
    const pairing = this.pairings[this.pairingIndex];
    const names = pairing.map((index) => displayName(this.warriors[index].name));
    this.active = { pairing, round: 0, roundWins: [0, 0], cycles: 0 };
    this.combatLog = `Battle ${this.pairingIndex + 1} / ${this.pairings.length}: ${names[0]} vs ${names[1]}\n`;
    this.fullLog += this.combatLog;
    const snapshot = this.engine.initialize([this.warriors[pairing[0]], this.warriors[pairing[1]]]);
    this.clearHistory(snapshot);
    return true;
  }

  private finishCombat(winner: number | null): void {
    const names = this.engine.names();
    const winnerName = winner === null ? "Draw" : names[winner];
    const line = `Battle winner: ${winnerName}\n`;
    this.combatLog += line;
    this.fullLog += line;
    this.completedCombats++;
    this.pairingIndex++;
    this.active = null;

    if (this.completedCombats >= this.pairings.length) {
      this.finishTournament();
    } else if (!this.autoRun) {
      this.workflow = "paused";
      this.message = "Battle complete. Resume for the next battle.";
    } else {
      this.message = "Battle complete. Starting the next battle…";
    }
  }

  private finishTournament(): void {
    if (this.workflow === "finished") return;
    this.stopLoop();
    this.workflow = "finished";
    this.message = "Tournament complete. Review the standings or download the report.";
    const completed = new Date().toISOString();
    this.fullLog += `Tournament finished ${completed}\n`;
    this.report = `${this.buildScores()}\n\nFULL LOG\n========\n${this.fullLog}`;
    this.consoleText = this.fullLog;
    this.emitState();
  }

  private buildScores(): string {
    if (this.warriors.length < 2) return "No scores yet.";
    const standings = this.warriors
      .map((warrior, index) => ({ name: displayName(warrior.name), points: this.scores[index] }))
      .sort((left, right) => right.points - left.points || left.name.localeCompare(right.name));
    const rows = standings.map((standing, index) => {
      const name = standing.name.length > 28 ? `${standing.name.slice(0, 25)}...` : standing.name;
      return `${String(index + 1).padStart(4)}  ${name.padEnd(28)}  ${String(standing.points).padStart(3)} round wins`;
    });
    return [
      `STANDINGS — ${this.completedCombats} / ${this.pairings.length} battles complete`,
      "",
      "RANK  WARRIOR                       RESULTS",
      "----  ----------------------------  ----------------",
      ...rows,
    ].join("\n");
  }

  private clearHistory(snapshot: EngineSnapshot): void {
    this.history = [snapshot];
    this.historyIndex = 0;
  }

  private recordHistory(snapshot: EngineSnapshot): void {
    this.history.push(snapshot);
    if (this.history.length > MAX_HISTORY) this.history.shift();
    this.historyIndex = this.history.length - 1;
  }

  private browseHistory(delta: number): void {
    if (!this.history.length) return;
    this.historyIndex = Math.max(0, Math.min(this.history.length - 1, this.historyIndex + delta));
    this.emitState(this.history[this.historyIndex]);
  }

  private emitState(snapshot?: EngineSnapshot): void {
    const state = emptyState();
    const selected = this.historyIndex >= 0 ? this.history[this.historyIndex] : undefined;
    const current = snapshot ?? selected ?? {
      players: [state.player1, state.player2] as [typeof state.player1, typeof state.player2],
      memory: emptyMemory(),
      activePlayer: -1,
      status: this.workflow === "loading" ? "Loading" : "Idle",
    };
    const browsing = this.historyIndex >= 0 && this.historyIndex < this.history.length - 1;
    Object.assign(state, {
      workflow: this.workflow,
      message: this.message,
      player1: current.players[0],
      player2: current.players[1],
      memory: current.memory,
      console: this.active ? this.combatLog : this.consoleText,
      status: browsing ? `History ${this.historyIndex + 1} / ${this.history.length} — ${current.status}` : current.status,
      scores: this.buildScores(),
      completedCombats: this.completedCombats,
      totalCombats: this.pairings.length,
      activePlayer: this.active ? current.activePlayer : -1,
      historyPosition: this.historyIndex >= 0 ? this.historyIndex + 1 : 0,
      historyCount: this.history.length,
      canBrowseEarlier: this.historyIndex > 0,
      canBrowseLater: this.historyIndex >= 0 && this.historyIndex < this.history.length - 1,
      report: this.report,
    } satisfies Partial<AppState>);
    this.emit(state);
  }

  private stopLoop(): void {
    this.autoRun = false;
  }
}

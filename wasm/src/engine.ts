import { architectureFromFilename, displayName, r2ArchitectureCommands, type Architecture } from "./architecture";
import { emptyMemory, type PlayerView, type WarriorSource } from "./protocol";
import { R2Core, RadareRuntime } from "./radare";

export const ARENA_SIZE = 1024;
export const MAX_WARRIOR_SIZE = 512;

interface MemoryAccess {
  address: number;
  size: number;
  kind: "read" | "write";
}

interface PlayerInfo {
  pc: number;
  pcSize: number;
  oldPc: number;
  oldPcSize: number;
  instruction: string;
  disassembly: string;
  registers: string;
  cycles: number;
  memoryAccess: MemoryAccess[];
  dead: boolean;
  deathReason: string;
  deathInstruction: string;
}

interface Player {
  filename: string;
  name: string;
  source: string;
  origin: number;
  size: number;
  code: string;
  info: PlayerInfo;
}

export interface EngineSnapshot {
  players: [PlayerView, PlayerView];
  memory: string[];
  activePlayer: number;
  status: string;
}

export interface TickResult {
  deadPlayer: number | null;
  deathReason: string;
  deathInstruction: string;
  snapshot: EngineSnapshot;
}

export type RandomSource = () => number;

const baseColors = ["b", "r"] as const;
const readColors = ["q", "y"] as const;
const writeColors = ["v", "o"] as const;

export function parseMemoryAccess(output: string): MemoryAccess[] {
  const accesses: MemoryAccess[] = [];
  const pattern = /^f mem\.(read|write)\.[^ ]*\s+0x([0-9a-f]+)\s+@\s+0x([0-9a-f]+)/i;
  for (const line of output.split("\n")) {
    const match = pattern.exec(line.trim());
    if (!match) continue;
    accesses.push({
      kind: match[1].toLowerCase() as "read" | "write",
      size: Number.parseInt(match[2], 16),
      address: Number.parseInt(match[3], 16),
    });
  }
  return accesses;
}

export function parseCycles(output: string): number {
  try {
    const operation = JSON.parse(output)[0];
    const cycles = Number(operation?.cycles ?? 0);
    return Number.isFinite(cycles) && cycles > 0 ? cycles : 0;
  } catch {
    return 0;
  }
}

function parseNumber(output: string): number {
  const value = output.trim().split(/\s+/).at(-1) ?? "0";
  if (/^-?0x/i.test(value)) return Number.parseInt(value, 16);
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function registersForDisplay(output: string): string {
  let registers: string[];
  try {
    const values = JSON.parse(output) as Record<string, number>;
    registers = Object.entries(values)
      .filter(([name]) => name !== "oeax")
      .map(([name, value]) => `${name} = 0x${Number(value).toString(16).padStart(8, "0")}`);
  } catch {
    registers = output
      .split("\n")
      .map((line) => line.trim().replace(/^aer\s+/, ""))
      .filter((line) => line && !line.startsWith("oeax"));
  }
  const rows: string[] = [];
  for (let index = 0; index < registers.length; index += 3) {
    rows.push(registers.slice(index, index + 3).join(" "));
  }
  return rows.join("\n");
}

function codeForDisplay(info: PlayerInfo): string {
  const lines = info.disassembly.trimEnd().split("\n");
  const highlighted = lines.map((line) => {
    const escaped = escapeHtml(line);
    const address = line.trimStart().match(/^(0x[0-9a-f]+)/i)?.[1];
    return address && parseNumber(address) === info.pc ? `<span class="s">${escaped}</span>` : escaped;
  });
  return `Cycles:${info.cycles}\nActual Instruction:\n ${escapeHtml(info.instruction)}\n${highlighted.join("\n")}`;
}

function emptyInfo(): PlayerInfo {
  return {
    pc: -1,
    pcSize: 0,
    oldPc: -1,
    oldPcSize: 0,
    instruction: "",
    disassembly: "",
    registers: "",
    cycles: 0,
    memoryAccess: [],
    dead: false,
    deathReason: "",
    deathInstruction: "",
  };
}

export class CombatEngine {
  private cores: [R2Core, R2Core] | null = null;
  private players: [Player, Player] | null = null;
  private memory = emptyMemory();
  private currentPlayer = 0;
  private status = "Idle";

  constructor(private runtime: RadareRuntime, private random: RandomSource = Math.random) {}

  initialize(warriors: [WarriorSource, WarriorSource]): EngineSnapshot {
    this.disposeCores();
    const assembled = warriors.map((warrior) => {
      const architecture = architectureFromFilename(warrior.name);
      const code = this.runtime.assemble(architecture, warrior.source);
      const size = code.length / 2;
      if (size > MAX_WARRIOR_SIZE) throw new Error(`${warrior.name}: warrior is ${size} bytes; the limit is ${MAX_WARRIOR_SIZE}`);
      return { warrior, architecture, code, size };
    }) as [
      { warrior: WarriorSource; architecture: Architecture; code: string; size: number },
      { warrior: WarriorSource; architecture: Architecture; code: string; size: number },
    ];

    const origins = this.pickOrigins(assembled[0].size, assembled[1].size);
    this.cores = [this.runtime.createCore(), this.runtime.createCore()];
    const players = assembled.map((entry, index) => {
      const core = this.cores![index];
      core.cmd([
        `o+ malloc://${ARENA_SIZE}`,
        r2ArchitectureCommands(entry.architecture),
        "e scr.color=false",
        "e asm.lines=false",
        "e asm.flags=false",
        "e asm.comments=false",
        "e asm.bytes=false",
        "e cfg.r2wars=true",
        "aei",
        "aeim",
        `w0 ${ARENA_SIZE} @ 0`,
        `wx ${entry.code} @ ${origins[index]}`,
        `aer PC=${origins[index]}`,
        `aer SP=SP+${origins[index]}`,
        "e cmd.esil.todo=f theend=1",
        "e cmd.esil.trap=f theend=2",
        "e cmd.esil.intr=f theend=3",
        "e cmd.esil.ioer=f theend=4",
        "f theend=0",
        `b ${ARENA_SIZE}`,
      ].join(";"));
      return {
        filename: entry.warrior.name,
        name: displayName(entry.warrior.name),
        source: entry.warrior.source,
        origin: origins[index],
        size: entry.size,
        code: entry.code,
        info: emptyInfo(),
      };
    }) as [Player, Player];

    for (let coreIndex = 0; coreIndex < 2; coreIndex++) {
      for (let playerIndex = 0; playerIndex < 2; playerIndex++) {
        if (coreIndex === playerIndex) continue;
        this.cores[coreIndex].cmd(`wx ${players[playerIndex].code} @ ${players[playerIndex].origin}`);
      }
    }

    this.players = players;
    this.currentPlayer = 0;
    this.status = "Paused — ready to step";
    this.resetMarkers();
    this.players[0].info = this.readInfo(0);
    this.players[1].info = this.readInfo(1);
    this.paintPc(0);
    this.paintPc(1);
    return this.snapshot();
  }

  resetRound(): EngineSnapshot {
    if (!this.players) throw new Error("No combat is loaded");
    const warriors: [WarriorSource, WarriorSource] = [
      { name: this.players[0].filename, source: this.players[0].source },
      { name: this.players[1].filename, source: this.players[1].source },
    ];
    return this.initialize(warriors);
  }

  tick(): TickResult {
    if (!this.players || !this.cores) throw new Error("No combat is loaded");
    const playerIndex = this.currentPlayer;
    const player = this.players[playerIndex];
    let deadPlayer: number | null = null;

    if (player.info.cycles === 0) {
      this.executeInstruction(playerIndex);
      if (player.info.dead) deadPlayer = playerIndex;
    } else {
      player.info.cycles--;
    }

    this.status = player.info.dead ? player.info.deathReason : "Running";
    if (!player.info.dead) {
      this.paintOldPc(playerIndex);
      this.paintAccesses(playerIndex, player.info.memoryAccess);
      this.paintPc(playerIndex);
      this.paintPc(1 - playerIndex);
    }
    this.currentPlayer = 1 - this.currentPlayer;
    return {
      deadPlayer,
      deathReason: player.info.deathReason,
      deathInstruction: player.info.deathInstruction,
      snapshot: this.snapshot(),
    };
  }

  setStatus(status: string): void {
    this.status = status;
  }

  snapshot(): EngineSnapshot {
    if (!this.players) {
      return {
        players: [
          { name: "Player 1", regs: "", code: "" },
          { name: "Player 2", regs: "", code: "" },
        ],
        memory: [...this.memory],
        activePlayer: -1,
        status: this.status,
      };
    }
    return {
      players: this.players.map((player) => ({
        name: player.name,
        regs: player.info.registers,
        code: codeForDisplay(player.info),
      })) as [PlayerView, PlayerView],
      memory: [...this.memory],
      activePlayer: this.currentPlayer,
      status: this.status,
    };
  }

  names(): [string, string] {
    if (!this.players) return ["Player 1", "Player 2"];
    return [this.players[0].name, this.players[1].name];
  }

  dispose(): void {
    this.disposeCores();
    this.players = null;
  }

  private executeInstruction(playerIndex: number): void {
    const core = this.cores![playerIndex];
    const other = this.cores![1 - playerIndex];
    const info = this.players![playerIndex].info;
    const executedInstruction = core.cmd("pd 1 @r:PC").replace(/\r/g, "").trim();
    const accessOutput = core.cmd("f-mem.*;aea*@r:PC").replace(/\r/g, "");
    const accesses = parseMemoryAccess(accessOutput);
    const oldPc = info.pc;
    const oldPcSize = info.pcSize;

    core.cmd("aes");

    for (const access of accesses) {
      if (access.kind !== "write" || access.size <= 0 || access.address < 0 || access.address >= ARENA_SIZE) continue;
      const available = Math.min(access.size, ARENA_SIZE - access.address);
      const bytes = core.cmd(`p8 ${available} @ ${access.address}`).trim();
      if (/^[0-9a-f]+$/i.test(bytes)) other.cmd(`wx ${bytes} @ ${access.address}`);
    }

    const pc = parseNumber(core.cmd("aer PC"));
    const trap = core.cmd("?v 1+theend").trim();
    let deathReason = "";
    if (executedInstruction.includes("unaligned")) deathReason = "Executed unaligned instruction";
    else if (executedInstruction.includes("invalid")) deathReason = "Executed invalid instruction";
    else if (pc < 0 || pc >= ARENA_SIZE) deathReason = "Instruction executed out of the arena";
    else if (!["0", "1", "0x0", "0x1", "0x00", "0x01"].includes(trap)) {
      const trapValue = parseNumber(trap);
      deathReason = trapValue === 5
        ? "Instruction read/write outside the arena"
        : trapValue === 4
          ? "Syscall or interrupt instruction executed"
          : `ESIL stopped execution (trap ${trap || "unknown"})`;
    }

    const next = this.readInfo(playerIndex);
    next.oldPc = oldPc;
    next.oldPcSize = oldPcSize;
    next.memoryAccess = accesses;
    next.dead = Boolean(deathReason);
    next.deathReason = deathReason;
    next.deathInstruction = executedInstruction;
    this.players![playerIndex].info = next;
    this.players![1 - playerIndex].info.disassembly = other.cmd("pd 8 @r:PC").replace(/\r/g, "");
  }

  private readInfo(playerIndex: number): PlayerInfo {
    const core = this.cores![playerIndex];
    const pc = parseNumber(core.cmd("aer PC"));
    const registerOutput = core.cmd("aerj").replace(/\r/g, "");
    return {
      ...emptyInfo(),
      pc,
      pcSize: parseNumber(core.cmd("?v $l@r:PC")),
      instruction: core.cmd("pd 1 @r:PC").replace(/\r/g, "").trim(),
      disassembly: core.cmd("pd 8 @r:PC").replace(/\r/g, ""),
      registers: registersForDisplay(registerOutput),
      cycles: parseCycles(core.cmd("aoj 1 @r:PC").replace(/\r/g, "")),
      memoryAccess: parseMemoryAccess(core.cmd("f-mem.*;aea*@r:PC").replace(/\r/g, "")),
    };
  }

  private pickOrigins(firstSize: number, secondSize: number): [number, number] {
    const lowerFirst = this.random() < 0.5;
    const aligned = (start: number, end: number): number => {
      const slots = Math.max(1, Math.floor((end - start) / 4) + 1);
      return start + Math.floor(this.random() * slots) * 4;
    };
    const lower = (size: number) => aligned(0, Math.max(0, 512 - size));
    const upper = (size: number) => aligned(512, Math.max(512, ARENA_SIZE - size));
    return lowerFirst ? [lower(firstSize), upper(secondSize)] : [upper(firstSize), lower(secondSize)];
  }

  private resetMarkers(): void {
    this.memory = emptyMemory();
    if (!this.players) return;
    for (let playerIndex = 0; playerIndex < 2; playerIndex++) {
      const player = this.players[playerIndex];
      this.paint(player.origin, player.size, baseColors[playerIndex]);
    }
  }

  private paintOldPc(playerIndex: number): void {
    const info = this.players![playerIndex].info;
    this.paint(info.oldPc, info.oldPcSize, baseColors[playerIndex]);
  }

  private paintPc(playerIndex: number): void {
    const info = this.players![playerIndex].info;
    this.paint(info.pc, Math.max(1, info.pcSize), `${baseColors[playerIndex]}X`);
  }

  private paintAccesses(playerIndex: number, accesses: MemoryAccess[]): void {
    for (const access of accesses) {
      const color = access.kind === "read" ? readColors[playerIndex] : writeColors[playerIndex];
      this.paint(access.address, access.size, `${color}${access.kind === "read" ? "R" : "W"}`);
    }
  }

  private paint(offset: number, count: number, value: string): void {
    for (let index = 0; index < count; index++) {
      const address = offset + index;
      if (address >= 0 && address < ARENA_SIZE) this.memory[address] = value;
    }
  }

  private disposeCores(): void {
    if (!this.cores) return;
    this.cores[0].dispose();
    this.cores[1].dispose();
    this.cores = null;
  }
}

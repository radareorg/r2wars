import { ConsoleStdout, File, OpenFile, WASI } from "@bjorn3/browser_wasi_shim";
import type { Architecture } from "./architecture";

interface R2Exports extends WebAssembly.Exports {
  memory: WebAssembly.Memory;
  malloc(size: number): number;
  free(pointer: number): void;
  r_core_new(): number;
  r_core_free(core: number): void;
  r_core_cmd_str(core: number, command: number): number;
  r_main_rasm2(argc: number, argv: number): number;
}

const encoder = new TextEncoder();
const decoder = new TextDecoder();

export class RadareRuntime {
  private exports!: R2Exports;
  private stdout = "";
  private stderr = "";

  async load(url: string): Promise<void> {
    const response = await fetch(url);
    if (!response.ok) throw new Error(`Cannot load radare2.wasm: HTTP ${response.status}`);
    await this.loadBytes(await response.arrayBuffer());
  }

  async loadBytes(bytes: BufferSource): Promise<void> {
    const stdout = new ConsoleStdout((chunk) => {
      this.stdout += decoder.decode(chunk, { stream: true });
    });
    const stderr = new ConsoleStdout((chunk) => {
      this.stderr += decoder.decode(chunk, { stream: true });
    });
    const wasi = new WASI(
      [],
      [],
      [new OpenFile(new File(new Uint8Array())), stdout, stderr],
    );
    const module = await WebAssembly.compile(bytes);
    const instance = await WebAssembly.instantiate(module, {
      wasi_snapshot_preview1: wasi.wasiImport,
    });
    wasi.initialize(instance as WebAssembly.Instance & { exports: R2Exports });
    this.exports = instance.exports as R2Exports;
  }

  createCore(): R2Core {
    const pointer = this.exports.r_core_new();
    if (!pointer) throw new Error("radare2 failed to create an RCore");
    return new R2Core(this, pointer);
  }

  command(core: number, command: string): string {
    const input = this.putString(command);
    const output = this.exports.r_core_cmd_str(core, input);
    try {
      return output ? this.getString(output) : "";
    } finally {
      if (output) this.exports.free(output);
      this.exports.free(input);
    }
  }

  freeCore(core: number): void {
    this.exports.r_core_free(core);
  }

  assemble(architecture: Architecture, source: string): string {
    this.stdout = "";
    this.stderr = "";
    const args = ["rasm2", "-a", architecture.arch, "-b", String(architecture.bits)];
    if (architecture.cpu) args.push("-c", architecture.cpu);
    args.push(source);
    const strings = args.map((argument) => this.putString(argument));
    const argv = this.exports.malloc(strings.length * 4);
    const view = new DataView(this.exports.memory.buffer);
    strings.forEach((pointer, index) => view.setUint32(argv + index * 4, pointer, true));
    let result: number;
    try {
      result = this.exports.r_main_rasm2(strings.length, argv);
    } finally {
      strings.forEach((pointer) => this.exports.free(pointer));
      this.exports.free(argv);
    }
    const hex = this.stdout.replace(/\s/g, "").toLowerCase();
    if (result !== 0 || !hex || !/^[0-9a-f]+$/.test(hex) || hex.length % 2 !== 0) {
      const detail = this.stderr.trim() || this.stdout.trim() || `rasm2 exited with ${result}`;
      throw new Error(`Cannot assemble warrior: ${detail}`);
    }
    return hex;
  }

  private putString(value: string): number {
    const bytes = encoder.encode(`${value}\0`);
    const pointer = this.exports.malloc(bytes.length);
    if (!pointer) throw new Error("radare2 WebAssembly ran out of memory");
    new Uint8Array(this.exports.memory.buffer).set(bytes, pointer);
    return pointer;
  }

  private getString(pointer: number): string {
    const memory = new Uint8Array(this.exports.memory.buffer);
    let end = pointer;
    while (end < memory.length && memory[end] !== 0) end++;
    return decoder.decode(memory.subarray(pointer, end));
  }
}

export class R2Core {
  private disposed = false;

  constructor(private runtime: RadareRuntime, private pointer: number) {}

  cmd(command: string): string {
    if (this.disposed) throw new Error("radare2 core is already disposed");
    return this.runtime.command(this.pointer, command);
  }

  dispose(): void {
    if (this.disposed) return;
    this.disposed = true;
    this.runtime.freeCore(this.pointer);
  }
}

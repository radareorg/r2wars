import { readFile } from "node:fs/promises";
import { describe, expect, it } from "vitest";
import { architectureFromFilename } from "./architecture";
import { CombatEngine } from "./engine";
import { RadareRuntime } from "./radare";

describe("radare2 WASI integration", () => {
  it("assembles labels, creates two cores, and advances a combat", async () => {
    const runtime = new RadareRuntime();
    const wasm = await readFile(new URL("../public/radare2.wasm", import.meta.url));
    await runtime.loadBytes(wasm);
    const publicDirectory = new URL("../public/warriors/", import.meta.url);
    const examples = JSON.parse(await readFile(new URL("manifest.json", publicDirectory), "utf8")) as string[];
    for (const name of examples) {
      const source = await readFile(new URL(name, publicDirectory), "utf8");
      expect(runtime.assemble(architectureFromFilename(name), source).length, name).toBeGreaterThan(0);
    }
    const engine = new CombatEngine(runtime, () => 0.25);
    const snapshot = engine.initialize([
      {
        name: "left.x86-32.asm",
        source: "call getpc\ngetpc:\npop eax\nloop:\nsub eax, 4\nmov dword ptr [eax], 0\njmp loop",
      },
      {
        name: "right.x86-32.asm",
        source: "call getpc\ngetpc:\npop edi\nloop:\nadd edi, 4\nmov dword ptr [edi], 0xcccccccc\njmp loop",
      },
    ]);
    expect(snapshot.players.map((player) => player.name)).toEqual(["left.x86-32", "right.x86-32"]);
    expect(snapshot.memory).toHaveLength(1024);
    let result = engine.tick();
    for (let tick = 0; tick < 16; tick++) result = engine.tick();
    expect(result.snapshot.activePlayer).toBe(1);
    expect(result.snapshot.players[0].regs.length).toBeGreaterThan(0);
    expect(engine.resetRound().players.map((player) => player.name)).toEqual(["left.x86-32", "right.x86-32"]);
    engine.dispose();
  }, 30_000);
});

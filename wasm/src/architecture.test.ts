import { describe, expect, it } from "vitest";
import { architectureFromFilename, r2ArchitectureCommands } from "./architecture";
import { parseCycles, parseMemoryAccess } from "./engine";

describe("architecture detection", () => {
  it("maps every supported filename family", () => {
    const mappings = [
      ["foo.x86-32.asm", "x86", 32],
      ["foo.x86-64.asm", "x86", 64],
      ["foo.arm-16.asm", "arm", 16],
      ["foo.arm-32.asm", "arm", 32],
      ["foo.arm-64.asm", "arm", 64],
      ["foo.mips-32.asm", "mips", 32],
      ["foo.mips-64.asm", "mips", 64],
      ["foo.riscv-32.asm", "riscv", 32],
      ["foo.riscv-64.asm", "riscv", 64],
      ["foo.gb.asm", "gb", 16],
      ["foo.8051.asm", "8051", 8],
    ] as const;
    for (const [filename, arch, bits] of mappings) {
      expect(architectureFromFilename(filename)).toMatchObject({ arch, bits });
    }
    expect(r2ArchitectureCommands(architectureFromFilename("foo.8051.asm"))).toContain("asm.cpu=8051-shared-code-xdata");
  });

  it("rejects untagged sources", () => {
    expect(() => architectureFromFilename("foo.asm")).toThrow(/architecture/);
  });
});

describe("radare output parsing", () => {
  it("parses ESIL memory flags", () => {
    expect(parseMemoryAccess("f mem.read.0 0x00000004 @ 0x00000020\nf mem.write.1 0x2 @ 0x3f")).toEqual([
      { kind: "read", size: 4, address: 0x20 },
      { kind: "write", size: 2, address: 0x3f },
    ]);
  });

  it("parses instruction cycles safely", () => {
    expect(parseCycles('[{"cycles":3,"size":1}]')).toBe(3);
    expect(parseCycles("not json")).toBe(0);
  });
});

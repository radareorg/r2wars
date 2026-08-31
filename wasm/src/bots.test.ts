import { describe, expect, it } from "vitest";
import { mergeWarriors, newWarrior, validateWarriors } from "./bots";

describe("bot roster", () => {
  it("merges imported bots by case-insensitive filename", () => {
    expect(mergeWarriors(
      [{ name: "one.x86-32.asm", source: "old" }],
      [{ name: "ONE.x86-32.asm", source: "updated" }, { name: "two.arm-32.asm", source: "new" }],
    )).toEqual([
      { name: "ONE.x86-32.asm", source: "updated" },
      { name: "two.arm-32.asm", source: "new" },
    ]);
  });

  it("creates unique editable x86 bot names", () => {
    expect(newWarrior([{ name: "new-warrior-1.x86-32.asm", source: "nop" }]).name)
      .toBe("new-warrior-2.x86-32.asm");
  });

  it("validates count, filenames, duplicates, and source", () => {
    expect(() => validateWarriors([{ name: "one.x86-32.asm", source: "nop" }])).toThrow(/at least two/i);
    expect(() => validateWarriors([
      { name: "one.x86-32.asm", source: "nop" },
      { name: "two.asm", source: "nop" },
    ])).toThrow(/architecture/i);
    expect(() => validateWarriors([
      { name: "one.x86-32.asm", source: "nop" },
      { name: "ONE.x86-32.asm", source: "nop" },
    ])).toThrow(/duplicate/i);
    expect(() => validateWarriors([
      { name: "one.x86-32.asm", source: "nop" },
      { name: "two.arm-32.asm", source: "" },
    ])).toThrow(/source is empty/i);
  });
});

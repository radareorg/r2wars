import { architectureFromFilename } from "./architecture";
import type { WarriorSource } from "./protocol";

export function cloneWarriors(warriors: WarriorSource[]): WarriorSource[] {
  return warriors.map((warrior) => ({ ...warrior }));
}

export function mergeWarriors(current: WarriorSource[], incoming: WarriorSource[]): WarriorSource[] {
  const merged = cloneWarriors(current);
  for (const warrior of incoming) {
    const index = merged.findIndex((entry) => entry.name.toLowerCase() === warrior.name.toLowerCase());
    if (index >= 0) merged[index] = { ...warrior };
    else merged.push({ ...warrior });
  }
  return merged;
}

export function newWarrior(warriors: WarriorSource[]): WarriorSource {
  const names = new Set(warriors.map((warrior) => warrior.name.toLowerCase()));
  let suffix = 1;
  while (names.has(`new-warrior-${suffix}.x86-32.asm`)) suffix++;
  return {
    name: `new-warrior-${suffix}.x86-32.asm`,
    source: "start:\n  nop\n  jmp start\n",
  };
}

export function validateWarriors(warriors: WarriorSource[]): void {
  if (warriors.length < 2) throw new Error("Add at least two bots before starting a tournament.");
  const names = new Set<string>();
  for (const warrior of warriors) {
    const name = warrior.name.trim();
    if (!name.toLowerCase().endsWith(".asm")) throw new Error(`${name || "Unnamed bot"}: filename must end in .asm`);
    architectureFromFilename(name);
    const key = name.toLowerCase();
    if (names.has(key)) throw new Error(`Duplicate bot filename: ${name}`);
    names.add(key);
    if (!warrior.source.trim()) throw new Error(`${name}: source is empty`);
  }
}

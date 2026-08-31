export interface Architecture {
  id: string;
  arch: string;
  bits: number;
  cpu?: string;
}

const architectures: Array<[string, Architecture]> = [
  [".arm-16.", { id: "arm-16", arch: "arm", bits: 16 }],
  [".arm-32.", { id: "arm-32", arch: "arm", bits: 32 }],
  [".arm-64.", { id: "arm-64", arch: "arm", bits: 64 }],
  [".mips-32.", { id: "mips-32", arch: "mips", bits: 32 }],
  [".mips-64.", { id: "mips-64", arch: "mips", bits: 64 }],
  [".x86-32.", { id: "x86-32", arch: "x86", bits: 32 }],
  [".x86-64.", { id: "x86-64", arch: "x86", bits: 64 }],
  [".riscv-32.", { id: "riscv-32", arch: "riscv", bits: 32 }],
  [".riscv-64.", { id: "riscv-64", arch: "riscv", bits: 64 }],
  [".gb.", { id: "gb", arch: "gb", bits: 16 }],
  [".8051.", { id: "8051", arch: "8051", bits: 8, cpu: "8051-shared-code-xdata" }],
];

export function architectureFromFilename(filename: string): Architecture {
  const lower = filename.toLowerCase();
  const match = architectures.find(([marker]) => lower.includes(marker));
  if (!match) {
    throw new Error(`${filename}: architecture must be encoded in the filename, for example .x86-32.asm or .arm-32.asm`);
  }
  return match[1];
}

export function r2ArchitectureCommands(architecture: Architecture): string {
  const commands = [`e asm.arch=${architecture.arch}`, `e asm.bits=${architecture.bits}`];
  if (architecture.cpu) commands.push(`e asm.cpu=${architecture.cpu}`);
  return commands.join(";");
}

export function displayName(filename: string): string {
  return filename.replace(/\.asm$/i, "");
}

import { createHash } from "node:crypto";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { join } from "node:path";
import { unzipSync } from "fflate";

const version = "6.2.0";
const archiveSha256 = "f7ce3b9919cc526ab6d6dbb2c5a12783781a0097b74d8de608b5639201c75db6";
const wasmSha256 = "d583b19828317aabdd7de942d470b6fe1b7370929763ff38483158157c7ef3ae";
const output = join(import.meta.dirname, "..", "public", "radare2.wasm");
const source = `https://github.com/radareorg/radare2/releases/download/${version}/radare2-${version}-wasi-api.zip`;

const digest = (data) => createHash("sha256").update(data).digest("hex");

try {
  const present = await readFile(output);
  if (digest(present) === wasmSha256) process.exit(0);
} catch {
  // Fetch below.
}

console.log(`Downloading radare2 ${version} WASI API…`);
const response = await fetch(source);
if (!response.ok) throw new Error(`radare2 download failed: ${response.status} ${response.statusText}`);
const archive = new Uint8Array(await response.arrayBuffer());
if (digest(archive) !== archiveSha256) throw new Error("radare2 archive checksum mismatch");

const files = unzipSync(archive);
const entry = Object.entries(files).find(([name]) => name.endsWith("/radare2.wasm"));
if (!entry) throw new Error("radare2.wasm was not found in the release archive");
if (digest(entry[1]) !== wasmSha256) throw new Error("radare2.wasm checksum mismatch");

await mkdir(join(import.meta.dirname, "..", "public"), { recursive: true });
await writeFile(output, entry[1]);
console.log(`Saved ${output}`);

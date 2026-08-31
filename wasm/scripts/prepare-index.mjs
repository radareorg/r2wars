import { mkdir, readFile, writeFile } from "node:fs/promises";
import { join } from "node:path";

const root = join(import.meta.dirname, "..");
const sourcePath = join(root, "..", "csharp", "wwwroot", "index.html");
const outputPath = join(root, "index.html");

let html = await readFile(sourcePath, "utf8");
html = html.replace('data-r2wars-engine="dotnet"', 'data-r2wars-engine="wasm"');
html = html.replace('/assets/r2wars-ui.js', './src/main.ts');

await mkdir(root, { recursive: true });
await writeFile(outputPath, html);

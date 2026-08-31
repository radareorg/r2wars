import { mkdir, readFile, writeFile } from "node:fs/promises";
import { join } from "node:path";

const root = join(import.meta.dirname, "..");
const sourcePath = join(root, "..", "csharp", "wwwroot", "index.html");
const outputPath = join(root, "index.html");

let html = await readFile(sourcePath, "utf8");
html = html.replace(/^\uFEFF?<script>[\s\S]*?<\/script>/, "");
const closing = html.indexOf("</html>");
if (closing >= 0) html = html.slice(0, closing + "</html>".length);
html = html.replace(
  "</body>",
  '    <script type="module" src="./src/main.ts"></script>\n</body>',
);

await mkdir(root, { recursive: true });
await writeFile(outputPath, html);

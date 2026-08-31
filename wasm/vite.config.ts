import path from "node:path";
import { defineConfig } from "vite";

export default defineConfig({
  base: "./",
  root: import.meta.dirname,
  publicDir: path.resolve(import.meta.dirname, "public"),
  build: {
    outDir: path.resolve(import.meta.dirname, "dist"),
    emptyOutDir: true,
    sourcemap: true,
  },
  worker: {
    format: "es",
  },
});

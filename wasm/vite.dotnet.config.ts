import path from "node:path";
import { defineConfig } from "vite";

export default defineConfig({
  base: "/assets/",
  root: import.meta.dirname,
  publicDir: false,
  define: {
    "import.meta.env.VITE_R2WARS_ENGINE": JSON.stringify("dotnet"),
  },
  plugins: [{
    name: "drop-unused-wasm-worker",
    generateBundle(_options, bundle) {
      for (const filename of Object.keys(bundle)) {
        if (filename.startsWith("assets/worker-")) delete bundle[filename];
      }
    },
  }],
  build: {
    outDir: path.resolve(import.meta.dirname, "../csharp/wwwroot/assets"),
    emptyOutDir: true,
    sourcemap: false,
    rollupOptions: {
      input: path.resolve(import.meta.dirname, "src/main.ts"),
      output: {
        entryFileNames: "r2wars-ui.js",
        chunkFileNames: "[name]-[hash].js",
        assetFileNames: "[name]-[hash][extname]",
      },
    },
  },
  worker: {
    format: "es",
  },
});

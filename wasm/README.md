# r2wars WebAssembly

This directory contains the standalone, browser-only r2wars implementation.
It is additive: the C# application, its WebSocket protocol, Docker image, and
native `radare2`/`rasm2` execution remain unchanged.

## Architecture

- `src/main.ts` adapts the existing r2wars HTML UI to a local Web Worker.
- `src/bots.ts` manages and validates the editable draft bot roster.
- `src/worker.ts` owns the tournament controller and keeps ESIL work off the UI
  thread.
- `src/tournament.ts` implements round-robin scheduling, scoring, pause/step,
  bounded history, timeouts, and downloadable reports.
- `src/engine.ts` implements the two-warrior shared arena and synchronizes ESIL
  memory writes between two `RCore` objects.
- `src/radare.ts` is the small WASI/FFI wrapper around radare2.

The build reuses the maintained UI markup and styles from
`csharp/wwwroot/index.html`; it replaces only the WebSocket script with the
local browser controller.

## Commands

    npm install
    npm test
    npm run build
    npm run dev

`npm run build` produces a self-contained static directory at `dist/`.
From the repository root, `make wasm-dist` also creates
`wasm/r2wars-wasm.zip`. Extract its contents directly into a static server's
document root; `index.html` is at the archive root.

The **Bots** dialog can create, import, rename, edit, and delete browser-local
warriors. These edits remain in a draft roster and are sent to the worker only
when a new tournament starts, so they cannot mutate an active combat. Each
editor input event also makes an isolated assembly request and displays the
radare2 result, byte count, and byte array without loading that code into the
tournament engine.

## radare2 artifact

`scripts/fetch-r2.mjs` downloads `radare2-6.2.0-wasi-api.zip` from the official
radare2 release, verifies both the archive and module SHA-256 digests, and puts
the extracted module in `public/radare2.wasm`. The large generated module and
build output are intentionally ignored by git.

The released WASI plugin set covers x86, ARM, MIPS, RISC-V, and Game Boy
warriors. The 6.2.0 WASI release does not include the 8051 plugin, so 8051
warriors currently report an assembly error in the browser build; the native
.NET/Docker path retains 8051 support.

## Static and offline use

The `dist/` directory requires no application backend. Serve it from any
ordinary static host. Browsers generally restrict `file://` WebAssembly module
loading, so opening `index.html` directly from disk is not a supported launch
method.

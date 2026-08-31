# r2wars

![r2wars logo](csharp/resources/r2wars_logo_transparent.png?raw=true "r2wars")

r2wars is a Core War-style tournament powered by radare2 and ESIL. Two
assembly warriors share a 1 KiB arena and take turns executing instructions.
Each warrior tries to corrupt its opponent until the opponent crashes, traps,
or leaves the arena.

Warriors may target different CPU architectures while competing in the same
address space.

## Choose how to run it

| Mode | Requirements | Execution | Best for |
| --- | --- | --- | --- |
| Docker | Docker with Compose | .NET and native radare2 in containers | Fastest local setup |
| Native | .NET 10 and radare2 | Local .NET server and native radare2 | Development and full architecture support |
| WebAssembly | Node.js/npm for building | Entirely inside the browser | Static hosting and serverless deployment |

The WebAssembly implementation is an alternative execution path. It does not
replace or change the existing .NET and Docker flows.

## Quick start with Docker

From the repository root:

```sh
make start
```

Open <http://127.0.0.1:9664/>. The local `warriors/` directory is mounted
read-only into the container.

Useful commands:

```sh
make build   # build the Docker image
make start   # start r2wars and its nginx frontend
make stop    # stop the containers
make clean   # stop and remove the r2wars image
```

The Docker image pins radare2 6.2.0 and verifies the official `amd64` or
`arm64` release package before installing it.

## Run natively

Install the .NET 10 SDK and radare2, then run:

```sh
dotnet run --project csharp/r2wars.csproj -- warriors
```

Open <http://127.0.0.1:9664/>.

On Linux, macOS, and BSD, `radare2` and `rasm2` are loaded from `PATH`. On
Windows, put `radare2.exe` and `rasm2.exe` in `PATH` or alongside the published
r2wars application.

## WebAssembly version

The browser version runs the tournament controller and radare2 ESIL locally in
a Web Worker. It has no Kestrel or WebSocket connection, does not require
Docker or .NET at runtime, and never uploads warrior source or tournament state.

### Build and run locally

```sh
make wasm-build
make wasm-run
```

Then open <http://127.0.0.1:5173/>. The production site is generated in
`wasm/dist/`.

The first build downloads the official radare2 6.2.0 WASI API module and
verifies the checksums of both the release archive and extracted Wasm module.
The module is about 44 MB before HTTP or ZIP compression.

### Create a deployable ZIP

```sh
make wasm-dist
```

This creates `wasm/r2wars-wasm.zip`. The archive contains the complete static
site with `index.html` at its root:

```text
index.html
radare2.wasm
assets/
warriors/
```

Extract those files directly into a static server's document root. No
application backend, .NET runtime, radare2 installation, or WebSocket proxy is
needed. The paths are relative, so the site can also be hosted below a URL
prefix.

The server should deliver `.wasm` files as `application/wasm`. Opening
`index.html` through `file://` is not supported because browsers restrict local
Wasm and module loading; use any ordinary HTTP(S) static server instead.

You can choose a different archive name when needed:

```sh
make wasm-dist WASM_ARCHIVE=my-r2wars-build.zip
```

### Browser controls

- Three example warriors are loaded initially.
- Open **Bots** while the tournament is not running to inspect the complete
  roster, create or import bots, rename them, edit their assembly, or remove
  them.
- Bot edits are drafts. They do not modify a running or paused tournament and
  are validated and loaded only when **Start new tournament** is selected.
- Every filename or source edit is assembled immediately by radare2 Wasm. The
  editor shows a red or green result, the compiled size, and the complete byte
  array; results over the 512-byte warrior limit are marked red.
- Select **Choose warriors** to replace the draft roster with local `.asm`
  files. Dropping `.asm` files onto the page adds or updates bots by filename.
- Tournaments support run, pause, single-cycle stepping, bounded history,
  standings, and downloadable reports.
- Tournament execution stays in a worker so the interface remains responsive.

### Browser architecture support

The official radare2 6.2.0 WASI module contains x86, ARM, MIPS, RISC-V, and
Game Boy support. It does not contain the 8051 plugin, so 8051 warriors require
the native or Docker version for now.

See [wasm/README.md](wasm/README.md) for implementation details and direct npm
commands.

## Writing warriors

A tournament requires at least two non-empty `.asm` files. The architecture
and bitness are encoded before the `.asm` extension:

```text
scanner.x86-32.asm
hammer.arm-64.asm
probe.mips-32.asm
```

Supported filename markers are:

- `.x86-32.asm` and `.x86-64.asm`
- `.arm-16.asm`, `.arm-32.asm`, and `.arm-64.asm`
- `.mips-32.asm` and `.mips-64.asm`
- `.riscv-32.asm` and `.riscv-64.asm`
- `.gb.asm`
- `.8051.asm` in the native and Docker versions

Examples for several architectures are available in `examples/`. The default
native and Docker tournament reads warriors from `warriors/`.

## WebAssembly development commands

Run these from the repository root:

```sh
make wasm-test   # test parsers and the engine against the real radare2 Wasm
make wasm-build  # type-check and build wasm/dist/
make wasm-run    # start the Vite development server
make wasm-dist   # build and package wasm/r2wars-wasm.zip
```

The dedicated WebAssembly CI workflow runs the tests and production build, then
uploads `wasm/dist/` as the `r2wars-wasm` artifact.

## Background

The original proof of concept lives in
[radare2-extras](https://github.com/radare/radare2-extras/tree/master/r2wars).
This repository evolved the engine into a portable C# implementation by
SkUaTeR, replacing the original Windows-only MFC interface with a web UI.

Further introductions:

- [r2wars for N00bs — r2con 2020](https://www.youtube.com/watch?v=PB0AFBqFwGQ)
- [r2wars competition — r2con 2017](https://www.youtube.com/watch?v=sB-i5yUatx4)
- [Tournament library](https://github.com/otac0n/tournaments)

— pancake

/// <reference lib="webworker" />

import type { MainToWorker, WorkerToMain } from "./protocol";
import { architectureFromFilename } from "./architecture";
import { MAX_WARRIOR_SIZE } from "./engine";
import { RadareRuntime } from "./radare";
import { TournamentController } from "./tournament";

const context: DedicatedWorkerGlobalScope = self as unknown as DedicatedWorkerGlobalScope;
const runtime = new RadareRuntime();
let controller: TournamentController | null = null;

function post(message: WorkerToMain): void {
  context.postMessage(message);
}

context.onmessage = async (event: MessageEvent<MainToWorker>) => {
  const message = event.data;
  try {
    if (message.type === "init") {
      await runtime.load(message.wasmUrl);
      controller = new TournamentController(runtime, (state) => post({ type: "state", state }));
      post({ type: "ready" });
      return;
    }
    if (!controller) throw new Error("The WebAssembly engine is not ready yet");
    if (message.type === "assemble") {
      try {
        const architecture = architectureFromFilename(message.warrior.name.trim());
        const hex = runtime.assemble(architecture, message.warrior.source);
        const bytes = hex.match(/.{2}/g)?.map((byte) => Number.parseInt(byte, 16)) ?? [];
        const oversized = bytes.length > MAX_WARRIOR_SIZE;
        post({
          type: "assembly",
          requestId: message.requestId,
          ok: !oversized,
          bytes,
          size: bytes.length,
          message: oversized
            ? `Compiles, but exceeds the ${MAX_WARRIOR_SIZE}-byte warrior limit.`
            : "Compiles successfully.",
        });
      } catch (error) {
        post({
          type: "assembly",
          requestId: message.requestId,
          ok: false,
          bytes: [],
          size: 0,
          message: error instanceof Error ? error.message : String(error),
        });
      }
      return;
    }
    if (message.type === "load") controller.load(message.warriors);
    else if (message.type === "start") controller.start(message.warriors);
    else if (message.type === "command") controller.command(message.command);
  } catch (error) {
    if (controller) controller.fail(error);
    else post({ type: "error", message: error instanceof Error ? error.message : String(error) });
  }
};

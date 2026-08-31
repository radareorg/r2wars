/// <reference lib="webworker" />

import type { MainToWorker, WorkerToMain } from "./protocol";
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
    if (message.type === "load") controller.load(message.warriors);
    else if (message.type === "command") controller.command(message.command);
  } catch (error) {
    if (controller) controller.fail(error);
    else post({ type: "error", message: error instanceof Error ? error.message : String(error) });
  }
};

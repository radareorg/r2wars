import { emptyState, type AppState, type MainToWorker, type WarriorSource, type WorkerToMain } from "./protocol";

const worker = new Worker(new URL("./worker.ts", import.meta.url), { type: "module" });
let state = emptyState();
let ready = false;
let actionPending = false;

const element = <T extends HTMLElement>(id: string): T => {
  const value = document.getElementById(id);
  if (!value) throw new Error(`Missing UI element #${id}`);
  return value as T;
};

function send(message: MainToWorker): void {
  worker.postMessage(message);
}

function sendCommand(command: string): void {
  if (!ready) return;
  actionPending = true;
  send({ type: "command", command });
  syncControls();
}

function installLocalControls(): HTMLInputElement {
  document.title = "r2wars — WebAssembly arena";
  const description = document.createElement("meta");
  description.name = "description";
  description.content = "Run multi-architecture r2wars tournaments entirely in your browser with radare2 WebAssembly.";
  document.head.append(description);

  const style = document.createElement("style");
  style.textContent = `
    .connection.local { background:#d8e8ff; color:#174f85; }
    .drop-active::after { content:'Drop .asm warriors to load them'; position:fixed; inset:18px; z-index:10; display:grid; place-items:center; border:4px dashed springgreen; background:rgba(0,0,0,.88); color:springgreen; font:700 1.5rem monospace; }
    .wasm-note { color:#444; font-size:.72em; margin-right:auto; }
    #report_download { margin-left:8px; position:absolute; right:100px; top:24px; width:auto; padding:7px 18px; }
    @media (max-width: 900px), (orientation: portrait) {
      body { overflow:auto; margin:0; }
      .div1,.div2,.divM,.divM2,.divM3 { box-sizing:border-box; height:auto; left:auto; position:relative; right:auto; top:auto; width:100%; }
      .div1,.div2 { min-height:420px; }
      .divM { min-height:560px; overflow:auto; }
      .divM2 { bottom:auto; min-height:165px; }
      .divM3 { bottom:auto; height:260px; }
      .control-row { grid-template-columns:repeat(3,1fr); }
      .legend1,.legend2 { display:none; }
      .console2 { min-height:220px; }
    }
  `;
  document.head.append(style);

  const input = document.createElement("input");
  input.type = "file";
  input.accept = ".asm,text/plain";
  input.multiple = true;
  input.hidden = true;
  input.setAttribute("aria-label", "Choose warrior assembly files");
  document.body.append(input);

  const inspection = document.querySelector(".inspection-row");
  if (inspection) {
    const note = document.createElement("span");
    note.className = "wasm-note";
    note.textContent = "Local engine · drop .asm files anywhere";
    inspection.prepend(note);
  }

  const reportButton = document.createElement("button");
  reportButton.id = "report_download";
  reportButton.textContent = "Download report";
  reportButton.hidden = true;
  element("overlay").append(reportButton);
  reportButton.addEventListener("click", downloadReport);
  return input;
}

const fileInput = installLocalControls();

fileInput.addEventListener("change", async () => {
  if (fileInput.files?.length) await loadFiles(Array.from(fileInput.files));
  fileInput.value = "";
});

element("cmd_load").addEventListener("click", () => fileInput.click());
element("cmd_run").addEventListener("click", () => sendCommand(state.workflow === "running" ? "cmd_stop" : "cmd_run"));
element("cmd_step").addEventListener("click", () => sendCommand("cmd_step"));
element("cmd_reset").addEventListener("click", () => {
  const active = state.workflow === "running" || state.workflow === "paused";
  if (!active || window.confirm("Reset this tournament? Current progress and scores will be cleared.")) sendCommand("cmd_reset");
});
element("cmd_scores").addEventListener("click", showScores);
element("log_prev").addEventListener("click", () => sendCommand("cmd_prevlog"));
element("log_next").addEventListener("click", () => sendCommand("cmd_nextlog"));
element("score_close").addEventListener("click", hideScores);
element("nowarriorsclose").addEventListener("click", hideNoWarriorsDialog);
element("overlay").addEventListener("click", (event) => {
  if (event.target === element("overlay")) hideScores();
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    hideScores();
    hideNoWarriorsDialog();
  }
});

document.addEventListener("dragover", (event) => {
  event.preventDefault();
  document.body.classList.add("drop-active");
});
document.addEventListener("dragleave", (event) => {
  if (!event.relatedTarget) document.body.classList.remove("drop-active");
});
document.addEventListener("drop", async (event) => {
  event.preventDefault();
  document.body.classList.remove("drop-active");
  const files = Array.from(event.dataTransfer?.files ?? []).filter((file) => file.name.toLowerCase().endsWith(".asm"));
  if (files.length) await loadFiles(files);
});

worker.onmessage = async (event: MessageEvent<WorkerToMain>) => {
  const message = event.data;
  if (message.type === "ready") {
    ready = true;
    element("connection").textContent = "Local Wasm";
    element("connection").className = "connection local";
    await loadExamples();
  } else if (message.type === "state") {
    actionPending = false;
    state = message.state;
    updateUI();
  } else {
    showFatal(message.message);
  }
};

worker.onerror = (event) => showFatal(event.message || "The WebAssembly worker stopped unexpectedly");

async function loadFiles(files: File[]): Promise<void> {
  const warriors: WarriorSource[] = await Promise.all(files.map(async (file) => ({ name: file.name, source: await file.text() })));
  send({ type: "load", warriors });
}

async function loadExamples(): Promise<void> {
  try {
    const base = import.meta.env.BASE_URL;
    const manifestResponse = await fetch(`${base}warriors/manifest.json`);
    if (!manifestResponse.ok) throw new Error(`HTTP ${manifestResponse.status}`);
    const names = await manifestResponse.json() as string[];
    const warriors = await Promise.all(names.map(async (name) => {
      const response = await fetch(`${base}warriors/${encodeURIComponent(name)}`);
      if (!response.ok) throw new Error(`${name}: HTTP ${response.status}`);
      return { name, source: await response.text() };
    }));
    send({ type: "load", warriors });
  } catch (error) {
    state.workflow = "idle";
    state.message = `Choose at least two warrior files. Bundled examples could not be loaded: ${error instanceof Error ? error.message : error}`;
    updateUI();
  }
}

function showFatal(message: string): void {
  ready = false;
  actionPending = false;
  state.workflow = "error";
  state.message = message;
  state.status = "WebAssembly error";
  element("connection").textContent = "Engine error";
  element("connection").className = "connection disconnected";
  updateUI();
}

function syncControls(): void {
  const available = ready && !actionPending;
  const running = state.workflow === "running";
  const paused = state.workflow === "paused";
  const readyToRun = state.workflow === "ready";
  const finished = state.workflow === "finished";

  const load = element<HTMLButtonElement>("cmd_load");
  load.disabled = !ready || running;
  load.textContent = state.workflow === "loading" ? "Loading engine…" : "Choose warriors";
  load.title = "Choose two or more architecture-tagged .asm warrior files";

  const run = element<HTMLButtonElement>("cmd_run");
  run.disabled = !available || (!readyToRun && !paused && !running);
  run.textContent = running ? "Pause" : paused ? "Resume" : "Start tournament";
  run.className = running ? "primary pause" : "primary";
  element<HTMLButtonElement>("cmd_step").disabled = !available || !paused;
  element<HTMLButtonElement>("cmd_reset").disabled = !available || state.workflow === "idle" || state.workflow === "loading";
  element<HTMLButtonElement>("cmd_reset").textContent = finished ? "Play again" : "Reset";
  element<HTMLButtonElement>("cmd_scores").disabled = !ready || state.totalCombats === 0;
  element<HTMLButtonElement>("cmd_scores").textContent = finished ? "Final scores" : "Scores";
  element<HTMLButtonElement>("log_prev").disabled = !available || !state.canBrowseEarlier || (!paused && !finished);
  element<HTMLButtonElement>("log_next").disabled = !available || !state.canBrowseLater || (!paused && !finished);
  element<HTMLButtonElement>("report_download").hidden = !state.report;
}

function updateUI(): void {
  element("memory").innerHTML = memoryWidget();
  element("status").textContent = state.status || "";
  element("console").textContent = state.console || "";
  element("workflow_message").textContent = state.message || "";
  element("regs1").textContent = state.player1.regs || "";
  element("code1").innerHTML = state.player1.code || "";
  element("player1").textContent = state.player1.name || "Player 1";
  element("regs2").textContent = state.player2.regs || "";
  element("code2").innerHTML = state.player2.code || "";
  element("player2").textContent = state.player2.name || "Player 2";
  setActivePlayer(state.activePlayer);

  const labels: Record<string, string> = {
    loading: "LOADING ENGINE",
    idle: "NO TOURNAMENT",
    ready: "READY TO START",
    running: "RUNNING",
    paused: "PAUSED",
    finished: "TOURNAMENT OVER",
    error: "ENGINE ERROR",
  };
  const titles: Record<string, string> = {
    loading: "Starting the local engine",
    idle: "Choose warriors",
    ready: "Tournament ready",
    finished: "Tournament over",
    error: "The local engine stopped",
  };
  element("workflow_badge").textContent = labels[state.workflow] || state.workflow;
  element("workflow_badge").className = `workflow-badge ${state.workflow}`;
  element("progress").textContent = state.workflow === "idle" || state.workflow === "loading"
    ? "Runs entirely in this browser"
    : `${state.completedCombats} / ${state.totalCombats} battles${state.workflow === "finished" ? " complete" : ""}`;
  element("stage_title").textContent = titles[state.workflow] || "";
  element("stage_detail").textContent = state.message || "";
  const notice = element("stage_notice");
  notice.className = `stage-notice ${state.workflow}`;
  notice.hidden = state.workflow === "running" || state.workflow === "paused";
  syncControls();
}

function setActivePlayer(activePlayer: number): void {
  for (let index = 0; index < 2; index++) {
    const marker = element(`active${index + 1}`);
    const active = activePlayer === index;
    marker.classList.toggle("active", active);
    marker.setAttribute("aria-label", active ? `Player ${index + 1} has the active turn` : `Player ${index + 1} is waiting`);
  }
}

function memoryWidget(): string {
  let html = '<table class="memtable">';
  for (let row = 0; row < 32; row++) {
    let cells = "";
    for (let column = 0; column < 32; column++) {
      const value = state.memory[row * 32 + column] || "";
      const marker = value.length > 1 && /^[RWX]$/.test(value[1]) ? value[1] : "&nbsp;";
      cells += `<td style="background-color:${memoryColor(value[0])}" class="box">${marker}</td>`;
    }
    html += `<tr><td class="addr">0x${(row * 32).toString(16).padStart(4, "0")}</td>${cells}</tr>`;
  }
  return `${html}</table>`;
}

function memoryColor(value = ""): string {
  const colors: Record<string, string> = { r: "red", y: "yellow", g: "green", w: "white", n: "black", b: "blue", q: "aqua", o: "orange", v: "violet" };
  return colors[value] || "white";
}

function showScores(): void {
  element("score_title").textContent = state.workflow === "finished" ? "Final standings" : "Tournament scores";
  element("scores").textContent = state.scores || "No scores yet.";
  element<HTMLElement>("overlay").style.display = "block";
  element<HTMLButtonElement>("score_close").focus();
}

function hideScores(): void {
  element<HTMLElement>("overlay").style.display = "none";
}

function hideNoWarriorsDialog(): void {
  element<HTMLElement>("nowarriorsdlg").style.display = "none";
}

function downloadReport(): void {
  if (!state.report) return;
  const blob = new Blob([state.report], { type: "text/plain;charset=utf-8" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `${new Date().toISOString().replaceAll(":", "-").slice(0, 19)}.r2wars.txt`;
  link.click();
  setTimeout(() => URL.revokeObjectURL(link.href), 0);
}

state.memory = Array.from({ length: 1024 }, () => "");
updateUI();
const wasmUrl = new URL(`${import.meta.env.BASE_URL}radare2.wasm`, window.location.href).href;
send({ type: "init", wasmUrl });

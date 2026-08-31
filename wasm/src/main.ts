import { emptyState, type AppState, type MainToWorker, type WarriorSource, type WorkerToMain } from "./protocol";
import { cloneWarriors, mergeWarriors, newWarrior, validateWarriors } from "./bots";

const dotnetMode = import.meta.env.VITE_R2WARS_ENGINE === "dotnet"
  || document.documentElement.dataset.r2warsEngine === "dotnet";
let worker: Worker | null = null;
let socket: WebSocket | null = null;
let reconnectTimer = 0;
let initialBotsResolve: ((warriors: WarriorSource[]) => void) | null = null;
let initialBotsReject: ((error: Error) => void) | null = null;
let initialized = false;
let state = emptyState();
let ready = false;
let actionPending = false;
let bots: WarriorSource[] = [];
let selectedBot = -1;
let botsDirty = false;
let fileMode: "replace" | "merge" = "replace";
let latestAssemblyRequest = 0;

const element = <T extends HTMLElement>(id: string): T => {
  const value = document.getElementById(id);
  if (!value) throw new Error(`Missing UI element #${id}`);
  return value as T;
};

function send(message: MainToWorker): void {
  if (!dotnetMode) {
    worker?.postMessage(message);
    return;
  }
  if (!socket || socket.readyState !== WebSocket.OPEN) throw new Error("The .NET connection is not ready");
  socket.send(message.type === "command" ? message.command : JSON.stringify(message));
}

function sendCommand(command: string): void {
  if (!ready) return;
  actionPending = true;
  send({ type: "command", command });
  syncControls();
}

function installLocalControls(): HTMLInputElement {
  document.title = `r2wars — ${dotnetMode ? ".NET" : "WebAssembly"} arena`;
  const description = document.createElement("meta");
  description.name = "description";
  description.content = dotnetMode
    ? "Run multi-architecture r2wars tournaments with the native .NET and radare2 engine."
    : "Run multi-architecture r2wars tournaments entirely in your browser with radare2 WebAssembly.";
  document.head.append(description);

  const style = document.createElement("style");
  style.textContent = `
    .connection.local { background:#d8e8ff; color:#174f85; }
    .drop-active::after { content:'Drop .asm warriors to load them'; position:fixed; inset:18px; z-index:10; display:grid; place-items:center; border:4px dashed springgreen; background:rgba(0,0,0,.88); color:springgreen; font:700 1.5rem monospace; }
    .wasm-note { color:#444; font-size:.72em; margin-right:auto; }
    #report_download { margin-left:8px; position:absolute; right:100px; top:24px; width:auto; padding:7px 18px; }
    .control-row { grid-template-columns:repeat(3,1fr); }
    .bot-manager[hidden] { display:none; }
    .bot-manager { position:fixed; inset:2%; z-index:20; display:grid; place-items:center; background:rgba(0,0,0,.84); border:3px solid #999; border-radius:7px; }
    .bot-manager-panel { box-sizing:border-box; display:flex; flex-direction:column; width:min(1100px,96%); height:min(760px,94%); overflow:hidden; color:#111; background:#ddd; border:2px solid #eee; box-shadow:0 12px 42px #000; }
    .bot-manager-header { display:flex; align-items:center; justify-content:space-between; gap:16px; padding:10px 14px; color:white; background:linear-gradient(90deg,#123b8d,#8b1717); }
    .bot-manager-header h2 { margin:0; font-size:1.15rem; }
    .bot-manager-header span { font-size:.78rem; }
    .bot-manager-body { min-height:0; flex:1; display:grid; grid-template-columns:minmax(220px,30%) 1fr; gap:10px; padding:10px; }
    .bot-sidebar,.bot-editor { min-height:0; display:flex; flex-direction:column; gap:8px; }
    .bot-editor.no-selection > :not(.bot-editor-empty) { display:none; }
    .bot-list { min-height:120px; flex:1; overflow:auto; padding:4px; background:#171717; border:1px inset #aaa; }
    .bot-list button { margin-bottom:4px; padding:7px 8px; overflow:hidden; text-align:left; text-overflow:ellipsis; white-space:nowrap; color:#e8e8e8; background:#292929; border:1px solid #555; }
    .bot-list button.selected { color:#111; background:#9ed1ff; border-color:#3275aa; font-weight:bold; }
    .bot-sidebar-actions,.bot-manager-footer { display:flex; gap:8px; }
    .bot-sidebar-actions button,.bot-manager-footer button { width:auto; padding:7px 14px; }
    .bot-editor label { font-size:.8rem; font-weight:bold; }
    .bot-editor input,.bot-editor textarea { box-sizing:border-box; width:100%; color:#d8ffd8; background:#111; border:1px inset #aaa; font:14px/1.4 Consolas,'Courier New',monospace; }
    .bot-editor input { padding:7px; }
    .bot-editor textarea { min-height:180px; flex:1; padding:10px; resize:none; tab-size:2; }
    .bot-assembly { min-height:84px; max-height:150px; display:flex; flex-direction:column; gap:5px; overflow:auto; padding:7px; color:#ddd; background:#171717; border:1px inset #aaa; }
    .bot-assembly-summary { display:flex; align-items:center; gap:7px; font-size:.78rem; }
    .bot-assembly-summary strong { min-width:0; overflow-wrap:anywhere; }
    .bot-compile-indicator { width:11px; height:11px; flex:0 0 11px; border-radius:50%; background:#777; box-shadow:0 0 5px #555; }
    .bot-compile-indicator.pending { background:#e5b72f; box-shadow:0 0 6px #e5b72f; }
    .bot-compile-indicator.valid { background:#39e75f; box-shadow:0 0 6px #39e75f; }
    .bot-compile-indicator.invalid { background:#f04444; box-shadow:0 0 6px #f04444; }
    .bot-assembly-size { margin-left:auto; color:#9ed1ff; white-space:nowrap; }
    .bot-byte-array { min-height:38px; margin:0; overflow:auto; color:#b7e6ff; font:12px/1.35 Consolas,'Courier New',monospace; white-space:pre-wrap; word-break:break-all; }
    .bot-editor-empty { display:grid; flex:1; place-items:center; color:#555; border:1px dashed #999; }
    .bot-manager-footer { align-items:center; padding:9px 12px; border-top:1px solid #999; }
    .bot-manager-status { min-height:1.2em; flex:1; color:#444; font-size:.78rem; }
    .bot-manager-status.error { color:#8c1010; font-weight:bold; }
    #bot_start { background:#d8f5de; border-color:#318244; font-weight:bold; }
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
      .bot-manager { inset:0; border:0; }
      .bot-manager-panel { height:100%; width:100%; }
      .bot-manager-body { grid-template-columns:1fr; grid-template-rows:minmax(150px,32%) 1fr; }
      .bot-manager-header span { display:none; }
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
    note.textContent = `${dotnetMode ? "Native engine" : "Local Wasm"} · drop .asm files anywhere`;
    inspection.prepend(note);
  }

  const manageButton = document.createElement("button");
  manageButton.id = "cmd_bots";
  manageButton.textContent = "Bots";
  manageButton.title = "Add, remove, or edit bots for the next tournament";
  document.querySelector(".control-row")?.append(manageButton);

  const manager = document.createElement("div");
  manager.id = "bot_manager";
  manager.className = "bot-manager";
  manager.hidden = true;
  manager.setAttribute("role", "dialog");
  manager.setAttribute("aria-modal", "true");
  manager.setAttribute("aria-labelledby", "bot_manager_title");
  manager.innerHTML = `
    <div class="bot-manager-panel">
      <header class="bot-manager-header">
        <h2 id="bot_manager_title">Bot manager</h2>
        <span>Draft changes are loaded only when a new tournament starts</span>
      </header>
      <div class="bot-manager-body">
        <aside class="bot-sidebar">
          <strong id="bot_count">0 bots</strong>
          <div id="bot_list" class="bot-list" role="listbox" aria-label="Loaded bots"></div>
          <div class="bot-sidebar-actions">
            <button id="bot_add" type="button">New bot</button>
            <button id="bot_import" type="button">Import .asm</button>
          </div>
        </aside>
        <section class="bot-editor">
          <label for="bot_name">Filename and architecture</label>
          <input id="bot_name" type="text" autocomplete="off" spellcheck="false" placeholder="name.x86-32.asm">
          <label for="bot_source">Assembly source</label>
          <textarea id="bot_source" spellcheck="false" aria-label="Bot assembly source"></textarea>
          <div class="bot-assembly" aria-live="polite">
            <div class="bot-assembly-summary">
              <span id="bot_compile_indicator" class="bot-compile-indicator" aria-hidden="true"></span>
              <strong id="bot_compile_status">Not checked</strong>
              <span id="bot_assembly_size" class="bot-assembly-size">0 bytes</span>
            </div>
            <pre id="bot_byte_array" class="bot-byte-array">[]</pre>
          </div>
          <div class="bot-sidebar-actions">
            <button id="bot_delete" type="button">Delete selected bot</button>
          </div>
          <div id="bot_editor_empty" class="bot-editor-empty" hidden>Select a bot to edit its source.</div>
        </section>
      </div>
      <footer class="bot-manager-footer">
        <span id="bot_manager_status" class="bot-manager-status" role="status"></span>
        <button id="bot_close" type="button">Close</button>
        <button id="bot_start" type="button">Start new tournament</button>
      </footer>
    </div>`;
  document.body.append(manager);

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
  if (fileInput.files?.length) {
    const imported = await readFiles(Array.from(fileInput.files));
    bots = fileMode === "replace" ? imported : mergeWarriors(bots, imported);
    const lastImported = imported.at(-1)?.name.toLowerCase();
    selectedBot = lastImported ? bots.findIndex((bot) => bot.name.toLowerCase() === lastImported) : -1;
    botsDirty = true;
    showBotManager();
  }
  fileInput.value = "";
});

element("cmd_load").addEventListener("click", () => {
  fileMode = "replace";
  fileInput.click();
});
element("cmd_bots").addEventListener("click", showBotManager);
element("cmd_run").addEventListener("click", () => {
  if (state.workflow === "running") sendCommand("cmd_stop");
  else if (state.workflow === "paused") sendCommand("cmd_run");
  else startNewTournament();
});
element("cmd_step").addEventListener("click", () => sendCommand("cmd_step"));
element("cmd_reset").addEventListener("click", () => {
  const active = state.workflow === "running" || state.workflow === "paused";
  if (!active || window.confirm("Reset this tournament? Current progress and scores will be cleared.")) prepareNewTournament();
});
element("cmd_scores").addEventListener("click", showScores);
element("log_prev").addEventListener("click", () => sendCommand("cmd_prevlog"));
element("log_next").addEventListener("click", () => sendCommand("cmd_nextlog"));
element("score_close").addEventListener("click", hideScores);
element("nowarriorsclose").addEventListener("click", hideNoWarriorsDialog);
element("bot_add").addEventListener("click", addBot);
element("bot_import").addEventListener("click", () => {
  fileMode = "merge";
  fileInput.click();
});
element("bot_delete").addEventListener("click", deleteSelectedBot);
element("bot_close").addEventListener("click", hideBotManager);
element("bot_start").addEventListener("click", startNewTournament);
element<HTMLInputElement>("bot_name").addEventListener("input", (event) => {
  if (selectedBot < 0) return;
  bots[selectedBot].name = (event.currentTarget as HTMLInputElement).value;
  botsDirty = true;
  renderBotList();
  updateBotManagerStatus();
  syncControls();
  requestBotAssembly();
});
element<HTMLTextAreaElement>("bot_source").addEventListener("input", (event) => {
  if (selectedBot < 0) return;
  bots[selectedBot].source = (event.currentTarget as HTMLTextAreaElement).value;
  botsDirty = true;
  updateBotManagerStatus();
  requestBotAssembly();
});
element("overlay").addEventListener("click", (event) => {
  if (event.target === element("overlay")) hideScores();
});
element("bot_manager").addEventListener("click", (event) => {
  if (event.target === element("bot_manager")) hideBotManager();
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    if (!element<HTMLDivElement>("bot_manager").hidden) hideBotManager();
    else {
      hideScores();
      hideNoWarriorsDialog();
    }
  }
});

document.addEventListener("dragover", (event) => {
  event.preventDefault();
  if (state.workflow !== "running") document.body.classList.add("drop-active");
});
document.addEventListener("dragleave", (event) => {
  if (!event.relatedTarget) document.body.classList.remove("drop-active");
});
document.addEventListener("drop", async (event) => {
  event.preventDefault();
  document.body.classList.remove("drop-active");
  if (state.workflow === "running") return;
  const files = Array.from(event.dataTransfer?.files ?? []).filter((file) => file.name.toLowerCase().endsWith(".asm"));
  if (files.length) {
    const imported = await readFiles(files);
    bots = mergeWarriors(bots, imported);
    const lastImported = imported.at(-1)?.name.toLowerCase();
    selectedBot = lastImported ? bots.findIndex((bot) => bot.name.toLowerCase() === lastImported) : -1;
    botsDirty = true;
    showBotManager();
  }
});

async function handleTransportMessage(message: WorkerToMain): Promise<void> {
  if (message.type === "ready") {
    ready = true;
    element("connection").textContent = dotnetMode ? ".NET server" : "Local Wasm";
    element("connection").className = "connection local";
    if (!initialized) {
      initialized = true;
      await loadExamples();
    } else if (dotnetMode) {
      send({ type: "command", command: "cmd_state" });
    }
  } else if (message.type === "bots") {
    initialBotsResolve?.(message.warriors);
    initialBotsResolve = null;
    initialBotsReject = null;
  } else if (message.type === "assembly") {
    showAssemblyResult(message);
  } else if (message.type === "state") {
    actionPending = false;
    state = mergeAppState(state, message.state);
    updateUI();
  } else {
    if (message.recoverable) {
      actionPending = false;
      initialBotsReject?.(new Error(message.message));
      initialBotsResolve = null;
      initialBotsReject = null;
      if (initialized) {
        showBotManager();
        updateBotManagerStatus(message.message);
      }
      syncControls();
    } else {
      showFatal(message.message);
    }
  }
}

function mergeAppState(current: AppState, update: Partial<AppState>): AppState {
  return {
    ...current,
    ...update,
    player1: { ...current.player1, ...update.player1 },
    player2: { ...current.player2, ...update.player2 },
    memory: update.memory ?? current.memory,
  };
}

async function readFiles(files: File[]): Promise<WarriorSource[]> {
  return Promise.all(files.map(async (file) => ({ name: file.name, source: await file.text() })));
}

async function loadExamples(): Promise<void> {
  try {
    const warriors = dotnetMode ? await requestDotnetBots() : await fetchBundledBots();
    bots = cloneWarriors(warriors);
    selectedBot = bots.length ? 0 : -1;
    botsDirty = false;
    send({ type: "load", warriors: cloneWarriors(bots) });
  } catch (error) {
    state.workflow = "idle";
    state.message = `Choose at least two warrior files. Bundled examples could not be loaded: ${error instanceof Error ? error.message : error}`;
    updateUI();
  }
}

async function fetchBundledBots(): Promise<WarriorSource[]> {
  const base = import.meta.env.BASE_URL;
  const manifestResponse = await fetch(`${base}warriors/manifest.json`);
  if (!manifestResponse.ok) throw new Error(`HTTP ${manifestResponse.status}`);
  const names = await manifestResponse.json() as string[];
  return Promise.all(names.map(async (name) => {
    const response = await fetch(`${base}warriors/${encodeURIComponent(name)}`);
    if (!response.ok) throw new Error(`${name}: HTTP ${response.status}`);
    return { name, source: await response.text() };
  }));
}

function requestDotnetBots(): Promise<WarriorSource[]> {
  return new Promise((resolve, reject) => {
    initialBotsResolve = resolve;
    initialBotsReject = reject;
    socket?.send(JSON.stringify({ type: "bots" }));
    window.setTimeout(() => {
      if (initialBotsResolve !== resolve) return;
      initialBotsResolve = null;
      initialBotsReject = null;
      reject(new Error("Timed out while loading bots from the .NET server"));
    }, 5000);
  });
}

function preparedBots(): WarriorSource[] {
  return bots.map((bot) => ({ name: bot.name.trim(), source: bot.source }));
}

function startNewTournament(): void {
  try {
    const warriors = preparedBots();
    validateWarriors(warriors);
    bots = cloneWarriors(warriors);
    botsDirty = false;
    actionPending = true;
    hideBotManager();
    send({ type: "start", warriors: cloneWarriors(bots) });
    syncControls();
  } catch (error) {
    showBotManager();
    updateBotManagerStatus(error instanceof Error ? error.message : String(error));
  }
}

function prepareNewTournament(): void {
  try {
    const warriors = preparedBots();
    validateWarriors(warriors);
    bots = cloneWarriors(warriors);
    botsDirty = false;
    actionPending = true;
    send({ type: "load", warriors: cloneWarriors(bots) });
    syncControls();
  } catch (error) {
    showBotManager();
    updateBotManagerStatus(error instanceof Error ? error.message : String(error));
  }
}

function showBotManager(): void {
  if (!ready || state.workflow === "running") return;
  const manager = element<HTMLDivElement>("bot_manager");
  manager.hidden = false;
  renderBotManager();
  if (selectedBot >= 0) element<HTMLInputElement>("bot_name").focus();
  else element<HTMLButtonElement>("bot_add").focus();
}

function hideBotManager(): void {
  element<HTMLDivElement>("bot_manager").hidden = true;
  element<HTMLButtonElement>("cmd_bots").focus({ preventScroll: true });
}

function addBot(): void {
  bots.push(newWarrior(bots));
  selectedBot = bots.length - 1;
  botsDirty = true;
  renderBotManager();
  const name = element<HTMLInputElement>("bot_name");
  name.focus();
  name.select();
}

function deleteSelectedBot(): void {
  if (selectedBot < 0) return;
  const name = bots[selectedBot].name || "this bot";
  if (!window.confirm(`Delete ${name} from the next tournament?`)) return;
  bots.splice(selectedBot, 1);
  selectedBot = bots.length ? Math.min(selectedBot, bots.length - 1) : -1;
  botsDirty = true;
  renderBotManager();
}

function selectBot(index: number): void {
  selectedBot = index;
  renderBotManager();
  element<HTMLTextAreaElement>("bot_source").focus();
}

function renderBotList(): void {
  const list = element<HTMLDivElement>("bot_list");
  list.replaceChildren();
  bots.forEach((bot, index) => {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = bot.name || "Unnamed bot";
    button.title = bot.name || "Unnamed bot";
    button.classList.toggle("selected", index === selectedBot);
    button.setAttribute("role", "option");
    button.setAttribute("aria-selected", String(index === selectedBot));
    button.addEventListener("click", () => selectBot(index));
    list.append(button);
  });
  element("bot_count").textContent = `${bots.length} bot${bots.length === 1 ? "" : "s"}`;
  element<HTMLButtonElement>("cmd_bots").textContent = `Bots (${bots.length})`;
}

function renderBotManager(): void {
  if (selectedBot >= bots.length) selectedBot = bots.length - 1;
  renderBotList();
  const selected = selectedBot >= 0 ? bots[selectedBot] : undefined;
  const editor = document.querySelector<HTMLElement>(".bot-editor");
  editor?.classList.toggle("no-selection", !selected);
  element<HTMLDivElement>("bot_editor_empty").hidden = Boolean(selected);
  const name = element<HTMLInputElement>("bot_name");
  const source = element<HTMLTextAreaElement>("bot_source");
  name.value = selected?.name ?? "";
  source.value = selected?.source ?? "";
  name.disabled = !selected;
  source.disabled = !selected;
  element<HTMLButtonElement>("bot_delete").disabled = !selected;
  element<HTMLButtonElement>("bot_start").disabled = bots.length < 2 || actionPending;
  updateBotManagerStatus();
  requestBotAssembly();
}

function updateBotManagerStatus(error = ""): void {
  const status = element("bot_manager_status");
  status.classList.toggle("error", Boolean(error));
  status.textContent = error || (botsDirty
    ? "Draft changed — the active tournament is untouched."
    : "This roster matches the most recently loaded tournament.");
}

function requestBotAssembly(): void {
  const requestId = ++latestAssemblyRequest;
  const selected = selectedBot >= 0 ? bots[selectedBot] : undefined;
  const indicator = element("bot_compile_indicator");
  indicator.className = `bot-compile-indicator${selected ? " pending" : ""}`;
  element("bot_compile_status").textContent = selected ? "Checking…" : "Not checked";
  element("bot_assembly_size").textContent = "0 bytes";
  element("bot_byte_array").textContent = "[]";
  if (!selected || !ready) return;
  send({ type: "assemble", requestId, warrior: { ...selected } });
}

function showAssemblyResult(result: Extract<WorkerToMain, { type: "assembly" }>): void {
  if (result.requestId !== latestAssemblyRequest) return;
  const indicator = element("bot_compile_indicator");
  indicator.className = `bot-compile-indicator ${result.ok ? "valid" : "invalid"}`;
  element("bot_compile_status").textContent = result.message;
  element("bot_assembly_size").textContent = `${result.size} byte${result.size === 1 ? "" : "s"}`;
  element("bot_byte_array").textContent = result.bytes.length
    ? `[${result.bytes.map((byte) => `0x${byte.toString(16).padStart(2, "0")}`).join(", ")}]`
    : "[]";
}

function showFatal(message: string): void {
  ready = false;
  actionPending = false;
  state.workflow = "error";
  state.message = message;
  state.status = dotnetMode ? ".NET engine error" : "WebAssembly error";
  element("connection").textContent = "Engine error";
  element("connection").className = "connection disconnected";
  updateUI();
}

function connectTransport(): void {
  if (dotnetMode) {
    connectDotnet();
    return;
  }
  worker = new Worker(new URL("./worker.ts", import.meta.url), { type: "module" });
  worker.onmessage = (event: MessageEvent<WorkerToMain>) => void handleTransportMessage(event.data);
  worker.onerror = (event) => showFatal(event.message || "The WebAssembly worker stopped unexpectedly");
  const wasmUrl = new URL(`${import.meta.env.BASE_URL}radare2.wasm`, window.location.href).href;
  worker.postMessage({ type: "init", wasmUrl } satisfies MainToWorker);
}

function connectDotnet(): void {
  window.clearTimeout(reconnectTimer);
  const scheme = window.location.protocol === "https:" ? "wss" : "ws";
  const url = window.R2WARS_WS_URL || `${scheme}://${window.location.hostname}:9966/r2wars`;
  socket = new WebSocket(url);
  element("connection").textContent = "Connecting…";
  element("connection").className = "connection connecting";
  socket.onopen = () => void handleTransportMessage({ type: "ready" });
  socket.onmessage = (event) => handleDotnetPayload(String(event.data));
  socket.onerror = () => {
    element("connection").textContent = "Connection error";
    element("connection").className = "connection disconnected";
  };
  socket.onclose = () => {
    ready = false;
    actionPending = false;
    initialBotsReject?.(new Error("The .NET connection closed while loading bots"));
    initialBotsResolve = null;
    initialBotsReject = null;
    element("connection").textContent = "Disconnected — reconnecting…";
    element("connection").className = "connection disconnected";
    syncControls();
    reconnectTimer = window.setTimeout(connectDotnet, 2000);
  };
}

function handleDotnetPayload(payload: string): void {
  if (payload === "none") return;
  if (payload === "on") {
    send({ type: "command", command: "moreflow" });
    return;
  }
  if (payload === "nowarriors") {
    initialBotsResolve?.([]);
    initialBotsResolve = null;
    initialBotsReject = null;
    return;
  }
  try {
    const parsed = JSON.parse(payload) as Record<string, unknown>;
    if (parsed.type === "bots" || parsed.type === "assembly" || parsed.type === "error") {
      void handleTransportMessage(parsed as unknown as WorkerToMain);
    } else {
      void handleTransportMessage({ type: "state", state: parsed as Partial<AppState> });
    }
  } catch {
    showFatal("The .NET server sent an invalid update");
  }
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
  const manage = element<HTMLButtonElement>("cmd_bots");
  manage.disabled = !available || running;
  manage.textContent = `Bots (${bots.length})`;
  element<HTMLButtonElement>("log_prev").disabled = !available || !state.canBrowseEarlier || (!paused && !finished);
  element<HTMLButtonElement>("log_next").disabled = !available || !state.canBrowseLater || (!paused && !finished);
  element<HTMLButtonElement>("report_download").hidden = !state.report;
  if (running) element<HTMLDivElement>("bot_manager").hidden = true;
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
    ? (dotnetMode ? "Connected to the native engine" : "Runs entirely in this browser")
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
connectTransport();

declare global {
  interface Window {
    R2WARS_WS_URL?: string;
  }
}

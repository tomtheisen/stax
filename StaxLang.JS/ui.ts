import { Runtime, ExecutionState } from './stax';
import { pendWork } from './timeoutzero';
import { compress } from './huffmancompression';
import { isPacked, unpack, pack } from './packer';

// duration to run stax program before yielding to ui and pumping messages
const workMilliseconds = 20;

const runButton = document.getElementById("run") as HTMLButtonElement;
const stepButton = document.getElementById("step") as HTMLButtonElement;
const stopButton = document.getElementById("stop") as HTMLButtonElement;
const codeArea = document.getElementById("code") as HTMLTextAreaElement;
const inputArea = document.getElementById("stdin") as HTMLTextAreaElement;
const statusEl = document.getElementById("status") as HTMLElement;
const propsEl = document.getElementById("properties") as HTMLElement;
const outputEl = document.getElementById("output") as HTMLPreElement;
const saveLink = document.getElementById("savelink") as HTMLAnchorElement;
const packButton = document.getElementById("pack") as HTMLButtonElement;
const compressorInputEl = document.getElementById("compressorInput") as HTMLInputElement;
const compressorOutputEl = document.getElementById("compressorOutput") as HTMLInputElement;
const debugContainer = document.getElementById("debugState") as HTMLElement;
const autoCheckEl = document.getElementById("autoRunPermalink") as HTMLInputElement;

let activeRuntime: Runtime | null = null;
let activeStateIterator: Iterator<ExecutionState> | null = null;
let steps = 0, start = 0;
let pendingBreak = false;

// prepare for new run
function resetRuntime() {
    steps = 0;
    start = performance.now();
    outputEl.textContent = "";
    pendingBreak = false;

    packButton.disabled = codeArea.disabled = inputArea.disabled = true;
    debugContainer.hidden = true;

    let code = codeArea.value, stdin = inputArea.value.split(/\r?\n/);
    activeRuntime = new Runtime(line => outputEl.textContent += line + "\n");
    activeStateIterator = activeRuntime.runProgram(code, stdin);
}

// mark program finished
function cleanupRuntime() {
    activeRuntime = activeStateIterator = null;
    packButton.disabled = codeArea.disabled = inputArea.disabled = false;
    stopButton.disabled = debugContainer.hidden = true;
}

function stop() {
    cleanupRuntime();
    statusEl.textContent = "Stopped";
}
stopButton.addEventListener("click", stop);

function isActive() {
    return !!activeRuntime;
}

function runProgramTimeSlice() {
    if (!activeStateIterator || pendingBreak) {
        pendingBreak = false;
        return;
    }

    let result: IteratorResult<ExecutionState>, sliceStart = performance.now();
    try {
        while (!(result = activeStateIterator.next()).done) {
            steps += 1;
            if(performance.now() - sliceStart > workMilliseconds) break;
        }
        if (result.done) cleanupRuntime();
        else pendWork(runProgramTimeSlice);
    }
    catch (e) {
        if (e instanceof Error) outputEl.textContent += "\nStax runtime error: " + e.message;
        cleanupRuntime();
        return;
    }
    
    let elapsed = (performance.now() - start) / 1000;
    statusEl.textContent = `${ steps } steps, ${ elapsed.toFixed(2) }s`;
}

function run() {
    pendingBreak = false;
    if (!isActive()) resetRuntime();
    runProgramTimeSlice();
}

runButton.addEventListener("click", run);

// gets instruction pointer if still running
function step() : number | null {
    if (!isActive()) resetRuntime();
    let result = activeStateIterator!.next();
    if (result.done) {
        cleanupRuntime();
        statusEl.textContent = `${ steps } steps, complete`;
        return null;
    }
    else {
        pendingBreak = true;
        steps += 1;
        stopButton.disabled = false;
        showDebugInfo(result.value.ip);
        statusEl.textContent = `${ steps } steps, paused`;
        return result.value.ip;
    }
}

stepButton.addEventListener("click", step);

function showDebugInfo(ip: number) {
    if (!activeRuntime) return;
    debugContainer.hidden = false;

    const debugPreEl = document.getElementById("debugCodePre")!,
        debugPostEl = document.getElementById("debugCodePost")!;
    let code = codeArea.value;
    if (isPacked(code)) {
        debugPreEl.textContent = debugPostEl.textContent = "";
    }
    else {
        debugPreEl.textContent = code.substr(0, ip);
        debugPostEl.textContent = code.substr(ip);
    }

    let state = activeRuntime.getDebugState();
    document.getElementById("watchX")!.textContent = state.x;
    document.getElementById("watchY")!.textContent = state.y;
    document.getElementById("watchi")!.textContent = state.index.toString();
    document.getElementById("watch_")!.textContent = state._;
    
    const watchMainEl = document.getElementById("watchMain") as HTMLOListElement;
    watchMainEl.innerText = "";
    state.main.forEach(e => {
        let li = document.createElement("li");
        li.textContent = e;
        watchMainEl.appendChild(li);
    });
    
    const watchInputEl = document.getElementById("watchInput") as HTMLOListElement;
    watchInputEl.innerText = "";
    state.input.forEach(e => {
        let li = document.createElement("li");
        li.textContent = e;
        watchInputEl.appendChild(li);
    });

}

function load() {
    let params = new URLSearchParams(location.hash.substr(1));
    if (params.has('c')) codeArea.value = params.get('c')!;
    if (params.has('i')) inputArea.value = params.get('i')!;
    if (params.get('a')) {
        autoCheckEl.checked = true;
        run();
    }
}
load();

function updateStats() {
    let params = new URLSearchParams;
    params.set('c', codeArea.value);
    params.set('i', inputArea.value);
    if (autoCheckEl.checked) params.set('a', '1');
    saveLink.href = '#' + params.toString();

    let packed = isPacked(codeArea.value);
    let type = packed ? "packed" : "ascii";

    let size = codeArea.value.length;
    propsEl.textContent = `${ size } bytes, ${ type }`;

    packButton.textContent = packed ? "Unpack" : "Pack";
}
updateStats();

autoCheckEl.addEventListener("change", updateStats);

let statsTimeout: number | null = null;
function pendUpdateStats() {
    if (statsTimeout) clearTimeout(statsTimeout);
    statsTimeout = window.setTimeout(updateStats, 100);
}

codeArea.addEventListener("input", pendUpdateStats);
inputArea.addEventListener("input", pendUpdateStats);

function doCompressor() {
    let input = compressorInputEl.value;
    let result: string;
    if (input === "") {
        result = "z";
    }
    else if (input.length === 1) {
        result = "'" + input;
    }
    else if (input.length === 2) {
        result = "." + input;
    }
    else {
        let compressed = compress(input);
        if (compressed) result = '`' + compressed + '`';
        else result = '"' + input.replace('"', '`"') + '"'
    }
    compressorOutputEl.value = result;
}
doCompressor();
compressorInputEl.addEventListener("input", doCompressor);

const compressorDialog = document.getElementById("compressorDialog") as HTMLDialogElement;
document.getElementById("compressorOpen")!.addEventListener("click", () => {
    compressorDialog.showModal();
});
document.getElementById("compressorClose")!.addEventListener("click", () => {
    compressorDialog.close();
})

packButton.addEventListener("click", () => {
    let code = codeArea.value, packed = isPacked(code);
    codeArea.value = packed ? unpack(code) : pack(code);
    updateStats();
});

function setVersion() {
    let out: string;
    let rt = new Runtime(o => { out = o; });
    for (let s of rt.runProgram("V?", [])) ;
    let version = out!.match(/[0-9.]+/)![0]
    document.getElementById("version")!.textContent = `v${ version }`;
}
setVersion();

document.addEventListener("keydown", ev => {
    switch (ev.key) {
        case "F8":
            run();
            ev.preventDefault();
            break;
        case "F11":
            step();
            ev.preventDefault();
            break;
        case "Escape":
            if (isActive()) stop();
            ev.preventDefault();
            break;
    }
});

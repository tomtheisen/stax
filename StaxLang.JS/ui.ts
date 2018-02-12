import { Runtime, ExecutionState } from './stax';
import { pendWork } from './timeoutzero';
import { compress } from './huffmancompression';
import { isPacked, unpack, pack } from './packer';
import 'url-search-params-polyfill';

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
const multiInputEl = document.getElementById("multiInput") as HTMLInputElement;

let activeRuntime: Runtime | null = null;
let activeStateIterator: Iterator<ExecutionState> | null = null;
let steps = 0, start = 0;
let pendingBreak = false;
let pendingInputs: string[] = [];

// prepare for new run
function resetRuntime() {
    steps = 0;
    start = performance.now();
    outputEl.textContent = "";
    pendingBreak = false;

    packButton.disabled = codeArea.disabled = inputArea.disabled = true;
    debugContainer.hidden = true;
    stopButton.disabled = false;

    if (multiInputEl.checked) pendingInputs = inputArea.value.split(/(?:\r?\n){2,}/);
    else pendingInputs = [inputArea.value];
    startNextInput();
}

// begin the next test case, or clean up if done
function startNextInput() {
    if (pendingInputs.length === 0) {
        cleanupRuntime();
        return;
    }

    let code = codeArea.value, stdin = pendingInputs.shift()!.split(/\r?\n/);
    activeRuntime = new Runtime(line => outputEl.textContent += line + "\n");
    activeStateIterator = activeRuntime.runProgram(code, stdin);
    steps = 0;
    if (outputEl.textContent) outputEl.textContent += "\n";
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

    debugContainer.hidden = true;

    let result: IteratorResult<ExecutionState>, sliceStart = performance.now();
    try {
        while (!(result = activeStateIterator.next()).done) {
            steps += 1;
            if (result.value.break) {
                showDebugInfo(result.value.ip, steps);
                return;
            }
            if(performance.now() - sliceStart > workMilliseconds) break;
        }
        if (result.done) startNextInput();
        pendWork(runProgramTimeSlice);
    }
    catch (e) {
        if (e instanceof Error) outputEl.textContent += "\nStax runtime error: " + e.message;
        startNextInput();
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
        startNextInput();
        statusEl.textContent = `${ steps } steps, complete`;
        return null;
    }
    else {
        pendingBreak = true;
        showDebugInfo(result.value.ip, ++steps);
        return result.value.ip;
    }
}

stepButton.addEventListener("click", step);

function showDebugInfo(ip: number, steps: number) {
    if (!activeRuntime) return;
    debugContainer.hidden = false;

    stopButton.disabled = false;
    statusEl.textContent = `${ steps } steps, paused`;

    const debugPreEl = document.getElementById("debugCodePre")!,
        debugPostEl = document.getElementById("debugCodePost")!;
    let code = codeArea.value;
    if (isPacked(code)) code = unpack(code);
    debugPreEl.textContent = code.substr(0, ip);
    debugPostEl.textContent = code.substr(ip);

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

function sizeTextArea(el: HTMLTextAreaElement) {
    el.rows = Math.max(el.rows, 2, el.value.split("\n").length);
}

function load() {
    let params = new URLSearchParams(location.hash.substr(1));
    if (params.has('c')) {
        codeArea.value = params.get('c')!;
        sizeTextArea(codeArea);
    }
    if (params.has('i')) {
        inputArea.value = params.get('i')!;
        sizeTextArea(inputArea);
    }
    if (params.has('m')) multiInputEl.checked = true;
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
    if (multiInputEl.checked) params.set('m', '1');
    saveLink.href = '#' + params.toString();

    let packed = isPacked(codeArea.value);
    let type = packed ? "packed" : "ascii";

    let size = codeArea.value.length;
    propsEl.textContent = `${ size } bytes, ${ type }`;

    packButton.textContent = packed ? "Unpack" : "Pack";

    let packable = !packed && !!codeArea.value.match(/^[ -~]+$/);
    packButton.disabled = !packed && !packable;
}
updateStats();

autoCheckEl.addEventListener("change", updateStats);

let statsTimeout: number | null = null;
function pendUpdate() {
    const el = this instanceof HTMLTextAreaElement ? this : null;
    if (el) sizeTextArea(el);
    if (statsTimeout) clearTimeout(statsTimeout);
    statsTimeout = window.setTimeout(updateStats, 100);
}

codeArea.addEventListener("input", pendUpdate);
inputArea.addEventListener("input", pendUpdate);

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

const compressorDialog = document.getElementById("compressorDialog") as HTMLDivElement;
document.getElementById("compressorOpen")!.addEventListener("click", () => {
    compressorDialog.hidden = !compressorDialog.hidden;
});

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

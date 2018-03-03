import { Runtime, ExecutionState } from './stax';
import { pendWork } from './timeoutzero';
import { compress } from './huffmancompression';
import { isPacked, unpack, pack } from './packer';
import 'url-search-params-polyfill';

declare var __COMMIT_HASH__: string;
declare var __BUILD_DATE__: string;
document.getElementById("buildInfo")!.textContent = `${__COMMIT_HASH__} built ${__BUILD_DATE__.replace(/:\d{2}\.\d{3}Z/, "Z")}`;

// duration to run stax program before yielding to ui and pumping messages
const workMilliseconds = 40;

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
const blankSplitEl = document.getElementById("blankSplit") as HTMLInputElement;
const lineSplitEl = document.getElementById("lineSplit") as HTMLInputElement;
const noSplitEl = document.getElementById("noSplit") as HTMLInputElement;

let activeRuntime: Runtime | null = null;
let activeStateIterator: Iterator<ExecutionState> | null = null;
let steps = 0, start = 0, input = 0;
let pendingBreak = false;
let pendingInputs: string[] = [];

// prepare for new run
function resetRuntime() {
    input = steps = 0;
    start = performance.now();
    outputEl.textContent = "";
    pendingBreak = false;

    packButton.disabled = codeArea.disabled = inputArea.disabled = true;
    debugContainer.hidden = true;
    stopButton.disabled = false;

    if (blankSplitEl.checked) pendingInputs = inputArea.value.split(/(?:\r?\n){2,}/);
    else if (lineSplitEl.checked) pendingInputs = inputArea.value.split(/\r?\n/);
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
    if (input++) outputEl.textContent += "\n";
}

// mark program finished
function cleanupRuntime() {
    activeRuntime = activeStateIterator = null;
    codeArea.disabled = inputArea.disabled = false;
    stopButton.disabled = debugContainer.hidden = true;
    updateStats();
}

function stop() {
    cleanupRuntime();
    statusEl.textContent = "Stopped";
}
stopButton.addEventListener("click", stop);

function isActive() {
    return !!activeRuntime;
}

function showError(err: Error) {
    outputEl.textContent += "Stax runtime error: " + err.message + "\n";
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
        if (e instanceof Error) showError(e);
        startNextInput();
        pendWork(runProgramTimeSlice);
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
    function caseComplete() {
        startNextInput();
        debugContainer.hidden = true;
        if (isActive()) statusEl.textContent = `${ steps } steps, program ended`
        else statusEl.textContent = `${ steps } steps, complete`;
    }

    if (!isActive()) resetRuntime();
    try {
        let result = activeStateIterator!.next();
        if (result.done) {
            caseComplete();
            return null;
        }
        else {
            pendingBreak = true;
            showDebugInfo(result.value.ip, ++steps);
            return result.value.ip;
        }
    }
    catch (e) {
        if (e instanceof Error) showError(e);
        caseComplete();
        return null;
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

function updateStats() {
    let params = new URLSearchParams;
    params.set('c', codeArea.value);
    params.set('i', inputArea.value);
    if (autoCheckEl.checked) params.set('a', '1');
    if (blankSplitEl.checked) params.set('m', '1');
    if (lineSplitEl.checked) params.set('m', '2');
    saveLink.href = '#' + params.toString();

    packButton.disabled = false;
    if (isPacked(codeArea.value)) {
        propsEl.textContent = `${ codeArea.value.length } bytes, packed`;
        packButton.textContent = "Unpack";
    }
    else {
        packButton.textContent = "Pack";
        let unknown = false;
        let pairs = 0;
        for (let i = 0; i < codeArea.value.length; i++) {
            let charCode = codeArea.value.charCodeAt(i);
            let codePoint = codeArea.value.codePointAt(i);
            if (charCode !== codePoint) pairs += 1;
            if (charCode < 32 || charCode > 127) {
                packButton.disabled = true;
                unknown = unknown || (charCode !== 9 && charCode !== 10 && charCode !== 13); 
            }
        }

        if (unknown) propsEl.textContent = `${ codeArea.value.length - pairs } characters`
        else propsEl.textContent = `${ codeArea.value.length } bytes, ascii`;
    }
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
    switch (params.get('m')) {
        case '1':
            blankSplitEl.checked = true;
            break;
        case '2':
            lineSplitEl.checked = true;
            break;
        default: 
            noSplitEl.checked = true;
            break;
    }

    updateStats();

    if (params.get('a')) {
        autoCheckEl.checked = true;
        debugger;
        run();
    }
}
load();

autoCheckEl.addEventListener("change", updateStats);
lineSplitEl.addEventListener("change", updateStats);
blankSplitEl.addEventListener("change", updateStats);
noSplitEl.addEventListener("change", updateStats);

let statsTimeout: number | null = null;
function pendUpdate() {
    const el = this instanceof HTMLTextAreaElement ? this : null;
    if (el) sizeTextArea(el);
    if (statsTimeout) clearTimeout(statsTimeout);
    statsTimeout = window.setTimeout(updateStats, 100);
}

codeArea.addEventListener("input", pendUpdate);
codeArea.addEventListener("keydown", ev => {
    if (ev.key === "i" && ev.ctrlKey) {
        ev.preventDefault();
        let s = codeArea.selectionStart;
        codeArea.value = codeArea.value.substr(0, s)
            + "\t" + codeArea.value.substr(s);
        codeArea.selectionEnd = s + 1;
        pendUpdate();
    }
});
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
            ev.preventDefault();
            run();
            break;
        case "F11":
            ev.preventDefault();
            step();
            break;
        case "Escape":
            ev.preventDefault();
            if (isActive()) stop();
            break;
    }
});

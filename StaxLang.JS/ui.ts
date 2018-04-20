import { Runtime, ExecutionState } from './stax';
import { parseProgram, Block } from './block';
import { pendWork } from './timeoutzero';
import { setClipboard } from './clipboard';
import { compress } from './huffmancompression';
import { cram } from './crammer';
import { isPacked, unpack, pack, staxDecode, staxEncode, unpackBytes } from './packer';
import * as bigInt from 'big-integer';
import 'url-search-params-polyfill';
type BigInteger = bigInt.BigInteger;

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
const warningsEl = document.getElementById("warnings") as HTMLUListElement;
const saveLink = document.getElementById("savelink") as HTMLAnchorElement;
const postLink = document.getElementById("generatepost") as HTMLAnchorElement;
const packButton = document.getElementById("pack") as HTMLButtonElement;
const golfButton = document.getElementById("golf") as HTMLButtonElement;
const downLink = document.getElementById("download") as HTMLAnchorElement;
const upButton = document.getElementById("upload") as HTMLButtonElement;
const fileInputEl = document.getElementById("uploadFile") as HTMLInputElement;
const compressorInputEl = document.getElementById("compressorInput") as HTMLInputElement;
const compressorOutputEl = document.getElementById("compressorOutput") as HTMLInputElement;
const compressorForceEl = document.getElementById("compressorForce") as HTMLInputElement;
const crammerInputEl = document.getElementById("crammerInput") as HTMLInputElement;
const crammerOutputEl = document.getElementById("crammerOutput") as HTMLInputElement;
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

enum CodeType { ASCII, Packed, UnpackedWithExtended }
let codeType: CodeType;
let codeBytes: number;
let codeChars: number;

runButton.disabled = false;
stepButton.disabled = false;

// prepare for new run
function resetRuntime() {
    input = steps = 0;
    start = performance.now();
    outputEl.textContent = "";
    warningsEl.textContent = "";
    pendingBreak = false;

    golfButton.disabled = packButton.disabled = upButton.disabled = true;
    codeArea.disabled = inputArea.disabled = true;
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
    activeRuntime = new Runtime(
        line => outputEl.textContent += line + "\n",
        warning => warningsEl.innerHTML += `<li>${ warning }`
    );
    activeStateIterator = activeRuntime.runProgram(code, stdin);
    if (input++) outputEl.textContent += "\n";
}

// mark program finished
function cleanupRuntime() {
    activeRuntime = activeStateIterator = null;
    upButton.disabled = codeArea.disabled = inputArea.disabled = false;
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
    function stripComments(stax: string): string {
        return stax.split(/\n/g).map(line => line.replace(/\t.*/, "")).join("\n");
    }
    if (!activeRuntime) return;
    debugContainer.hidden = false;

    stopButton.disabled = false;
    statusEl.textContent = `${ steps } steps, paused`;

    const debugPreEl = document.getElementById("debugCodePre")!,
        debugPostEl = document.getElementById("debugCodePost")!;
    let code = codeArea.value;
    if (isPacked(code)) code = unpack(code);
    debugPreEl.textContent = stripComments(code.substr(0, ip));
    debugPostEl.textContent = stripComments(code.substr(ip));

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

function encodePacked(packed: string): string | null {
    let bytes = staxDecode(packed);
    if (bytes.some(b => b < 0)) return null;
    let result = "";
    bytes.forEach(b => {
        result += (256 + b).toString(16).substr(1);
    });
    return result;
}

function decodePacked(packed: string): string {
    let bytes = [];
    for (let i = 0; i < packed.length; i += 2) bytes.push(parseInt(packed.substr(i, 2), 16));
    return staxEncode(bytes);
}

function countUtf8Bytes(s: string) {
    return new Blob([s]).size;
}

// show code size and update permalink
function updateStats() {
    let params = new URLSearchParams;
    if (isPacked(codeArea.value)) {
        let packed = encodePacked(codeArea.value);
        if (packed) params.set('p', packed);
    }
    if (!params.has('p')) params.set('c', codeArea.value);
    params.set('i', inputArea.value);
    if (autoCheckEl.checked) params.set('a', '1');
    if (blankSplitEl.checked) params.set('m', '1');
    if (lineSplitEl.checked) params.set('m', '2');
    
    saveLink.href = '#' + params.toString()
        .replace(/%2C/g, ",")
        .replace(/%5B/g, "[")
        .replace(/%5D/g, "]");

    packButton.hidden = false;
    golfButton.disabled = packButton.disabled = isActive();
    if (isPacked(codeArea.value)) {
        golfButton.hidden = true;
        codeType = CodeType.Packed;
        codeChars = codeBytes = codeArea.value.length;
        propsEl.textContent = `${ codeArea.value.length } bytes, packed`;
        packButton.textContent = "Unpack";
    }
    else {
        packButton.textContent = "Pack";
        let unknown = false, extraWhitespace = false;
        let pairs = 0;
        for (let i = 0; i < codeArea.value.length; i++) {
            let charCode = codeArea.value.charCodeAt(i);
            let codePoint = codeArea.value.codePointAt(i);
            if (charCode !== codePoint) pairs += 1;
            if (charCode < 32 || charCode > 127) {
                packButton.hidden = true;

                // could be calculated better by parsing
                extraWhitespace = extraWhitespace || (charCode === 9 || charCode === 10 || charCode === 13);
                unknown = unknown || !extraWhitespace; 
            }
        }
        golfButton.hidden = !extraWhitespace;

        if (unknown) {
            codeChars = codeArea.value.length - pairs;
            codeType = CodeType.UnpackedWithExtended;
            codeBytes = countUtf8Bytes(codeArea.value);
            propsEl.textContent = `${ codeChars } characters, ${ codeBytes } bytes UTF-8`;
        }
        else {
            codeType = CodeType.ASCII;
            codeChars = codeBytes = codeArea.value.length;
            propsEl.textContent = `${ codeArea.value.length } bytes, ASCII`;
        }
    }

    // make download blob link
    if (downLink.href !== "#") URL.revokeObjectURL(downLink.href);
    let buffer = new ArrayBuffer(codeBytes);
    let blob: Blob;
    if (codeType === CodeType.Packed) {
        let view = new Uint8Array(buffer);
        let decoded = staxDecode(codeArea.value);
        for (let i = 0; i < codeBytes; i++) view[i] = decoded[i];
        blob = new Blob([buffer], { type: "x/stax" });
    }
    else {
        blob = new Blob([codeArea.value], { type: "x/stax" });
    }
    downLink.href = URL.createObjectURL(blob);
}

function load() {
    let params = new URLSearchParams(location.hash.substr(1));
    if (params.has('p')) {
        codeArea.value = decodePacked(params.get('p')!);
        sizeTextArea(codeArea);
    }
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

postLink.addEventListener("click", ev => {
    let template = "# [Stax](https://github.com/tomtheisen/stax), ";
    switch (codeType) {
        case CodeType.ASCII:
            template += `${ codeBytes } bytes`;
            break;
        case CodeType.Packed:
            template += `${ codeBytes } [bytes](https://github.com/tomtheisen/stax/blob/master/docs/packed.md#packed-stax)`;
            break;
        case CodeType.UnpackedWithExtended:
            template += `${ codeBytes } bytes, (${ codeChars } chars, UTF-8)`;
            break;
    }
    template += "\n\n";
    for (let line of codeArea.value.split(/\n/g)) template += `\t${ line }\n`
    template += `\n[Run and debug it](${ saveLink.href })`

    setClipboard(template);
});

function doCompressor() {
    let input = compressorInputEl.value;
    let result: string;
    if (input === "") result = "z";
    else if (compressorForceEl.checked) result = '`' + compress(input) + '`'
    else if (input.length === 1) result = "'" + input;
    else if (input.length === 2) result = "." + input;
    else {
        let compressed = compress(input);
        if (compressed && compressed.length < input.length) result = '`' + compressed + '`';
        else result = '"' + input.replace('"', '`"') + '"'
    }
    compressorOutputEl.value = result;
    (document.getElementById("compressorInfo") as HTMLDivElement).textContent = `${ compressorOutputEl.value.length } bytes`;
}
doCompressor();
compressorInputEl.addEventListener("input", doCompressor);
compressorForceEl.addEventListener("change", doCompressor);

const compressorDialog = document.getElementById("compressorDialog") as HTMLDivElement;
document.getElementById("compressorOpen")!.addEventListener("click", () => {
    compressorDialog.hidden = !compressorDialog.hidden;
});

function doCrammer() {
    let matches = crammerInputEl.value.match(/-?\d+/g);
    if (matches) {
        let crammed = cram(matches.map(e => bigInt(e)));
        crammerOutputEl.value = `"${ crammed }"!`;
    }
    else crammerOutputEl.value = "z";
    (document.getElementById("crammerInfo") as HTMLDivElement).textContent = `${ crammerOutputEl.value.length } bytes`;
}
doCrammer();
crammerInputEl.addEventListener("input", doCrammer);

const crammerDialog = document.getElementById("crammerDialog") as HTMLDivElement;
document.getElementById("crammerOpen")!.addEventListener("click", () => {
    crammerDialog.hidden = !crammerDialog.hidden;
});

packButton.addEventListener("click", ev => {
    let code = codeArea.value, packed = isPacked(code);
    codeArea.value = packed ? unpack(code) : pack(code);
    updateStats();
});

function golf(tokens: (string | Block)[]): string {
    let golfed = tokens.map(t => {
        if (typeof t === "string") return /^\s/.test(t) ? "" : t;
        // golf block
        return "{" + golf(t.tokens) + (t.explicitlyTerminated ? "}" : "");
    });
    return golfed.join("");
}
golfButton.addEventListener("click", ev => {
    let program = parseProgram(codeArea.value);
    let golfed = golf(program.tokens), count = program.getGotoTargetCount();
    for (let i = 1; i <= count; i++) {
        golfed += golf(program.getGotoTarget(i).tokens);
    }
    codeArea.value = golfed;
    sizeTextArea(codeArea);
    updateStats();
});

upButton.addEventListener("click", ev => {
    fileInputEl.click();
})
fileInputEl.addEventListener("change", ev => {
    let file = fileInputEl.files && fileInputEl.files[0];
    if (!file) return;

    let reader = new FileReader;
    reader.addEventListener("load", ev => {
        let buffer = reader.result as ArrayBuffer;
        let bytes = new Uint8Array(buffer);
        if (bytes.length === 0) {
            alert("Uploaded file is empty.");
            return;
        }
        else if (bytes[0] >= 0x80) { // packed
            codeArea.value = staxEncode(bytes);
            updateStats();
        }
        else { // ascii or utf8
            let stringReader = new FileReader;
            stringReader.addEventListener("load", ev => {
                codeArea.value = stringReader.result as string;
                updateStats();
            });
            stringReader.readAsText(file!); 
        }
    });
    reader.readAsArrayBuffer(file);
})

function setVersion() {
    let out: string;
    let rt = new Runtime(o => { out = o; });
    for (let s of rt.runProgram("V?", [])) ;
    let version = out!.match(/[0-9.]+/)![0]
    document.getElementById("version")!.textContent = `v${ version }`;
}
setVersion();

function setupQuickRef() {
    const quickrefEl = document.getElementById("quickref") as HTMLDivElement;

    quickrefEl.innerHTML += require("../docs/instructions.md") as string;
    quickrefEl.innerHTML += require("../docs/generators.md") as string;
}
setupQuickRef();

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

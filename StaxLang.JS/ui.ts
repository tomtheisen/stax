import { Runtime, ExecutionState } from './stax';
import { 
    parseProgram, Block, getCodeType, CodeType, LiteralTypes, 
    compressLiterals, decompressLiterals, 
    squareLinesAndComments, hasNewLineInLiteral 
} from './block';
import { pendWork } from './timeoutzero';
import { setClipboard } from './clipboard';
import { compressLiteral } from './huffmancompression';
import { cram, cramSingle, compressIntAray } from './crammer';
import { isPacked, unpack, pack, staxDecode, staxEncode } from './packer';
import 'url-search-params-polyfill';
import { S2A } from './types';
import { nativeSortIsStable } from './stable-sort';

declare var __COMMIT_HASH__: string;
declare var __BUILD_DATE__: string;
document.getElementById("buildInfo")!.textContent = `
    ${__COMMIT_HASH__}
    built ${__BUILD_DATE__.replace(/:\d{2}\.\d{3}Z/, "Z")},
    ${ nativeSortIsStable() ? 'stable' : 'unstable' } sort`;

// duration to run stax program before yielding to ui and pumping messages
const workMilliseconds = 40;
const saveKey = "saved-state";

function el<T extends HTMLElement>(id: string): T {
    const result = document.getElementById(id) as T;
    if (!result) throw new Error(`could not find ${id}`);
    return result;
}

const root = document.firstElementChild as HTMLHtmlElement;
const runButton = el<HTMLButtonElement>("run");
const stepButton = el<HTMLButtonElement>("step");
const stopButton = el<HTMLButtonElement>("stop");
const codeArea = el<HTMLTextAreaElement>("code");
const inputArea = el<HTMLTextAreaElement>("stdin");
const statusEl = el<HTMLElement>("status");
const propsEl = el<HTMLElement>("properties");
const outputEl = el<HTMLPreElement>("output");
const copyOutputButton = el<HTMLButtonElement>("outputCopy");
const warningsEl = el<HTMLUListElement>("warnings");
const saveLink = el<HTMLAnchorElement>("savelink");
const postLink = el<HTMLButtonElement>("generatepost");
const newLink = el<HTMLAnchorElement>("newfile");
const quickrefFilter = el<HTMLInputElement>("quickref-filter");
const packButton = el<HTMLButtonElement>("pack");
const golfButton = el<HTMLButtonElement>("golf");
const compressButton = el<HTMLButtonElement>("compress");
const dumpButton = el<HTMLButtonElement>("dump");
const uncompressButton = el<HTMLButtonElement>("uncompress");
const downLink = el<HTMLAnchorElement>("download");
const upButton = el<HTMLButtonElement>("upload");
const fileInputEl = el<HTMLInputElement>("uploadFile");
const stringInputEl = el<HTMLInputElement>("stringInput");
const stringOutputEl = el<HTMLInputElement>("stringOutput");
const stringInfoEl = el<HTMLDivElement>("stringInfo");
const integerInputEl = el<HTMLInputElement>("integerInput");
const integerOutputEl = el<HTMLInputElement>("integerOutput");
const integerInfoEl = el<HTMLDivElement>("integerInfo");
const autoCheckEl = el<HTMLInputElement>("autoRunPermalink");
const blankSplitEl = el<HTMLInputElement>("blankSplit");
const lineSplitEl = el<HTMLInputElement>("lineSplit");
const noSplitEl = el<HTMLInputElement>("noSplit");
const darkThemeEl = el<HTMLInputElement>("darkTheme");

let activeRuntime: Runtime | null = null, lastExecutedProgram: string = "";
let activeStateIterator: Iterator<ExecutionState> | null = null;
let steps = 0, start = 0, input = 0;
let pendingBreak = false, dumping = false;
let pendingInputs: string[] = [];

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
    dumping = pendingBreak = false;

    golfButton.disabled = packButton.disabled = upButton.disabled = true;
    codeArea.disabled = inputArea.disabled = true;
    dumpButton.disabled = compressButton.disabled = uncompressButton.disabled = true;
    root.classList.remove("debugging");
    stopButton.disabled = false;
    copyOutputButton.hidden = true;

    if (blankSplitEl.checked) {
        // split on \n\n+ *unless* some block starts with triple quote
        const pattern = /^(?!"""$).(?:.+\n?)+|^"""$(?:.|\n)*?(?:^"""$|(?!.|\n))/gm;
        pendingInputs = [];
        let match: ReturnType<typeof pattern.exec>;
        while (match = pattern.exec(inputArea.value)) pendingInputs.push(match[0]);
    }
    else if (lineSplitEl.checked) pendingInputs = inputArea.value.split(/\r?\n/);
    else pendingInputs = [inputArea.value];
    startNextInput();
}

// when running, this triggers the scroll to the end after each time slice
let newOutput = false;
// begin the next test case, or clean up if done
function startNextInput() {
    if (pendingInputs.length === 0) {
        cleanupRuntime();
        return;
    }

    lastExecutedProgram = codeArea.value;
    let stdin = pendingInputs.shift()!.split(/\r?\n/);
    activeRuntime = new Runtime(
        content => {
            if (/\f|\r|\x08/.exec(content)) {
                for (let char of content) {
                    if (outputEl.textContent == null) outputEl.textContent = "";
                    switch (char) {
                        case "\f":
                            outputEl.textContent = "";
                            break;
                        case "\r":
                            outputEl.textContent = outputEl.textContent.slice(0, outputEl.textContent.lastIndexOf('\n'));
                            break;
                        case "\b":
                            outputEl.textContent = outputEl.textContent.slice(0, -1);
                            break;
                        default:
                            outputEl.textContent += char;
                            break;
                    }
                }
            }
            else outputEl.textContent += content;
            copyOutputButton.hidden = false;
            newOutput = true;
        },
        warning => warningsEl.innerHTML += `<li>${ warning }`
    );
    activeStateIterator = activeRuntime.runProgram(lastExecutedProgram, stdin);
    if (input++) outputEl.textContent += "\n";
}

// mark program finished
function cleanupRuntime() {
    activeRuntime = activeStateIterator = null;
    upButton.disabled = codeArea.disabled = inputArea.disabled = false;
    stopButton.disabled = true;
    root.classList.remove("debugging");
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
    pendWork(function work() {
        if (!activeStateIterator || pendingBreak) {
            pendingBreak = false;
            return;
        }

        root.classList.remove("debugging");

        let result: IteratorResult<ExecutionState>, sliceEnd = performance.now() + workMilliseconds;
        try {
            while (!(result = activeStateIterator.next()).done) {
                steps += 1;
                if (dumping && lastExecutedProgram[result.value.ip] === "\t") {
                    const prefix = lastExecutedProgram.substr(0, result.value.ip);
                    const lineidx = prefix.split(/\n/g).length - 1, lines = codeArea.value.split(/\n/g);
                    if (activeRuntime && lines[lineidx].endsWith("\t")) {
                        const state = activeRuntime.getDebugState({ zeroString: " " });
                        const main = state.main.filter(e => !e.startsWith("Block")).join(" "),
                            input = state.input.filter(e => !e.startsWith("Block")).join(" ");
                        if (!main.startsWith("Block")) {
                            lines[lineidx] += (input && `input:${ input } `) + (main && `main:${ main } `);
                            codeArea.value = lines.join("\n");
                        }
                    }
                }
                else if (result.value.break) {
                    showDebugInfo(result.value.ip, steps);
                    return;
                }
                else if (result.value.frameSleep) return requestAnimationFrame(work);
                if (performance.now() > sliceEnd) break;
            }
            if (result.done) startNextInput();
            runProgramTimeSlice();
        }
        catch (e) {
            if (e instanceof Error) showError(e);
            startNextInput();
            runProgramTimeSlice();
        }
        finally {
            if (newOutput) window.scrollTo(0, document.body.scrollHeight);
            newOutput = false;
        }
        
        let elapsed = (performance.now() - start) / 1000;
        statusEl.textContent = `${ steps } steps, ${ elapsed.toFixed(2) }s`;
    });
}

function run() {
    pendingBreak = false;
    if (!isActive()) resetRuntime();
    statusEl.innerText = "Starting";
    runProgramTimeSlice();
}
runButton.addEventListener("click", run);

// gets instruction pointer if still running
function step() : number | null {
    function caseComplete() {
        startNextInput();
        root.classList.remove("debugging");
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

copyOutputButton.addEventListener("click", ev => {
    const range = document.createRange();
    range.selectNodeContents(outputEl);
    let selection = getSelection();
    if (selection) {
        selection.removeAllRanges();
        selection.addRange(range);
        document.execCommand("copy")
    }
});

document.addEventListener("click", ev => {
    if (ev.target instanceof HTMLElement && ev.target.classList && ev.target.classList.contains("debug-omit")) {
        ev.target.replaceWith(ev.target.title);
    }
});

function showDebugInfo(ip: number, steps: number) {
    function stripComments(stax: string): string {
        return stax.split(/\n/g).map(line => line.replace(/\t.*/, "")).join("\n");
    }
    function showValue(el: HTMLElement, value: string): void {
        const maxLength = 300;
        if (value.length <= maxLength) el.textContent = value;
        else {
            let start = document.createElement("span"), mid = document.createElement("a"), end = document.createElement("span");
            start.textContent = value.substr(0, maxLength >> 1);
            end.textContent = value.substr(value.length - (maxLength >> 1));
            mid.textContent = '«…»';
            mid.title = value.substr(maxLength >> 1, value.length - maxLength);
            mid.href = "javascript:void(0)";
            mid.classList.add("debug-omit");
            el.innerHTML = "";
            el.append(start, mid, end);
        }
    }
    if (!activeRuntime) return;
    root.classList.add("debugging");

    stopButton.disabled = false;
    statusEl.textContent = `${ steps } steps, paused`;

    const debugPreEl = document.getElementById("debugCodePre")!,
        debugPostEl = document.getElementById("debugCodePost")!;
    let code = codeArea.value;
    if (isPacked(code)) code = unpack(code);
    debugPreEl.textContent = stripComments(code.substr(0, ip));
    debugPostEl.textContent = stripComments(code.substr(ip));

    let state = activeRuntime.getDebugState();
    showValue(document.getElementById("watchX")!, state.x);
    showValue(document.getElementById("watchY")!, state.y);
    showValue(document.getElementById("watchi")!, state.index.toString());
    showValue(document.getElementById("watch_")!, state._);
    
    const watchMainEl = document.getElementById("watchMain") as HTMLOListElement;
    watchMainEl.innerText = "";
    state.main.forEach(e => {
        let li = document.createElement("li");
        showValue(li, e);
        watchMainEl.appendChild(li);
    });
    
    const watchInputEl = document.getElementById("watchInput") as HTMLOListElement;
    watchInputEl.innerText = "";
    state.input.forEach(e => {
        let li = document.createElement("li");
        showValue(li, e);
        watchInputEl.appendChild(li);
    });
}

function sizeTextArea(el: HTMLTextAreaElement, maxAutoRows = 30) {
    let newRows = Math.max(el.rows, 2, el.value.split("\n").length);
    newRows = Math.min(newRows, maxAutoRows);
    el.rows = newRows;
}

function encodePacked(packed: string): string | null {
    let bytes = staxDecode(packed), result = "";
    if (!bytes) return null;
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
    
    const saved: string = '#' + params.toString()
        .replace(/%2C/g, ",")
        .replace(/%5B/g, "[")
        .replace(/%5D/g, "]");
    
    saveLink.href = saved;
    try { sessionStorage.setItem(saveKey, saved); } 
    catch (ex) { /* sessionStorage throws on file system in chrome */ }

    packButton.hidden = false;
    dumpButton.disabled = compressButton.disabled = uncompressButton.disabled = golfButton.disabled = packButton.disabled = isActive();
    let literalTypes: LiteralTypes;
    [codeType, literalTypes] = getCodeType(codeArea.value);
    if (codeType === CodeType.Packed) {
        dumpButton.hidden = compressButton.hidden = uncompressButton.hidden = golfButton.hidden = true;
        codeChars = codeBytes = codeArea.value.length;
        propsEl.textContent = `${ codeArea.value.length } bytes, packed`;
        packButton.textContent = "Unpack";
    }
    else {
        packButton.textContent = "Pack";

        packButton.hidden = codeType != CodeType.TightAscii || codeArea.value === "";
        golfButton.hidden = codeType != CodeType.LooseAscii && codeType != CodeType.UnpackedLooseNonAscii;
        dumpButton.hidden = codeType != CodeType.LooseAscii || hasNewLineInLiteral(codeArea.value);
        compressButton.hidden = !(literalTypes & (LiteralTypes.CompressableString | LiteralTypes.CompressableInt | LiteralTypes.CrammableSequence));
        uncompressButton.hidden = !(literalTypes & (LiteralTypes.CompressedString | LiteralTypes.CompressedInt | LiteralTypes.CrammedArray));

        codeChars = [...codeArea.value].length;
        if (codeType === CodeType.UnpackedTightNonAscii || codeType === CodeType.UnpackedLooseNonAscii) {
            codeBytes = countUtf8Bytes(codeArea.value);
            propsEl.textContent = `${ codeChars } characters, ${ codeBytes } bytes UTF-8`;
        }
        else {
            codeBytes = codeArea.value.length;
            propsEl.textContent = `${ codeArea.value.length } bytes, ASCII`;
        }
    }

    // make download blob link
    if (downLink.href !== "#") URL.revokeObjectURL(downLink.href);
    let buffer = new ArrayBuffer(codeBytes), blob: Blob;
    if (codeType === CodeType.Packed) {
        let view = new Uint8Array(buffer), decoded = staxDecode(codeArea.value);
        if (decoded == null) throw new Error("not a packed program");
        for (let i = 0; i < codeBytes; i++) view[i] = decoded[i];
        blob = new Blob([buffer], { type: "x/stax" });
    }
    else blob = new Blob([codeArea.value], { type: "x/stax" });
    downLink.href = URL.createObjectURL(blob);
}

function load() {
    let saved: string | null = null, conflicted = false;
    try {
        if (!location.hash.startsWith("#force=1&")) saved = sessionStorage.getItem(saveKey);
        if (saved && location.hash && saved !== location.hash) {
            conflicted = true;
            warningsEl.innerHTML += "<li>Restored unsaved program state from session.  <a id=load-from-url href='javascript:void(0)'>Load from URL instead.</a>";
            document.getElementById("load-from-url")!.addEventListener("click", ev => {
                sessionStorage.removeItem(saveKey);
                load();
                warningsEl.innerHTML = "";
            });
        }
    } 
    catch (ex) { /* sessionStorage throws on file system in chrome */ }
    if (!saved) saved = location.hash;
    let params = new URLSearchParams(saved.substr(1));
    
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
        if (!conflicted) run();
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
        // Ctrl + I to insert tab, which is a line comment in ascii stax
        ev.preventDefault();
        let s = codeArea.selectionStart;
        codeArea.value = codeArea.value.substr(0, s)
            + "\t" + codeArea.value.substr(s);
        codeArea.selectionEnd = s + 1;
        pendUpdate();
    }
});
inputArea.addEventListener("input", pendUpdate);

postLink.disabled = location.href.startsWith("file:");
postLink.addEventListener("click", ev => {
    let template = "# [Stax](https://github.com/tomtheisen/stax), ";
    switch (codeType) {
        case CodeType.LooseAscii:
        case CodeType.TightAscii:
            template += `${ codeBytes } bytes`;
            break;
        case CodeType.Packed:
            template += `${ codeBytes } [bytes](https://github.com/tomtheisen/stax/blob/master/docs/packed.md#packed-stax)`;
            break;
        case CodeType.UnpackedTightNonAscii:
            template += `${ codeBytes } bytes, (${ codeChars } chars, UTF-8)`;
            break;
    }
    template += "\n\n";
    for (let line of codeArea.value.split(/\n/g)) template += `\t${ line }\n`
    template += `\n[Run and debug it](${ saveLink.href })`

    setClipboard(template);
});

newLink.addEventListener("click", ev => {
    if (!confirm("Reset program and input?")) return;

    for (let area of [codeArea, inputArea]) {
        area.style.height = area.value = "";
        area.rows = 2;
    }
    outputEl.textContent = "";
    copyOutputButton.hidden = true;
    warningsEl.textContent = "";
    stop();
});

function doStringCoder() {
    let input = stringInputEl.value;
    let result: string;
    if (input === "") result = "z";
    else if (input.length === 1) result = "'" + input;
    else if (input.length === 2) result = "." + input;
    else {
        const compressed = compressLiteral(input);
        if (compressed && compressed.length - 2 < input.length) {
            result = compressed;
        }
        else {
            result = [...input].some(c => c < ' ' || c > '~')
                ? cram(S2A(input))
                : '"' + input.replace(/([`"])/g, '`$1') + '"';
        }
    }
    stringOutputEl.value = result;
    stringInfoEl.textContent = `${ stringOutputEl.value.length } bytes`;
}
doStringCoder();
stringInputEl.addEventListener("input", doStringCoder);

function doIntegerCoder() {
    let matches = integerInputEl.value.match(/-?\d+/g);
    if (matches) {
        let ints = matches.map(e => BigInt(e));
        if (ints.length === 1) {
            integerOutputEl.value = cramSingle(ints[0]);
            integerInfoEl.textContent = `${ integerOutputEl.value.length } bytes (scalar)`;
        }
        else {
            integerOutputEl.value = compressIntAray(ints);
            integerInfoEl.textContent = `${ integerOutputEl.value.length } bytes (array)`;
        }
    }
    else integerInfoEl.textContent = integerOutputEl.value = "";
}
doIntegerCoder();
integerInputEl.addEventListener("input", doIntegerCoder);

packButton.addEventListener("click", ev => {
    let code = codeArea.value, packed = isPacked(code);
    codeArea.value = packed ? unpack(code) : pack(code);
    updateStats();
});

function golf(tokens: (string | Block)[]): string {
    const isNumberLiteral = (token: (string | Block)) => typeof token === "string" && /^\d/.test(token);

    let golfed = tokens.map((t, i) => {
        if (t instanceof Block) return "{" + golf(t.tokens) + (t.explicitlyTerminated ? "}" : "");
        else if (!/^\s/.test(t)) return t;
        // space is necessary between numeric literal tokens unless the first one is '0'
        else if (tokens[i - 1] === "0") return "";
        else if (isNumberLiteral(tokens[i - 1]) && isNumberLiteral(tokens[i + 1])) return " ";
        else return "";
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

compressButton.addEventListener("click", ev => {
    codeArea.value = compressLiterals(codeArea.value);
    updateStats();
});

uncompressButton.addEventListener("click", ev => {
    codeArea.value = decompressLiterals(codeArea.value);
    updateStats();
});

dumpButton.addEventListener("click", ev => {
    codeArea.value = squareLinesAndComments(codeArea.value);
    pendingBreak = false;
    resetRuntime();
    statusEl.innerText = "Dumping";
    dumping = true;
    runProgramTimeSlice();
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

warningsEl.addEventListener("click", ev => {
    if (ev.target === warningsEl) warningsEl.innerHTML = "";
});

function setVersion() {
    let out: string;
    let rt = new Runtime(o => { out = o; });
    for (let _ of rt.runProgram("V?", [])) ;
    let version = out!.match(/[0-9.]+/)![0]
    document.getElementById("version")!.textContent = `v${ version }`;
}
setVersion();

function setupQuickRef() {
    const contentEl = document.getElementById("quickref-content") as HTMLDivElement;
    contentEl.innerHTML += require("../docs/instructions.md") as string;
    contentEl.innerHTML += require("../docs/generators.md") as string;

    let els = Array.from(contentEl.childNodes);
    els.forEach(el => {
        if (!["H2", "TABLE"].includes(el.nodeName)) {
            contentEl.removeChild(el);
        }
    });

    function searchQuickref() {
        function isInstruction(str: string): boolean {
            switch (str.length) {
                case 1:
                    return !":|Vg'.\"".includes(str);
                case 2:
                    return ":|V".includes(str[0])
                default:
                    return false;
            }
        }

        let h2s = Array.from(contentEl.getElementsByTagName("h2"));
        h2s.forEach(h2 => h2.hidden = true);

        let trs = Array.from(contentEl.getElementsByTagName("tr"));
        trs.forEach(tr => {
            let foundMatch = false;
            if (isInstruction(quickrefFilter.value)) {
                let codes = tr.querySelectorAll("code");
                foundMatch = Array.from(codes).map(c => c.textContent).includes(quickrefFilter.value);
            }
            else {
                foundMatch = (tr.textContent || '').toLowerCase().includes(quickrefFilter.value.toLowerCase());
            }
            tr.hidden = !foundMatch;
            if (foundMatch) {
                let table = tr.parentElement!.parentElement as HTMLTableElement;
                table.querySelector("tr")!.hidden = false;
                (table.previousElementSibling as HTMLHeadingElement).hidden = false;
            }
        });
    }
    quickrefFilter.addEventListener("input", searchQuickref);
    searchQuickref();
}
setupQuickRef();

function isQuickRef(): boolean {
    return !!document.documentElement && document.documentElement.classList.contains("show-quickref"); 
}
function toggleQuickRef() {
    if (isTools()) toggleTools();
    document.documentElement && document.documentElement.classList.toggle("show-quickref");
    if (isQuickRef()) {
        quickrefFilter.focus();
        quickrefFilter.setSelectionRange(0, quickrefFilter.value.length);
    }
    else codeArea.focus();
}
for (let id of ["quickref-close", "quickref-link"]) {
    (document.getElementById(id) as HTMLElement).addEventListener("click", toggleQuickRef);
}

function isTools(): boolean {
    return !!document.documentElement && document.documentElement.classList.contains("show-tools"); 
}
function toggleTools() {
    if (isQuickRef()) toggleQuickRef();
    document.documentElement && document.documentElement.classList.toggle("show-tools");
    if (isTools()) document.querySelector("#tools")?.querySelector("summary")?.focus();
}
document.getElementById("tools-link")!.addEventListener("click", toggleTools);

function setLayout() {
    let checkedEl = document.querySelector("#layout :checked");
    if (checkedEl instanceof HTMLInputElement) root.setAttribute("data-layout", checkedEl.value);
}
setLayout();
document.getElementById("layout")!.addEventListener("change", setLayout);

document.addEventListener("keydown", ev => {
    switch (ev.key) {
        case "S":
        case "s":
            if (ev.ctrlKey) {
                window.location.href = saveLink.href;
                ev.preventDefault();
            }
            break;
        case "F1":
            ev.preventDefault();
            toggleQuickRef();
            break;
        case "F2":
            ev.preventDefault();
            codeArea.focus();
            break;
        case "F4":
            ev.preventDefault();
            inputArea.focus();
            break;
        case "F8":
            ev.preventDefault();
            run();
            break;
        case "F9":
            ev.preventDefault();
            toggleTools();
            break;
        case "F11":
            ev.preventDefault();
            stepButton.focus(); // ensure document's active element is not disabled
            step();
            break;
        case "Escape":
            ev.preventDefault();
            if (isActive()) stop();
            else if (isQuickRef()) toggleQuickRef();
            else if (isTools()) toggleTools();
            break;
        case "ArrowUp":
        case "ArrowDown":
            for (let e = document.activeElement; e; e = e.parentElement) {
                if (e instanceof HTMLInputElement) break;
                let sibling = ev.key === "ArrowUp" ? e.previousElementSibling : e.nextElementSibling;
                if (e instanceof HTMLDetailsElement && sibling instanceof HTMLDetailsElement) {
                    sibling.querySelector("summary")?.focus();
                    ev.preventDefault();
                    break;
                }
            } 
            break;
        case "ArrowLeft":
        case "ArrowRight":
            for (let e = document.activeElement; e; e = e.parentElement) {
                if (e instanceof HTMLInputElement) break;
                if (e instanceof HTMLDetailsElement) {
                    e.open = ev.key === "ArrowRight";
                    e.querySelector("summary")?.focus();
                    ev.preventDefault();
                    break;
                }
            }
            break;
    }
});

// theme stuff
const darkChannel = typeof BroadcastChannel === "function" ? new BroadcastChannel('theme-dark') : null;
let receivingDarkMessage = false;
function setDarkState() {
    document.documentElement.classList.toggle("theme-dark", darkThemeEl.checked);
}
darkThemeEl.addEventListener("change", () => {
    setDarkState();
    if (!receivingDarkMessage) {
        localStorage.setItem("theme-dark", darkThemeEl.checked.toString());
        darkChannel?.postMessage(darkThemeEl.checked);
    }
});
darkChannel?.addEventListener("message", ev => {
    receivingDarkMessage = true;
    darkThemeEl.checked = ev.data;
    setDarkState();
    receivingDarkMessage = false;
});
darkThemeEl.checked = localStorage.getItem("theme-dark") === true.toString();
setDarkState();

// hide top link when at the top
const topLink = document.getElementById("toplink") as HTMLAnchorElement;
function handleScroll() {
    topLink.style.display = window.scrollY < codeArea.offsetTop ? "none" : "";
}
handleScroll();
document.addEventListener("scroll", handleScroll);

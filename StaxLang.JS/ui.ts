import { Runtime, ExecutionState } from './stax';
import { setTimeout } from 'timers';
import { pendWork } from './timeoutzero';
import { compress } from './huffmancompression';
import { isPacked, unpack, pack } from './packer';

// duration to run stax program before yielding to ui and pumping messages
const workMilliseconds = 20;

const runButton = document.getElementById("run") as HTMLButtonElement;
const codeArea = document.getElementById("code") as HTMLTextAreaElement;
const inputArea = document.getElementById("stdin") as HTMLTextAreaElement;
const statusEl = document.getElementById("status") as HTMLElement;
const propsEl = document.getElementById("properties") as HTMLElement;
const outputEl = document.getElementById("output") as HTMLPreElement;
const saveLink = document.getElementById("savelink") as HTMLAnchorElement;
const packButton = document.getElementById("pack") as HTMLButtonElement;
const compressorInputEl = document.getElementById("compressorInput") as HTMLInputElement;
const compressorOutputEl = document.getElementById("compressorOutput") as HTMLInputElement;

let activeStateIterator: Iterator<ExecutionState> | null = null;
let steps = 0, start = 0;

function iterateProgramState() {
    if (!activeStateIterator) return;

    let result: IteratorResult<ExecutionState>, sliceStart = performance.now();
    while (!(result = activeStateIterator.next()).done) {
        steps += 1;
        if(performance.now() - sliceStart > workMilliseconds) break;
    }
    if (!result.done) pendWork(iterateProgramState);
    
    let elapsed = Math.ceil(performance.now() - start);
    statusEl.textContent = `${ steps } steps, ${ elapsed }ms`;
}

runButton.addEventListener("click", () => {
    steps = 0;
    start = performance.now();
    outputEl.textContent = "";

    let code = codeArea.value, stdin = inputArea.value.split(/\r?\n/);
    let rt = new Runtime(line => outputEl.textContent += line + "\n");
    activeStateIterator = rt.runProgram(code, stdin);

    iterateProgramState();
});

function load() {
    let params = new URLSearchParams(location.hash.substr(1));
    if (params.has('c')) codeArea.value = params.get('c')!;
    if (params.has('i')) inputArea.value = params.get('i')!;
}
load();

function updateStats() {
    let params = new URLSearchParams;
    params.set('c', codeArea.value);
    params.set('i', inputArea.value);
    saveLink.href = '#' + params.toString();

    let packed = isPacked(codeArea.value);
    let type = packed ? "packed" : "ascii";

    let size = codeArea.value.length;
    propsEl.textContent = `${ size } bytes, ${ type }`;

    packButton.textContent = packed ? "Unpack" : "Pack";
}
updateStats();

codeArea.addEventListener("change", updateStats);
inputArea.addEventListener("change", updateStats);

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

packButton.addEventListener("click", () => {
    let code = codeArea.value, packed = isPacked(code);
    codeArea.value = packed ? unpack(code) : pack(code);
    updateStats();
});
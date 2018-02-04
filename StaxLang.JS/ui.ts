import { Runtime, ExecutionState } from './stax'
import { setTimeout } from 'timers';

// duration to run stax program before yielding to ui and pumping messages
const workMilliseconds = 20;

const runButton = document.getElementsByTagName("button")[0];
const codeArea = document.getElementById("code") as HTMLTextAreaElement;
const inputArea = document.getElementById("stdin") as HTMLTextAreaElement;
const statusEl = document.getElementById("status") as HTMLDivElement;
const outputEl = document.getElementById("output") as HTMLDivElement;

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
    statusEl.innerText = `${ steps } steps, ${ elapsed }ms`;
}

runButton.addEventListener("click", () => {
    steps = 0;
    start = performance.now();
    outputEl.textContent = "";

    let rt = new Runtime(line => outputEl.textContent += line + "\n");
    let code = codeArea.value;
    let stdin = inputArea.value.split("\n");
    activeStateIterator = rt.runProgram(code, stdin);

    iterateProgramState();
});

const timeouts: (() => void)[] = [];
const messageName = "stax-work-pend";

function pendWork(fn: () => void) {
    timeouts.push(fn);
    window.postMessage(messageName, "*");
}

window.addEventListener("message", event => {
    if (event.source === window && event.data === messageName) {
        event.stopPropagation();
        if (timeouts.length) timeouts.shift()!();
    }
}, true);
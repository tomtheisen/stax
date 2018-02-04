import { Runtime, ExecutionState } from './stax';
import { setTimeout } from 'timers';
import { pendWork } from './timeoutzero';

// duration to run stax program before yielding to ui and pumping messages
const workMilliseconds = 20;

const runButton = document.getElementById("run") as HTMLButtonElement;
const codeArea = document.getElementById("code") as HTMLTextAreaElement;
const inputArea = document.getElementById("stdin") as HTMLTextAreaElement;
const statusEl = document.getElementById("status") as HTMLDivElement;
const outputEl = document.getElementById("output") as HTMLPreElement;

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
import { Runtime, ExecutionState } from './stax'
import { setTimeout } from 'timers';

const runButton = document.getElementsByTagName("button")[0];
const codeArea = document.getElementById("code") as HTMLTextAreaElement;
const inputArea = document.getElementById("stdin") as HTMLTextAreaElement;
const statusEl = document.getElementById("status") as HTMLDivElement;
const outputEl = document.getElementById("output") as HTMLDivElement;

let activeStateIterator: Iterator<ExecutionState> | null = null;
let steps = 0, start = 0, output: string[] = [];

function iterateProgramState() {
    if (!activeStateIterator) return;

    steps += 1;
    let elapsed = Math.ceil(new Date().valueOf() - start);
    statusEl.innerText = `${ steps } steps, ${ elapsed }ms`;

    let result = activeStateIterator.next();
    if (result.done) {
        outputEl.innerText = output.join("\n");
    }
    else {
        setTimeout(iterateProgramState, 0);
    }
}

runButton.addEventListener("click", () => {
    output = []; 
    steps = 0;
    start = new Date().valueOf();

    let rt = new Runtime(output.push.bind(output));
    let code = codeArea.value;
    let stdin = inputArea.value.split("\n");
    activeStateIterator = rt.runProgram(code, stdin);
    
    iterateProgramState();
});
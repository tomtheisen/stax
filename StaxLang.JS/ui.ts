import { Runtime, ExecutionState } from './stax'

let runButton = document.getElementsByTagName("button")[0];

runButton.addEventListener("click", () => {
    let output: string[] = [];
    let rt = new Runtime(output.push.bind(output)), steps = 0;
    let code = (document.getElementById("code") as HTMLTextAreaElement).value;
    let stdin = (document.getElementById("stdin") as HTMLTextAreaElement).value.split("\n");

    let start = new Date;
    for (let s of rt.runProgram(code, stdin)) steps += 1;
    let end = new Date;

    let msg = `${ steps } steps, ${ Math.round(end.valueOf() - start.valueOf()) }ms`;
    (document.getElementById("status") as HTMLDivElement).innerText = msg;


    (document.getElementById("output") as HTMLDivElement).innerText = output.join("\n");
});
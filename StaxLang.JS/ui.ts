import { Runtime, ExecutionState } from './stax'

let runButton = document.getElementsByTagName("button")[0];

runButton.addEventListener("click", () => {
    let output: string[] = [];
    let rt = new Runtime(output.push.bind(output)), steps = 0;
    let code = (document.getElementById("code") as HTMLTextAreaElement).value;
    let stdin = (document.getElementById("stdin") as HTMLTextAreaElement).value.split("\n");

    for (let s of rt.runProgram(code, stdin)) steps += 1;

    document.getElementsByTagName("div")[0].innerText = output.join("\n");
});
import { Runtime, ExecutionState } from './stax'

let runButton = document.getElementsByTagName("button")[0];

runButton.addEventListener("click", () => {
    let output: string[] = [];
    let rt = new Runtime(output.push.bind(output)), steps = 0;
    let code = document.getElementsByTagName("textarea")[0].value;

    for (let s of rt.runProgram(code, [])) steps += 1;

    document.getElementsByTagName("div")[0].innerText = output.join("\n");
});
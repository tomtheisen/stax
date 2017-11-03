import {Block, Program, parseProgram} from './block';

export class ExecutionState {
    public ip: number;
    public cancel: boolean;

    constructor(ip: number, cancel = false) {
        this.ip = ip;
        this.cancel = cancel;
    }
}

export class Runtime {
    private lineOut: (line: string) => void;
    private outBuffer = ""; // unterminated line output

    constructor(output: (line: string) => void) {
        this.lineOut = output;
    }

    public *runSteps(block: Block | string): Iterator<ExecutionState> {
        if (typeof block === "string") block = parseProgram(block);

        let ip = 0;
        for (let token of block.tokens) {
            yield new ExecutionState(ip);
            ip += token.length;
        }
    }
}
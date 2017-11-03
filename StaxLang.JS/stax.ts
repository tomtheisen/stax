import {Block, Program} from './block';

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

    constructor(output: (line: string) => void) {
        this.lineOut = output;
    }

    public *runSteps(block: Block): Iterator<ExecutionState> {
        let ip = 0;
        for (let token of block.tokens) {
            yield new ExecutionState(ip);
            ip += token.length;
        }
    }
}
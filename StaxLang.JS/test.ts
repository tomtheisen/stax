import { Block, parseProgram } from './block';
import { Runtime, ExecutionState } from './stax';
import * as _ from 'lodash';
import { readFileSync, readdirSync } from 'fs';

interface TestCase {
    name: string;
    io: {in: string[], expected: string[]}[];
    programs: { line: number, code: string }[];
}

enum TestFileState { None, In, Out, Code }

class TestFiles{
    mode = TestFileState.None;
    cases: TestCase[] = [];
    currentCase: TestCase | null = null;
    name: string;
    attempts: number;
    passed: number;
    constructor(path: string, name: string) {
        this.name = name;
        let testFile = readFileSync(path + "/" + name, 'utf8'),
            lines = testFile.split(/\r?\n/);

        lines.forEach((fin, i) => {
            if (fin.startsWith("\tname:")) {
                this.evaluateSet();
                name = fin.split(":", 2)[1];
                this.currentCase = null;
                this.cases = [];
                this.mode = TestFileState.None;
            }
            else if (fin.startsWith("\t#")) return; // comment
            else if (fin === "\tin") {
                if (this.mode === TestFileState.Code) this.evaluateSet();
                this.ensureCurrentCase();
                this.currentCase!.io.push({ in: [], expected: [] });
                this.mode = TestFileState.In;
            }
            else if (fin === "\tout") {
                if (this.mode === TestFileState.Code) this.evaluateSet();
                this.ensureCurrentCase();
                if (this.mode === TestFileState.Out || this.mode === TestFileState.Code) this.currentCase!.io.push({ in: [], expected: [] });
                this.mode = TestFileState.Out;
            }
            else if (fin === "\tstax") {
                this.ensureCurrentCase();
                this.mode = TestFileState.Code;
            }
            else {
                switch (this.mode) {
                    case TestFileState.In:
                        _.last(this.currentCase!.io)!.in.push(fin);
                        break;
                    case TestFileState.Out:
                        _.last(this.currentCase!.io)!.expected.push(fin);
                        break;
                    case TestFileState.Code:
                        this.currentCase!.programs.push({ line: i + 1, code: fin });
                        break;
                }
            }
        });
    }

    runCases(cases: TestCase[]) {
        for (let c of cases) {
            for (let prog of c.programs) {
                for (let io of c.io) {
                    ++this.attempts;
                    let output: string[] = [];
                    var rt = new Runtime(output.push.bind(output));

                    try {
                        for (let s of rt.runProgram(prog.code, io.in));
                    }
                    catch (e) {
                        console.error(e);
                        console.error(`Code(${prog.line}): ${prog.code}`);
                        continue;
                    }

                    let expectedFlat = io.expected.join("\n").replace(/[\r\n]+$/, "");
                    let outputFlat = output.join("\n").replace(/[\r\n]+$/, "");
                    if (expectedFlat === outputFlat) {
                        ++this.passed;
                    } else {
                        console.error("Expected:");
                        for (let e of io.expected) console.error(e);
                        console.error("Got:");
                        for (let o of output) console.error(o);
                        console.error(`Code(${prog.line}): ${prog.code}`);
                        console.error();
                    }
                }
            }
        }
    }

    evaluateSet() {
        if (this.currentCase) this.cases.push(this.currentCase);
        this.runCases(this.cases);
        this.currentCase = null;
        this.cases = [];
    }

     ensureCurrentCase() {
        if (!this.currentCase) this.currentCase = { io: [], programs: [], name:this.name };
    }
}

let testFiles: File[] = [];
let path = process.argv[2];
let tests = readdirSync(path, 'utf8')
    .filter(file => file.match(/.*staxtest/))
    .map(file => new TestFiles(path, file));
tests.forEach(test => test.evaluateSet());

let totPassed = tests.reduce((accumulator, current) => accumulator + current.passed, 0);
let totAttempts = tests.reduce((accumulator, current) => accumulator + current.attempts, 0);

console.log(`Total Passed: ${ totPassed }/${ totAttempts }`);
process.exit();
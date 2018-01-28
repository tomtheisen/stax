import { Block, parseProgram } from './block';
import { Runtime, ExecutionState } from './stax';
import * as _ from 'lodash';
import * as fs from 'fs';
import * as path from 'path';

const TimeoutMs = 2000;

class TestCase {
    name: string;
    io: {in: string[], expected: string[]}[] = [];
    programs: { line: number, code: string }[] = [];
}

enum TestFileState { Name, In, Out, Code }

class TestFiles{
    cases: TestCase[] = [];
    name: string = '';//test name
    attempts: number = 0;
    passed: number = 0;

    constructor(fullPath: string) {
        this.name = path.basename(fullPath);
        let testFile = fs.readFileSync(fullPath, 'utf8'),
            lines = testFile.split(/\r?\n/);
        this.cases = this.parse(lines);
    }

    private parse(lines: string[]): TestCase[] {
        let mode = TestFileState.Name;
        let currentCase = new TestCase();
        let cases: TestCase[] = [];

        lines.forEach((fin, i) => {
            if (fin.startsWith("\tname:")) {
                cases.push(currentCase = new TestCase());
                currentCase.name = fin.split(":", 2)[1];
                mode = TestFileState.Name;
            }
            else if (fin.startsWith("\t#")) return; // comment
            else if (fin === "\tin") {
                if (mode === TestFileState.Code) {
                    let lastName = currentCase.name;
                    cases.push(currentCase = new TestCase);
                    currentCase.name = lastName;
                }
                currentCase.io.push({ in: [], expected: [] });
                mode = TestFileState.In;
            }
            else if (fin === "\tout") {
                if (mode === TestFileState.Code) {
                    let lastName = currentCase.name;
                    cases.push(currentCase = new TestCase);
                    currentCase.name = lastName;
                }
                if(!currentCase.io.length)
                    currentCase.io.push({ in: [], expected: [] });
                mode = TestFileState.Out;
            }
            else if (fin === "\tstax") {
                mode = TestFileState.Code;
            }
            else {
                switch (mode) {
                    case TestFileState.In:
                        _.last(currentCase!.io)!.in.push(fin);
                        break;
                    case TestFileState.Out:
                        _.last(currentCase!.io)!.expected.push(fin);
                        break;
                    case TestFileState.Code:
                        currentCase!.programs.push({ line: i + 1, code: fin });
                        break;
                }
            }
        });
        cases.push(currentCase);
        return cases;        
    }

    runCases() {
        console.log("Starting Test Suite: "+this.name);
        for (let c of this.cases) {
            for (let prog of c.programs) {
                for (let io of c.io) {
                    ++this.attempts;
                    let output: string[] = [];
                    var rt = new Runtime(output.push.bind(output));

                    try {
                        let i = 0, start = (new Date).valueOf();
                        for (let s of rt.runProgram(prog.code, io.in)) {
                            if (++i % 1000 === 0 && ((new Date).valueOf() - start) > TimeoutMs) {
                                throw new Error(`Timeout in ${ i } steps`);
                            }
                        }
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
        console.log("Attempts: " + this.attempts + " Passed: " + this.passed);
    }
}

function allFiles(dir: string): string[] {
    if (fs.statSync(dir).isFile()) return [dir];
    let result: string[] = [];
    for (let f of fs.readdirSync(dir)) {
        let joined = path.join(dir, f);
        if (fs.statSync(joined).isDirectory()) {
            result.push(...allFiles(joined));
        }
        else if (f.match(/.*\.staxtest/)) {
            result.push(joined);
        }
    }
    return result;
}

let testFiles: File[] = [];
let argpath = process.argv[2];
let tests = allFiles(argpath).map(file => new TestFiles(file));
tests.forEach(test => test.runCases());

let totPassed = tests.reduce((accumulator, current) => accumulator + current.passed, 0);
let totAttempts = tests.reduce((accumulator, current) => accumulator + current.attempts, 0);

console.log(`Total Passed: ${ totPassed }/${ totAttempts }`);
process.exit();
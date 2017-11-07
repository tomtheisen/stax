import { Block, parseProgram } from './block';
import { Runtime, ExecutionState } from './stax';
import * as _ from 'lodash';
import { readFileSync } from 'fs';

interface TestCase {
    name: string;
    io: {in: string[], expected: string[]}[];
    programs: {line: number, code: string}[];
}

enum TestFileState { None, In, Out, Code }

let filename = process.argv[2],
name = filename,
testFile = readFileSync(filename, 'utf8'), 
lines = testFile.split(/\r?\n/),
mode = TestFileState.None,
cases: TestCase[] = [],
currentCase: TestCase | null = null;

let attempts = 0, passed = 0;
function runCases(cases: TestCase[]) {
    for (let c of cases) {
        for (let prog of c.programs) {
            for (let io of c.io) {
                ++attempts;
                let output: string[] = [];
                var rt = new Runtime(output.push.bind(output));
                
                try {
                    for (let s of rt.runProgram(prog.code, io.in)) ;
                }
                catch (e) {
                    console.error(e);
                    console.error(`Code(${prog.line}): ${prog.code}`);
                    continue;
                }
                
                let expectedFlat = io.expected.join("\n").replace(/[\r\n]+$/, "");
                let outputFlat = output.join("\n").replace(/[\r\n]+$/, "");
                if (expectedFlat === outputFlat) {
                    ++passed;
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

function evaluateSet() {
    if (currentCase) cases.push(currentCase);
    runCases(cases);
    currentCase = null;
    cases = [];
}
function ensureCurrentCase() {
    if (!currentCase) currentCase = { io: [], programs: [], name };
}

lines.forEach((fin, i) => {
    if (fin.startsWith("\tname:")) {
        evaluateSet();
        name = fin.split(":", 2)[1];
        currentCase = null;
        cases = [];
        mode = TestFileState.None;
    }
    else if (fin.startsWith("\t#")) return; // comment
    else if (fin === "\tin") {
        if (mode === TestFileState.Code) evaluateSet();
        ensureCurrentCase();
        currentCase!.io.push({in: [], expected: []});
        mode = TestFileState.In;
    }
    else if (fin === "\tout") {
        if (mode === TestFileState.Code) evaluateSet();
        ensureCurrentCase();
        if (mode === TestFileState.Out) currentCase!.io.push({in: [], expected: []});
        mode = TestFileState.Out;
    }
    else if (fin === "\tstax") {
        ensureCurrentCase();
        mode = TestFileState.Code;
    }
    else {
        switch(mode) {
            case TestFileState.In:
            _.last(currentCase!.io)!.in.push(fin);
            break;
            case TestFileState.Out:
            _.last(currentCase!.io)!.expected.push(fin);
            break;
            case TestFileState.Code:
            currentCase!.programs.push({line: i + 1, code: fin});
            break;
        }
    }
});
evaluateSet();
console.log(`Passed: ${ passed }/${ attempts }`);

process.exit();

// var rt = new Runtime(o => console.log(o));
// for (let s of rt.runProgram('"foo"P 32!q!P')){
//   //lol
// }

//var stdin = process.openStdin();
/*
stdin.addListener("data", function(d) {
    // note:  d is an object, and when converted to a string it will
    // end with a linefeed.  so we (rather crudely) account for that  
    // with toString() and then trim() 
    let prog = d.toString().trim();
    let block = parseProgram(prog);
    console.log(`you entered: [${prog}]`);
    block.tokens.forEach(_ => console.log(_));
});
*/

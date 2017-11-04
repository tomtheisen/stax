import { Block, parseProgram } from './block';
import { Runtime, ExecutionState } from './stax';
import * as _ from 'lodash';
import { readFile } from 'fs';

var stdin = process.openStdin();

for (let arg of process.argv) {
  console.log(arg);
}

var rt = new Runtime(o => console.log(o));
for (let s of rt.runProgram('"foo"P 32!q!P')){
  //lol
}

let filename = process.argv[2];
readFile(filename, (err, data) => {});

process.exit();

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

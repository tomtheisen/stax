import {Block, parseProgram} from './block';

var stdin = process.openStdin();

stdin.addListener("data", function(d) {
    // note:  d is an object, and when converted to a string it will
    // end with a linefeed.  so we (rather crudely) account for that  
    // with toString() and then trim() 
    let prog = d.toString().trim();
    let block = parseProgram(prog);
    console.log(`you entered: [${prog}]`);
    block.tokens.forEach(_ => console.log(_));
  });
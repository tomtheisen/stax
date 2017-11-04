import * as _ from 'lodash';

export class Block {
    contents: string;
    tokens: (string | Block)[];
    explicitlyTerminated = false;
    get length(): number { return this.contents.length; }
    
    constructor(contents: string, tokens: (string | Block)[], explicitlyTerminated = false) {
        this.contents = contents;
        this.tokens = tokens;
        this.explicitlyTerminated = explicitlyTerminated;
    }
}

export class Program extends Block {
    gotoTargets: number[];
    
    constructor(contents: string, tokens: (string | Block)[], gotoTargets: number[]) {
        super(contents, tokens);
        this.gotoTargets = gotoTargets;
    }
}

export function parseProgram(program: string): Program {
    return parseCore(program, true);
}

function parseCore(program: string, wholeProgram: true): Program;
function parseCore(program: string, wholeProgram: false): Block;
function parseCore(program: string, wholeProgram: boolean): Block {
    let gotoTargets = [0];
    let tokens: (string | Block)[] = [];
    let pos = 0;
    if (!wholeProgram) {
        console.assert(program[0] === "{");
        pos = 1;
    }

    while (pos < program.length) {
        if (!wholeProgram && "wWmfFkKgo".indexOf(program[pos]) >= 0) {
            return new Block(program.substr(0, pos), tokens, false);
        }

        switch (program[pos]) {
            case 'V':
            case '|':
            case ':':
                tokens.push(program.substr(pos, 2));
                pos += 2;
                break;

            case '0': case '1': case '2': case '3': case '4': 
            case '5': case '6': case '7': case '8': case '9': 
                let n = parseNum(program, pos);
                pos += n.length;
                tokens.push(n);
                break;

            case '"':
                let s = parseString(program, pos);
                pos += s.length;
                tokens.push(s);
                break;

            case '`':
                let c = parseCompressedString(program, pos);
                pos += c.length;
                tokens.push(c);
                break;

            case '{':
                let b = parseCore(program.substr(pos), false);
                pos += b.contents.length;
                tokens.push(b);
                break;

            case '}':
                if (wholeProgram) {
                    tokens.push("}");
                    gotoTargets.push(++pos);
                }
                else {
                    return new Block(program.substr(0, ++pos), tokens, true);
                }
                break;

            default:
                tokens.push(program[pos++]);
                break;
        }
    }

    if (wholeProgram) return new Program(program, tokens, gotoTargets);
    return new Block(program, tokens);
}

function parseNum(program: string, pos: number): string {
    let matches = program.substr(pos).match(/^[1-9]\d*|\d+!\d*|0/);
    if (!matches) throw "tried to parse a number out of a non-number";
    return matches[0] === "10" ? "1" : matches[0];
}

function parseString(program: string, pos: number) {
    let matches = program.substr(pos).match(/^"([^`"]|`([V:|].|[^V:|]))*("|$)/);
    if (!matches) throw "tried to parse a string, but no quote";
    return matches[0];
}

function parseCompressedString(program: string, pos: number) {
    let matches = program.substr(pos).match(/`[^`]+(`|$)/);
    if (!matches) throw "tried to parse a compressed string, but no backtick";
    return matches[0];
}
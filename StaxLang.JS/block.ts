import * as _ from 'lodash';

export class Block {
    contents: string;
    tokens: (string | Block)[];
    explicitlyTerminated = false;
    get length(): number { return this.contents.length; }
    
    constructor(contents: string | null, tokens: (string | Block)[], explicitlyTerminated = false) {
        if (contents == null) contents = tokens.map(t => t instanceof Block ? t.contents : t).join("");
        this.contents = contents;
        this.tokens = tokens;
        this.explicitlyTerminated = explicitlyTerminated;
    }
}

export class Program extends Block {
    private gotoTargets: Block[];
    
    constructor(contents: string, tokens: (string | Block)[], gotoTargets: Block[]) {
        super(contents, tokens);
        this.gotoTargets = gotoTargets;
    }

    getGotoTarget(callDepth: number): Block {
        return this.gotoTargets[callDepth] || _.last(this.gotoTargets) || this;
    }
}

export function parseProgram(program: string): Program {
    return parseCore(program, true);
}

function parseCore(program: string, wholeProgram: true): Program;
function parseCore(program: string, wholeProgram: false): Block;
function parseCore(program: string, wholeProgram: boolean): Block {
    let blockTokens: (string | Block)[] = [];
    let gotoTargets: (string | Block)[][] = [];

    function pushToken(token: string | Block) {
        (_.last(gotoTargets) || blockTokens).push(token);
    }

    let pos = 0;
    if (!wholeProgram) {
        pos = 1;
    }

    while (pos < program.length) {
        if (!wholeProgram && "wWmfFkKgo".indexOf(program[pos]) >= 0) {
            return new Block(program.substr(0, pos), blockTokens, false);
        }

        switch (program[pos]) {
            case 'V':
            case '|':
            case ':':
            case "'":
                pushToken(program.substr(pos, 2));
                pos += 2;
                break;

            case '.':
                pushToken(program.substr(pos, 3));
                pos += 3;
                break;

            case '0': case '1': case '2': case '3': case '4': 
            case '5': case '6': case '7': case '8': case '9': 
                let n = parseNum(program, pos);
                pos += n.length;
                pushToken(n);
                break;

            case '"':
                let s = parseString(program, pos);
                pos += s.length;
                pushToken(s);
                break;

            case '`':
                let c = parseCompressedString(program, pos);
                pos += c.length;
                pushToken(c);
                break;

            case '{':
                let b = parseCore(program.substr(pos), false);
                pos += b.contents.length;
                pushToken(b);
                break;

            case '}':
                if (wholeProgram) {
                    ++pos;
                    pushToken("}");
                    gotoTargets.push([]);
                }
                else {
                    return new Block(program.substr(0, ++pos), blockTokens, true);
                }
                break;

            default:
                pushToken(program[pos++]);
                break;
        }
    }

    if (wholeProgram) {
        let targets = gotoTargets.map(ts => new Block(null, ts));
        return new Program(program, blockTokens, targets);
    }
    return new Block(program, blockTokens);
}

function parseNum(program: string, pos: number): string {
    let matches = program.substr(pos).match(/^\d+!(\d*[1-9])?|[1-9]\d*|0/);
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
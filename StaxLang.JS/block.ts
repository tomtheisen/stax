import { last } from './types';
import { isPacked } from './packer';

export class Block {
    contents: string;
    tokens: (string | Block)[];
    offset: number;
    explicitlyTerminated = false;
    get length(): number { return this.contents.length; }

    constructor(contents: string | null, tokens: (string | Block)[], programOffset: number, explicitlyTerminated = false) {
        if (contents == null) contents = tokens.map(t => t instanceof Block ? t.contents : t).join("");
        this.contents = contents;
        this.tokens = tokens;
        this.explicitlyTerminated = explicitlyTerminated;
        this.offset = programOffset;
    }

    isEmpty(): boolean {
        for (let token of this.tokens) {
            if (typeof token === 'string') {
                if (/^\S/.exec(token[0])) return token[0] === '}';
            }
            else if (!token.isEmpty()) return false;
        }
        return true;
    }
}

export class Program extends Block {
    private gotoTargets: Block[];

    constructor(contents: string, tokens: (string | Block)[], gotoTargets: Block[]) {
        super(contents, tokens, 0);
        this.gotoTargets = gotoTargets;
    }

    getGotoTarget(callDepth: number): Block {
        return this.gotoTargets[callDepth - 1] || last(this.gotoTargets) || this;
    }

    getGotoTargetCount(): number {
        return this.gotoTargets.length;
    }
}

export enum CodeType {
    LooseAscii,         // all ascii with extra whitespace or comments
    LowAscii,           // ascii with no extra whitespace, but can't be packed, e.g. newline in string literal
    TightAscii,         // minified ascii, could be packed
    Packed,             // PackedStax, tightest representation known
    UnpackedNonascii,   // Unpackable, due to emojis or something
}

export function getCodeType(program: string) : CodeType {
    if (isPacked(program)) return CodeType.Packed;

    let lowAscii = false;
    for (let pos = 0; pos < program.length; pos++) {
        if (program.codePointAt(pos)! >= 0x80) return CodeType.UnpackedNonascii;
        if (program.codePointAt(pos)! < 0x20) lowAscii = true;
    }

    let extraWhitespace = false;
    for (let pos = 0; pos < program.length; pos++) {
        switch (program[pos]) {
            case ' ':
            case '\t':
            case '\r':
            case '\n':
                extraWhitespace = true;
                break;
            case ':':
            case '|':
            case "'":
                pos += 1;
                break;
            case '.':
                pos +=2 ;
                break;
            case '"':
                pos += parseString(program, pos).length - 1;
                break;
            case '`':
                pos += parseCompressedString(program, pos).length - 1;
                break;
        }
    }
    if (extraWhitespace) return CodeType.LooseAscii;
    return lowAscii ? CodeType.LowAscii : CodeType.TightAscii;
}

export function parseProgram(program: string): Program {
    return parseCore(program, 0, true);
}

function parseCore(program: string, programOffset: number, wholeProgram: true): Program;
function parseCore(program: string, programOffset: number, wholeProgram: false): Block;
function parseCore(program: string, programOffset: number, wholeProgram: boolean): Block {
    let blockTokens: (string | Block)[] = [];
    let gotoTargets: {offset: number, tokens: (string | Block)[]}[] = [];

    function pushToken(token: string | Block) {
        if (gotoTargets.length) last(gotoTargets)!.tokens.push(token);
        else blockTokens.push(token);
    }

    let pos = wholeProgram ? 0 : 1, firstInstruction = programOffset + pos;

    while (pos < program.length) {
        if (!wholeProgram && "wWmfFkKgo".indexOf(program[pos]) >= 0) {
            return new Block(program.substr(0, pos), blockTokens, firstInstruction, false);
        }

        switch (program[pos]) {
            case 'V':
            case '|':
            case ':':
            case 'g':
                pushToken(program.substr(pos, 2));
                pos += 2;
                break;

            case "'": {
                // test for surrogate pair
                let length = (program.charCodeAt(pos + 1) !== program.codePointAt(pos + 1)) ? 3 : 2;
                pushToken(program.substr(pos, length));
                pos += length;
                break;
            }

            case '.': {
                let length = 1;
                for (let i = 0; i < 2; i++, length++) {
                    if (program.charCodeAt(pos + length) !== program.codePointAt(pos + length)) length += 1;
                }
                pushToken(program.substr(pos, length));
                pos += length;
                break;
            }

            case ' ':
            case '\n':
                let token = program.substr(pos).match(/[ \n]+/)![0];
                pushToken(token);
                pos += token.length;
                break;

            case '\t': {
                let lineEnd = program.indexOf('\n', pos);
                if (lineEnd < 0) lineEnd = program.length - 1;
                pushToken(program.substring(pos, lineEnd + 1));
                pos = lineEnd + 1;
                break;
            }

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
                let b = parseCore(program.substr(pos), programOffset + pos, false);
                pos += b.contents.length;
                pushToken(b);
                // implicit block terminator characters
                if (program[pos] === "g") {
                    pushToken(program.substr(pos, 2));
                    pos += 2;
                }
                else if ("wWmfFkKo".indexOf(program[pos]) >= 0) {
                    pushToken(program[pos++]);
                }
                break;

            case '}':
                if (wholeProgram) {
                    pushToken("}");
                    gotoTargets.push({ offset: programOffset + ++pos, tokens: []});
                }
                else {
                    return new Block(program.substr(0, ++pos), blockTokens, firstInstruction, true);
                }
                break;

            default:
                pushToken(program[pos++]);
                break;
        }
    }

    if (wholeProgram) {
        let targets = gotoTargets.map(ts => new Block(null, ts.tokens, ts.offset));
        return new Program(program, blockTokens, targets);
    }
    return new Block(program, blockTokens, programOffset);
}

function parseNum(program: string, pos: number): string {
    let matches = program.substr(pos).match(/^\d+!(\d*[1-9])?|[1-9]\d*|0/);
    if (!matches) throw "tried to parse a number out of a non-number";
    return matches[0] === "10" ? "1" : matches[0];
}

function parseString(program: string, pos: number) {
    let matches = program.substr(pos).match(/^"([^`"]|`([V:|].|[^V:|]))*("!?|$)/);
    if (!matches) throw "tried to parse a string, but no quote";
    return matches[0];
}

function parseCompressedString(program: string, pos: number) {
    let matches = program.substr(pos).match(/`[^`]+(`|$)/);
    if (!matches) throw "tried to parse a compressed string, but no backtick";
    return matches[0];
}
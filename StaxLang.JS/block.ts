import { last, literalFor } from './types';
import { isPacked } from './packer';
import { decompress, compressLiteral } from './huffmancompression';
import { cramSingle, uncramSingle, uncram, cram } from './crammer';
import * as int from './integer';

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
    LooseAscii,              // all ascii with extra whitespace or comments
    LowAscii,                // ascii with no extra whitespace, but can't be packed, e.g. newline in string literal
    TightAscii,              // minified ascii, could be packed
    Packed,                  // PackedStax, tightest representation known
    UnpackedTightNonAscii,   // Unpackable, due to emojis or something
    UnpackedLooseNonAscii,   // Unpackable, due to emojis or something
}

export enum LiteralTypes {
    None                 = 0b00000000,
    CompressedString     = 0b00000001,
    CompressableString   = 0b00000010,
    UncompressableString = 0b00000100,
    CompressedInt        = 0b00001000,
    CompressableInt      = 0b00010000,
    UncompressableInt    = 0b00100000,
    CrammedArray         = 0b01000000,
    CrammableSequence    = 0b10000000,
}

export function getCodeType(program: string) : [CodeType, LiteralTypes] {
    if (isPacked(program)) return [CodeType.Packed, LiteralTypes.None];

    let lowAscii = false, highCodepoint = false, literals = LiteralTypes.None;
    for (let pos = 0; pos < program.length; pos++) {
        if (program.codePointAt(pos)! >= 0x7f) highCodepoint = true;
        if (program.codePointAt(pos)! < 0x20) lowAscii = true;
    }

    let extraWhitespace = false;
    for (let pos = 0; pos < program.length; pos++) {
        switch (program[pos]) {
            case '\t':
                pos = program.indexOf("\n", pos);
                if (pos < 0) pos = program.length - 1;
            case ' ':
            case '\r':
            case '\n':
                extraWhitespace = true;
                break;
            case 'V':
            case ':':
            case '|':
            case "'":
                pos += 1;
                break;
            case '.':
                pos +=2 ;
                break;
            case '0': case '1': case '2': case '3': case '4':
            case '5': case '6': case '7': case '8': case '9':
                let n = parseNum(program, pos);
                if (/^\d+$/.test(n)) {
                    const crammed = cramSingle(int.make(n));
                    if (crammed.length < n.length) literals |= LiteralTypes.CompressableInt;
                    else literals |= LiteralTypes.UncompressableInt;
                }
                pos += / \d/.test(program.substr(pos + n.length, 2))
                    ? n.length // skip one necessary space between numeric literals
                    : n.length - 1;
                break;
            case '"':
                let literal = parseString(program, pos);
                if (literal.endsWith('"%')) literals |= LiteralTypes.CompressedInt;
                else if (literal.endsWith('"!')) literals |= LiteralTypes.CrammedArray;
                else {
                    let contents = literal.replace(/^"|"$/g, "");
                    let compressable = !contents.match(/[^ !',-.:?ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz]/);
                    if (literal.endsWith('"') && literal.length <= 4) compressable = true;
                    else if (compressable) {
                        let compressed = compressLiteral(contents);
                        if (!compressed || compressed.length >= literal.length) compressable = false;
                    }
                    literals |= compressable
                        ? LiteralTypes.CompressableString
                        : LiteralTypes.UncompressableString;
                }
                pos += literal.length - 1;
                break;
            case '`':
                pos += parseCompressedString(program, pos).length - 1;
                literals |= LiteralTypes.CompressedString;
                break;
            case 'z':
                let expanded = program.slice(pos).match(/^z(?:(?:A|(?!10\+)[1-9]\d*)N?\+){2,}/);
                if (expanded) literals |= LiteralTypes.CrammableSequence;
        }
    }

    if (highCodepoint) {
        if (extraWhitespace) return [CodeType.UnpackedLooseNonAscii, literals];
        else return [CodeType.UnpackedTightNonAscii, literals];
    }
    if (extraWhitespace) return [CodeType.LooseAscii, literals];
    if (lowAscii) return [CodeType.LowAscii, literals];
    return [CodeType.TightAscii, literals];
}

export function compressLiterals(program: string): string {
    if (isPacked(program)) throw "not implemented for packed programs";

    let result = "";
    for (let pos = 0; pos < program.length; pos++) {
        switch (program[pos]) {
            case '\t':
                let newpos = program.indexOf("\n", pos);
                if (newpos < 0) newpos = program.length - 1;
                result += program.substring(pos, newpos + 1);
                pos = newpos;
                break;
            case 'V':
            case ':':
            case '|':
            case "'":
                result += program.substr(pos, 2);
                pos += 1;
                break;
            case '.':
                result += program.substr(pos, 3);
                pos += 2;
                break;
            case '0': case '1': case '2': case '3': case '4':
            case '5': case '6': case '7': case '8': case '9':
                let n = parseNum(program, pos);
                result += cramSingle(int.make(n));
                pos += n.length - 1;
                break;
            case '"': {
                let literal = parseString(program, pos), contents = literal.replace(/^"|"$/g, "");
                let compressable = !contents.match(/[^ !',-.:?ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz]/);
                if (literal.endsWith('"') && literal.length <= 4) {
                    switch (literal.length) {
                        case 2:
                            result += "z";
                            break;
                        case 3:
                            result += "'" + contents;
                            break;
                        case 4:
                            result += "." + contents;
                            break;
                    }
                }
                else if (compressable) {
                    if (literal.endsWith('"')) {
                        let compressed = compressLiteral(contents);
                        if (!compressed || compressed.length >= literal.length) compressable = false;
                        else result += compressed;
                    }
                    else {
                        let compressed = compressLiteral(contents);
                        if (!compressed || compressed.length >= contents.length) compressable = false;
                        else {
                            result += '`' + compressed;
                            if (literal.endsWith('"')) result += '`';
                        }
                    }
                }
                if (!compressable) result += literal;
                pos += literal.length - 1;
                break;
            }
            case '`': {
                let compressed = parseCompressedString(program, pos);
                result += compressed;
                pos += compressed.length - 1;
                break;
            }
            case 'z': {
                const expanded = program.slice(pos).match(/^z(?:(?:A|(?!10\+)[1-9]\d*)N?\+){2,}/);
                if (expanded) {
                   const ints = expanded[0]
                        .replace(/A/g, "10")
                        .replace(/(\d+)N/g, "-$1")
                        .replace(/^z|\+$/g, "")
                        .split('+')
                        .map(int.make);
                    result += cram(ints);
                    pos += expanded[0].length - 1;
                }
                else result += program[pos];
                break;
            }
            default:
                result += program[pos];
                break;
        }
    }
    return result;
}

export function decompressLiterals(program: string): string {
    if (isPacked(program)) throw "not implemented for packed programs";

    let result = "";
    for (let pos = 0; pos < program.length; pos++) {
        switch (program[pos]) {
            case 'V':
            case ':':
            case '|':
            case "'":
                result += program.substr(pos, 2);
                pos += 1;
                break;
            case '.':
                result += program.substr(pos, 3);
                pos += 2;
                break;
            case '"': {
                let literal = parseString(program, pos);
                if (literal.endsWith("%")) { // crammed scalar int
                    if (/\d!?$/.test(result)) result += ' '; // keep space between literals
                    result += uncramSingle(literal.replace(/^"|"%$/g, '')).toString();
                    if (/^[0-9!]/.test(program.substr(pos + literal.length))) result += ' ';
                }
                else if (literal.endsWith("!")) { // crammed int array
                    result += 'z';
                    for (let e of uncram(literal.replace(/^"|"!$/g, ''))) {
                        result += literalFor(e) + '+';
                    }
                }
                else result += literal;
                pos += literal.length - 1;
                break;
            }
            case '`': {
                let compressed = parseCompressedString(program, pos), contents = compressed.replace(/^`|`$/g, "");
                result += '"' + decompress(contents);
                if (compressed.substring(1).endsWith('`')) result += '"';
                pos += compressed.length - 1;
                break;
            }
            default:
                result += program[pos];
                break;
        }
    }
    return result;
}

export function squareLinesAndComments(program: string): string {
    let lines = program.split("\n").map(l => l.split(/\t/)[0].trimRight());
    const maxlen = Math.max(...lines.map(l => l.length));
    lines = lines.map(l => {
        while (l.length < maxlen) l += " "; // no dependency on pad-right lol
        return l + "\t";
    });
    return lines.join("\n");
}

export function hasNewLineInLiteral(program: string): boolean {
    if (isPacked(program)) return false;

    for (let pos = 0; pos < program.length; pos++) {
        switch (program[pos]) {
            case '\t':
                pos = program.indexOf("\n", pos);
                if (pos < 0) pos = program.length - 1;
                break;
            case 'V':
            case ':':
            case '|':
            case "'":
                if (program[++pos] == "\n") return true;
                break;
            case '.':
                if (program.substr(pos + 1, 2).includes("\n")) return true;
                pos += 2;
                break;
            case '"': {
                let literal = parseString(program, pos);
                if (literal.includes("\n")) return true;
                pos += literal.length - 1;
                break;
            }
            case '`':
                pos += parseCompressedString(program, pos).length - 1;
                break;
            default: break;
        }
    }
    return false;
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
    let matches = program.substr(pos).match(/^"([^`"]|`([V:|].|[^V:|]))*("[!%]?|$)/);
    if (!matches) throw "tried to parse a string, but no quote";
    return matches[0];
}

function parseCompressedString(program: string, pos: number) {
    let matches = program.substr(pos).match(/`[^`]*(`|$)/);
    if (!matches) throw "tried to parse a compressed string, but no backtick";
    return matches[0];
}
import { isInt } from './integer';
import { StaxValue, isArray } from './types';
import { Block } from './block';
import { Rational } from './rational';

type NodeChildren = {[key: string]: MacroTreeNode};

export class MacroTreeNode {
    code: string;
    deprecation?: string;
    children: NodeChildren | undefined;

    hasChildren() { 
        return this.children != null;
    }

    constructor(code?: string | null, deprecation?: string | undefined) {
        if (typeof code === 'string') this.code = code;
        else this.children = {};
        this.deprecation = deprecation;
    }

    addMacro(types: string, code: string, deprecation?: string) {
        // types: (a)rray, (b)lock, (f)raction, (i)nt, (r)eal
        if (types.length === 0) throw new Error("not enough types");
        if (types.length === 1) this.children![types[0]] = new MacroTreeNode(code, deprecation);
        else {
            let key = types[types.length - 1];
            if (!(key in this.children!)) this.children![key] = new MacroTreeNode;
            this.children![key].addMacro(types.substr(0, types.length - 1), code, deprecation);
        }
    }
}

export const macroTrees: NodeChildren = {};

interface Macro {
    alias: string;
    types: string;
    code: string;
    deprecation?: string;
}

function setup() {
    const macros: Macro[] = [
        { alias: "!", types: "ii", code: "|g1="},
        { alias: "!", types: "a", code: "c%{[|!m s{{+k}M"},
        { alias: "#", types: "i", code: "1!*"},
        { alias: "#", types: "f", code: "1!*"},
        { alias: "#", types: "r", code: ""},
        { alias: "0", types: "a", code: "{Cim"},
        { alias: "1", types: "a", code: "{!Cim"},
        { alias: "1", types: "i", code: "2|E1#"},
        { alias: "2", types: "i", code: "2|L@"},
        { alias: "2", types: "f", code: "2|L@"},
        { alias: "2", types: "r", code: "2|L@"},
        { alias: "2", types: "a", code: "c|*"},
        { alias: "3", types: "a", code: "Vac13|)\\:fc^+|t"},
        { alias: "~", types: "a", code: "VaVA\\{+kcr+|t"},
        { alias: "~", types: "i", code: "c0>{|B%|2v}{UT}?"},
        { alias: "@", types: "a", code: "{f%"},
        { alias: "@", types: "ii", code: "|>2%"},
        { alias: "@", types: "ai", code: "n%%b|@aa@"},
        { alias: "$", types: "ai", code: "c{sM]*{{+K{+kk}{dz]}?"},
        { alias: "$", types: "a", code: "c{{Mm{{+K{+kk}z]?"},
        { alias: ":", types: "ai", code: "/{hm"},
        { alias: ":", types: "ii", code: "|2S"},
        { alias: ":", types: "ia", code: "{|2SF"},
        { alias: "/", types: "ii", code: "ssn{0~{b%Csn/s,^~Wdd,}{ddVI}?"},
        { alias: "/", types: "ai", code: "n%NcN:cc0<{n%+}Mb(aat", deprecation: "<code>:/</code> for split-at is deprecated.  Use <code>%</code> instead."},
        { alias: "/", types: "ia", code: "sn%NcN:cc0<{n%+}Mb(aat", deprecation: "<code>:/</code> for split-at is deprecated.  Use <code>s%</code> instead."},
        { alias: "/", types: "aa", code: "b[Is%^%~n,(~%t;%t,s"},
        { alias: "\\", types: "aa", code: "|\\{E=Cim"},
        { alias: "=", types: "aa", code: "|\\{E=!Cim"},
        { alias: "|", types: "a", code: "|ZM{|<mM", deprecation: "<code>:|</code> for column-align is deprecated.  Use <code>:&lt;</code> instead."},
        { alias: "_", types: "ii", code: "1!*/"},
        { alias: "_", types: "if", code: "1!*/"},
        { alias: "_", types: "fi", code: "1!*/"},
        { alias: "_", types: "ff", code: "1!*/"},
        { alias: "_", types: "a", code: "css|g1T~{;/m,d" },
        { alias: "*", types: "a", code: "O{*F"},
        { alias: "+", types: "f", code: "c0>s0<-"},
        { alias: "+", types: "r", code: "c0>s0<-"},
        { alias: "+", types: "i", code: "c0>s0<-"},
        { alias: "+", types: "a", code: "{+}C"},
        { alias: "<", types: "a", code: "|ZM{|<mM"},
        { alias: ">", types: "a", code: "|ZM{|>mM"},
        { alias: "^", types: "a", code: "co="},
        { alias: "[", types: "aa", code: "~;%(,="},
        { alias: "[", types: "ii", code: "|2NvI"},
        { alias: "[", types: "ia", code: "{|2NvIF"},
        { alias: "]", types: "aa", code: "~;%),="},
        { alias: "]", types: "ii", code: "|2M"},
        { alias: "]", types: "ia", code: "{|2MF"},
        { alias: "(", types: "a", code: "c%r{[|(msd"},
        { alias: ")", types: "a", code: "c%r{[|)msd"},
        { alias: "{", types: "a", code: "'(s+')+"},
        { alias: "{", types: "i", code: "$'(s+')+"},
        { alias: "{", types: "r", code: "$'(s+')+"},
        { alias: "{", types: "f", code: "$'(s+')+"},
        { alias: "}", types: "a", code: "'[s+']+"},
        { alias: "}", types: "i", code: "$'[s+']+"},
        { alias: "}", types: "r", code: "$'[s+']+"},
        { alias: "}", types: "f", code: "$'[s+']+"},
        { alias: "-", types: "ii", code: "-|a"},
        { alias: "-", types: "ir", code: "-|a"},
        { alias: "-", types: "if", code: "-|a"},
        { alias: "-", types: "ri", code: "-|a"},
        { alias: "-", types: "rr", code: "-|a"},
        { alias: "-", types: "rf", code: "-|a"},
        { alias: "-", types: "fi", code: "-|a"},
        { alias: "-", types: "fr", code: "-|a"},
        { alias: "-", types: "ff", code: "-|a"},
        { alias: "-", types: "a", code: "2B{Es-m"},
        { alias: ".", types: "a", code: "v\"\\b.\"{^}R"},
        { alias: ",", types: "aa", code: "rsrs|\\r"},
        { alias: "a", types: "a", code: "c|m|I"},
        { alias: "a", types: "ii", code: "s|Bs|z"},
        { alias: "A", types: "a", code: "c|M|I"},
        { alias: "A", types: "i", code: "A|L@"},
        { alias: "A", types: "f", code: "A|L@"},
        { alias: "A", types: "r", code: "A|L@"},
        { alias: "b", types: "iii", code: "a~;>s,>!*"},
        { alias: "b", types: "fii", code: "a~;>s,>!*"},
        { alias: "b", types: "rii", code: "a~;>s,>!*"},
        { alias: "b", types: "a", code: "2|E"},
        { alias: "B", types: "ia", code: "~;%|E{;@m,d"},
        { alias: "B", types: "i", code: "2|E"},
        { alias: "B", types: "aa", code: "s{]ni@*mzs+{+ksd"},
        { alias: "c", types: "iii", code: "atT"},
        { alias: "c", types: "rii", code: "atT1!*"},
        { alias: "c", types: "ai", code: "svc^?"},
        { alias: "c", types: "a", code: "{[?k"},
        { alias: "C", types: "a", code: "{{f|cc{Dms{hmgC" },
        { alias: "C", types: "i", code: "~;H;|C,^/"},
        { alias: "d", types: "i", code: "c|a{[%!fsd"},
        { alias: "d", types: "a", code: "oc%vh~;t,Tc|+s%u*"},
        { alias: "D", types: "ai", code: "~;|w,|W"},
        { alias: "D", types: "aa", code: "~;|w,|W"},
        { alias: "e", types: "a", code: "|]{|[mc{{+k}M"},
        { alias: "E", types: "a", code: "0U\\@"},
        { alias: "f", types: "a", code: "zs{+F"},
        { alias: "f", types: "i", code: "|f|R"},
        { alias: "F", types: "i", code: "|fu"},
        { alias: "F", types: "a", code: "{Cim", deprecation: "<code>:F</code> for falsy indices is deprecated.  Use <code>:0</code> instead."},
        { alias: "g", types: "a", code: "|R{hm"},
        { alias: "g", types: "i", code: "c{2:/|2}M"},
        { alias: "G", types: "a", code: "|R{Hm"},
        { alias: "G", types: "i", code: "2|E|22|E"},
        { alias: "h", types: "a", code: "'<s+'>+"},
        { alias: "h", types: "ii", code: "s|Hs|z"},
        { alias: "i", types: "a", code: "{f{|cBB~]{f+,gC"},
        { alias: "I", types: "aa", code: "{[Imsd"},
        { alias: "J", types: "a", code: "c%|Qe~;J(,/"},
        { alias: "J", types: "ii", code: "JsJs"},
        { alias: "J", types: "ir", code: "JsJs"},
        { alias: "J", types: "if", code: "JsJs"},
        { alias: "J", types: "ri", code: "JsJs"},
        { alias: "J", types: "rr", code: "JsJs"},
        { alias: "J", types: "rf", code: "JsJs"},
        { alias: "J", types: "fi", code: "JsJs"},
        { alias: "J", types: "fr", code: "JsJs"},
        { alias: "J", types: "ff", code: "JsJs"},
        { alias: "m", types: "ai", code: "0Tbs%/^a*s("},
        { alias: "m", types: "ii", code: "~;|%10?+,*"},
        { alias: "m", types: "a", code: "cr+"},
        { alias: "M", types: "a", code: "cu{[]|&%i_3lm|MHsd"},
        { alias: "n", types: "ii", code: "vs~z]s{{cO{*F;s/c|a{[%!fsd{ns+msdmzs{+F}*{;_{/F+m,d"},
        { alias: "N", types: "ii", code: "c2%{s|as}Mb|Ns#="},
        { alias: "o", types: "aa", code: "Vi|\\{|Mm"},
        { alias: "p", types: "i", code: "v{|p}{vgs"},
        { alias: "p", types: "a", code: "c{%mVI+|ms{[)msd"},
        { alias: "P", types: "i", code: "{|p}{gs"},
        { alias: "P", types: "a", code: "c{%mVI+|ms{[(msd"},
        { alias: "r", types: "aaa", code: "aa/s*"},
        { alias: "r", types: "i", code: "|aNcN^|r"},
        { alias: "R", types: "a", code: "r\"())([]][{}}{<>><\\//\\\"|t"},
        { alias: "s", types: "a", code: "c|Ms|m-"},
        { alias: "S", types: "aa", code: "s-!"},
        { alias: "t", types: "aa", code: "2|*|(|t"},
        { alias: "t", types: "i", code: "c|fu{u1-N*FuR"},
        { alias: "T", types: "a", code: "{!Cim", deprecation: "<code>:T</code> for truthy indices is deprecated.  Use <code>:1</code> instead."},
        { alias: "T", types: "i", code: "c^*h", deprecation: "<code>:T</code> for triangular numbers is deprecated.  Use <code>|+</code> instead."},
        { alias: "u", types: "a", code: "u%1="},
        { alias: "v", types: "a", code: "cor="},
        { alias: "V", types: "a", code: "c%us|+*"},
        { alias: "w", types: "a", code: "c1T:R+"},
        { alias: "W", types: "a", code: "c:R+"},
        { alias: "x", types: "a", code: "\"[$()*+.?[\\\\\\]^{|}]\" \"\\$&\"R"},
    ];

    for (let macro of macros) {
        if (!(macro.alias in macroTrees)) macroTrees[macro.alias] = new MacroTreeNode;
        macroTrees[macro.alias].addMacro(macro.types, macro.code, macro.deprecation);
    }
}
setup();

export function getTypeChar(o: StaxValue): string {
    if (isArray(o)) return 'a';
    if (o instanceof Block) return 'b';
    if (o instanceof Rational) return 'f';
    if (isInt(o)) return 'i';
    if (typeof o === 'number') return 'r';
    throw new Error('no type char for unknown type');
}
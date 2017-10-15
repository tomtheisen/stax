# Packed Stax
Stax is written using the 95 printable ascii characters.  These are easy to type on most keyboards, but for golfing applications, this is pretty wasteful.  Therefore, stax code has an alternative representation called "PackedStax".  They can be executed by the same interpreter with no additional switches or settings.  The high bit of the first byte is the toggle for detecting the difference.  Converting to packed stax representation reduces code bloat by about 18%.

## How does it work?
The stax code is converted to a big integer using the ascii characters as base 95.  Then the number is converted to base 256, which gives a byte array.  There's a little extra piece to make sure leading 0s aren't lost.  

## Portability
For display purposes, a clipboard-friendly variant of [CP437](https://en.wikipedia.org/wiki/Code_page_437#Character_set) is used to display packed stax.  00 and FF were replaced so they can survive a round-trip through a web browser and clipboard.

## Packed Stax character set
This is the character set used to present packed stax.  The charcters themselves aren't meaningful.  It's identical to CP437 except for 00 and FF.
|\_0|\_1|\_2|\_3|\_4|\_5|\_6|\_7|\_8|\_9|\_a|\_b|\_c|\_d|\_e|\_f
0\_|ø|☺|☻|♥|♦|♣|♠|•|◘|○|◙|♂|♀|♪|♫|☼
1\_|►|◄|↕|‼|¶|§|▬|↨|↑|↓|→|←|∟|↔|▲|▼
2\_| |!|"|#|$|%|&|'|(|)|\*|+|,|-|.|/
3\_|0|1|2|3|4|5|6|7|8|9|:|;|<|=|>|?
4\_|@|A|B|C|D|E|F|G|H|I|J|K|L|M|N|O
5\_|P|Q|R|S|T|U|V|W|X|Y|Z|[|\\|]|^|_
6\_|\`|a|b|c|d|e|f|g|h|i|j|k|l|m|n|o
7\_|p|q|r|s|t|u|v|w|x|y|z|{|||}|~|⌂
8\_|Ç|ü|é|â|ä|à|å|ç|ê|ë|è|ï|î|ì|Ä|Å
9\_|É|æ|Æ|ô|ö|ò|û|ù|ÿ|Ö|Ü|¢|£|¥|₧|ƒ
a\_|á|í|ó|ú|ñ|Ñ|ª|º|¿|⌐|¬|½|¼|¡|«|»
b\_|░|▒|▓|│|┤|╡|╢|╖|╕|╣|║|╗|╝|╜|╛|┐
c\_|└|┴|┬|├|─|┼|╞|╟|╚|╔|╩|╦|╠|═|╬|╧
d\_|╨|╤|╥|╙|╘|╒|╓|╫|╪|┘|┌|█|▄|▌|▐|▀
e\_|α|ß|Γ|π|Σ|σ|µ|τ|Φ|Θ|Ω|δ|∞|φ|ε|∩
f\_|≡|±|≥|≤|⌠|⌡|÷|≈|°|∙|·|√|ⁿ|²|■|Δ
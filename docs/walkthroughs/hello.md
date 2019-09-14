# Hello World Walkthrough

Let's write hello world.  Head over to https://staxlang.xyz, but keep this tab open.  In the "Code" box, enter this program:

    "Hello, world!"P

[Run it](https://staxlang.xyz/#c=%22Hello,+world%21%22P&i=&a=1)

Press "Run", and you should see the message in the output section.

There are two distinct instructions in this program. The first pushes a string literal to the [main stack](https://github.com/tomtheisen/stax/blob/master/docs/stacks.md#stacks). The subsequent `P` instruction pops the top value off the stack and prints it to standard output. You can observe the stack in action by pressing the "Step" button instead of "Run".

But just because you have a working program doesn't mean you're done. As a stax programmer, you are duty bound to make every program as small as possible.

## Remove `P`

Let's start with implicit output.  Any program that doesn't explicitly produce any output automatically prints the top of the stack when it finishes.  That means we can omit the final `P`

    "Hello, world!"

[Run it](https://staxlang.xyz/#c=%22Hello,+world%21%22&i=&a=1)

The literal still pushes to the stack, but the output now happens after the [last instruction is executed](https://github.com/tomtheisen/stax/blob/master/docs/execution.md#execution).

## Unterminated Literal

String literals at the end of a program can be unterminated.  When this happens, they're immediately printed.

    "Hello, world!

[Run it](https://staxlang.xyz/#c=%22Hello,+world%21&i=&a=1)

In this case, it's not relying on the implicit output.  An unterminated literal is considered to be an explicit output instruction.  So `"foo"P"bar` prints "foo" and "bar".

## Compressed Literal

String literals can sometimes be [compressed](https://github.com/tomtheisen/stax/blob/master/docs/compressed.md#compressed-strings). 

    `;Kp0TDt

[Run it](https://staxlang.xyz/#c=%60%3BKp0TDt&i=&a=1)

You don't have to memorize some goofy algorithm to write them though.  There are a couple of tools for generating compressed literals.  Click "≡ Tools" in the top right, and then "Source".  A number of context-sensitive buttons will be revealed.  "Compress Literals" will compress all possible string literals in-place in the current program.  It will also compress integer literals that can be shortened.

Additionally, for more speculative compression tasks, "String Literals" and "Integer/Array Literals" will compress values as you type without requiring changes to the current program source.

## Packed Stax

The last step for minimizing most stax program is usually packing.  Most ascii stax programs have an equivalent [packed-stax](https://github.com/tomtheisen/stax/blob/master/docs/packed.md#packed-stax) representation that's shorter.

    ö∩Å╟‼ñî


[Run it](https://staxlang.xyz/#p=94ef8fc713a48c&i=&a=1)

Under the source tools, click "Pack" or "Unpack" to convert back and forth.  Ascii programs that contain a tab or newline can't be packed.
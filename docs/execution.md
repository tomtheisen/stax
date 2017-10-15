# Execution

1. Unpack
2. Unannotate
3. Interpret special first characters
4. Implicit eval
5. Perform instructions
6. Output check

At the beginning each line from standard input is pushed to the input stack in reverse.  The top of the stack is the first line from input.

## Unpack
The high bit of the first byte of the program is checked.  If it's set, then the packed stax code is transformed into plain ASCII.

## Unannotate
Annotated code is executable directly.  But that only works in non-trivial cases because this step transforms it appropriately.  If the first character is a tab, then a reverse annotation transform is applied.  The effect is that extra whitespace, newlines, and comments are all removed.

## Special First Characters
Some instruction characters have special meaning when in the first position of a program.  These behaviors are explained in the Instructions section.

## Implicit Eval
When the standard input consists of exactly one line, then stax will attempt to automatically parse it and push the resulting values onto the stack.  For example, consider this standard input.

    "abc" [1 [2.3 4/5]]

The input stack will contain two arrays.  One represents the string, and the other will be the nested array structure of numbers.  Commas are optional.  Unrecognized characters cause the eval to fail.  If the eval fails, then the stack just keeps the string representation of standard input.

## Perform Instructions
This is where all the stuff happens.

## Output Check
After execution is complete, if no output has happened yet, a single popped value is printed.

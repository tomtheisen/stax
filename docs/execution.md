# Execution

1. Unpack
2. Interpret special first characters
3. Implicit eval
4. Perform instructions
5. Output check

At the beginning each line from standard input is pushed to the input stack in reverse.  The top of the stack is the first line from input.

## Unpack
The high bit of the first byte of the program is checked.  If it's set, then the packed stax code is transformed into plain ASCII.

## Special First Characters
Some instruction characters have special meaning when in the first position of a program.  These behaviors are explained in the Instructions section.

## Implicit Eval
Normally, the input stack starts with one element per line of standard input.  Implicit eval generally converts certain patterns of input into different kinds of values.  There are two kinds of inputs that can be evaled: single-line expressions, and multi-line string. To suppress this eval, start your program with `i`.

### Single-line Expressions
When the standard input consists of exactly one line, then stax will attempt to automatically parse it and push the resulting values onto the stack.  For example, consider this standard input.

    "abc" [1 ∞ [-2.3 4/5]]

The input stack will contain two arrays.  One represents the string, and the other will be the nested array structure of numbers.  Commas are optional.  Unrecognized characters cause the eval to fail.  If the eval fails, then the stack just keeps the string representation of standard input.  Integers, fractions, and floats are all interpreted successfully.  The infinity symbols `∞` and `-∞` are interpreted as the corresponding floating point values.

### Multi-line String
If the entire input is to be considered one string you can achieve this by using python-style triple quotes.

    """
    Dear Recipient,
    Your request has been denied.
        With Authority,
        Your compliance official
    """

When an input has at least two lines, and the first and last are triple quotes, the input stack will start with a single value which consists of the newline-joined string.

## Perform Instructions
This is where all the stuff happens.  After executing the program, if the top of the stack is a block, the block is executed.  This is repeated until the top of the stack is not a block.  This is mostly useful for recursion.

## Output Check
After execution is complete, if no output has happened yet, a single popped value is printed.

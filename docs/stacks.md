# Stacks
This is the thing the whole language was named after!  Stax has two stax; "main" and "input".  At the start of the program, all input is on the input stack.  When a pop is executed in the program, it will first be attempted on the main stack.  If it's empty, it will then try the input stack.  If that's empty too, the program will terminate.  Pushes always go to the main stack, except in the case of the `~` instruction.  There are a few other instructions that explicitly manipulate the input stack.  But usually, you can just treat it all as one stack.

## Other State
There are two global registers, `x`, and `y`.  There is also an implicit iteration variable `_` and iteration counter `i`.
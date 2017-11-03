# Generators
Generators create streams data.  They work best when each element is a function of the last.  Generators are always invoked using the `g` instruction, but there are lots of variations of how it can be used.

Generators can have these parts.

 * **Generator block** - generates the values
 * **Termination mode** - determines when to stop
 * **Value production mode** - controls how the values are retrieved from the generator block
 * **Filter block** - filters values produced by the generator block, if present

The `g` instruction is always followed by a mode specifier character which encodes the generator's modes.  Here is a lookup for the different mode specifiers.  Columns are value production modes, and rows are termination modes.  Pre-peek means that the first value produced by the generator is the one on top of the stack before it starts.  After each invokation of the generator block, the stack will be peeked again.

## Specifiers and Modes
Modes    	|Pre-peek	|Post-pop	|Termination Mode Description
---      	|---     	|---     	|---
Duplicate	|`u`     	|`U`     	|Stops when finding any duplicate value
Filter   	|`f`     	|`F`     	|Stops when any value fails the filter; a filter is required for this mode
Cancel   	|`c`     	|`C`     	|Stops on explicit cancellation only
Invariant	|`i`     	|`I`     	|Stops when finding two equal values subsequently
Fixpoint 	|`p`     	|`P`     	|Stops when finding two equal values subsequently; produces that single value, not an array
Target   	|`t`     	|`T`     	|Stops when finding the target value, which is popped before the generator begins
First    	|`s`     	|`S`     	|Stops after the first value;  produces a single value, not array
Index    	|`e`     	|`E`     	|Stops after finding the nth value;  produces a single value, not array - n is popped before the generator begins
Loop     	|`l`     	|`L`     	|Stops after finding any duplicate value - only keeps the portion that forms a loop
Count    	|`n`     	|`N`     	|Stops after finding n values - n is popped before the generator begins
Count 1  	|`1`     	|`!`     	|Shorthand for Count mode with n=1
Count 2  	|`2`     	|`@`     	|Shorthand for Count mode with n=2
Count 3  	|`3`     	|`#`     	|Shorthand for Count mode with n=3
Count 4  	|`4`     	|`$`     	|Shorthand for Count mode with n=4
Count 5  	|`5`     	|`%`     	|Shorthand for Count mode with n=5
Count 6  	|`6`     	|`^`     	|Shorthand for Count mode with n=6
Count 7  	|`7`     	|`&`     	|Shorthand for Count mode with n=7
Count 8  	|`8`     	|`*`     	|Shorthand for Count mode with n=8
Count 9  	|`9`     	|`(`     	|Shorthand for Count mode with n=9
Count 10 	|`10`    	|`)`     	|Shorthand for Count mode with n=10

## Generator Block
In shorthand mode, the rest of the program is treated as the generator block.  Shorthand mode is active when

 * There is no open block
 * **OR** `g` is directly following a `}`, in which case that curly brace closes the filter that will be used.

When shorthand mode is active, the generator's results are printed on separate lines.  When shorthand mode isn't active, the result will be pushed.

If the generator block is empty `{^}` will be used, which increments an integer.

## Example

Here is a program that prints the 5 largets prime numbers under 100, in descending order.

    100{|p}g5v

This produces

     97
     89
     83
     79
     73

Every part of this program is used by the generator.  Here's how.

 * `100` is the initial value produced by the generator block.  It could also be written as `AJ`. (ten squared)
 * `{|p}` is the filter block. `|p` means is-prime.
 * `g5` is the generator instruction and specifier.  `5` generate 5 count elements.
 * `v` is the generator block.  That means each value produced for the filter is 1 less than the previous.

Since `g` follows `}`, the generator is operating in shorthand mode.  It prints each resulting element on a separate line.


# Instructions

## Output
chars   	|Description
---     	|---
`p`     	|Pop and print with no newline.
`P`     	|Pop and print with a newline.
`q`     	|Peek and print with no newline.
`Q`     	|Peek and print with a newline.
`\|_`   	|(space, not underscore) Print a single space character.
``\|` ``	|Dump debug state.  Shows registers, iteration info, and both stacks.
`\|P`   	|Print newline.

## Stack
chars	|Example           	|Name         	|Description
---  	|----              	|----         	|--------------
`a`  	|… a b c -> … b c a	|alter-stack  	|Moves the third element in the stack to the top.
`b`  	|… a b -> … a b a b	|both-copy    	|Copies the top two stack elements
`c`  	|… a -> … a a      	|copy         	|Copy the top stack element
`d`  	|… a b -> … a      	|discard      	|Pop and discard top stack element
`n`  	|… a b -> … a b a  	|next         	|Copy second element to top of stack
`[`  	|… a b -> … a a b  	|under        	|Copy second element in-place
`~`  	|                  	|input-push   	|Pop top item, and push it to input stack.
`;`  	|                  	|input-peek   	|Peek from input stack, and push to main stack.
`,`  	|                  	|input-pop    	|Pop from input stack, and push to main stack.
`e`  	|arr               	|eval         	|Parse string as data to be pushed on the stack. e.g. [12, 3.4, [5/6, "a string"]]  Multiple top level values in the same string will be pushed separately.
`E`  	|arr               	|explode      	|Push all elements from array onto stack.
`L`  	|… -> […]          	|listify-stack	|Clear both stacks, and put all items in an array back on stack.
`l`  	|int               	|listify-n    	|Pop n items, and put them in an array on top of the stack.
`O`  	|… a -> … 1 a      	|tuck-1       	|Push the value 1 under the top element of the stack.
`s`  	|… a b -> … b a    	|swap         	|Swap top two stack elements.
`Z`  	|… a -> … 0 a      	|tuck-0       	|Push the value 0 under the top element of the stack.
`\|d`	|                  	|main-depth   	|Size of main stack.
`\|D`	|                  	|input-depth  	|Size of input stack.


## Numerics
chars        	|Types      	|Name          	|Pseudo-code        	|Description
---          	|---        	|---           	|---                	|---
`0123456789` 	|           	|              	|                   	|Integer literal.  Leading `0` is always separate number. Use `A` for 10. `10` is 1 and 0.
`A`          	|           	|              	|10                 	|Constant 10, as in hexidecimal.
`01234!56789`	|           	|              	|                   	|Float literal.  `!` indicates a decimal point.  A trailing `0` after a `!` is always a separate int literal.
`+`          	|num num    	|add           	|a + b              	|Add. Integers widen to fractions.  Fractions widen to floats.
`-`          	|num num    	|sub           	|a - b              	|Subtract.
`*`          	|num num    	|mul           	|a * b              	|Multiply.
`/`          	|num num    	|div           	|a / b              	|Integers will use integer division.
`%`          	|int int    	|mod           	|a % b              	|Modulus.
`@`          	|frac       	|floor         	|floor(a)           	|Integer floor of fraction.
`@`          	|float      	|floor         	|floor(a)           	|Integer floor of float.
`v`          	|num        	|dec           	|a - 1              	|Decrement by 1.
`^`          	|num        	|inc           	|a + 1              	|Increment by 1.
`e`          	|frac       	|ceil          	|ceiling(a)         	|Integer ceiling of fraction.
`e`          	|float      	|ceil          	|ceiling(a)         	|Integer ceiling of float.
`E`          	|int        	|dec-digits    	|                   	|Array of decimal digits.
`E`          	|frac       	|num-den       	|                   	|Push numerator and denominator separately.
`h`          	|int        	|halve         	|a / 2              	|Floor divide by 2.
`h`          	|float      	|halve         	|a / 2              	|Divide by 2.
`h`          	|frac       	|numerator     	|numerator(a)       	|Get the numerator.
`H`          	|int        	|un-halve      	|a * 2              	|Double.
`H`          	|float      	|un-halve      	|a * 2              	|Double.
`H`          	|frac       	|denom         	|denom(a)           	|Get the denominator.
`j`          	|float int  	|float-round   	|round(a, b)        	|Round float to n decimal places.  Format as string.
`J`          	|num        	|square        	|a ** 2             	|Square number.
`l`          	|frac       	|listify-frac  	|a/b -> [a b]       	|Turn a fraction into a 2-array of numerator and denominator.
`u`          	|int        	|fractionalize 	|1 / a              	|Turn integer upside down; into fraction.
`u`          	|frac       	|invert-frac   	|1 / a              	|Turn fraction upside down; invert.
`U`          	|           	|              	|-1                 	|Negative unit.
`\|%`        	|int int    	|divmod        	|a / b, a % b       	|Perform division and modulus.
`\|~`        	|int        	|bit-not       	|~a                 	|Bitwise not.
`\|&`        	|int int    	|bit-and       	|a & b              	|Bitwise and.
`\|\|`       	|int int    	|bit-or        	|a | b              	|Bitwise or.
`\|^`        	|int int    	|bit-xor       	|a ^ b              	|Bitwise xor.
`\|*`        	|int int    	|pow           	|a ** b             	|Exponent.	
`\|*`        	|frac int   	|pow           	|a ** b             	|Exponent.	
`\|/`        	|int int    	|div-all       	|                   	|Divide a by b as many times as it will go evenly.
`\|<`        	|int int    	|shift-left    	|a << b             	|Bitshift left.
`\|>`        	|int int    	|shift-right   	|a >> b             	|Bitshift right.
`\|1`        	|int        	|parity-sign   	|(-1) ** a          	|Power of negative one.
`\|2`        	|int        	|2-power       	|2 ** a             	|Power of two.
`\|3`        	|int        	|base-36       	|                   	|Convert to base 36.
`\|3`        	|arr        	|base-36       	|                   	|Convert from base 36.
`\|7`        	|num        	|cosine        	|cos(a)             	|Cosine in radians.
`\|8`        	|num        	|sine          	|sin(a)             	|Sine in radians.
`\|9`        	|num        	|tangent       	|tan(a)             	|Tangent in radians.
`\|a`        	|num        	|abs           	|abs(a)             	|Absolute value.
`\|A`        	|int        	|10-power      	|10 ** a            	|Power of ten.
`\|b`        	|int int    	|convert-base  	|                   	|Convert to base, up to 36.
`\|b`        	|arr int    	|convert-base  	|                   	|Convert from base, up to 36.
`\|B`        	|int        	|convert-binary	|                   	|Convert to base 2.
`\|B`        	|arr        	|convert-binary	|                   	|Convert from base 2.
`\|e`        	|int        	|is-even       	|(a + 1) % 2        	|Is even?
`\|E`        	|int int    	|digits        	|                   	|Generate array of digit values in base.
`\|F`        	|int        	|factorial     	|a!                 	|Factorial.
`\|g`        	|int int    	|gcd           	|                   	|Greatest common denominator.
`\|H`        	|int        	|base-16       	|                   	|Convert to base 16.
`\|H`        	|arr        	|base-16       	|                   	|Convert from base 16.
`\|l`        	|int int    	|lcm           	|                   	|Least common multiple.
`\|L`        	|num num    	|log-n         	|log(a, b)          	|Logarithm in base b of a.
`\|m`        	|num num    	|min           	|min(a, b)          	|Lower of two values.
`\|M`        	|num num    	|max           	|max(a, b)          	|Higher of two values.
`\|p`        	|int        	|is-prime      	|                   	|Is prime?
`\|q`        	|num        	|int-sqrt      	|floor(sqrt(abs(a)))	|Integer square root of absolute value.
`\|Q`        	|num        	|sqrt          	|sqrt(abs(a))       	|Float square root of absolute value.
`:-`         	|num num    	|abs-diff      	|abs(a - b)         	|Absolute difference.
`:/`         	|int int    	|multiplicity  	|                   	|Number of times b will evenly divide a.
`:1`         	|int        	|popcount      	|                   	|Count of set bits.
`:2`         	|num        	|floor-log-2   	|floor(log(a, 2))   	|Floor of log base 2.
`:A`         	|num        	|floor-log-10  	|floor(log(a, 10))  	|Floor of log base 10.
`:B`         	|int arr    	|custom-base   	|                   	|Encode number in custom base from string characters.
`:b`         	|num int int	|between       	|b <= a < c         	|Value is in range?
`:c`         	|num int int	|clamp         	|min(max(a, b), c)  	|Ensure value is in range.
`:f`         	|int        	|factorize-exp 	|                   	|Factorize into pairs of [prime, exponent].
`:F`         	|int        	|dist-factors  	|                   	|Distinct prime factors.
`:m`         	|int int    	|next-multiple 	|                   	|If necessary, increase a until it is a multiple of b

## Logic
chars	|Name        	|Description
---  	|---         	|---
`!`  	|not         	|Logical not.  Produces 0 or 1.  Numeric 0 and empty lists are considered falsy.  All other values are truthy.
`<`  	|less        	|Is less than.  Arrays use string-style lexicographic ordering.
`>`  	|greater     	|Is greater than.  Arrays use string-style lexicographic ordering.
`=`  	|equal       	|Equals.  Numberic types are coerced as necessary.
`?`  	|if-then-else	|If the first value, then yield the second, else the third.  If the result is a block, execute it.


## String
Strings are really just arrays of integers, but some operations are oriented towards strings anyway.  In these contexts, 0 is usually converted to 32, so that 0 can be used as a space in addition to its normal codepoint.

chars    	|Types      	|Name             	|Description
---      	|---        	|---              	|---
`#`      	|arr arr    	|count-substrings 	|Count occurrences of substring b in a.
`"…"`    	|           	|string-literal   	|String literal stored as an array of codepoints. `` ` `` is the escape character.  Unterminated string literals will be printed implicitly.
`` `…` ``	|           	|compressed-string	|Compressed string literal encoded with contextual Huffman trees.  Not all strings can be encoded this way, but most that can will be smaller.  Unterminated compressed literals will be printed implicitly.
`'a`     	|           	|char-literal     	|Create a single character string literal.
`.ab`    	|           	|two-char-literal 	|Create a two character string literal.
`/`      	|arr arr    	|split            	|Split on substrings.
`*`      	|arr int    	|repeat           	|Repeat string n times.  If n is negative, reverse array.
`*`      	|int arr    	|repeat           	|Repeat string n times.  If n is negative, reverse array.
`*`      	|arr arr    	|join             	|Join array of strings with delimiter.
`$`      	|num        	|tostring         	|Convert number to string.
`$`      	|arr        	|arr-tostring     	|Convert each element to string, and concat.
`v`      	|arr        	|lower            	|To lower case.
`^`      	|arr        	|upper            	|To upper case.
`I`      	|arr arr    	|substring-index  	|Get the index of the first occurrence of the substring.
`j`      	|arr        	|space-split      	|String split by space.
`J`      	|arr        	|space-join       	|Join strings by space.
`R`      	|arr arr arr	|regex-replace    	|Regex replace using ECMA regex.
`t`      	|arr        	|trim-left        	|Trim whitespace from left of string.
`T`      	|arr        	|trim-right       	|Trim whitespace from right of string.
`\|C`    	|arr int    	|center           	|Center string in n characters.
`\|C`    	|arr        	|center-block     	|Center lines of text, using longest line.
`\|I`    	|arr arr    	|str-index-all    	|Find all indexes of the substring.
`\|J`    	|arr        	|join-newline     	|Join strings with newline.
`\|s`    	|arr arr    	|regex-split      	|Split by ECMA regex. Captured groups will be included in result.
`\|S`    	|arr arr    	|surround         	|Prepend and append string/array.
`\|t`    	|arr arr    	|translate        	|Translate first string using pairs of elements in the second array.  Instances of the first in a pair will be replaced by the second.
`\|z`    	|arr int    	|zero-fill        	|Fill on the left with "0" to specified length.
`:[`     	|arr arr    	|starts-with      	|String starts with?
`:]`     	|arr arr    	|starts-with      	|String ends with?

## Array
chars	|Types        	|Name             	|Description
---  	|---          	|---              	|---
`#`  	|arr num      	|count-instances  	|Count instances of b in a.
`+`  	|arr arr      	|concat           	|Concatenate arrays.
`+`  	|num arr      	|prepend          	|Prepend element to array.
`+`  	|arr num      	|append           	|Append element to array.
`-`  	|arr arr      	|array-diff       	|Remove all elements in b from a.
`-`  	|arr num      	|array-remove     	|Remove all instances of b from a.
`/`  	|arr int      	|array-group      	|Split array into groups of specified size.  The last group will be smaller if it's not a multiple.
`%`  	|arr          	|length           	|Array length
`\`  	|num num      	|pair             	|Make a 2 length array.
`\`  	|arr num      	|array-pair       	|Make array of pairs, all having identical second element.
`\`  	|num arr      	|array-pair       	|Make array of pairs, all having identical first element.
`\`  	|arr arr      	|zip-repeat       	|Make array of pairs, zipped from two arrays.  The shorter is repeated as necessary.
`@`  	|arr int      	|element-at       	|Get element at 0-based modular index.  (-1 is the last element)
`@`  	|arr arr      	|elements-at      	|Get elements at all indices.
`&`  	|arr int any  	|assign-index     	|Assign element at index.
`&`  	|arr arr any  	|assign-indices   	|Assign element at all indices.
`&`  	|arr int block	|mutate-element   	|Mutate element at index using block.
`&`  	|arr arr block	|mutate-element   	|Mutate element at indices using block.
`(`  	|arr int      	|pad-right        	|Truncate or pad on right with 0s as necessary for target length.  Negative numbers remove that number of elements.
`)`  	|arr int      	|pad-left         	|Truncate or pad on left with 0s as necessary for target length.  Negative numbers remove that number of elements.
`]`  	|any          	|singleton        	|Make a 1 element array.
`B`  	|arr int      	|batch            	|Get all (overlapping) sub-arrays of specified length.
`B`  	|arr          	|uncons-left      	|Remove first element from array.  Push the tail of the array, then the removed element.
`h`  	|arr          	|first            	|Get first element.
`H`  	|arr          	|last             	|Get last element.
`I`  	|arr num      	|index-of         	|Get the index of the first occurrence.
`I`  	|num arr      	|index-of         	|Get the index of the first occurrence.
`N`  	|arr          	|uncons-right     	|Remove last element from array.  Push the beginning of the array, then the removed element.
`r`  	|int          	|0-range          	|Make range [0 .. n-1].
`r`  	|arr          	|reverse          	|Reverse array.
`R`  	|int          	|1-range          	|Make range [1 .. n].
`t`  	|arr int      	|remove-left      	|Trim n elements from left of array.
`T`  	|arr int      	|remove-right     	|Trim n elements from right of array.
`u`  	|arr          	|unique           	|Keep only unique elements in array, maintaining first order of appearance.
`z`  	|             	|                 	|Push empty array/string.
`\|0`	|arr          	|falsy-index      	|Get index of first falsy element.
`\|1`	|arr          	|truthy-index     	|Get index of first truthy element.
`\|+`	|arr          	|sum              	|Sum of array.
`\|@`	|arr int      	|remove-at        	|Remove element from array at index.
`\|@`	|arr int num  	|insert-at        	|Insert element to array at index.
`\|&`	|arr arr      	|arr-intersect    	|Keep all elements from a that are in b.
`\|^`	|arr arr      	|arr-mismatch     	|Keep all elements from a that are not in b, followed by all elements in b that are not in a.
`\|*`	|arr int      	|repeat-elements  	|Repeat each element n times.
`\|*`	|arr arr      	|cross-product    	|Cartesian join of arrays, producing a flat array of pairs.
`\|-`	|arr arr      	|multiset-subtract	|Remove elements in b individually from a, if they're present.
`\|-`	|arr num      	|remove-first     	|Remove first instance of b from a.
`\|\`	|arr arr      	|zip-short        	|Zip arrays producing pairs.  The longer array is truncated.
`\|)`	|arr          	|rotate-right     	|Move the last element of an array to the front.
`\|)`	|arr int      	|rotate-right-n   	|Shift array n places to the right, rotating the end to the front.
`\|(`	|arr          	|rotate-left      	|Move the first element of an array to the end.
`\|(`	|arr int      	|rotate-left-n    	|Shift array n places to the right, rotating the front to the end.
`\|[`	|arr          	|prefixes         	|All prefixes of array.
`\|]`	|arr          	|suffixes         	|All suffixes of array.
`\|a`	|arr          	|any              	|Any elements of array are truthy?
`\|A`	|arr          	|all              	|All elements of array are truthy?
`\|g`	|arr          	|gcd              	|Greatest common denominator of array.
`\|I`	|arr num      	|find-all         	|Find all indexes of occurrences of the value.
`\|l`	|arr          	|lcm              	|Least common multiple of array.
`\|m`	|arr          	|min              	|Minimum value in array.
`\|M`	|arr          	|max              	|Maximum value in array.
`\|p`	|arr          	|palindromize     	|a + reversed(a[:-1]).  Always has an odd length.
`\|r`	|int int      	|explicit-range   	|Range [a .. b). If a is an array, use the opposite of its length instead.  If b is an array, use its length instead.
`\|R`	|int int int  	|stride-range     	|Range [a .. b) with stride of c.
`\|R`	|arr          	|run-length       	|Encode runs of elements into an array of [element, count] pairs.
`:0` 	|arr          	|falsy-indices    	|Get all indices of falsy elements
`:1` 	|arr          	|truthy-indices   	|Get all indices of truthy elements
`::` 	|arr int      	|every-nth        	|Every nth element in array, starting from the first.
`:*` 	|arr          	|product          	|Product of numbers in array.
`:-` 	|arr          	|deltas           	|Pairwise difference of array.
`:/` 	|arr int      	|split-at         	|Split array at index; push both parts.
`:(` 	|arr          	|left-rotations   	|All left rotations, starting from original.
`:)` 	|arr          	|right-rotations  	|All right rotations, starting from original.
`:f` 	|arr          	|flatten          	|Flatten array of arrays one time.
`:I` 	|arr arr      	|find-index-all   	|For each element in b, find the index of the first occurrence in a.
`:J` 	|arr          	|squarify         	|Wrap array into smallest fitting square, filling any extra spaces with 0.
`:m` 	|arr int      	|repeat-to        	|Repeat array until it is exactly length n.
`:m` 	|arr          	|mirror           	|Append reversed copy to array.
`:r` 	|arr arr      	|replace-all      	|Replace all substring occurrences.
`:r` 	|int          	|centered-range   	|Make range [-n .. n] centered around 0.
`:S` 	|arr arr      	|is-superset-of   	|Is a a (non-strict) superset of b?
`:u` 	|arr          	|multi-single     	|Array contains exactly 1 distinct element?


## Blocks

chars	|Types        	|Name           	|Description
---  	|---          	|---            	|---
`{`  	|             	|               	|Begin a block.  Blocks can be ended by any block terminator, not just }.
`}`  	|             	|               	|Terminate a block and push to stack.  If there is not a block currently open, start program execution over.
`*`  	|int block    	|do-times       	|Perform block n times.
`*`  	|block int    	|do-times       	|Perform block n times.
`C`  	|any          	|cancel         	|If value is truthy, cancel current block execution.
`f`  	|arr block    	|filter         	|Terminate a block and filter array using it as a predicate.
`f`  	|arr          	|filter-short   	|If there is no open block, use the rest of the program as the predicate.  Print passing elements on separate lines.
`f`  	|int          	|do-times       	|Execute the rest of the program n times.  _ will give the 1-based iteration count.
`F`  	|arr block    	|foreach        	|Terminate a block.  Push each element of the array, and execute the block for each.
`F`  	|arr          	|foreach-short  	|If there is no open block, use the rest of the program as the block.  Execute it after pushing each element.
`F`  	|int          	|for-short      	|Perform `foreach-short` using the range [1 .. n].
`g`  	|             	|generator      	|Generate values.  See `generators` for details.
`i`  	|             	|index          	|Get the current 0-based iteration index of the inner loop.
`\|i`	|             	|outer-index    	|Get the 0-based iteration index of the outer loop.
`k`  	|arr block    	|reduce         	|Terminate a block and reduce (fold) using the block.
`k`  	|int block    	|reduce-range   	|Terminate a block and reduce (fold) [1 .. n] using the block.
`K`  	|arr arr block	|cross-map      	|Terminate a block and map using over a cartesian join.  Both elements will be pushed to the stack.  `_` will also push both to stack.  The result will be a single flat array.
`m`  	|arr block    	|map            	|Terminate a block and map using a block.  If the block execution is cancelled, that element won't be included in the result.
`m`  	|arr          	|map-short      	|If there is no open block, use the rest of the program as the block.  Print each mapped element with a new-line.
`m`  	|int          	|map-range-short	|Use the rest of the program as a block to map [1 .. n].  Print each mapped element with a new-line.
`M`  	|any block    	|maybe          	|Execute block if value is truthy.  Does not terminate a block.
`o`  	|arr block    	|order          	|Terminate a block and order array by key.  If there are no open blocks, order the array itself.
`w`  	|block        	|do-while       	|Terminate a block and iterate until it produces a falsy value.
`w`  	|             	|do-while-short 	|If there is no open block, use the rest of the program as the block.
`W`  	|block        	|while          	|Terminate a block and iterate forever.  Cancelling will terminate, as with all blocks.
`W`  	|             	|while-short    	|If there is no open block, use the rest of the program as the block.
`_`  	|             	|current        	|Get the current iteration value.  If there are no blocks executing, this will be all of standard input, as one string.
`\|c`	|             	|contend        	|Assert top of stack is truthy.  Cancel if not.  Do not pop.
`\|I`	|arr block    	|filter-index   	|Get all indexes in the array that produce a truthy value from the block.

## Registers
chars	|Description
---  	|---
`x`  	|Value of register x.  Default is parsed integer value from standard input, or 0.
`X`  	|Peek and write register x.
`\|x`	|Decrement register x and push.
`\|X`	|Increment register x and push.
`y`  	|Value of register y.  Default is first line of standard input.
`Y`  	|Peek and write register y.

## Prefix Directives
Some instructions behave differently when they are the first character in a program.

char	|Name        	|Description
--- 	|---         	|---
`f` 	|line-filter 	|Use the rest of the program to filter input lines.  Print lines that produce a truthy result.
`F` 	|line-foreach	|Execute the rest of the program once for each line of input.
`m` 	|line-map    	|Map each line of standard input.  Print each result with a newline.
`i` 	|no-eval     	|Suppress auto-eval.  e.g. If the input is a binary number, this will prevent incorrectly parsing it as an integer.

## Constants

chars	|Value
---  	|---
`V?` 	|Version info
`V0` 	|rational 0/1
`VA` 	|"ABCDEFGHIJKLMNOPQRSTUVWXYZ"
`Va` 	|"abcdefghijklmnopqrstuvwxyz"
`VB` 	|256
`VC` 	|"BCDFGHJKLMNPQRSTVWXYZ"
`Vc` 	|"bcdfghjklmnpqrstvwxyz"
`Vd` 	|"0123456789"
`Vk` 	|1000
`Vl` 	|"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
`VL` 	|"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
`VM` 	|1000000
`VP` 	|pi
`VV` 	|AEIOU
`Vv` 	|aeiou
`VW` 	|"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
`Vw` 	|"0123456789abcdefghijklmnopqrstuvwxyz"
`Vs` 	|all ascii whitespace
`Vn` 	|newline (newline is also `A]`)

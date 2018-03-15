# Instructions

There are a lot of instructions here.  For ease of `Ctrl + F`, they're all prefixed with "i:".  To look up `|f`, search for "i:|f".

## Input / Output
chars     	|Description
---       	|---
i:`p`     	|Pop and print with no newline.
i:`P`     	|Pop and print with a newline.
i:`q`     	|Peek and print with no newline.
i:`Q`     	|Peek and print with a newline.
i:`\|_`   	|(space, not underscore) Print a single space character.
i:``\|` ``	|Dump debug state.  Shows registers, iteration info, and both stacks.  In the web-based environment, break execution.
i:`\|P`   	|Print newline.
i:`\|V`   	|Array of command line arguments.  This will be an empty array for non-command-line invocations.

## Stack
chars	|Example           	|Name         	|Description
---  	|----              	|----         	|--------------
i:`a`  	|… a b c -> … b c a	|alter-stack  	|Moves the third element in the stack to the top.
i:`b`  	|… a b -> … a b a b	|both-copy    	|Copies the top two stack elements
i:`c`  	|… a -> … a a      	|copy         	|Copy the top stack element
i:`d`  	|… a b -> … a      	|discard      	|Pop and discard top stack element
i:`n`  	|… a b -> … a b a  	|next         	|Copy second element to top of stack
i:`[`  	|… a b -> … a a b  	|under        	|Copy second element in-place
i:`~`  	|                  	|input-push   	|Pop top item, and push it to input stack.
i:`;`  	|                  	|input-peek   	|Peek from input stack, and push to main stack.
i:`,`  	|                  	|input-pop    	|Pop from input stack, and push to main stack.
i:`e`  	|arr               	|eval         	|Parse string as data to be pushed on the stack. e.g. [12, 3.4, [5/6, "a string"]]  Multiple top level values in the same string will be pushed separately.
i:`E`  	|arr               	|explode      	|Push all elements from array onto stack.
i:`L`  	|… -> […]          	|listify-stack	|Clear both stacks, and put all items in an array back on stack.
i:`l`  	|int               	|listify-n    	|Pop n items, and put them in an array on top of the stack.
i:`O`  	|… a -> … 1 a      	|tuck-1       	|Push the value 1 under the top element of the stack.
i:`s`  	|… a b -> … b a    	|swap         	|Swap top two stack elements.
i:`Z`  	|… a -> … 0 a      	|tuck-0       	|Push the value 0 under the top element of the stack.
i:`\|d`	|                  	|main-depth   	|Size of main stack.
i:`\|D`	|                  	|input-depth  	|Size of input stack.


## Numerics
chars        	|Types      	|Name          	|Pseudo-code        	|Description
---          	|---        	|---           	|---                	|---
i:`0123456789` 	|           	|              	|                   	|Integer literal.  Leading `0` is always separate number. Use `A` for 10. `10` is 1 and 0.
i:`A`          	|           	|              	|10                 	|Constant 10, as in hexidecimal.
i:`01234!56789`	|           	|              	|                   	|Float literal.  `!` indicates a decimal point.  A trailing `0` after a `!` is always a separate int literal.
i:`+`          	|num num    	|add           	|a + b              	|Add. Integers widen to fractions.  Fractions widen to floats.
i:`-`          	|num num    	|sub           	|a - b              	|Subtract.
i:`*`          	|num num    	|mul           	|a * b              	|Multiply.
i:`/`          	|num num    	|div           	|a / b              	|Integers will use integer division.
i:`%`          	|int int    	|mod           	|a % b              	|Modulus.
i:`@`          	|frac       	|floor         	|floor(a)           	|Integer floor of fraction.
i:`@`          	|float      	|floor         	|floor(a)           	|Integer floor of float.
i:`v`          	|num        	|dec           	|a - 1              	|Decrement by 1.
i:`^`          	|num        	|inc           	|a + 1              	|Increment by 1.
i:`B`          	|frac       	|properize     	|floor(a), a%1      	|Properize fraction.  Push integer floor, and proper remainder separately.
i:`D`          	|num        	|frac-part     	|a%1                	|Get fractional non-integer part of rational or float.
i:`e`          	|frac       	|ceil          	|ceiling(a)         	|Integer ceiling of fraction.
i:`e`          	|float      	|ceil          	|ceiling(a)         	|Integer ceiling of float.
i:`E`          	|int        	|dec-digits    	|                   	|Array of decimal digits.
i:`E`          	|frac       	|num-den       	|                   	|Push numerator and denominator separately.
i:`h`          	|int        	|halve         	|a / 2              	|Floor divide by 2.
i:`h`          	|num        	|halve         	|a / 2              	|Divide by 2.
i:`H`          	|num        	|un-halve      	|a * 2              	|Double.
i:`j`          	|float int  	|float-round   	|round(a, b)        	|Round float to n decimal places.  Format as string.
i:`j`          	|float      	|round         	|round(a)           	|Round to nearest integer.
i:`j`          	|frac       	|round         	|round(a)           	|Round to nearest integer.
i:`J`          	|num        	|square        	|a ** 2             	|Square number.
i:`l`          	|frac       	|listify-frac  	|a/b -> [a b]       	|Turn a fraction into a 2-array of numerator and denominator.
i:`N`          	|number     	|negate        	|-a                 	|Negate a number.
i:`r`          	|frac       	|numerator     	|numerator(a)       	|Get the numerator.
i:`R`          	|frac       	|denom         	|denom(a)           	|Get the denominator.
i:`u`          	|int        	|fractionalize 	|1 / a              	|Turn integer upside down; into fraction.
i:`u`          	|frac       	|invert-frac   	|1 / a              	|Turn fraction upside down; invert.
i:`u`          	|float      	|invert-float  	|1 / a              	|Floating point inversion.
i:`U`          	|           	|              	|-1                 	|Negative unit.
i:`\|%`        	|int int    	|divmod        	|a / b, a % b       	|Perform division and modulus.
i:`\|&`        	|int int    	|bit-and       	|a & b              	|Bitwise and.
i:`\|\|`       	|int int    	|bit-or        	|a | b              	|Bitwise or.
i:`\|^`        	|int int    	|bit-xor       	|a ^ b              	|Bitwise xor.
i:`\|*`        	|int int    	|pow           	|a ** b             	|Exponent.	
i:`\|*`        	|frac int   	|pow           	|a ** b             	|Exponent.	
i:`\|/`        	|int int    	|div-all       	|                   	|Divide a by b as many times as it will go evenly.
i:`\|<`        	|int int    	|shift-left    	|a << b             	|Bitshift left.
i:`\|>`        	|int int    	|shift-right   	|a >> b             	|Bitshift right.
i:`\|1`        	|int        	|parity-sign   	|(-1) ** a          	|Power of negative one.
i:`\|2`        	|int        	|2-power       	|2 ** a             	|Power of two.
i:`\|3`        	|int        	|base-36       	|                   	|Convert to base 36.
i:`\|3`        	|arr        	|base-36       	|                   	|Convert from base 36.
i:`\|5`        	|num        	|nth-fib       	|                   	|Get nth fibonacci number; 0-indexed.
i:`\|6`        	|num        	|nth-prime     	|                   	|Get nth prime; 0-indexed.
i:`\|7`        	|num        	|cosine        	|cos(a)             	|Cosine in radians.
i:`\|8`        	|num        	|sine          	|sin(a)             	|Sine in radians.
i:`\|9`        	|num        	|tangent       	|tan(a)             	|Tangent in radians.
i:`\|a`        	|num        	|abs           	|abs(a)             	|Absolute value.
i:`\|A`        	|int        	|10-power      	|10 ** a            	|Power of ten.
i:`\|b`        	|int int    	|convert-base  	|                   	|Convert to base, up to 36.
i:`\|b`        	|arr int    	|convert-base  	|                   	|Convert from base, up to 36.
i:`\|B`        	|int        	|convert-binary	|                   	|Convert to base 2.
i:`\|B`        	|arr        	|convert-binary	|                   	|Convert from base 2.
i:`\|C`        	|int int    	|choose        	|choose(a, b)       	|Binomial coefficient - calculate a choose b.
i:`\|e`        	|int        	|is-even       	|(a + 1) % 2        	|Is even?
i:`\|E`        	|int int    	|digits        	|                   	|Generate array of digit values in base.
i:`\|E`        	|arr int    	|un-digit      	|                   	|Produce the number represented by the array of digits in the given base.
i:`\|f`        	|int        	|factorize     	|                   	|Prime factorization array. 
i:`\|F`        	|int        	|factorial     	|a!                 	|Factorial.
i:`\|g`        	|int int    	|gcd           	|                   	|Greatest common denominator.
i:`\|H`        	|int        	|base-16       	|                   	|Convert to base 16.
i:`\|H`        	|arr        	|base-16       	|                   	|Convert from base 16.
i:`\|l`        	|int int    	|lcm           	|                   	|Least common multiple.
i:`\|L`        	|num num    	|log-n         	|log(a, b)          	|Logarithm in base b of a.
i:`\|m`        	|num num    	|min           	|min(a, b)          	|Lower of two values.
i:`\|M`        	|num num    	|max           	|max(a, b)          	|Higher of two values.
i:`\|n`        	|int        	|prime-exps    	|                   	|Exponents of sequential primes in factorization. (eg. 20 -> [2 0 1])
i:`\|p`        	|int        	|is-prime      	|                   	|Is prime?
i:`\|q`        	|num        	|int-sqrt      	|floor(sqrt(abs(a)))	|Integer square root of absolute value.
i:`\|Q`        	|num        	|sqrt          	|sqrt(abs(a))       	|Float square root of absolute value.
i:`:~`         	|int        	|set-interior  	|                   	|Set all interior bits in number.  The result will always be one less than a power of 2.
i:`:!`         	|int int    	|coprime       	|iscoprime(a, b)    	|Are a and b coprime?
i:`:#`         	|num        	|floatify      	|float(a)           	|Convert to float.
i:`:-`         	|num num    	|abs-diff      	|abs(a - b)         	|Absolute difference.
i:`:/`         	|int int    	|multiplicity  	|                   	|Number of times b will evenly divide a.
i:`:+`         	|num        	|sign          	|sign(a)            	|Sign; 1 for positive, -1 for negative.
i:`:_`         	|num num    	|float-div     	|1.0 * a / b        	|Float division.
i:`:1`         	|int        	|popcount      	|                   	|Count of set bits.
i:`:2`         	|num        	|floor-log-2   	|floor(log(a, 2))   	|Floor of log base 2.
i:`:A`         	|num        	|floor-log-10  	|floor(log(a, 10))  	|Floor of log base 10.
i:`:b`         	|num int int	|between       	|b <= a < c         	|Value is in range?
i:`:b`         	|arr        	|binary-decode 	|                   	|Produce a number given as an array of bits.
i:`:B`         	|int arr    	|custom-base   	|                   	|Encode number in custom base from string characters.
i:`:B`         	|int        	|binary-digits 	|                   	|Generate array of binary values.
i:`:c`         	|num int int	|clamp         	|min(max(a, b), c)  	|Ensure value is in range.
i:`:d`         	|int        	|divisors      	|                   	|Get all divisors of n.
i:`:f`         	|int        	|factorize-exp 	|                   	|Factorize into pairs of [prime, exponent].
i:`:F`         	|int        	|dist-factors  	|                   	|Distinct prime factors.
i:`:g`         	|int        	|low-bit       	|                   	|Unset all but the low set bit.
i:`:G`         	|int        	|high-bit      	|                   	|Unset all but the high set bit.
i:`:J`         	|num num    	|square-two    	|a\*\*2, b\*\*2     	|Square top two elements; useful for hypotenuse and things.
i:`:m`         	|int int    	|next-multiple 	|                   	|If necessary, increase a until it is a multiple of b.
i:`:p`         	|int        	|last-prime    	|                   	|Last prime < n.
i:`:P`         	|int        	|next-prime    	|                   	|Next prime >= n.
i:`:t`         	|int        	|totient       	|totient(a)         	|Euler's totient of n.
i:`:T`         	|int        	|triangular-num	|a * (a+1) / 2      	|Get a triangular number.

## Logic
chars  	|Name        	|Description
---    	|---         	|---
i:`!`  	|not         	|Logical not.  Produces 0 or 1.  Numeric 0 and empty lists are considered falsy.  All other values are truthy.
i:`<`  	|less        	|Is less than.  Arrays use string-style lexicographic ordering.
i:`>`  	|greater     	|Is greater than.  Arrays use string-style lexicographic ordering.
i:`=`  	|equal       	|Equals.  Numberic types are coerced as necessary.  Arrays are equal to scalars if their first element is equal.
i:`?`  	|if-then-else	|If the first value, then yield the second, else the third.  If the result is a block, execute it.
i:`\|4`	|is-array    	|Tests if a value is an array or not.  Produces 0 or 1.

## String
Strings are really just arrays of integers, but some operations are oriented towards strings anyway.  In these contexts, 0 is usually converted to 32, so that 0 can be used as a space in addition to its normal codepoint.

chars    	|Types          	|Name              	|Description
---      	|---            	|---               	|---
i:`#`      	|arr arr        	|count-substrings  	|Count occurrences of substring b in a.
i:`"…"`    	|               	|string-literal    	|String literal stored as an array of codepoints.  Unterminated string literals will be printed implicitly.  `` ` `` is the escape character for `` ` `` and `"`. The characters `01234` yield `\0 \n \t \r \v` respectively when escaped. All other escaped single characters will execute as stax code and then include the popped value as a template. (Space is no-op)
i:`` `…` ``	|               	|compressed-string 	|Compressed string literal encoded with contextual Huffman trees.  Not all strings can be encoded this way, but most that can will be smaller.  Unterminated compressed literals will be printed implicitly.
i:`'a`     	|               	|char-literal      	|Create a single character string literal.
i:`.ab`    	|               	|two-char-literal  	|Create a two character string literal.
i:`/`      	|arr arr        	|split             	|Split on substrings.
i:`*`      	|arr int        	|repeat            	|Repeat string n times.  If n is negative, reverse array.
i:`*`      	|int arr        	|repeat            	|Repeat string n times.  If n is negative, reverse array.
i:`*`      	|arr arr        	|join              	|Join array of strings with delimiter.
i:`$`      	|num            	|tostring          	|Convert number to string.
i:`$`      	|arr            	|arr-tostring      	|Convert each element to string, and concat. Nested arrays will just be flattened. 
i:`(`      	|arr arr        	|begin-with        	|Overwrite the end of b with a.
i:`)`      	|arr arr        	|end-with          	|Overwrite the beginning of b with a.
i:`v`      	|arr            	|lower             	|To lower case.
i:`^`      	|arr            	|upper             	|To upper case.
i:`I`      	|arr arr        	|substring-index   	|Get the index of the first occurrence of the substring.
i:`j`      	|arr            	|space-split       	|String split by space.
i:`J`      	|arr            	|space-join        	|Join strings by space.
i:`R`      	|arr arr arr    	|regex-replace     	|Regex replace using ECMA regex.
i:`t`      	|arr            	|trim-left         	|Trim whitespace from left of string.
i:`T`      	|arr            	|trim-right        	|Trim whitespace from right of string.
i:`\|~`    	|arr arr        	|last-index-of     	|Get the index of the last occurrence of the substring.
i:`\|<`    	|arr            	|left-align        	|Left align lines of text, padding to longest line.
i:`\|>`    	|arr            	|right-align       	|Right align lines of text, padding to longest line.
i:`\|\|`   	|arr int int arr	|embed-grid        	|Embed a grid inside another grid at the specfied coordinates.  Negative coordinates are not allowed.  OOB extends the necessary dimensions.
i:`\|e`    	|arr arr arr    	|replace-first     	|String replace; first instance only.
i:`\|C`    	|arr int        	|center            	|Center string in n characters.
i:`\|C`    	|arr            	|center-block      	|Center lines of text, using longest line.
i:`\|F`    	|arr arr        	|regex-find-all    	|Get all regex pattern matches.
i:`\|I`    	|arr arr        	|str-index-all     	|Find all indexes of the substring.
i:`\|J`    	|arr            	|join-newline      	|Join strings with newline.
i:`\|q`    	|arr arr        	|regex-indices     	|Get all indices of regex matches.
i:`\|Q`    	|arr arr        	|regex-is-match    	|Regex matches entire string?
i:`\|s`    	|arr arr        	|regex-split       	|Split by ECMA regex. Captured groups will be included in result.
i:`\|S`    	|arr arr        	|surround          	|Prepend and append string/array.
i:`\|t`    	|arr arr        	|translate         	|Translate first string using pairs of elements in the second array.  Instances of the first in a pair will be replaced by the second.
i:`\|z`    	|arr int        	|zero-fill         	|Fill on the left with "0" to specified length.
i:`:~`     	|arr            	|toggle-case       	|Toggle case of letters in string.
i:`:.`     	|arr            	|title-case        	|Convert string to title case.
i:`:/`     	|arr arr        	|split-once        	|Split on the first occurrence of a substring.  Push both parts separately.
i:`:[`     	|arr arr        	|starts-with       	|String starts with?
i:`:]`     	|arr arr        	|starts-with       	|String ends with?
i:`:{`     	|any            	|parenthesize      	|Embed value in parentheses as string.
i:`:}`     	|any            	|bracercise        	|Embed value in square braces as string.
i:`:3`     	|arr            	|rot-13            	|Rot13 encode/decode; dual-purpose.
i:`:C`     	|arr            	|invert-case       	|Invert case of letters a-z.
i:`:D`     	|arr int        	|trim-both         	|Trim element from both ends of string.
i:`:D`     	|arr arr        	|trim-both         	|Trim all characters from both ends of string.
i:`:e`     	|arr            	|excerpts          	|Get all contiguous subarrays.
i:`:R`     	|arr            	|brace-reflect     	|Reflect string, `(<[{/` becomes `\}]>)`
i:`:t`     	|arr arr        	|ring-tranlate     	|Map matching elements to the subsequent element in the translation ring.  The ring wraps around.
i:`:w`     	|arr            	|brace-palindromize	|Concatenate all but the last character reversed.  Braces and slashes are individually reversed also.
i:`:W`     	|arr            	|brace-mirror      	|Concatenate the string reversed.  Braces and slashes are individually reversed also.

## Array
chars	|Types              	|Name              	|Description
---  	|---                	|---               	|---
i:`"…"!`	|                   	|crammed-array     	|Crammed array of integers. This uses a scheme to represent arbitrary integers in a string literal, and then uncram them.  There will be at most one number per character in the string literal.  Large numbers require more characters.
i:`#`  	|arr num            	|count-instances   	|Count instances of b in a.
i:`#`  	|num arr            	|count-instances   	|Count instances of b in a.
i:`+`  	|arr arr            	|concat            	|Concatenate arrays.
i:`+`  	|num arr            	|prepend           	|Prepend element to array.
i:`+`  	|arr num            	|append            	|Append element to array.
i:`-`  	|arr arr            	|array-diff        	|Remove all elements in b from a.
i:`-`  	|arr num            	|array-remove      	|Remove all instances of b from a.
i:`/`  	|arr int            	|array-group       	|Split array into groups of specified size.  The last group will be smaller if it's not a multiple.
i:`%`  	|arr                	|length            	|Array length
i:`\`  	|num num            	|pair              	|Make a 2 length array.
i:`\`  	|arr num            	|array-pair        	|Make array of pairs, all having identical second element.
i:`\`  	|num arr            	|array-pair        	|Make array of pairs, all having identical first element.
i:`\`  	|arr arr            	|zip-repeat        	|Make array of pairs, zipped from two arrays.  The shorter is repeated as necessary.
i:`@`  	|arr int            	|element-at        	|Get element at 0-based modular index.  (-1 is the last element)
i:`@`  	|arr int int ...    	|element-at        	|Get element in multi-dimensional array using all integer indices.
i:`@`  	|arr arr            	|elements-at       	|Get elements at all indices.
i:`&`  	|arr int any        	|assign-index      	|Assign element at index.  Negatives index backwards.  OOB extends the array.
i:`&`  	|arr int ... int any	|assign-index      	|Assign element in multidimensional array of arrays located at specified coordinates.  Negative coordinates are not allowed.  OOB extends the array(s);
i:`&`  	|arr arr any        	|assign-indices    	|Assign element at all indices.  If indices array is an array of arrays, then treat them as a path to navigate a multidimensional array of arrays.
i:`&`  	|arr int block      	|mutate-element    	|Mutate element at index using block.
i:`&`  	|arr arr block      	|mutate-element    	|Mutate element at indices using block. If indices array is an array of arrays, then treat them as a path to navigate a multidimensional array of arrays.
i:`(`  	|arr int            	|pad-right         	|Truncate or pad on right with 0s as necessary for target length.  Negative numbers remove that number of elements.
i:`)`  	|arr int            	|pad-left          	|Truncate or pad on left with 0s as necessary for target length.  Negative numbers remove that number of elements.
i:`]`  	|any                	|singleton         	|Make a 1 element array.
i:`B`  	|arr int            	|batch             	|Get all (overlapping) sub-arrays of specified length.
i:`B`  	|arr                	|uncons-left       	|Remove first element from array.  Push the tail of the array, then the removed element.
i:`D`  	|arr                	|drop-first        	|Remove first element from array.
i:`h`  	|arr                	|first             	|Get first element.
i:`H`  	|arr                	|last              	|Get last element.
i:`I`  	|arr num            	|index-of          	|Get the index of the first occurrence.
i:`I`  	|num arr            	|index-of          	|Get the index of the first occurrence.
i:`M`  	|arr int            	|chunkify          	|Partition a into b chunks of almost equal size.  The largest chunks come first. They're one larger than the smaller ones.
i:`M`  	|arr                	|transpose         	|Flip array of arrays about the diagonal. Non-array elements are considered to be singleton arrays.  Arrays are padded with zeroes as necessary to preserve layout. 
i:`N`  	|arr                	|uncons-right      	|Remove last element from array.  Push the beginning of the array, then the removed element.
i:`r`  	|int                	|0-range           	|Make range [0 .. n-1].
i:`r`  	|arr                	|reverse           	|Reverse array.
i:`R`  	|int                	|1-range           	|Make range [1 .. n].
i:`t`  	|arr int            	|remove-left       	|Trim n elements from left of array.
i:`T`  	|arr int            	|remove-right      	|Trim n elements from right of array.
i:`S`  	|arr                	|powerset          	|Get all combinations of elements in array.  If the array was ordered, all combinations will be in lexicographic order.
i:`S`  	|arr int            	|combinations      	|Get all combinations of specified size from array.  If the array was ordered, all combinations will be in lexicographic order.
i:`u`  	|arr                	|unique            	|Keep only unique elements in array, maintaining first order of appearance.
i:`z`  	|                   	|                  	|Push empty array/string.
i:`\|0`	|arr                	|falsy-index       	|Get index of first falsy element.
i:`\|1`	|arr                	|truthy-index      	|Get index of first truthy element.
i:`\|2`	|arr                	|diagonal          	|Get the diagonal of a matrix.  Missing elements are filled with 0.
i:`\|=`	|arr                	|multi-mode        	|Get all tied modes of array.
i:`\|+`	|arr                	|sum               	|Sum of array.
i:`\|!`	|int int            	|int-partitions    	|Get all the distinct b-length arrays of positive integers that sum to a.
i:`\|!`	|arr int            	|arr-partitions    	|Get all the ways of splitting array a into b pieces.
i:`\|!`	|arr                	|multi-anti-mode   	|Get all tied rarest elements of array.
i:`\|@`	|arr int            	|remove-at         	|Remove element from array at index.
i:`\|@`	|arr int num        	|insert-at         	|Insert element to array at index.
i:`\|#`	|arr arr            	|verbatim-count    	|Counts number of occurrences of b as an element of a, without any string flattening.
i:`\|%`	|arr int arr        	|embed-array       	|Embed c in a, starting at position b.  Negative indexes from the end.  OOB extend the array.
i:`\|&`	|arr arr            	|arr-intersect     	|Keep all elements from a that are in b.
i:`\|^`	|arr arr            	|arr-xor           	|Keep all elements from a that are not in b, followed by all elements in b that are not in a.
i:`\|^`	|arr int            	|multi-self-join   	|Generate all arrays of size b using elements from a.
i:`\|*`	|arr int            	|repeat-elements   	|Repeat each element in a, abs(b) times.
i:`\|*`	|arr arr            	|cross-product     	|Cartesian join of arrays, producing a flat array of pairs.
i:`\|/`	|arr arr            	|multi-group       	|Partition a into differently sized groups from b.
i:`\|-`	|arr arr            	|multiset-subtract 	|Remove elements in b individually from a, if they're present.
i:`\|-`	|arr num            	|remove-first      	|Remove first instance of b from a.
i:`\|\`	|arr arr            	|zip-short         	|Zip arrays producing pairs.  The longer array is truncated.
i:`\|\`	|arr arr num        	|zip-fill          	|Zip arrays producing pairs.  The shorter array is extended using the fill element.
i:`\|)`	|arr                	|rotate-right      	|Move the last element of an array to the front.
i:`\|)`	|arr int            	|rotate-right-n    	|Shift array n places to the right, rotating the end to the front.
i:`\|(`	|arr                	|rotate-left       	|Move the first element of an array to the end.
i:`\|(`	|arr int            	|rotate-left-n     	|Shift array n places to the right, rotating the front to the end.
i:`\|[`	|arr                	|prefixes          	|All prefixes of array.
i:`\|]`	|arr                	|suffixes          	|All suffixes of array.
i:`\|a`	|arr                	|any               	|Any elements of array are truthy?
i:`\|A`	|arr                	|all               	|All elements of array are truthy?
i:`\|b`	|arr arr            	|multiset-intersect	|Keep the elements from a that occur in b, no more than the number of times they occur in b.
i:`\|g`	|arr                	|gcd               	|Greatest common denominator of array.
i:`\|G`	|arr                	|round-flatten     	|Flatten array of arrays using round-robin distribution.  Cycle between inner arrays until all elements are reached.
i:`\|I`	|arr num            	|find-all          	|Find all indexes of occurrences of the value.
i:`\|l`	|arr                	|lcm               	|Least common multiple of array.
i:`\|L`	|arr arr            	|multiset-union    	|Combine elements from a and b, with each occurring the max of its occurrences from a and b.
i:`\|m`	|arr                	|min               	|Minimum value in array.
i:`\|M`	|arr                	|max               	|Maximum value in array.
i:`\|n`	|arr arr            	|multiset-xor      	|Combine elements from a and b, removing common elements only as many times as they mutually occur.
i:`\|N`	|arr                	|next-perm         	|Get the next permuation of elements in the array, ordered lexicographically.
i:`\|o`	|arr                	|ordered-indices   	|Calculate destination index of each element if array were to be sorted.
i:`\|p`	|arr                	|palindromize      	|a + reversed(a[:-1]).  Always has an odd length.
i:`\|r`	|int int            	|explicit-range    	|Range [a .. b). If a is an array, use the opposite of its length instead.  If b is an array, use its length instead.
i:`\|R`	|int int int        	|stride-range      	|Range [a .. b) with stride of c.
i:`\|R`	|arr                	|run-length        	|Encode runs of elements into an array of [element, count] pairs.
i:`\|T`	|arr                	|permutations      	|Get all orderings of elements in the array.  Duplicate elements are not considered.
i:`\|T`	|arr int            	|permutations      	|Get all b-length orderings of elements in a.  Duplicate elements are not considered.
i:`\|w`	|arr arr            	|trim-left-els     	|Remove matching leading elements from array.
i:`\|W`	|arr arr            	|trim-right-els    	|Remove matching trailing elements from array.
i:`\|Z`	|arr                	|rectangularize    	|Rectangularize an array of arrays, using "" for missing elements.
i:`:0` 	|arr                	|falsy-indices     	|Get all indices of falsy elements.
i:`:1` 	|arr                	|truthy-indices    	|Get all indices of truthy elements.
i:`:2` 	|arr                	|self-cross-product	|Pairs of elements, with replacement.
i:`::` 	|arr int            	|every-nth         	|Every nth element in array, starting from the first.
i:`:<` 	|arr                	|col-align-left    	|Left-align columns by right-padding with spaces.
i:`:>` 	|arr                	|col-align-right   	|Right-align columns by left-padding with spaces.
i:`:*` 	|arr                	|product           	|Product of numbers in array.
i:`:-` 	|arr                	|deltas            	|Pairwise difference of array.
i:`:+` 	|arr                	|prefix-sums       	|Get the sums of all prefixes.
i:`:/` 	|arr int            	|split-at          	|Split array at index; push both parts.
i:`:/` 	|int arr            	|split-at          	|Split array at index; push both parts.
i:`:\` 	|arr arr            	|diff-indices      	|Get indices of unequal elements between arrays.
i:`:_` 	|arr                	|reduce-array      	|Divide all integers in array by their collective gcd.
i:`:=` 	|arr arr            	|equal-indices     	|Get indices of equal elements between arrays.
i:`:!` 	|arr                	|all-partitions    	|Get all the ways of splitting array into pieces.
i:`:\|`	|arr                	|column-align      	|Right pad each column to equal length.
i:`:@` 	|arr                	|truthy-count      	|Count the number of truthy elements in the array.
i:`:(` 	|arr                	|left-rotations    	|All left rotations, starting from original.
i:`:)` 	|arr                	|right-rotations   	|All right rotations, starting from original.
i:`:^` 	|arr                	|non-descending    	|Is array non-descending? (has no adjacent pair of descending elements)
i:`:v` 	|arr                	|non-ascending     	|Is array non-ascending? (has no adjacent pair of ascending elements)
i:`:a` 	|arr                	|minima-indices    	|Get indices of array minima.
i:`:A` 	|arr                	|maxima-indices    	|Get indices of array maxima.
i:`:B` 	|arr arr            	|element-repeats   	|Repeat element in a by corresponding integer in b, wrapped.  e.g. `"abcdef"`, `[0,1,2]` -> `"bcceff"`
i:`:c` 	|arr                	|coalesce          	|Get first truthy element of array.
i:`:d` 	|arr                	|median            	|Get the median of array.  Integers produce a rational result.  Floats produce a float.
i:`:f` 	|arr                	|flatten           	|Flatten array; for each element of the array unwrap it if it's an array.  e.g. `[3,4,[5,[6]]]` -> `[3,4,5,[6]]`
i:`:F` 	|arr                	|falsy-indices     	|Get all indices of falsy elements.
i:`:g` 	|arr                	|run-elements      	|Remove adjacent duplicate elements from array.
i:`:G` 	|arr                	|run-lengths       	|Get the lengths of runs of duplicate elements.
i:`:I` 	|arr arr            	|find-index-all    	|For each element in b, find the index of the first occurrence in a.
i:`:J` 	|arr                	|squarify          	|Wrap array into smallest fitting square, filling any extra spaces with 0.
i:`:m` 	|arr int            	|repeat-to         	|Repeat array until it is exactly length n.
i:`:m` 	|arr                	|mirror            	|Append reversed copy to array.
i:`:M` 	|arr                	|mode              	|Mode.  In case of tie, the last element to appear wins.
i:`:o` 	|arr arr            	|overlay           	|Keep the maximum element respective element from two arrays.
i:`:r` 	|arr arr            	|replace-all       	|Replace all substring occurrences.
i:`:r` 	|int                	|centered-range    	|Make range [-n .. n] centered around 0.
i:`:s` 	|arr                	|span              	|Get the span of an array. (maximum minus minimum)
i:`:S` 	|arr arr            	|is-superset-of    	|Is a a (non-strict) superset of b?
i:`:T` 	|arr                	|truthy-indices    	|Get all indices of truthy elements.
i:`:u` 	|arr                	|multi-single      	|Array contains exactly 1 distinct element?
i:`:V` 	|arr                	|mean              	|Mean of array. (rational or float)

## Blocks

chars	|Types        	|Name               	|Description
---  	|---          	|---                	|---
i:`{`  	|             	|                   	|Begin a block.  Blocks can be ended by any block terminator, not just }.
i:`}`  	|             	|                   	|Terminate a block and push to stack.  If there is not a block currently open, start program execution over.
i:`*`  	|int block    	|do-times           	|Perform block n times.
i:`*`  	|block int    	|do-times           	|Perform block n times.
i:`!`  	|block        	|execute-block      	|Execute a block.  Does not terminate a block.
i:`/`  	|arr block    	|group-by           	|Group adjacent values that produce equal values using the block.  Does not terminate a block.
i:`(`  	|arr block    	|partition-when     	|Partition the original array into consecutive subarrays that begin when the block produces a truthy value. The block is provided with the element following the boundary.
i:`)`  	|arr block    	|partition-when-pair	|Partition the original array into consecutive subarrays that begin when the block produces a truthy value. The block is provided with the pair of elements around the boundary.
i:`C`  	|any          	|cancel             	|If value is truthy, cancel current block execution.
i:`C`  	|arr block    	|collect            	|Reduce using block, but collect each value in result array.  Does not terminate a block.
i:`D`  	|int          	|do-times           	|Execute the rest of the program n times.  _ will give the 1-based iteration count.
i:`e`  	|block        	|min-by             	|Get the values which yield the minimum value when applying the block to the array. Does not terminate a block.
i:`E`  	|block        	|max-by             	|Get the values which yield the maximum value when applying the block to the array. Does not terminate a block.
i:`f`  	|arr block    	|filter             	|Terminate a block and filter array using it as a predicate.
i:`f`  	|arr          	|filter-short       	|If there is no open block, use the rest of the program as the predicate.  Print passing elements on separate lines.
i:`F`  	|arr block    	|foreach            	|Terminate a block.  Push each element of the array, and execute the block for each.
i:`F`  	|arr          	|foreach-short      	|If there is no open block, use the rest of the program as the block.  Execute it after pushing each element.
i:`F`  	|int          	|for-short          	|Perform `foreach-short` using the range [1 .. n].
i:`g`  	|             	|generator          	|Generate values.  See `generators` for details.
i:`G`  	|             	|goto               	|Jump to an unmatched trailing `}` at the end of the program.  If there are are none, jump to the beginning of the current program.  Come back when finished.
i:`h`  	|arr block    	|take-while         	|Keep run of matching elements, if any at the beginning of the array.  Does not terminate a block.
i:`H`  	|arr block    	|take-while-end     	|Keep run of matching elements, if any at the end of the array.  Does not terminate a block.
i:`i`  	|             	|index              	|Get the current 0-based iteration index of the inner loop.
i:`\|;`	|             	|iteration-parity   	|Get the parity of the current iteration index. (0 or 1)
i:`\|i`	|             	|outer-index        	|Get the 0-based iteration index of the outer loop.
i:`I`  	|arr block    	|index-of-block     	|Get the index of the first element that yields a truthy value from the block. Does not terminate a block.
i:`j`  	|arr block    	|first-match        	|Get the first match from the array - the first value for which the block produces a truthy value.  Does not terminate a block.
i:`J`  	|arr block    	|last-match         	|Get the last match from the array - the last value for which the block produces a truthy value.  Does not terminate a block.
i:`k`  	|arr block    	|reduce             	|Terminate a block and reduce (fold) using the block.
i:`k`  	|int block    	|reduce-range       	|Terminate a block and reduce (fold) [1 .. n] using the block.
i:`k`  	|int          	|reduce-short       	|If there is no open block, use the rest of the program as the block to reduce the array.  Implicitly print the result.
i:`k`  	|arr          	|reduce-short-range 	|If there is no open block, use the rest of the program as the block to reduce [1 .. n].  Implicitly print the result.
i:`K`  	|arr arr block	|cross-map          	|Terminate a block and map using over a cartesian join.  Both elements will be pushed to the stack.  `_` will also push both to stack.  The result will be an array of arrays.
i:`m`  	|arr block    	|map                	|Terminate a block and map using a block.  If the block execution is cancelled, that element won't be included in the result.
i:`m`  	|arr          	|map-short          	|If there is no open block, use the rest of the program as the block.  Print each mapped element with a new-line.
i:`m`  	|int          	|map-range-short    	|Use the rest of the program as a block to map [1 .. n].  Print each mapped element with a new-line.
i:`M`  	|any block    	|maybe              	|Execute block if value is truthy.  Does not terminate a block.
i:`o`  	|arr block    	|order              	|Terminate a block and order array by key.  If there are no open blocks, order the array itself.
i:`t`  	|arr block    	|trim-start-block   	|Remove elements from the start of the array that are matched the block predicate.  Does not terminate a block.
i:`T`  	|arr block    	|trim-end-block     	|Remove elements from the end of the array that are matched the block predicate.  Does not terminate a block.
i:`w`  	|block        	|do-while           	|Terminate a block and iterate until it produces a falsy value.
i:`w`  	|             	|do-while-short     	|If there is no open block, use the rest of the program as the block.
i:`W`  	|block        	|while              	|Terminate a block and iterate forever.  Cancelling will terminate, as with all blocks.
i:`W`  	|             	|while-short        	|If there is no open block, use the rest of the program as the block.
i:`_`  	|             	|current            	|Get the current iteration value.  If there are no blocks executing, this will be all of standard input, as one string.
i:`\|c`	|             	|contend            	|Assert top of stack is truthy.  Cancel if not.  Do not pop.
i:`\|I`	|arr block    	|filter-index       	|Get all indexes in the array that produce a truthy value from the block.

## Registers
chars	|Description
---  	|---
i:`x`  	|Value of register x.  Default is parsed integer value from standard input, or 0.
i:`X`  	|Peek and write register x.
i:`\|x`	|Decrement register x and push.  If x is not an integer, set it to 0 first.
i:`\|X`	|Increment register x and push.  If x is not an integer, set it to 0 first.
i:`y`  	|Value of register y.  Default is first line of standard input.
i:`Y`  	|Peek and write register y.
i:`\|y`	|Decrement register y and push.  If y is not an integer, set it to 0 first.
i:`\|Y`	|Increment register y and push.  If y is not an integer, set it to 0 first.

## Prefix Directives
Some instructions behave differently when they are the first character in a program.  These directives do not apply if the implicit eval of standard input succeeded.

char	|Name        	|Description
--- 	|---         	|---
i:`f` 	|line-filter 	|Use the rest of the program to filter input lines.  Print lines that produce a truthy result.
i:`F` 	|line-foreach	|Execute the rest of the program once for each line of input.
i:`m` 	|line-map    	|Map each line of standard input.  Print each result with a newline.
i:`i` 	|no-eval     	|Suppress auto-eval.  e.g. If the input is a binary number, this will prevent incorrectly parsing it as an integer.

## Constants

chars	|Value
---  	|---
i:`\|?`	|source of current program for quines or something
i:`V?` 	|Version info
i:`V!`  	|"[a-z]"
i:`V@`  	|"[A-Z]"
i:`V#`  	|"[a-zA-Z]"
i:`V$`  	|"[a-z]+"
i:`V%`  	|"[A-Z]+"
i:`V^`  	|"[a-zA-Z]+"
i:`V&`  	|"[a-z]*"
i:`V*`  	|"[A-Z]*"
i:`V(`  	|"[a-zA-Z]*"
i:`V:`  	|"http://"
i:`V;`  	|"https://"
i:`V0` 	|rational 0/1
i:`V2` 	|0.5
i:`V3` 	|semitone ratio in equal temperment (pow(2, 1/12))
i:`V/` 	|pi/3
i:`V%` 	|[0, 0]
i:`VA` 	|"ABCDEFGHIJKLMNOPQRSTUVWXYZ"
i:`Va` 	|"abcdefghijklmnopqrstuvwxyz"
i:`Vb` 	|"()[]{}<>"
i:`VB` 	|256
i:`VC` 	|"BCDFGHJKLMNPQRSTVWXYZ"
i:`Vc` 	|"bcdfghjklmnpqrstvwxyz"
i:`Vd` 	|"0123456789"
i:`VD` 	|sqrt(2)
i:`Ve` 	|natural log base
i:`VE` 	|sqrt(3)
i:`Vh` 	|"0123456789abcdef"
i:`VH` 	|"0123456789ABCDEF"
i:`Vi` 	|negative infinity
i:`VI` 	|positive infinity
i:`Vk` 	|1000
i:`Vl` 	|"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
i:`VL` 	|"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
i:`Vm` 	|0x7ffffffff
i:`VM` 	|1000000
i:`Vn` 	|newline (newline is also `A]`)
i:`Vp` 	|all printable ascii characters
i:`VP` 	|pi
i:`Vq` 	|pi/2
i:`Vs` 	|all ascii whitespace
i:`VS` 	|4/3 pi
i:`Vt` 	|tau (2pi)
i:`VT` 	|10.0
i:`Vu` 	|pow(2, 32)
i:`VV` 	|"AEIOU"
i:`Vv` 	|"aeiou"
i:`VW` 	|"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
i:`Vw` 	|"0123456789abcdefghijklmnopqrstuvwxyz"

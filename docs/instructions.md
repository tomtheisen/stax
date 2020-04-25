# Instructions

## Input / Output
op                  	|Description
---                 	|---
`p`                 	|Pop and print with no newline.
`P`                 	|Pop and print with a newline.
`q`                 	|Peek and print with no newline.
`Q`                 	|Peek and print with a newline.
<code>&#124;_</code>	|(space, not underscore) Print a single space character.
<code>&#124;`</code>	|Dump debug state.  Shows registers, iteration info, and both stacks.  In the web-based environment, break execution.
<code>&#124;u</code>	|Un-eval.  Produce string representation of array. e.g. `"[1, 2, 3, [4, 5]]"`
<code>&#124;V</code>	|Array of command line arguments.  This will be an empty array for non-command-line invocations.

## Stack
op                  	|Example           	|Name         	|Description
---                 	|----              	|----         	|--------------
`a`                 	|… a b c -> … b c a	|alter-stack  	|Moves the third element in the stack to the top.
`b`                 	|… a b -> … a b a b	|both-copy    	|Copies the top two stack elements
`c`                 	|… a -> … a a      	|copy         	|Copy the top stack element
`d`                 	|… a b -> … a      	|discard      	|Pop and discard top stack element
`n`                 	|… a b -> … a b a  	|next         	|Copy second element to top of stack
`[`                 	|… a b -> … a a b  	|under        	|Copy second element in-place
`~`                 	|                  	|input-push   	|Pop top item, and push it to input stack.
`;`                 	|                  	|input-peek   	|Peek from input stack, and push to main stack.
`,`                 	|                  	|input-pop    	|Pop from input stack, and push to main stack.
`e`                 	|arr               	|eval         	|Parse string as data to be pushed on the stack. e.g. [12, 3.4, 0x7fff, 0b110011, [5/6, "a string"]]  Multiple top level values in the same string will be pushed separately. `\"`, `\\`, `\n`, and `\x41` are escapes.
`E`                 	|arr               	|explode      	|Push all elements from array onto stack.
`L`                 	|… -> […]          	|listify-stack	|Clear both stacks, and put all items in an array back on stack.
`l`                 	|int               	|listify-n    	|Pop n items, and put them in an array on top of the stack.
`O`                 	|… a -> … 1 a      	|tuck-1       	|Push the value 1 under the top element of the stack.
`s`                 	|… a b -> … b a    	|swap         	|Swap top two stack elements.
`Z`                 	|… a -> … 0 a      	|tuck-0       	|Push the value 0 under the top element of the stack.
<code>&#124;d</code>	|                  	|main-depth   	|Size of main stack.
<code>&#124;D</code>	|                  	|input-depth  	|Size of input stack.


## Numerics
op                       	|Types      	|Name          	|Pseudo-code        	|Description
---                      	|---        	|---           	|---                	|---
`0123456789`             	|           	|              	|                   	|Integer literal.  Leading `0` is always separate number. Use `A` for 10. `10` is 1 and 0.
`A`                      	|           	|              	|10                 	|Constant 10, as in hexadecimal.
`01234!56789`            	|           	|              	|                   	|Float literal.  `!` indicates a decimal point.  A trailing `0` after a `!` is always a separate int literal.
`+`                      	|num num    	|add           	|a + b              	|Add. Integers widen to fractions.  Fractions widen to floats.
`-`                      	|num num    	|sub           	|a - b              	|Subtract.
`*`                      	|num num    	|mul           	|a * b              	|Multiply.
`/`                      	|num num    	|div           	|a / b              	|Integers will use integer division.
`%`                      	|int int    	|mod           	|a % b              	|Modulus.
`@`                      	|frac       	|floor         	|floor(a)           	|Integer floor of fraction.
`@`                      	|float      	|floor         	|floor(a)           	|Integer floor of float.
`#`                      	|num num    	|pow           	|a ** b             	|Exponent. Can produce an integer, rational or float depending on inputs.
`v`                      	|num        	|dec           	|a - 1              	|Decrement by 1.
`^`                      	|num        	|inc           	|a + 1              	|Increment by 1.
`B`                      	|frac       	|properize     	|floor(a), a%1      	|Properize fraction.  Push integer floor, and proper remainder separately.
`B`                      	|float      	|float-binary   |                   	|Get binary IEEE-754 representation as 64-element array of bits.
`D`                      	|num        	|frac-part     	|a%1                	|Get fractional non-integer part of rational or float.
`e`                      	|frac       	|ceil          	|ceiling(a)         	|Integer ceiling of fraction.
`e`                      	|float      	|ceil          	|ceiling(a)         	|Integer ceiling of float.
`E`                      	|int        	|dec-digits    	|                   	|Array of decimal digits.  Absolute value is used in case of negative number.
`E`                      	|frac       	|num-den       	|                   	|Push numerator and denominator separately.
`h`                      	|int        	|halve         	|a / 2              	|Floor divide by 2.
`h`                      	|num        	|halve         	|a / 2              	|Divide by 2.
`H`                      	|num        	|un-halve      	|a * 2              	|Double.
`I`                      	|int int    	|bit-and       	|a & b              	|Bitwise and.
`M`                      	|int int    	|bit-or        	|a &#124; b         	|Bitwise or.
`S`                      	|int int    	|bit-xor       	|a ^ b              	|Bitwise xor.
`j`                      	|float int  	|float-round   	|round(a, b)        	|Round float to n decimal places.  Format as string.
`j`                      	|float      	|round         	|round(a)           	|Round to nearest integer.
`j`                      	|frac       	|round         	|round(a)           	|Round to nearest integer.
`J`                      	|num        	|square        	|a ** 2             	|Square number.
`l`                      	|frac       	|listify-frac  	|a/b -> [a b]       	|Turn a fraction into a 2-array of numerator and denominator.
`N`                      	|number     	|negate        	|-a                 	|Negate a number.
`r`                      	|frac       	|numerator     	|numerator(a)       	|Get the numerator.
`R`                      	|frac       	|denom         	|denom(a)           	|Get the denominator.
`t`                      	|num num    	|min           	|min(a, b)          	|Lower of two values.
`T`                      	|num num    	|max           	|max(a, b)          	|Higher of two values.
`u`                      	|int        	|fractionalize 	|1 / a              	|Turn integer upside down; into fraction.
`u`                      	|frac       	|invert-frac   	|1 / a              	|Turn fraction upside down; invert.
`u`                      	|float      	|invert-float  	|1 / a              	|Floating point inversion.
`U`                      	|           	|              	|-1                 	|Negative unit.
<code>&#124;%</code>     	|num num    	|divmod        	|a / b, a % b       	|Perform division and modulus.
<code>&#124;/</code>     	|int int    	|div-all       	|                   	|Divide a by b as many times as it will go evenly.
<code>&#124;&lt;</code>  	|int int    	|shift-left    	|a << b             	|Bitshift left.
<code>&#124;></code>     	|int int    	|shift-right   	|a >> b             	|Bitshift right.
<code>&#124;1</code>     	|int        	|parity-sign   	|(-1) ** a          	|Power of negative one.
<code>&#124;2</code>     	|int        	|2-power       	|2 ** a             	|Power of two.
<code>&#124;3</code>     	|int        	|base-36       	|                   	|Convert to base 36.
<code>&#124;3</code>     	|arr        	|base-36       	|                   	|Convert from base 36.
<code>&#124;5</code>     	|num        	|nth-fib       	|                   	|Get nth fibonacci number; 0-indexed.
<code>&#124;6</code>     	|num        	|nth-prime     	|                   	|Get nth prime; 0-indexed.
<code>&#124;7</code>     	|num        	|cosine        	|cos(a)             	|Cosine in radians.
<code>&#124;8</code>     	|num        	|sine          	|sin(a)             	|Sine in radians.
<code>&#124;9</code>     	|num        	|tangent       	|tan(a)             	|Tangent in radians.
<code>&#124;a</code>     	|num        	|abs           	|abs(a)             	|Absolute value.
<code>&#124;A</code>     	|int        	|10-power      	|10 ** a            	|Power of ten.
<code>&#124;b</code>     	|int int    	|convert-base  	|                   	|Convert to base, up to 36.
<code>&#124;b</code>     	|arr int    	|convert-base  	|                   	|Convert from base, up to 36.
<code>&#124;B</code>     	|int        	|convert-binary	|                   	|Convert to binary. (base 2)
<code>&#124;B</code>     	|arr        	|convert-binary	|                   	|Convert from binary. (base 2)
<code>&#124;C</code>     	|int int    	|choose        	|choose(a, b)       	|Binomial coefficient - calculate a choose b.
<code>&#124;e</code>     	|int        	|is-even       	|(a + 1) % 2        	|Is even?
<code>&#124;E</code>     	|int int    	|digits        	|                   	|Generate array of digit values in base.  Absolute value is used in case of negative number.
<code>&#124;E</code>     	|arr int    	|un-digit      	|                   	|Produce the number represented by the array of digits in the given base.
<code>&#124;f</code>     	|int        	|factorize     	|                   	|Prime factorization array.
<code>&#124;F</code>     	|int        	|factorial     	|a!                 	|Factorial.
<code>&#124;g</code>     	|int int    	|gcd           	|                   	|Greatest common denominator.
<code>&#124;H</code>     	|int        	|base-16       	|                   	|Convert to hexadecimal. (base 16)
<code>&#124;H</code>     	|arr        	|base-16       	|                   	|Convert from hexadecimal. (base 16)
<code>&#124;j</code>     	|num        	|rationalize   	|                   	|Convert any number to a rational with minimal loss of precision.  Stax attempts to minimize the denominator.
<code>&#124;l</code>     	|int int    	|lcm           	|                   	|Least common multiple.
<code>&#124;L</code>     	|num num    	|log-n         	|log(a, b)          	|Logarithm in base b of a.
<code>&#124;n</code>     	|int        	|prime-exps    	|                   	|Exponents of sequential primes in factorization. (e.g. `20` -> `[2 0 1]`)
<code>&#124;N</code>     	|int int    	|int-nth-root  	|floor(pow(a, 1/b)) 	|Nth root of an integer, rounded down.  This doesn't use floats internally, and is accurate for large integers.
<code>&#124;p</code>     	|int        	|is-prime      	|                   	|Is prime?
<code>&#124;q</code>     	|num        	|int-sqrt      	|floor(sqrt(abs(a)))	|Integer square root of absolute value.
<code>&#124;Q</code>     	|num        	|sqrt          	|sqrt(abs(a))       	|Float square root of absolute value.
`:~`                     	|int        	|set-interior  	|                   	|Set all interior bits in number.  The result will always be one less than a power of 2.
`:!`                     	|int int    	|coprime       	|iscoprime(a, b)    	|Are a and b coprime?
`:#`                     	|num        	|floatify      	|float(a)           	|Convert to float.
`:-`                     	|num num    	|abs-diff      	|abs(a - b)         	|Absolute difference.
`:@`                     	|int int    	|get-bit       	|(a >> b) & 1       	|Extract a single bit from an integer.
`:[`                     	|int int    	|unset-bit     	|a & ~(1 << b)       	|Unset bit at the specified index.
`:[`                     	|int arr    	|unset-bits    	|                   	|Unset bits at all specified indices.
`:]`                     	|int int    	|set-bit       	|a | (1 << b)       	|Set bit at the specified index.
`:]`                     	|int arr    	|set-bits      	|                   	|Set bits at all specified indices.
`::`                     	|int int    	|toggle-bit    	|a ^ (1 << b)       	|Toggle bit at the specified index.
`::`                     	|int arr    	|toggle-bits   	|                   	|Toggle bits at all specified indices.
`:/`                     	|int int    	|multiplicity  	|                   	|Number of times b will evenly divide a.
`:+`                     	|num        	|sign          	|sign(a)            	|Sign; 1 for positive, -1 for negative.
`:_`                     	|num num    	|float-div     	|1.0 * a / b        	|Float division.
`:1`                     	|int        	|popcount      	|                   	|Count of set bits.
`:2`                     	|num        	|floor-log-2   	|floor(log(a, 2))   	|Floor of log base 2.
`:a`                     	|int int    	|fixed-binary  	|zfill(bin(a), b)   	|Binary representation of a zero-padded to b digits. (e.g. `11`, `6` -> `"001011"`)
`:A`                     	|num        	|floor-log-10  	|floor(log(a, 10))  	|Floor of log base 10.
`:b`                     	|num int int	|between       	|b <= a < c         	|Value is in range?
`:b`                     	|arr        	|binary-decode 	|                   	|Produce a number given as an array of bits (binary digits).
`:B`                     	|int arr    	|custom-base   	|                   	|Encode number in custom base from string characters.
`:B`                     	|int        	|binary-digits 	|                   	|Generate array of bits (binary digits).  Absolute value is used in case of negative number.
`:c`                     	|num int int	|clamp         	|min(max(a, b), c)  	|Ensure value is in range.
`:C`                     	|int        	|catalan       	|choose(2a,a)/(a+1) 	|Catalan number; number of strings with a matched pairs of brackets.
`:d`                     	|int        	|divisors      	|                   	|Get all divisors of n.
`:f`                     	|int        	|factorize-exp 	|                   	|Factorize into pairs of [prime, exponent].
`:F`                     	|int        	|dist-factors  	|                   	|Distinct prime factors.
`:g`                     	|int        	|low-bit       	|                   	|Unset all but the low set bit.
`:G`                     	|int        	|high-bit      	|                   	|Unset all but the high set bit.
`:h`                     	|int int    	|fixed-hex     	|zfill(hex(a), b)   	|Hexadecimal representation of a zero-padded to b digits. (e.g. `258`, `4` -> `"0102"`)
`:J`                     	|num num    	|square-two    	|a\*\*2, b\*\*2     	|Square top two elements; useful for hypotenuse and things.
`:m`                     	|int int    	|next-multiple 	|                   	|If necessary, increase a until it is a multiple of b.
`:n`                     	|int int    	|n-factorize   	|                   	|Get all n-part factorizations. (e.g. `4`, `3` => `[[1,1,4],[1,2,2],[1,4,1],[2,1,2],[2,2,1],[4,1,1]]`)
`:N`                     	|int int    	|is-power      	|                   	|Can a be expressed as some integer to the b power?
`:p`                     	|int        	|last-prime    	|                   	|Last prime < n.
`:P`                     	|int        	|next-prime    	|                   	|Next prime >= n.
`:t`                     	|int        	|totient       	|totient(a)         	|Euler's totient of n.

## Logic
op                  	|Name        	|Description
---                 	|---         	|---
`!`                 	|not         	|Logical not.  Produces 0 or 1.  Numeric 0 and empty lists are considered falsy.  All other values are truthy.
`<`                 	|less        	|Is less than.  Arrays use string-style lexicographic ordering.
`>`                 	|greater     	|Is greater than.  Arrays use string-style lexicographic ordering.
`=`                 	|equal       	|Equals.  Numberic types are coerced as necessary.  Arrays are equal to scalars if their first element is equal.
<code>&#124;$</code>	|cmp         	|Compare.  Produces -1, 0, or 1 depending on comparison of two values.
`?`                 	|if-then-else	|If the first value, then yield the second, else the third.  If the result is a block, execute it.
<code>&#124;4</code>	|is-array    	|Tests if a value is an array or not.  Produces 0 or 1.

## String
Strings are really just arrays of integers, but some operations are oriented towards strings anyway.  In these contexts, 0 is usually converted to 32, so that 0 can be used as a space in addition to its normal codepoint.

op                       	|Types          	|Name              	|Description
---                      	|---            	|---               	|---
`#`                      	|arr arr        	|count-substrings  	|Count non-overlapping occurrences of substring b in a.
`"…"`                    	|               	|string-literal    	|String literal stored as an array of codepoints.  Unterminated string literals will be printed implicitly.  `` ` `` is the escape character for `` ` `` and `"`. The characters `01234` yield `\0 \n \t \r \v` respectively when escaped. All other escaped single characters will execute as stax code and then include the popped value as a template. (Space is no-op)
`` `…` ``                	|               	|compressed-string 	|Compressed string literal encoded with contextual Huffman trees.  Not all strings can be encoded this way, but most that can will be smaller.  Unterminated compressed literals will be printed implicitly.
`'a`                     	|               	|char-literal      	|Create a single character string literal.
`.ab`                    	|               	|two-char-literal  	|Create a two character string literal.
`/`                      	|arr arr        	|split             	|Split on substrings.
`*`                      	|arr int        	|repeat            	|Repeat string n times.  If n is negative, reverse array.
`*`                      	|int arr        	|repeat            	|Repeat string n times.  If n is negative, reverse array.
`*`                      	|arr arr        	|join              	|Join array of strings with delimiter.
`$`                      	|num            	|tostring          	|Convert number to string.
`$`                      	|arr            	|arr-tostring      	|Convert each element to string, and concat. Nested arrays will just be flattened. 
`(`                      	|arr arr        	|begin-with        	|Overwrite the beginning of b with a.
`)`                      	|arr arr        	|end-with          	|Overwrite the end of b with a.
`v`                      	|arr            	|lower             	|To lower case.
`^`                      	|arr            	|upper             	|To upper case.
`I`                      	|arr arr        	|substring-index   	|Get the index of the first occurrence of the substring.
`j`                      	|arr            	|space-split       	|String split by space.
`J`                      	|arr            	|space-join        	|Join strings by space.
`R`                      	|arr arr arr    	|regex-replace     	|Regex replace using ECMA regex.
`t`                      	|arr            	|trim-left         	|Trim whitespace from left of string.
`T`                      	|arr            	|trim-right        	|Trim whitespace from right of string.
<code>&#124;~</code>     	|arr arr        	|last-index-of     	|Get the index of the last occurrence of the substring.
<code>&#124;&lt;</code>  	|arr            	|left-align        	|Left align lines of text, padding to longest line.
<code>&#124;></code>     	|arr            	|right-align       	|Right align lines of text, padding to longest line.
<code>&#124;&#124;</code>	|arr int int arr	|embed-grid        	|Embed a grid inside another grid at the specfied coordinates.  Negative coordinates are not allowed.  OOB extends the necessary dimensions.
<code>&#124;e</code>     	|arr arr arr    	|replace-first     	|String replace; first instance only.
<code>&#124;C</code>     	|arr int        	|center            	|Center string in n characters.
<code>&#124;C</code>     	|arr            	|center-block      	|Center lines of text, using longest line.
<code>&#124;F</code>     	|arr arr        	|regex-find-all    	|Get all regex pattern matches.
<code>&#124;I</code>     	|arr arr        	|str-index-all     	|Find all indices of the substring.
<code>&#124;j</code>     	|arr            	|split-newline     	|Split string on newlines.
<code>&#124;J</code>     	|arr            	|join-newline      	|Join strings with newline.
<code>&#124;q</code>     	|arr arr        	|regex-indices     	|Get all indices of regex matches.
<code>&#124;Q</code>     	|arr arr        	|regex-is-match    	|Regex matches entire string?
<code>&#124;s</code>     	|arr arr        	|regex-split       	|Split by ECMA regex. Captured groups will be included in result.
<code>&#124;S</code>     	|arr arr        	|surround          	|Prepend and append string/array.  Wraps the first array between copies of the second.
<code>&#124;t</code>     	|arr arr        	|translate         	|Translate first string using pairs of elements in the second array.  Instances of the first in a pair will be replaced by the second.
<code>&#124;z</code>     	|arr int        	|zero-fill         	|Fill on the left with "0" to specified length.
`:~`                     	|arr            	|toggle-case       	|Toggle case of letters in string.
`:.`                     	|arr            	|title-case        	|Convert string to title case.
`:/`                     	|arr arr        	|split-once        	|Split on the first occurrence of a substring.  Push both parts separately.
`:[`                     	|arr arr        	|starts-with       	|String starts with?
`:]`                     	|arr arr        	|ends-with         	|String ends with?
`:{`                     	|any            	|parenthesize      	|Embed value in parentheses as string.
`:}`                     	|any            	|bracercise        	|Embed value in square braces as string.
`:3`                     	|arr            	|rot-13            	|Rot13 encode/decode; dual-purpose.
`:c`                     	|arr int        	|set-case          	|Set case of string. If integer is zero, use upper case.  Otherwise, lower.
`:D`                     	|arr int        	|trim-both         	|Trim element from both ends of string.
`:D`                     	|arr arr        	|trim-both         	|Trim all characters from both ends of string.
`:e`                     	|arr            	|excerpts          	|Get all contiguous subarrays.
`:h`                     	|arr            	|taglify           	|Embed string in angle brackets.
`:R`                     	|arr            	|brace-reflect     	|Reflect string, `(<[{/` becomes `\}]>)`
`:t`                     	|arr arr        	|ring-translate    	|Map matching elements to the subsequent element in the translation ring.  The ring wraps around.
`:w`                     	|arr            	|brace-palindromize	|Concatenate all but the last character reversed.  Braces and slashes are individually reversed also.
`:W`                     	|arr            	|brace-mirror      	|Concatenate the string reversed.  Braces and slashes are individually reversed also.
`:x`                     	|arr            	|regex-escape      	|Escape regex special characters.  (e.g. `"c:\foo.txt"` -> `"c:\\foo\.txt"`)

## Array
op                  	|Types              	|Name              	|Description
---                 	|---                	|---               	|---
`"…"!`              	|                   	|crammed-array     	|Crammed array of integers. This uses a scheme to represent arbitrary integers in a string literal, and then uncram them.  There will be at most one number per character in the string literal.  Large numbers require more characters.
`#`                 	|arr num            	|count-instances   	|Count instances of b in a.
`#`                 	|num arr            	|count-instances   	|Count instances of b in a.
`+`                 	|arr arr            	|concat            	|Concatenate arrays.
`+`                 	|num arr            	|prepend           	|Prepend element to array.
`+`                 	|arr num            	|append            	|Append element to array.
`-`                 	|arr arr            	|array-diff        	|Remove all elements in b from a.
`-`                 	|arr num            	|array-remove      	|Remove all instances of b from a.
`*`                 	|matrix matrix      	|matrix-mul        	|Matrix multiplication. A matrix is any array that contains at least one array, and no non-arrays.
`/`                 	|arr int            	|array-group       	|Split array into groups of specified size.  The last group will be smaller if it's not a multiple.  If the divisor is zero, the result will be an empty array.  If the divisor is negative, the array will be reversed first.
`%`                 	|arr                	|length            	|Array length
`%`                 	|arr int            	|split-at          	|Split array at index; push both parts.
`\`                 	|num num            	|pair              	|Make a 2 length array.
`\`                 	|arr num            	|array-pair        	|Make array of pairs, all having identical second element.
`\`                 	|num arr            	|array-pair        	|Make array of pairs, all having identical first element.
`\`                 	|arr arr            	|zip-repeat        	|Make array of pairs, zipped from two arrays.  The shorter is repeated as necessary.
`@`                 	|arr int            	|element-at        	|Get element at 0-based modular index.  (-1 is the last element)
`@`                 	|int arr            	|element-at        	|Get element at 0-based modular index.  (-1 is the last element)
`@`                 	|arr int int ...    	|element-at        	|Get element in multi-dimensional array using all integer indices.
`@`                 	|arr arr            	|elements-at       	|Get elements at all indices.
`&`                 	|arr int any        	|assign-index      	|Assign non-block element at index.  Negatives index backwards.  OOB extends the array.
`&`                 	|arr int ... int any	|assign-index      	|Assign non-block element in multidimensional array of arrays located at specified coordinates.  Negative coordinates are not allowed.  OOB extends the array(s);
`&`                 	|arr arr any        	|assign-indices    	|Assign element at all indices.  If indices array is an array of arrays, then treat them as a path to navigate a multidimensional array of arrays.
`&`                 	|arr int block      	|mutate-element    	|Mutate element at index using block.
`&`                 	|arr arr block      	|mutate-element    	|Mutate element at indices using block. If indices array is an array of arrays, then treat them as a path to navigate a multidimensional array of arrays.
`(`                 	|arr int            	|pad-right         	|Truncate or pad on right with 0s as necessary for target length.  Negative numbers remove that number of elements.
`(`                 	|int arr            	|pad-right         	|Truncate or pad on right with 0s as necessary for target length.  Negative numbers remove that number of elements.
`)`                 	|arr int            	|pad-left          	|Truncate or pad on left with 0s as necessary for target length.  Negative numbers remove that number of elements.
`)`                 	|int arr            	|pad-left          	|Truncate or pad on left with 0s as necessary for target length.  Negative numbers remove that number of elements.
`]`                 	|any                	|singleton         	|Make a 1 element array.
`B`                 	|arr int            	|batch             	|Get all (overlapping) sub-arrays of specified length.
`B`                 	|arr                	|uncons-left       	|Remove first element from array.  Push the tail of the array, then the removed element.
`D`                 	|arr                	|drop-first        	|Remove first element from array.
`h`                 	|arr                	|first             	|Get first element.
`H`                 	|arr                	|last              	|Get last element.
`I`                 	|arr num            	|index-of          	|Get the index of the first occurrence.
`I`                 	|num arr            	|index-of          	|Get the index of the first occurrence.
`M`                 	|arr int            	|chunkify          	|Partition a into b chunks of almost equal size.  The largest chunks come first. They're one larger than the smaller ones.
`M`                 	|arr                	|transpose         	|Flip array of arrays about the diagonal. Non-array elements are considered to be singleton arrays.  Arrays are padded with zeroes as necessary to preserve layout. 
`N`                 	|arr                	|uncons-right      	|Remove last element from array.  Push the beginning of the array, then the removed element.
`r`                 	|int                	|0-range           	|Make range [0 .. n-1].
`r`                 	|arr                	|reverse           	|Reverse array.
`R`                 	|int                	|1-range           	|Make range [1 .. n].
`t`                 	|arr int            	|remove-left       	|Trim n elements from left of array.  Negative numbers prepend that many zeroes.
`T`                 	|arr int            	|remove-right      	|Trim n elements from right of array.  Negative number append that many zeroes.
`S`                 	|arr                	|powerset          	|Get all combinations of elements in array.  If the array was ordered, all combinations will be in lexicographic order.
`S`                 	|arr int            	|combinations      	|Get all combinations of specified size from array.  If the array was ordered, all combinations will be in lexicographic order.
`u`                 	|arr                	|unique            	|Keep only unique elements in array, maintaining first order of appearance.
`z`                 	|                   	|                  	|Push empty array/string.
<code>&#124;0</code>	|arr                	|falsy-index       	|Get index of first falsy element.
<code>&#124;1</code>	|arr                	|truthy-index      	|Get index of first truthy element.
<code>&#124;2</code>	|arr                	|diagonal          	|Get the diagonal of a matrix.  Missing elements are filled with 0.
<code>&#124;=</code>	|arr                	|multi-mode        	|Get all tied modes of array.
<code>&#124;+</code>	|arr                	|sum               	|Sum of array.
<code>&#124;!</code>	|int int            	|int-partitions    	|Get all the distinct b-length arrays of positive integers that sum to a.
<code>&#124;!</code>	|arr int            	|arr-partitions    	|Get all the ways of splitting array a into b pieces.
<code>&#124;!</code>	|arr                	|multi-anti-mode   	|Get all tied rarest elements of array.
<code>&#124;@</code>	|arr int            	|remove-at         	|Remove element from array at index.
<code>&#124;@</code>	|arr int num        	|insert-at         	|Insert element to array at index.
<code>&#124;#</code>	|arr arr            	|verbatim-count    	|Counts number of occurrences of b as an element of a, without any string flattening.
<code>&#124;%</code>	|arr int arr        	|embed-array       	|Embed c in a, starting at position b.  Negative indices from the end.  OOB extend the array.
<code>&#124;&</code>	|arr arr            	|arr-intersect     	|Keep all elements from a that are in b.
<code>&#124;^</code>	|arr arr            	|arr-xor           	|Keep all elements from a that are not in b, followed by all elements in b that are not in a.
<code>&#124;^</code>	|arr int            	|multi-self-join   	|Generate all arrays of size b using elements from a.
<code>&#124;*</code>	|arr int            	|repeat-elements   	|Repeat each element in a, abs(b) times.
<code>&#124;*</code>	|arr arr            	|cross-product     	|Cartesian join of arrays, producing a flat array of pairs.
<code>&#124;/</code>	|arr arr            	|multi-group       	|Partition a into differently sized groups from b.
<code>&#124;-</code>	|arr arr            	|multiset-subtract 	|Remove elements in b individually from a, if they're present.
<code>&#124;-</code>	|arr num            	|remove-first      	|Remove first instance of b from a.
<code>&#124;\\</code>	|arr arr            	|zip-short         	|Zip arrays producing pairs.  The longer array is truncated.
<code>&#124;\\</code>	|arr arr num        	|zip-fill          	|Zip arrays producing pairs.  The shorter array is extended using the fill element.
<code>&#124;)</code>	|arr                	|rotate-right      	|Move the last element of an array to the front.
<code>&#124;)</code>	|arr int            	|rotate-right-n    	|Shift array n places to the right, rotating the end to the front.
<code>&#124;(</code>	|arr                	|rotate-left       	|Move the first element of an array to the end.
<code>&#124;(</code>	|arr int            	|rotate-left-n     	|Shift array n places to the right, rotating the front to the end.
<code>&#124;[</code>	|arr                	|prefixes          	|All prefixes of array.
<code>&#124;]</code>	|arr                	|suffixes          	|All suffixes of array.
<code>&#124;{</code>	|arr arr            	|setwise-equal     	|Arrays are setwise equal? e.g. `[1,2,2]`, `[2,1]` -> `1`
<code>&#124;}</code>	|arr arr            	|multiset-equal    	|Arrays are different orderings of same elements? e.g. `[1,2,2]`, `[2,1,2]` -> `1`
<code>&#124;a</code>	|arr                	|any               	|Any elements of array are truthy?
<code>&#124;A</code>	|arr                	|all               	|All elements of array are truthy?
<code>&#124;b</code>	|arr arr            	|multiset-intersect	|Keep the elements from a that occur in b, no more than the number of times they occur in b.
<code>&#124;g</code>	|arr                	|gcd               	|Greatest common denominator of array.
<code>&#124;G</code>	|arr                	|round-flatten     	|Flatten array of arrays using round-robin distribution.  Cycle between inner arrays until all elements are reached.
<code>&#124;I</code>	|arr num            	|find-all          	|Find all indices of occurrences of the value.
<code>&#124;l</code>	|arr                	|lcm               	|Least common multiple of array.
<code>&#124;L</code>	|arr arr            	|multiset-union    	|Combine elements from a and b, with each occurring the max of its occurrences from a and b.
<code>&#124;m</code>	|arr                	|min               	|Minimum value in array.
<code>&#124;M</code>	|arr                	|max               	|Maximum value in array.
<code>&#124;n</code>	|arr arr            	|multiset-xor      	|Combine elements from a and b, removing common elements only as many times as they mutually occur.
<code>&#124;N</code>	|arr                	|next-perm         	|Get the next permuation of elements in the array, ordered lexicographically.
<code>&#124;o</code>	|arr                	|ordered-indices   	|Calculate destination index of each element if array were to be sorted.
<code>&#124;p</code>	|arr                	|palindromize      	|a + reversed(a[:-1]).  Always has an odd length.
<code>&#124;r</code>	|int int            	|explicit-range    	|Range [a .. b). If a is an array, use the opposite of its length instead.  If b is an array, use its length instead.
<code>&#124;R</code>	|int int int        	|stride-range      	|Range [a .. b) with stride of c.
<code>&#124;R</code>	|arr                	|run-length        	|Encode runs of elements into an array of [element, count] pairs.
<code>&#124;T</code>	|arr                	|permutations      	|Get all orderings of elements in the array.  Duplicate elements are not considered.
<code>&#124;T</code>	|arr int            	|permutations      	|Get all b-length orderings of elements in a.  Duplicate elements are not considered.
<code>&#124;w</code>	|arr arr            	|trim-left-els     	|Remove matching leading elements from array.
<code>&#124;W</code>	|arr arr            	|trim-right-els    	|Remove matching trailing elements from array.
<code>&#124;Z</code>	|arr                	|rectangularize    	|Rectangularize an array of arrays, using "" for missing elements.
`:0`                	|arr                	|falsy-indices     	|Get all indices of falsy elements.
`:1`                	|arr                	|truthy-indices    	|Get all indices of truthy elements.
`:2`                	|arr                	|self-cross-product	|Pairs of elements, with replacement.
`::`                	|arr int            	|every-nth         	|Every nth element in array, starting from the first.
`:<`                	|arr                	|col-align-left    	|Left-align columns by right-padding with spaces.
`:>`                	|arr                	|col-align-right   	|Right-align columns by left-padding with spaces.
`:*`                	|arr                	|product           	|Product of numbers in array.
`:-`                	|arr                	|deltas            	|Pairwise difference of array.
`:+`                	|arr                	|prefix-sums       	|Get the sums of all prefixes.
`:\`                	|arr arr            	|diff-indices      	|Get indices of unequal elements between arrays.
`:_`                	|arr                	|reduce-array      	|Divide all integers in array by their collective gcd.
`:=`                	|arr arr            	|equal-indices     	|Get indices of equal elements between arrays.
`:!`                	|arr                	|all-partitions    	|Get all the ways of splitting array into pieces.
`:@`                	|arr                	|truthy-count      	|Count the number of truthy elements in the array.
`:@`                	|arr int            	|pop-at            	|Remove element at specified index from array.  Push what's left of the array, and the element removed separately.
`:$`                	|arr int            	|cartesian-power   	|Cartesian power of array. e.g. `"ab"`, `3` -> `["aaa","aab","aba","abb","baa","bab","bba","bbb"]`
`:$`                	|arr                	|cartesian-product 	|Cartesian product of subarray. e.g. `["ab", "xy"]` -> `["ax","ay","bx","by"]`
`:,`                	|arr arr            	|zip-end           	|Zip arrays producing pairs.  The longer array has its prefix dropped, so that the ends of the arrays align.
`:(`                	|arr                	|left-rotations    	|All left rotations, starting from original.
`:)`                	|arr                	|right-rotations   	|All right rotations, starting from original.
`:^`                	|arr                	|non-descending    	|Is array non-descending? (has no adjacent pair of descending elements)
`:v`                	|arr                	|non-ascending     	|Is array non-ascending? (has no adjacent pair of ascending elements)
`:a`                	|arr                	|minima-indices    	|Get indices of array minima.
`:A`                	|arr                	|maxima-indices    	|Get indices of array maxima.
`:B`                	|arr arr            	|element-repeats   	|Repeat element in a by corresponding integer in b, wrapped.  e.g. `"abcdef"`, `[0,1,2]` -> `"bcceff"`
`:c`                	|arr                	|coalesce          	|Get first truthy element of array.
`:C`                	|arr                	|columns           	|Gets the columns of an array of arrays without rectangularizing.  Similar to transpose, but doesn't use a fill element.  Instead, collapse the gaps.  e.g. `[[1,2],[],[3],[4,5,6]]` -> `[[1,3,4],[2,5],[6]]`
`:d`                	|arr                	|median            	|Get the median of array.  Integers produce a rational result.  Floats produce a float.
`:E`                	|arr                	|first-last         |Get the first and last element of an array. e.g. `[1,2,3,4]` -> `[1, 4]`
`:f`                	|arr                	|flatten           	|Flatten array; for each element of the array unwrap it if it's an array.  e.g. `[3,4,[5,[6]]]` -> `[3,4,5,[6]]`
`:F`                	|arr                	|falsy-indices     	|~~Get all indices of falsy elements.~~ Deprecated.  Use `:0` instead.
`:g`                	|arr                	|run-elements      	|Remove adjacent duplicate elements from array.
`:G`                	|arr                	|run-lengths       	|Get the lengths of runs of duplicate elements.
`:i`                	|arr                	|interleave        	|Distribute elements from array of arrays in a round-robin fashion.  e.g. `[[1,2],[],[3],[4,5,6]]` -> `[1,3,4,2,5,6]`
`:I`                	|arr arr            	|find-index-all    	|For each element in b, find the index of the first occurrence in a.
`:J`                	|arr                	|squarify          	|Wrap array into smallest fitting square, filling any extra spaces with 0.
`:m`                	|arr int            	|repeat-to         	|Repeat array until it is exactly length n.
`:m`                	|arr                	|mirror            	|Append reversed copy to array.
`:M`                	|arr                	|mode              	|Mode.  In case of tie, the last element to appear wins.
`:o`                	|arr arr            	|overlay           	|Keep the maximum element respective element from two arrays.
`:p`                	|arr                	|shortest-suffixes 	|Keep suffixes of arrays only as long as shortest array.
`:P`                	|arr                	|shortest-prefixes 	|Keep prefixes of arrays only as long as shortest array.
`:r`                	|arr arr            	|replace-all       	|Replace all substring occurrences.
`:r`                	|int                	|centered-range    	|Make range [-n .. n] centered around 0.
`:s`                	|arr                	|span              	|Get the span of an array. (maximum minus minimum)
`:S`                	|arr arr            	|is-superset-of    	|Is a a (non-strict) superset of b?
`:T`                	|arr                	|truthy-indices    	|~~Get all indices of truthy elements.~~ Deprecated.  Use `:1` instead.
`:u`                	|arr                	|multi-single      	|Array contains exactly 1 distinct element?
`:V`                	|arr                	|mean              	|Mean of array. (rational or float)

## Blocks

op                  	|Types        	|Name               	|Description
---                 	|---          	|---                	|---
`{`                 	|             	|                   	|Begin a block.  Blocks can be ended by any block terminator, not just }.
`}`                 	|             	|                   	|Terminate a block and push to stack.  If there is not a block currently open, start program execution over.
`*`                 	|int block    	|do-times           	|Perform block n times.
`*`                 	|block int    	|do-times           	|Perform block n times.
`!`                 	|block        	|execute-block      	|Execute a block.  Does not terminate a block.
`/`                 	|arr block    	|group-by           	|Group adjacent values that produce equal values using the block.  Does not terminate a block.
`(`                 	|arr block    	|partition-when     	|Partition the original array into consecutive subarrays that begin when the block produces a truthy value. The block is provided with the element following the boundary.
`)`                 	|arr block    	|partition-when-pair	|Partition the original array into consecutive subarrays that begin when the block produces a truthy value. The block is provided with the pair of elements around the boundary.
`B`                 	|block        	|triple-execute     	|Execute a block thrice.  Does not terminate a block.
`C`                 	|any          	|cancel             	|If value is truthy, cancel current block execution.
`C`                 	|arr block    	|collect            	|Reduce using block, but collect each value in result array.  Does not terminate a block.
`D`                 	|int          	|do-times           	|Execute the rest of the program n times.  _ will give the 1-based iteration count.
`D`                 	|block        	|double-execute     	|Execute a block twice.  Does not terminate a block.
`e`                 	|block        	|min-by             	|Get the values which yield the minimum value when applying the block to the array. Does not terminate a block.
`E`                 	|block        	|max-by             	|Get the values which yield the maximum value when applying the block to the array. Does not terminate a block.
`f`                 	|arr block    	|filter             	|Terminate a block and filter array using it as a predicate.
`f`                 	|arr          	|filter-short       	|If there is no open block, use the rest of the program as the predicate.  Print passing elements on separate lines.
`f`                 	|int          	|filter-range-short 	|Use the rest of the program as a block to filter the range [1 .. n].  Use the rest of the program as the predicate.  Print passing elements on separate lines.
`f`                 	|inf          	|filter-inf-short   	|When a floating infinity `VI` is encountered, use the rest of the program as a block to filter positive integers [1 .. ] ceaselessly and without end.  Use the rest of the program as the predicate.  Print passing elements on separate lines.
`F`                 	|arr block    	|foreach            	|Terminate a block.  Push each element of the array, and execute the block for each.
`F`                 	|arr          	|foreach-short      	|If there is no open block, use the rest of the program as the block.  Execute it after pushing each element.
`F`                 	|int          	|for-short          	|Perform `foreach-short` using the range [1 .. n].
`g`                 	|             	|generator          	|Generate values.  See `generators` for details.
`G`                 	|             	|goto               	|Jump to an unmatched trailing `}` at the end of the program.  If there are are none, jump to the beginning of the current program.  Come back when finished.
`h`                 	|arr block    	|take-while         	|Keep run of matching elements, if any at the beginning of the array.  Does not terminate a block.
`H`                 	|arr block    	|take-while-end     	|Keep run of matching elements, if any at the end of the array.  Does not terminate a block.
`i`                 	|             	|index              	|Get the current 0-based iteration index of the inner loop.
<code>&#124;;</code>	|             	|iteration-parity   	|Get the parity of the current iteration index. (0 or 1)
<code>&#124;i</code>	|             	|outer-index        	|Get the 0-based iteration index of the outer loop.
`I`                 	|arr block    	|index-of-block     	|Get the index of the first element that yields a truthy value from the block. Does not terminate a block.
`j`                 	|arr block    	|first-match        	|Get the first match from the array - the first value for which the block produces a truthy value.  Does not terminate a block.
`J`                 	|arr block    	|last-match         	|Get the last match from the array - the last value for which the block produces a truthy value.  Does not terminate a block.
`k`                 	|arr block    	|reduce             	|Terminate a block and reduce (fold) using the block.
`k`                 	|int block    	|reduce-range       	|Terminate a block and reduce (fold) [1 .. n] using the block.
`k`                 	|arr          	|reduce-short       	|If there is no open block, use the rest of the program as the block to reduce the array.  Implicitly print the result.
`k`                 	|int          	|reduce-short-range 	|If there is no open block, use the rest of the program as the block to reduce [1 .. n].  Implicitly print the result.
`K`                 	|arr arr block	|cross-map          	|Terminate a block and map using it over a cartesian join.  If integers are provided, a 1-based range will be used.  Both elements will be pushed to the stack.  `_` will also push both to stack.  The result will be an array of arrays.
`K`                 	|arr arr      	|cross-map-short    	|If there is no open block, use the rest of the program to map over a cartesian join.  If integers are provided, a 1-based range will be used.  Both elements will be pushed to the stack.  `_` will also push both to stack.  Each row will be implicitly printed with a newline.
`m`                 	|arr block    	|map                	|Terminate a block and map using a block.  If the block execution is cancelled, that element won't be included in the result.
`m`                 	|arr          	|map-short          	|If there is no open block, use the rest of the program as the block.  Print each mapped element with a new-line.
`m`                 	|int          	|map-range-short    	|Use the rest of the program as a block to map [1 .. n].  Print each mapped element with a new-line.
`m`                 	|inf          	|map-inf-short      	|When a floating infinity `VI` is encountered, use the rest of the program as a block to map positive integers [1 .. ] ceaselessly and without end.  Print each mapped element with a new-line.
`M`                 	|any block    	|maybe              	|Execute block if value is truthy.  Does not terminate a block.
`N`                 	|block        	|repeat-from-input  	|Pop integer from input stack, and repeats block that many times. `{...}N` is functionally equivalent to `{...},*`
`o`                 	|arr block    	|order              	|Terminate a block and order array by key.  If there are no open blocks, order the array itself.
`t`                 	|arr block    	|trim-start-block   	|Remove elements from the start of the array that are matched the block predicate.  Does not terminate a block.
`T`                 	|arr block    	|trim-end-block     	|Remove elements from the end of the array that are matched the block predicate.  Does not terminate a block.
`w`                 	|block        	|do-while           	|Terminate a block and iterate until it produces a falsy value.
`w`                 	|             	|do-while-short     	|If there is no open block, use the rest of the program as the block.
`W`                 	|block        	|while              	|Terminate a block and iterate forever.  Cancelling will terminate, as with all blocks.
`W`                 	|             	|while-short        	|If there is no open block, use the rest of the program as the block.
`_`                 	|             	|current            	|Get the current iteration value.  If there are no blocks executing, this will be all of standard input, as one string.
<code>&#124;c</code>	|             	|contend            	|Assert top of stack is truthy.  Cancel if not.  Do not pop.
<code>&#124;I</code>	|arr block    	|filter-index       	|Get all indices in the array that produce a truthy value from the block.

## Registers
op                  	|Description
---                 	|---
`x`                 	|Value of register x.  Default is parsed integer value from standard input, or 0.
`X`                 	|Peek and write register x.
<code>&#124;x</code>	|Decrement register x and push.  If x is not an integer, set it to 0 first.
<code>&#124;X</code>	|Increment register x and push.  If x is not an integer, set it to 0 first.
`y`                 	|Value of register y.  Default is first line of standard input.
`Y`                 	|Peek and write register y.
<code>&#124;y</code>	|Decrement register y and push.  If y is not an integer, set it to 0 first.
<code>&#124;Y</code>	|Increment register y and push.  If y is not an integer, set it to 0 first.

## Prefix Directives
Some instructions behave differently when they are the first character in a program.  These directives do not apply if the implicit eval of standard input succeeded.

char	|Name        	|Description
--- 	|---         	|---
`f` 	|line-filter 	|Use the rest of the program to filter input lines.  Print lines that produce a truthy result.
`F` 	|line-foreach	|Execute the rest of the program once for each line of input.
`m` 	|line-map    	|Map each line of standard input.  Print each result with a newline.
`i` 	|no-eval     	|Suppress auto-eval.  e.g. If the input is a binary number, this will prevent incorrectly parsing it as an integer.

## Constants

op   	|Value
---  	|---
<code>&#124;?</code>	|source of current program for quines or something
`V?` 	|Version info
`V!`  	|"[a-z]"
`V@`  	|"[A-Z]"
`V#`  	|"[a-zA-Z]"
`V$`  	|"[a-z]+"
`V)`  	|"[A-Z]+"
`V^`  	|"[a-zA-Z]+"
`V&`  	|"[a-z]*"
`V*`  	|"[A-Z]*"
`V(`  	|"[a-zA-Z]*"
`V:`  	|"http://"
`V;`  	|"https://"
`V0` 	|rational 0/1
`V2` 	|0.5
`V3` 	|semitone ratio in equal temperment (pow(2, 1/12))
`V/` 	|pi/3
`V%` 	|[0, 0]
`Va` 	|"abcdefghijklmnopqrstuvwxyz"
`VA` 	|"ABCDEFGHIJKLMNOPQRSTUVWXYZ"
`Vb` 	|"()[]{}<>"
`VB` 	|256
`Vc` 	|"bcdfghjklmnpqrstvwxyz"
`VC` 	|"BCDFGHJKLMNPQRSTVWXYZ"
`Vd` 	|"0123456789"
`VD` 	|sqrt(2)
`Ve` 	|natural log base (e)
`VE` 	|sqrt(3)
`Vh` 	|"0123456789abcdef"
`VH` 	|"0123456789ABCDEF"
`Vi` 	|negative infinity
`VI` 	|positive infinity
`Vk` 	|1000
`Vl` 	|"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
`VL` 	|"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
`Vm` 	|0x7ffffffff
`VM` 	|1000000
`Vn` 	|newline (newline is also `A]`)
`Vp` 	|all printable ascii characters
`VP` 	|pi
`Vq` 	|pi/2
`Vs` 	|all ascii whitespace
`VS` 	|4/3 pi
`Vt` 	|tau (2pi)
`VT` 	|10.0
`Vu` 	|pow(2, 32)
`VV` 	|"AEIOU"
`Vv` 	|"aeiou"
`Vx` 	|[Packed stax character encoding](./packed.md) expressed as array of 256 codepoints
`VW` 	|"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
`Vw` 	|"0123456789abcdefghijklmnopqrstuvwxyz"
`VY` 	|"AEIOUY"
`Vy` 	|"aeiouy"
`VZ` 	|"BCDFGHJKLMNPQRSTVWXZ"
`Vz` 	|"bcdfghjklmnpqrstvwxz"

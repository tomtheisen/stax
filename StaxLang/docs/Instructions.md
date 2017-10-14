Section
instruction
Types
pseudocode
english


Stack
a	... a b c -> ... b c a	alter-stack  	Moves the third element in the stack to the top.
b	... a b -> ... a b a b	both-copy    	Copies the top two stack elements
c	... a -> ... a a      	copy         	Copy the top stack element
d	... a b -> ... a      	discard      	Pop and discard top stack element
n	... a b -> ... a b a  	next         	Copy second element to top of stack
[	... a b -> ... a a b  	under        	Copy second element in-place
~	                      	input-push   	Pop top item, and push it to input stack.
;	                      	input-peek   	Peek from input stack, and push to main stack.
,	                      	input-pop    	Pop from input stack, and push to main stack.
e	arr                   	eval         	Parse string as data to be pushed on the stack. e.g. [12, 3.4, [5/6, "a string"]]  Multiple top level values in the same string will be pushed separately.
E	arr                   	explode      	Push all elements from array onto stack.
L	... -> [...]          	listify-stack	Clear both stacks, and put all items in an array back on stack.
l	int                   	listify-n    	Pop n items, and put them in an array on top of the stack.
O	... a -> ... 1 a      	tuck-1       	Push the value 1 under the top element of the stack.
s	... a b -> ... b a    	swap         	Swap top two stack elements.
Z	... a -> ... 0 a      	tuck-0       	Push the value 0 under the top element of the stack.

Output
p	Pop and print with no newline.
P	Pop and print with a newline.
q	Peek and print with no newline.
Q	Peek and print with a newline.


Numbers
0123456789 	         	             	            	Integer literal.  Leading `0` is always separate number. `10` is `1` and `0`.
A          	         	             	10          	Constant 10, as in hexidecimal.
01234!56789	         	             	            	Float literal.  `!` indicates a decimal point.  A trailing `0` after a `!` is always a separate int literal.
+          	num num  	add          	a + b       	Add. Integers widen to fractions.  Fractions widen to floats.
-          	num num  	sub          	a - b       	Subtract.
*          	num num  	mul          	a * b       	Multiply.
/          	num num  	div          	a / b       	Integers will use integer division.
%          	int int  	mod          	a % b       	Modulus.
@          	frac     	floor        	floor(a)    	Integer floor of fraction.
@          	float    	floor        	floor(a)    	Integer floor of float.
v          	num      	dec          	a - 1       	Decrement by 1.
^          	num      	inc          	a + 1       	Increment by 1.
h          	int      	halve        	a / 2       	Floor divide by 2.
h          	float    	halve        	a / 2       	Divide by 2.
h          	frac     	numerator    	numerator(a)	Get the numerator.
H          	int      	un-halve     	a * 2       	Double.
H          	float    	un-halve     	a * 2       	Double.
H          	frac     	denom        	denom(a)    	Get the denominator.
j          	float int	float-round  	round(a, b) 	Round float to n decimal places.  Format as string.
J          	num      	square       	a ** 2      	Square number.
l          	frac     	listify-frac 	a/b -> [a b]	Turn a fraction into a 2-array of numerator and denominator.
u          	int      	fractionalize	1 / a       	Turn integer upside down; into fraction.
u          	frac     	invert-frac  	1 / a       	Turn fraction upside down; invert.
U          	         	             	-1          	Negative unit.

Logic
!	any        	not         	Logical not.  Produces 0 or 1.  Numeric 0 and empty lists are considered falsy.  All other values are truthy.
<	any any    	less        	Is less than.  Arrays use string-style lexicographic ordering.
>	any any    	greater     	Is greater than.  Arrays use string-style lexicographic ordering.
=	any any    	equal       	Equals.  Numberic types are coerced as necessary.
?	any any any	if-then-else	If the first value, then yield the second, else the third.  If the result is a block, execute it.


String
#    	arr arr    	count-substrings 	Count occurrences of substring b in a.
"..."	           	string-literal   	String literal stored as an array of codepoints. `` ` `` is the escape character.  Unterminated string literals will be printed implicitly.
`...`	           	compressed-string	Compressed string literal encoded with contextual Huffman trees.  Not all strings can be encoded this way, but most that can will be smaller.  Unterminated compressed literals will be printed implicitly.
'a   	           	char-literal     	Create a single character string literal.
/    	arr arr    	split            	Split on substrings.
*    	arr int    	repeat           	Repeat string n times.  If n is negative, reverse array.
*    	int arr    	repeat           	Repeat string n times.  If n is negative, reverse array.
*    	arr arr    	join             	Join array of strings with delimiter.
$    	num        	tostring         	Convert number to string.
$    	arr        	arr-tostring     	Convert each element to string, and concat.
v    	arr        	lower            	To lower case.
^    	arr        	upper            	To upper case.
I    	arr arr    	substring-index  	Get the index of the first occurrence of the substring.
j    	arr        	space-split      	String split by space.
J    	arr        	space-join       	Join strings by space.
R    	arr arr arr	regex-replace    	Regex replace using ECMA regex.
t    	arr        	trim-left        	Trim whitespace from left of string.
T    	arr        	trim-right       	Trim whitespace from right of string.

Array
#	arr num      	count-instances	Count instances of b in a.
+	arr arr      	concat         	Concatenate arrays.
+	num arr      	prepend        	Prepend element to array.
+	arr num      	append         	Append element to array.
-	arr arr      	array-diff     	Remove all elements in b from a.
-	arr num      	array-remove   	Remove all instances of b from a.
/	arr int      	array-group    	Split array into groups of specified size.  The last group will be smaller if it's not a multiple.
%	arr          	length         	Array length
\	num num      	pair           	Make a 2 length array.
\	arr num      	array-pair     	Make array of pairs, all having identical second element.
\	num arr      	array-pair     	Make array of pairs, all having identical first element.
\	arr arr      	zip-repeat     	Make array of pairs, zipped from two arrays.  The shorter is repeated as necessary.
@	arr int      	element-at     	Get element at 0-based index.
@	arr arr      	elements-at    	Get elements at all indices.
&	arr int any  	assign-index   	Assign element at index.
&	arr arr any  	assign-indices 	Assign element at all indices.
&	arr int block	mutate-element 	Mutate element at index using block.
&	arr arr block	mutate-element 	Mutate element at indices using block.
(	arr int      	pad-right      	Truncate or pad on right with 0s as necessary for target length.  Negative numbers remove that number of elements.
)	arr int      	pad-left       	Truncate or pad on left with 0s as necessary for target length.  Negative numbers remove that number of elements.
]	any          	singleton      	Make a 1 element array.
B	arr int      	batch          	Get all (overlapping) sub-arrays of specified length.
B	arr          	uncons-left    	Remove first element from array.  Push the tail of the array, then the removed element.
h	arr          	first          	Get first element.
H	arr          	last           	Get last element.
I	arr num      	index-of       	Get the index of the first occurrence.
I	num arr      	index-of       	Get the index of the first occurrence.
N	arr          	uncons-right   	Remove last element from array.  Push the beginning of the array, then the removed element.
r	int          	0-range        	Make range [0 .. n-1].
r	arr          	reverse        	Reverse array.
R	int          	1-range        	Make range [1 .. n].
t	arr int      	remove-left    	Trim n elements from left of array.
T	arr int      	remove-right   	Trim n elements from right of array.
u	arr          	unique         	Keep only unique elements in array, maintaining first order of appearance.
z	             	               	Push empty array/string.

Blocks
{	             	               	Begin a block.  Blocks can be ended by any block terminator, not just }.
}	             	               	Terminate a block and push to stack.  If there is not a block currently open, start program execution over.
*	int block    	do-times       	Perform block n times.
*	block int    	do-times       	Perform block n times.
C	any          	cancel         	If value is truthy, cancel current block execution.
f	arr block    	filter         	Terminate a block and filter array using it as a predicate.
f	arr          	filter-short   	If there is no open block, use the rest of the program as the predicate.  Print passing elements on separate lines.
f	int          	do-times       	Execute the rest of the program n times.  _ will give the 1-based iteration count.
F	arr block    	foreach        	Terminate a block.  Push each element of the array, and execute the block for each.
F	arr          	foreach-short  	If there is no open block, use the rest of the program as the block.  Execute it after pushing each element.
F	int          	for-short      	Perform `foreach-short` using the range [1 .. n].
g	             	               	Generate values.  See `generators` for details.
i	             	index          	Get the current 0-based iteration index of the inner loop.
k	arr block    	reduce         	Terminate a block and reduce (fold) using the block.
k	int block    	reduce-range   	Terminate a block and reduce (fold) [1 .. n] using the block.
K	arr arr block	cross-map      	Terminate a block and map using over a cartesian join.  Both elements will be pushed to the stack.  `_` will also push both to stack.  The result will be a single flat array.
m	arr block    	map            	Terminate a block and map using a block.  If the block execution is cancelled, that element won't be included in the result.
m	arr          	map-short      	If there is no open block, use the rest of the program as the block.  Print each mapped element with a new-line.
m	int          	map-range-short	Use the rest of the program as a block to map [1 .. n].  Print each mapped element with a new-line.
o	arr block    	order          	Terminate a block and order array by key.  If there are no open blocks, order the array itself.
w	block        	do-while       	Terminate a block and iterate until it produces a falsy value.
w	             	do-while-short 	If there is no open block, use the rest of the program as the block.
W	block        	while          	Terminate a block and iterate forever.  Cancelling will terminate, as with all blocks.
W	             	while-short    	If there is no open block, use the rest of the program as the block.
_	             	current        	Get the current iteration value.  If there are no blocks executing, this will be all of standard input, as one string.

Registers
x 	Value of register x.  Default is parsed integer value from standard input, or 0.
X 	Peek and write register x.
|x	Decrement register x and push.
|X	Increment register x and push.
y 	Value of register y.  Default is first line of standard input.
Y 	Peek and write register y.

Prefix Modes
Some instructions behave differently when they are the first character in a program.
f
F
m
e
i


Constants

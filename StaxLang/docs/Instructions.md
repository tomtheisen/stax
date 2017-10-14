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
L	... -> [...]          	listify-stack	Clear both stacks, and put all items in an array back on stack.
l	int                   	listify-n    	Pop n items, and put them in an array on top of the stack.
~	                      	input-push   	Pop top item, and push it to input stack.
;	                      	input-peek   	Peek from input stack, and push to main stack.
,	                      	input-pop    	Pop from input stack, and push to main stack.
e	arr                   	eval         	Parse string as data to be pushed on the stack. e.g. [12, 3.4, [5/6, "a string"]]  Multiple top level values in the same string will be pushed separately.
E	arr                   	explode      	Push all elements from array onto stack.

Numbers
0123456789 	       	     	Integer literal.  Leading `0` is always separate number. `10` is `1` and `0`.
A          	       	     	Constant 10, as in hexidecimal.
01234!56789	       	     	Float literal.  `!` indicates a decimal point.  A trailing `0` after a `!` is always a separate int literal.
+          	num num	add  	Add. Integers widen to fractions.  Fractions widen to floats.
-          	num num	sub  	Subtract.
*          	num num	mul  	Multiply.
/          	num num	div  	Integers will use integer division.
%          	int int	mod  	Modulus.
@          	frac   	floor	Integer floor of fraction.
@          	float  	floor	Integer floor of float.
v          	num    	dec  	Decrement by 1.
^          	num    	inc  	Increment by 1.

l	frac	a/b -> [a b]	listify-frac	Turn a fraction into a 2-array of numerator and denominator.

Logic
!	any        	not         	Logical not.  Produces 0 or 1.  Numeric 0 and empty lists are considered falsy.  All other values are truthy.
<	any any    	less        	Is less than.  Arrays use string-style lexicographic ordering.
>	any any    	greater     	Is greater than.  Arrays use string-style lexicographic ordering.
=	any any    	equal       	Equals.  Numberic types are coerced as necessary.
?	any any any	if-then-else	If the first value, then yield the second, else the third.  If the result is a block, execute it.


String
#    	arr arr	count-substrings 	Count occurrences of substring b in a.
"..."	       	string-literal   	String literal stored as an array of codepoints. `` ` `` is the escape character.  Unterminated string literals will be printed implicitly.
`...`	       	compressed-string	Compressed string literal encoded with contextual Huffman trees.  Not all strings can be encoded this way, but most that can will be smaller.  Unterminated compressed literals will be printed implicitly.
'a   	       	char-literal     	Create a single character string literal.
/    	arr arr	split            	Split on substrings.
*    	arr int	repeat           	Repeat string n times.  If n is negative, reverse array.
*    	int arr	repeat           	Repeat string n times.  If n is negative, reverse array.
*    	arr arr	join             	Join array of strings with delimiter.
$    	num    	tostring         	Convert number to string.
$    	arr    	arr-tostring     	Convert each element to string, and concat.
v    	arr    	lower            	To lower case.
^    	arr    	upper            	To upper case.

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
N	arr          	uncons-right   	Remove last element from array.  Push the beginning of the array, then the removed element.


Blocks
{	         	        	Begin a block.
}	         	        	End a block and push to stack.  If there is not a block currently open, start program execution over.
*	int block	do-times	Perform block n times.
*	block int	do-times	Perform block n times.
C	any      	cancel  	If value is truthy, cancel current block execution.
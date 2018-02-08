# Fizzbuzz Walkthrough
This is how you write FizzBuzz in Stax.  

> Count from 1 to 100, but replace multiples of 3 with "Fizz", and multiples of 5 with "Buzz".  Multiples of both should be replaced with "FizzBuzz".

Let's start by counting to 100.  Stax is a stack-based language, so all inputs and outputs [use one of the two stacks](https://github.com/tomtheisen/stax/blob/master/docs/stacks.md#stacks).

	100R{P}F

[Run it](http://stax.tomtheisen.com/#c=100R%7BP%7DF&i=&a=1)

Breaking it down into parts:

	100     	literal integer 100
	   R    	1-based range [1..100]
	    {P} 	Block of code containing a print instruction
	       F	Execute block for each element in array

Now let's add some fizz and some buzz

	100R{3%!"Fizz"*_5%!"Buzz"*+c_?P}F

[Run it](http://stax.tomtheisen.com/#c=100R%7B3%25%21%22Fizz%22*_5%25%21%22Buzz%22*%2Bc_%3FP%7DF&i=&a=1)

	100R{                            	For 1 to 100 do:
	     3%                          	Modulus 3
	       !                         	Logical not - yields 0 or 1
	        "Fizz"*                  	Repeat string literal 
	               _                 	Get the current iteration value
	                5%!"Buzz"*       	Repeat the string logic using 5 and Buzz
	                          +      	Concatenate two possibly empty strings
	                           c     	Copy value
	                            _    	Get the current iteration value
	                             ?   	3-value Conditional; pop (a, b, c), produce a ? b : c 
	                              P}F	Print as before

At this point we have a working FizzBuzz program, but it's just so big. 33 bytes. Hm. Here are some optimizations.

 * Remove the `}`.  The F will implicitly close the block.
 * Remove the `R`.  Iterating a block over an integer will implicitly convert it to a range.
 * Since the program ends with `F`, the `F` can be moved to the front of the block.  If `F` is encountered outside a block, the rest of the program is treated as the contents of the iterating block

 So now we have a slightly improved 30 byte program.

	100F3%!"Fizz"*_5%!"Buzz"*+c_?P

[Run it](http://stax.tomtheisen.com/#c=100F3%25%21%22Fizz%22*_5%25%21%22Buzz%22*%2Bc_%3FP&i=&a=1)

Hm.

 * `100` can be replaced with `AJ`.  `A` is `10`, and `J` is square integer.
 * Instead of using an `F` (for) loop, use an `m` (map) loop, which implicitly prints its results in shorthand mode.  (If `m` is used prior to the end of a program, it yields an array, just like a functional map)

	AJm3%!"Fizz"*_5%!"Buzz"*+c_?

[Run it](http://stax.tomtheisen.com/#c=AJm3%25%21%22Fizz%22*_5%25%21%22Buzz%22*%2Bc_%3F&i=&a=1)

That's 28. Hm. We can save a little more by using [compressed string literals](https://github.com/tomtheisen/stax/blob/master/docs/compressed.md#compressed-strings).  Enlish-looking strings can be encoded using a different kind of string enclosed in backticks.  You can use the string compression tool to automatically convert string literals.

	AJm3%!`M"(`*_5%!`-C`*+c_?

[Run it](http://stax.tomtheisen.com/#c=AJm3%25%21%60M%22%28%60*_5%25%21%60-C%60*%2Bc_%3F&i=&a=1)

That's 25 bytes. Hm. So far the program is using only printable ascii, which is kind of wasteful, since there are 256 different byte values, and printable ascii is only 95, unless you count tabs or newlines.  We can convert the program to the equivalent [packed-stax representation of the same program.

	åS╬╕ø┤╝Φûµ╡τ╓δR╚╦>«C▲

[Run it](http://stax.tomtheisen.com/#c=%C3%A5S%E2%95%AC%E2%95%95%C3%B8%E2%94%A4%E2%95%9D%CE%A6%C3%BB%C2%B5%E2%95%A1%CF%84%E2%95%93%CE%B4R%E2%95%9A%E2%95%A6%3E%C2%ABC%E2%96%B2&i=&a=1)

That's 21 bytes. I don't know how to make it any smaller. For now.

There's a lot more to the language.  Check out the rest of the docs.

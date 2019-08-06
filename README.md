# Stax
Stax Golfing Language

[Run and debug stax online.](https://staxlang.xyz/)

Have you ever noticed that software is just too *big*?  Hello world mobile apps are tens of megabytes.  Websites are making hundreds of requests for a single page.  There's just too much stuff.  Stax is the answer!  Unfortunately, it's the answer to another question.  That question is "is it possible to make another golfing language?".  It's not possible to write a mobile app or website in stax, but if it was, they'd probably be extremely small.  Don't make the mistake of thinking that means "efficient".  If you want a more efficient language, you should probably try any other language.  But small, definitely.

Stax is yet another stack-based programming language, optimized for code golf.  Stax is influenced by [GS2](https://github.com/nooodl/gs2), [05AB1E](https://github.com/Adriandmen/05AB1E), and others.  Stax is written in printable ASCII characters, although it can be optionally packed into bytes.  For more detail check the [docs](docs/README.md).  Or, you know, the code.

## Types
Falsy values are numeric zeroes and empty arrays.  All other values are truthy.
 * **Integer** Aribitrary size
 * **Float** Standard double precision
 * **Rational** Fractions of integers
 * **Block** Reference to unexecuted code for map/filter/etc
 * **Array** Heterogeneous lists of any values

Strings are not a type in stax.  Strings are represented as arrays of integer codepoints.  There are specific instructions to treat these as strings.  For instance `P` will output an array as if it was a string, followed by a newline.

## Features
Stax does lots of stuff, but here are the highlights and most interesting.

### Step through debugger
Stax's web environment features a debugger that lets you step through code as its executing.  You can also break execution to find out why a program is in an infinite loop.  When execution is broken, you can inspect all the internal state of the execution environment, including the register values, both stacks, and current instruction pointer.  If you want to break at a certain point in code, you can use the programmatic breakpoint instruction, which is `` |` ``. 

### Compressed string literals
Stax has a variant of string literals suitable for compressing English-like text with no special characters.  It is compressed using Huffman codes.  It uses a different set of Huffman codes for every pair of preceding characters.  The character weights were derived from a large corpus of English-like text.  `"Hello, World!"` could be written as  `` `jaH1"jS3!` ``.  The language comes with a compression utility.

### Crammed integers and integer arrays
Similarly to compressed string literals, stax also has features for efficiently representing single positive integers and arrays of arbitrary integers.  These literals use almost all the printable ascii characters.  For example `"abcd1234%^&"!` is an integer array literal for `[-722, 47, 51, 244, 58, 750]` and `"abcd1234%^&"%` represents the positive integer `3131686852857434499249`.  Don't worry about memorizing the rules for generating these.  There are encoding tools built into the web environment.

### Rationals
Stax supports fraction arithmetic.  You can use `u` to turn an integer upside down. So `3u` yields `1/3`.  Fractions are always in reduced terms.  `3u 6*` multiplies 1/3 by 6, but the result will be `2/1`.

### PackedStax
PackedStax is an alternative representation for Stax code.  It is never ambiguous with Stax, since PackedStax always has the leading bit of the first byte set.  That means the same interpreter can be used for both representations with no extra information.  For ease of clipboard use, PackedStax can be represented using a modified CP437 character encoding.  It yields ~18% savings over ASCII.

### Quick-reference
Just Press F1 in the web environment to get a complete list of all instructions and a few other oddities like constants and generator modes.
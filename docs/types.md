# Types
There are five types.

## Integer
All integers are big integers, meaning they can have arbitray size and never overflow.

## Floats
These are just regular boring double precision floats.

## Rationals
These are fractions of integers.  Most of the same arithmetic can be done on them, and they stay reduced.  There are no rational literals.  Instead, to produce an arbitrary rational like `3/8`, you could use `8u3*`. 

## Blocks
These are pieces of code, not yet executed, like functions.  Except they don't really have arguments.

## Arrays
Heterogeneous arrays of values, including other arrays.

There is no formal type for strings.  Strings are expressed as arrays of integer codepoints.  Some instructions say they operate on strings.  They really operate on arrays of integers.  But they do one thing differently.  String operations convert 0 into 32, so that they become printable spaces instead of NUL.

In the web-based interpreter, there are a couple of characters with special behavior.  
A carriage return (`0x0D`) causes the current line of output to removed.  Further output will resume in the left-most column.
A form feed (`0x0C`) causes all output to be reset.  Further output will resume from the top.
In a stax string literal, `` `3 `` will produce a carriage return and `` `5 `` will produce a form feed.

```
"This is the first line of output."P
"This is the second line of output."p
"`3Now there's a new second line."P
"`5Now this is the only output."P
```
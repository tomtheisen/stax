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
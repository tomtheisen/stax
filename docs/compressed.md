# Compressed strings
English-looking strings can be compressed in stax using a `` ` `` -delimited string.  For instance, these two literals have the same value.

    "The quick fox jumps over the lazy brown dog."
    `]J<3JM]a7y#lg|s3A0<9Z/%`

The compression is done with [Huffman trees](https://en.wikipedia.org/wiki/Huffman_coding).  Normally Huffman coding only considers the relative frequency of characters.  But stax considers contextual frequency.  There is a different Huffman tree for every 2 character prefix.  This helps encode things like how `u` usually comes after `q`, which traditional Huffman coding can't take advantage of.

Compressed strings must consist of these characters.

     !',-.:?ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz

## How it works
Each character is Huffman coded using the contextual Huffman tree for the last two characters.  The final result is a bit array.  This is interpreted as a number in base 94, using the alphabet of ASCII characters except the delimiter.

# Crammed integer arrays
Crammed integer arrays provide an efficient way of representing arbitrary integer arrays in stax code.  A crammed array looks like a string literal followed by a logical not, such as `"..."!`.  But it's really a different thing.  Since there's no use following a constant by a logical not, this construction invokes array un-cramming instead.  For instance, these two programs have the same result.

    "FJ).%,p&()(!'^pq kzi !X5&N^"!
    19968 20108 19977 22235 20116 20845 19971 20843 20061 21313 24471 11l

## How it works
92 printable ascii characters are used in integer cramming.  For each number in the array, the sign is recorded as a bit.  Each character in a crammed array also records a bit that represents whether the current number extends to the next character.  After these are accounted for, the rest of the possibilities for each character are used to store some of the number.  It works kind of like a variable number base.

### Encoding modes
The final character in the string cannot continue to the next character.  There is no next character!  So the continuation bit in the final character switches between two modes: flat and offset.

Flat mode is a straight-forward encoding using the steps outlined above.  Offset mode is useful for storing large magnitude numbers that are all near each other.  The difference between each pair of numbers is stored, rather than each number separately.
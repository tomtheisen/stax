# Compressed strings
English-looking strings can be compressed in stax using a `` ` `` -delimited string.  For instance, these two literals have the same value.

    "The quick fox jumps over the lazy brown dog."
    `]J<3JM]a7y#lg|s3A0<9Z/%`

The compression is done with [Huffman trees](https://en.wikipedia.org/wiki/Huffman_coding).  Normally Huffman coding only considers the relative frequency of characters.  But stax considers contextual frequency.  There is a different huffman tree for every 2 character prefix.  This helps encode things like how `u` usually comes after `q`, which traditional Huffman coding can't take advantage of.

Compressed strings must consist of these characters.

     !',-.:?ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz

## How it works
Each character is huffman coded using the contextual huffman tree for the last two characters.  The final result is a bit array.  This is interpreted as a number in base 94, using the alphabet of ascii characters except the delimiter.
# Annotated Stax
Stax is kind of hard to read.  You can actually add comments, which is a luxury not afforded in many golfing languages.  A tab character introduces a line comment.

    5m	(tab) foreach [1..5] print
    '#*	(tab) that many # characters

If you replace `(tab)` with actual tabs, this code is executable in stax.  It will produce this output.

    *
    **
    ***
    ****
    *****

Newlines and tabs must be removed before packing a stax program.  But come on, why would you try to pack a program that still had all that bloat in it?  Comments are the leading cause of code bloat.  Studies show.

## Automatic Annotation
The stax interpreter can generate annotations as it interprets, although they lack the human touch.  They sound very computery.  It can be pretty useful for making sense of a particularly impenetrable block of code.  At the time of this writing, the above program gets auto-annotated like this.

    (tab)Stax 0.0.0
    5m   	(tab)map range 1 to 5 using rest of program; print the results
      '#*	(tab)  repeat array single character string literal times

The leading tab tells the interpreter to un-annotate the code before re-annotating it.  If it wasn't for that, there would be a whitespace explosion.  And that would be pointless, because it's a no-op.


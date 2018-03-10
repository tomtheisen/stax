using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace StaxLang {
    public static class ArrayCrammer {
        private const string Symbols = " !#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}~";

        public static List<object> Uncram(string str) {
	        var result = new List<BigInteger>();
	        bool continuing = false;
	
	        for (int i = 0; i < str.Length; i++) {
		        int charValue = Symbols.IndexOf(str[i]);
		        if (charValue < 0) throw new ArgumentException("Bad character for uncram");
		        if (continuing) result[result.Count - 1] = result.Last() * 46 + charValue / (result.Last() < 0 ? -2 : 2);
		        else result.Add(charValue / (charValue % 4 >= 2 ? -4 : 4));
		        continuing = charValue % 2 == 1;
	        }
	
	        if (continuing) { // offset mode
		        for (int i = 1; i < result.Count; i++) result[i] += result[i - 1];
	        }
	        return result.Cast<object>().ToList();
        }

	    private static string Encode(IList<BigInteger> a, bool offsetMode) {
		    string result = "";
		    if (offsetMode) {
			    for (int i = a.Count - 1; i > 0; i--) a[i] -= a[i - 1];
		    }
		    for (int i = 0; i < a.Count; i++) {
			    var parts = new List<int>();
			    int signBit = a[i] < 0 ? 2 : 0;
			    int continuing = (offsetMode && i == a.Count - 1) ? 1 : 0;
			    var remain = BigInteger.Abs(a[i]);
			    for (; remain > 23; remain /= 46, continuing = 1) {
				    parts.Insert(0, (int)(remain % 46 * 2 + continuing));
			    }
			    parts.Insert(0, (int)(remain * 4 + signBit + continuing));
			    foreach (int part in parts) result += Symbols[part];
		    }
		    return result;
	    }

        public static string Cram(IList<BigInteger> arr) {
	        string flat = Encode(arr, false), offset = Encode(arr, true);
	        return offset.Length < flat.Length ? offset : flat;
        }
    }
}

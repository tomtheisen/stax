using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaxLang {
    struct IteratorPair {
        public dynamic Outer;
        public dynamic Inner;

        public IteratorPair(dynamic outer, dynamic inner) {
            Outer = outer;
            Inner = inner;
        }
    }
}

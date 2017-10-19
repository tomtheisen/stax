using System;

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

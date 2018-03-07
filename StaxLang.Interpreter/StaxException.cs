using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaxLang {
    class StaxException : Exception {
        public StaxException(string msg) : base(msg) { }
    }
}

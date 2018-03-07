using System;

namespace StaxLang {
    struct ExecutionState {
        public bool Cancel { get; set; }

        public static readonly ExecutionState CancelState = new ExecutionState{ Cancel = true };
    }
}

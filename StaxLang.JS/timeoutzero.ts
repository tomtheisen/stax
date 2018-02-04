const timeouts: (() => void)[] = [];
const messageName = "stax-work-pend";

export function pendWork(fn: () => void) {
    timeouts.push(fn);
    window.postMessage(messageName, "*");
}

window.addEventListener("message", event => {
    if (event.source === window && event.data === messageName) {
        event.stopPropagation();
        if (timeouts.length) timeouts.shift()!();
    }
}, true);
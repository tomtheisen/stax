let stable: boolean | null = null;
export function nativeSortIsStable() {
    function test() {
        const size = 1025, mod = 17;
        let arr = new Array(size).fill(0).map((e,i) => i);
        
        arr.sort((a,b) => b % mod - a % mod);
        
        for (let i = 1; i < size; i++) {
            let a = arr[i - 1], b = arr[i];
            if (a > b && a % mod === b % mod) return false;
        }
        return true;
    }
    return stable = stable ?? test();
}

let ensured = false;
export function ensureStableSort() {
    if (ensured) return;
    ensured = true;
    if (nativeSortIsStable()) return;

    function defaultCompare<T>(a: T, b: T) {
        const as = String(a), bs = String(b);
        return (bs > as ? 1 : 0) - (as > bs ? 1 : 0);
    }

    const originalSort = Array.prototype.sort;
    Array.prototype.sort = function sort<T>(this: T[], compareFn: (a: T, b: T) => number = defaultCompare) {
        let wrapped = this.map((el, idx) => ({ el, idx }));

        type WrapType = { el: T, idx: number };
        originalSort.call(wrapped, (a: WrapType, b: WrapType) => compareFn(a.el, b.el) || b.idx - a.idx);
        for (let i = 0; i < this.length; i++) this[i] = wrapped[i].el;
        return this;
    }
}

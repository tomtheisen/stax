// struct xorshift64_state {
//   uint64_t a;
// };

const UInt64Space = 1n << 64n;
const maxUInt64 = UInt64Space - 1n;

export class RandomNumberGenerator {
    private state = 0n;

    constructor(...state: (number | bigint | string)[]) {
        const stateString = state.map(s => s.toString()).join('\n');
        for (let c of stateString) {
            this.state *= BigInt(c.codePointAt(0))
            this.state &= maxUInt64;
        }
    }

    // https://en.wikipedia.org/wiki/Xorshift#Example_implementation
    private xorshift64(): bigint {
        this.state ^= this.state << 13n & maxUInt64;
        this.state ^= this.state >> 7n;
        this.state ^= this.state << 17n & maxUInt64;
        return this.state;
    }

    public nextInt(bound: bigint) {
        // create rejection threshold to next multiple of `bound` to avoid bias toward low end of range
        const threshold = UInt64Space / bound * bound;
        let choice: bigint;
        do choice = this.xorshift64(); while (choice >= threshold);
        return choice % bound;
    }

    public choice<T>(arr: T[]): T {
        return arr[Number(this.nextInt(BigInt(arr.length)))];
    }

    public nextFloat(): number {
        return Number(this.xorshift64()) / Number(maxUInt64);
    }
}
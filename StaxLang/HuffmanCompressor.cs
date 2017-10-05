using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace StaxLang {
    public static class HuffmanCompressor {
        private static Dictionary<string, HuffmanNode> Trees = new Dictionary<string, HuffmanNode>();

        static HuffmanCompressor() {
            var assembly = Assembly.GetExecutingAssembly();

            var serializer = new JsonSerializer();
            var resourceName = typeof(HuffmanCompressor).Namespace + ".english-huffman.json";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader)) {
                var packed = (JObject)serializer.Deserialize(jsonReader);

                foreach (var prop in packed.Properties()) {
                    var prefix = prop.Name;
                    Trees[prefix] = new HuffmanNode();
                    var treespec = (string)((JValue)prop.Value).Value;

                    var path = new List<char>(30);
                    for (int i = 0; i < treespec.Length; i += 2) {
                        char ch = treespec[i];
                        int zeroes = "0123456789abcdefghijklmnop".IndexOf(treespec[i + 1]);

                        if (i != 0) {
                            int idx = path.Count - 1;
                            while (path[idx] == '1') idx--;
                            path.RemoveRange(idx + 1, path.Count - idx - 1);
                            path[idx] = '1';
                        }
                        path.AddRange(Enumerable.Repeat('0', zeroes));

                        Trees[prefix].Populate(path, ch);
                    }
                }
            }
        }

        const string Symbols = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}~";
        private static Dictionary<string, string> MemoizedDecompress = new Dictionary<string, string>();
        public static string Decompress(string compressed) {
            if (MemoizedDecompress.TryGetValue(compressed, out var result)) return result;

            BigInteger big = 0;
            foreach (var ch in compressed.Reverse()) big = big * Symbols.Length + Symbols.IndexOf(ch);

            var path = new List<char>();
            while (big > 0) {
                path.Insert(0, (big & 1).ToString()[0]);
                big >>= 1;
            }

            result = ". ";
            int pathidx = 1;
            while (pathidx < path.Count) {
                var tree = Trees[result.Substring(result.Length - 2)];
                result += tree.Traverse(path, ref pathidx);
            }
            return result.Substring(2);
        }

        public static string Compress(string input) {
            input = ". " + input;
            var path = new List<char>();

            for (int i = 2; i < input.Length; i++) {
                var tree = Trees[input.Substring(i - 2, 2)];
                var cpath = tree.FindPath(input[i]);
                if (cpath == null) return null;
                if (i == input.Length - 1) {
                    while (cpath.Count >= 2 && cpath.Last() == '0') {
                        cpath.RemoveAt(cpath.Count - 1);
                    }
                }
                path.AddRange(cpath);
            }

            var big = BigInteger.One;
            for (int i = 0; i < path.Count; i++) {
                big <<= 1;
                big |= path[i] & 1;
            }
            string result = "";
            while (big > 0) {
                result += Symbols[(int)(big % Symbols.Length)];
                big /= Symbols.Length;
            }
            return result;
        }

        class HuffmanNode {
            public HuffmanNode Left { get; private set; }
            public HuffmanNode Right { get; private set; }
            public char? LeafValue { get; private set; }

            public void Populate(IList<char> path, char leaf, int pathidx = 0) {
                if (pathidx >= path.Count) {
                    LeafValue = leaf;
                    return;
                }

                Left = Left ?? new HuffmanNode();
                Right = Right ?? new HuffmanNode();
                (path[pathidx] == '1' ? Right : Left).Populate(path, leaf, pathidx + 1);
            }

            public char Traverse(IList<char> path, ref int idx) {
                if (LeafValue.HasValue) return LeafValue.Value;
                return (++idx <= path.Count && path[idx - 1] == '1' ? Right : Left).Traverse(path, ref idx);
            }

            public List<char> FindPath(char ch) {
                if (LeafValue.HasValue) return ch == LeafValue ? new List<char>() : null;

                List<char> result = Left.FindPath(ch);
                result?.Insert(0, '0');
                if (result != null) return result;
                result = Right.FindPath(ch);
                result?.Insert(0, '1');
                return result;
            }
        }
    }
}

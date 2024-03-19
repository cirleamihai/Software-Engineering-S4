using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageHandler {
    public class StorageHandler
    {
        public static void SaveToFile(string path, string data)
        {
            System.IO.File.WriteAllText(path, data);
        }

        public static string ReadFromFile(string path)
        {
            return System.IO.File.ReadAllText(path);
        }
    }
}

namespace Compression
{
    using StorageHandler;

    public class HuffmanNode
    {
        public char c;
        public int freq;
        public HuffmanNode left;
        public HuffmanNode right;

        public HuffmanNode(char c, int freq)
        {
            this.c = c;
            this.freq = freq;
            left = right = null;
        }
    }

    public class HuffmanFreq
    {
        String input;
        public List<char> data;
        public List<int> frequency;
        public Dictionary<char, int> frequencyDict;

        public HuffmanFreq(String input)
        {
            this.input = input;
            frequencyDict = new Dictionary<char, int>();
            frequency = new List<int>();
            data = new List<char>();

            // building the frequency in the constructor
            BuildFrequency();
        }

        public void BuildFrequency()
        {
            foreach (char c in input)
            {
                if (frequencyDict.ContainsKey(c))
                {
                    int index = data.IndexOf(c);
                    frequencyDict[c]++;
                    frequency[index]++;
                }
                else
                {
                    frequency.Add(1);
                    data.Add(c);
                    frequencyDict.Add(c, 1);
                }
            }
        }

        public int GetFreqSize() { return frequency.Count; }
    }

    public class CompareNode : IComparer<HuffmanNode>
    {
        public int Compare(HuffmanNode x, HuffmanNode y)
        {
            int result = x.freq.CompareTo(y.freq);

            if (result == 0)
            {
                return x.c.CompareTo(y.c);
            }

            return result;
        }
    }

    public class Serializer
    {
        public static void SaveSerializedData(HuffmanNode huffmanNode, string path)
        {
            StringBuilder serializedData = new StringBuilder();
            SerializeTree(huffmanNode, serializedData);
            StorageHandler.SaveToFile(path, serializedData.ToString());
        }

        public static void SerializeTree(HuffmanNode root, StringBuilder serializedData)
        {
            if (root == null)
            {
                return;
            }

            if (root.c != '$')
            {
                serializedData.Append("1");
                serializedData.Append(root.c);
            } else
            {
                serializedData.Append("0");
            }

            SerializeTree(root.left, serializedData);
            SerializeTree(root.right, serializedData);
        }

    }

    public class Deserializer
    {
        public static HuffmanNode LoadDeserializedTree(string path)
        {
            string serializedData = StorageHandler.ReadFromFile(path);
            int index = 0;
            return DeserializeTree(ref index, serializedData);
        }

        public static HuffmanNode DeserializeTree(ref int index, string serializedData)
        {
            if (index >= serializedData.Length)
            {
                return null;
            }

            char c = serializedData[index++];
            if (c == '1')
            {
                return new HuffmanNode(serializedData[index++], -1);  // -1 for freq since it's not used in deserialization

            }
            else if (c == '0')
            {
                HuffmanNode root = new HuffmanNode('$', -1);
                root.left = DeserializeTree(ref index, serializedData);  // first we create the left node
                root.right = DeserializeTree(ref index, serializedData); // then the right node
                return root;
            }

            // in case we get unexpected input
            return null;
        }

    }

    public class Huffman
    {
        class HuffmanReferenceSystem
        {
            Dictionary<char, String> refSystem;

            public HuffmanReferenceSystem()
            {
                refSystem = new Dictionary<char, String>();
            }

            public void Add(char c, String code)
            {
                refSystem.Add(c, code);
            }

            public String getCharCode(char c)
            {
                return refSystem[c];
            }

            public char getChar(String code)
            {
                foreach (var entry in refSystem)
                {
                    if (entry.Value == code)
                        return entry.Key;
                }
                return '$';
            }
        }

        static void CreateReferences(HuffmanNode root, String s, HuffmanReferenceSystem refSys)
        {
            if (root == null)
                return;

            if (root.c != '$')
            {
                refSys.Add(root.c, s);
                Console.WriteLine(root.c + " " + s);
            }

            CreateReferences(root.left, s + "0", refSys);
            CreateReferences(root.right, s + "1", refSys);
        }


        static HuffmanNode CreateHuffmanTree(HuffmanFreq decomposedFreq, HuffmanReferenceSystem refSys)
        {
            // unpacking the frequency
            List<char> data = decomposedFreq.data;
            List<int> freq = decomposedFreq.frequency;
            int size = decomposedFreq.GetFreqSize();

            HuffmanNode left, right, top;
            var minHeap = new SortedSet<HuffmanNode>(new CompareNode());

            for (int i = 0; i < size; ++i) {
                minHeap.Add(new HuffmanNode(data[i], freq[i]));
            }

            Console.WriteLine(size + " " + minHeap.Count);

            while (minHeap.Count > 1)
            {
                left = minHeap.First();
                minHeap.Remove(left);

                right = minHeap.First();
                minHeap.Remove(right);

                top = new HuffmanNode('$', left.freq + right.freq);
                top.left = left;
                top.right = right;

                minHeap.Add(top);
            }

            Huffman.CreateReferences(minHeap.First(), "", refSys);
            return minHeap.First();
        }

        public static String Compress(string input)
        {
            var decomposedFreq = new HuffmanFreq(input);
            var refSys = new HuffmanReferenceSystem();

            var rootTree = CreateHuffmanTree(decomposedFreq, refSys);

            String compressedString = "";
            for (int i = 0; i < input.Length; i++)
            {
                compressedString += refSys.getCharCode(input[i]);
            }

            Console.WriteLine("Compressed string: " + compressedString);
            Serializer.SaveSerializedData(rootTree, "compressedTree.txt");
            
            return compressedString;
        }

        public static String Decompress(string input)
        {
            var rootTree = Deserializer.LoadDeserializedTree("compressedTree.txt");
            var root = rootTree;
            var decodedString = new StringBuilder();

            foreach (var bit in input)
            {
                rootTree = bit == '0' ? rootTree.left : rootTree.right;
                
                if (rootTree.left == null && rootTree.right == null)
                {
                    decodedString.Append(rootTree.c);
                    rootTree = root;
                }
            }

            Console.WriteLine("Decompressed string: " + decodedString);

            return decodedString.ToString();
        }
    }



    internal class Program
    {
        static void Main(string[] args)
        {
            String input = "AABBCDCAASDBSAAABB";
            var compressedString = Huffman.Compress(input);

            Huffman.Decompress(compressedString);

            Console.ReadKey();
        }
    }
}

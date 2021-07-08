using System;
using System.Collections.Generic;

namespace Penguin.Web.IPServices.Arin.Readers
{
    internal class BlockTxtReader : BlockReader<Dictionary<string, string>>
    {
        public int ThreadCount { get; set; }

        private int newLineCount = 0;

        public BlockTxtReader(string filePath, IProgress<float> reportProgress, int bufferSize = 3000) : base(filePath, bufferSize, reportProgress)
        {
        }

        private List<string> ReadNextText()
        {
            List<string> Lines = new List<string>(20);

            string line;

            lock (this.lockObject)
            {
                while ((line = this.TextReader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        this.newLineCount++;

                        if (this.newLineCount == 3)
                        {
                            if (Lines.Count > 0)
                            {
                                this.ReportProgress();
                                return Lines;
                            }
                        }

                        continue;
                    }
                    else
                    {
                        this.newLineCount = 0;
                    }

                    Lines.Add(line);
                }
            }

            return null;
        }

        private Dictionary<string, string> DeserializeNextBlock()
        {
            List<string> Lines = this.ReadNextText();

            if (Lines is null)
            {
                return null;
            }

            Dictionary<string, string> thisBlock = new Dictionary<string, string>(15);

            foreach (string line in Lines)
            {
                int i = line.IndexOf(':');
                if (i != -1)
                {
                    string name = line.Substring(0, i);

                    if (!thisBlock.ContainsKey(name))
                    {
                        thisBlock.Add(name, line.Substring(16));
                    }
                }
            }

            return thisBlock;
        }

        private readonly object lockObject = new object();

        public IEnumerable<Dictionary<string, string>> Blocks()
        {
            Dictionary<string, string> block;
            while ((block = this.DeserializeNextBlock()) != null)
            {
                this.ReportProgress();
                yield return block;
            }
        }
    }
}
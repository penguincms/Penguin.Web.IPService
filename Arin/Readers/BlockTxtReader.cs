using Penguin.Extensions.String;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public class BlockTxtReader : BlockReader<Dictionary<string, string>>
    {
        public int ThreadCount { get; set; }


        int newLineCount = 0;

        public BlockTxtReader(string filePath, IProgress<float> reportProgress, int bufferSize = 3000) : base(filePath, bufferSize, reportProgress)
        {

        }
      

        public async Task<Dictionary<string, string>> GetNextBlock()
        {

            Dictionary<string, string> thisBlock = null;

            string line;

            while ((line = await TextReader.ReadLineAsync()) != null)
            {

                if (string.IsNullOrWhiteSpace(line))
                {
                    newLineCount++;

                    if (newLineCount == 3)
                    {
                        if (thisBlock.Count > 0)
                        {
                            newLineCount = 0;

                            return thisBlock;                           
                        }
                    }

                    continue;
                } else
                {
                    thisBlock = thisBlock ?? new Dictionary<string, string>();
                    newLineCount = 0;
                }


                if (line.Contains(":"))
                {
                    string name = line.To(":");
                    string val = line.From(":").Trim();

                    if (!thisBlock.ContainsKey(name))
                    {
                        thisBlock.Add(name, val);
                    }
                }

            }

            return null;
        }
    }
}

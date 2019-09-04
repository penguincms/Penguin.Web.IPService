using Penguin.Extensions.String;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public class BlockTxtReader : BlockReader<Dictionary<string, string>>
    {
        public int ThreadCount { get; set; }
        float lastProgress { get; set; }
        public BlockTxtReader(string filePath, int bufferSize = 3000, int threadCount = 0) : base(filePath, bufferSize)
        {
            if (ThreadCount < 1)
            {
                threadCount = System.Environment.ProcessorCount;
            }
        }

        public Action<float> ReportProgress { get; set; }


        protected override void DeckFiller_DoWork(object sender, DoWorkEventArgs e)
        {
            int newLineCount = 0;

            string line = null;
            Dictionary<string, string> thisBlock = new Dictionary<string, string>();

            
            float streamLength = FileReader.BaseStream.Length;


            while (OnDeck.Count < 100 && (line = FileReader.ReadLine()) != null)
            {

                if (ReportProgress != null)
                {
                    float thisProgress = (float)(Math.Truncate(FileReader.BaseStream.Position / streamLength * 100.0) / 100.0);

                    if (lastProgress != thisProgress)
                    {
                        lastProgress = thisProgress;

                        ReportProgress.Invoke(thisProgress);
                    }
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    newLineCount++;

                    if (newLineCount == 3)
                    {
                        if (thisBlock.Count > 0)
                        {
                            OnDeck.Enqueue(thisBlock);
                            newLineCount = 0;
                        }

                        thisBlock = new Dictionary<string, string>();
                    }

                    continue;
                } else
                {
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

            if (line is null)
            {
                MoreToGo = false;
            }

            if (thisBlock.Count > 0)
            {
                OnDeck.Enqueue(thisBlock);
            }
        }
    }
}

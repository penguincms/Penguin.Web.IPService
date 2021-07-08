using Penguin.Reflection.Serialization.XML;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Penguin.Web.IPServices.Arin.Readers
{
    internal class BlockXmlReader<T> : BlockReader<T> where T : class
    {
        private XMLSerializer Serializer { get; set; }
        public Task<T> NextBlock { get; set; }

        public BlockXmlReader(string blockName, string filePath, int bufferSize = 128, IProgress<float> reportProgress = null) : base(filePath, bufferSize, reportProgress)
        {
            this.Serializer = new XMLSerializer(new XMLDeserializerOptions()
            {
                CaseSensitive = false,
                AttributesAsProperties = true,
                StartNode = blockName
            });

            _ = this.TextReader.ReadLine();
            _ = this.TextReader.ReadLine();
        }

        private readonly object lockObject = new object();

        public T GetNextBlock()
        {
            T block = null;
            lock (this.lockObject)
            {
                if (this.StreamEnd)
                {
                    return null;
                }

                block = this.Serializer.DeserializeObject<T>(this.TextReader);
                this.ReportProgress();
            }

            return block;
        }

        //int i = 0;
        public IEnumerable<T> Blocks()
        {
            T block;
            while ((block = this.GetNextBlock()) != null)
            {
                //Debug.WriteLine(i++);
                yield return block;
            }
        }
    }
}
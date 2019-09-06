using Penguin.Reflection.Serialization.XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Penguin.Web.IPServices.Arin.Readers
{
    internal class BlockXmlReader<T> : BlockReader<T> where T : class
    {
        private XMLSerializer Serializer { get; set; }
        public Task<T> NextBlock { get; set; }

        public BlockXmlReader(string blockName, string filePath, int bufferSize = 128, IProgress<float> reportProgress = null) : base(filePath, bufferSize, reportProgress)
        {
            Serializer = new XMLSerializer(new XMLDeserializerOptions()
            {
                CaseSensitive = false,
                AttributesAsProperties = true,
                StartNode = blockName
            });

            TextReader.ReadLine();
            TextReader.ReadLine();
        }

        private object lockObject = new object();

        public T GetNextBlock()
        {
            T block = null;
            lock (lockObject)
            {
                if (StreamEnd)
                {
                    return null;
                };

                block = Serializer.DeserializeObject<T>(TextReader);
                ReportProgress();
            }
            return block;
        }

        //int i = 0;
        public IEnumerable<T> Blocks()
        {
            T block = null;
            while ((block = GetNextBlock()) != null)
            {
                //Debug.WriteLine(i++);
                yield return block;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Penguin.Reflection.Serialization.XML;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public class BlockXmlReader<T> : BlockReader<T> where T : class
    {
        string BlockName { get; set; }
        string BlockStart { get; set; }
        string BlockEnd { get; set; }

        StringBuilder BlockContents { get; set; }
        XMLSerializer Serializer { get; set; }

        public BlockXmlReader(string blockName, string filePath, IProgress<float> reportProgress, int bufferSize = 128) : base(filePath, bufferSize, reportProgress)
        {
            BlockContents = new StringBuilder();

            BlockName = blockName;
            BlockStart = $"<{blockName}>";
            BlockEnd = $"</{blockName}>";

            Serializer = new XMLSerializer(new XMLDeserializerOptions()
            {
                CaseSensitive = false,
                AttributesAsProperties = true,
                StartNode = blockName
            });

            
        }



        public async Task<T> GetNextBlock(object sender, DoWorkEventArgs e)
        {
            string line;

            BlockContents.Clear();

            bool add = false;
            
            while ((line = await TextReader.ReadLineAsync()) != null)
            {

                if (line.Contains(BlockStart))
                {
                    add = true;
                }

                if (add)
                {
                    BlockContents.Append(line);
                }

                if (line.Contains(BlockEnd))
                {

                    return Serializer.DeserializeObject<T>(BlockContents.ToString());

                }
            }

            return null;
        }

    }
}

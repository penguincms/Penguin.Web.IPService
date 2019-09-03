using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Penguin.Reflection.Serialization.XML;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public class BlockReader<T> : IDisposable
    {
        string BlockName { get; set; }
        string BlockStart { get; set; }
        string BlockEnd { get; set; }

        XMLSerializer Serializer { get; set; }
        StreamReader FileReader { get; set; }

        public BlockReader(string blockName, string filePath, int bufferSize = 128)
        {
            BlockName = blockName;
            BlockStart = $"<{blockName}>";
            BlockEnd = $"</{blockName}>";

            Serializer = new XMLSerializer(new XMLDeserializerOptions()
            {
                CaseSensitive = false,
                AttributesAsProperties = true,
                StartNode = blockName
            });

            FileReader = new StreamReader(File.OpenRead(filePath), Encoding.UTF8, true, bufferSize);
        }

        public Action<float> ReportProgress { get; set; }

        public IEnumerable<T> Blocks()
        {
            String line;
            StringBuilder blockContents = new StringBuilder();
            bool add = false;
            float lastProgress = 0;
            float streamLength = FileReader.BaseStream.Length;

            while ((line = FileReader.ReadLine()) != null) {             
            
                if(ReportProgress != null)
                {
                    float thisProgress = (float)(Math.Truncate(FileReader.BaseStream.Position / streamLength * 100.0) / 100.0);

                    if(lastProgress != thisProgress)
                    {
                        lastProgress = thisProgress;

                        ReportProgress.Invoke(thisProgress);
                    }
                }

                if(line.Contains(BlockStart))
                {
                    add = true;
                }

                if(add)
                {
                    blockContents.Append(line);
                }

                if(line.Contains(BlockEnd))
                {

                    T block = Serializer.DeserializeObject<T>(blockContents.ToString());

                    blockContents.Clear();

                    add = false;

                    yield return block;
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BlockReader()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}

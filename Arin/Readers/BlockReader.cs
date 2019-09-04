using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public abstract class BlockReader<T> : IDisposable
    {
        protected TextReader TextReader { get; set; }
        protected IProgress<float> ProgressReporter { get; set; }
        float LastProgress { get; set; }
        float FileLength { get; set; }

        public BlockReader(string filePath, int bufferSize = 128, IProgress<float> reportProgress)
        {

            ProgressReporter = reportProgress;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"Reader of type {this.GetType().Name} was passed empty file path");
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"Reader of type {this.GetType().Name} can not be created with file that does not exist");
            }

            StreamReader fileStream = new StreamReader(File.OpenRead(filePath), Encoding.UTF8, true, bufferSize);

            TextReader = fileStream;
            LastProgress = fileStream.BaseStream.Length;
        }

        public void ReportProgress()
        {
            if (ProgressReporter != null)
            {
                float thisProgress = (float)(Math.Truncate((TextReader as StreamReader).BaseStream.Position / FileLength * 100.0));

                if (LastProgress != thisProgress)
                {
                    LastProgress = thisProgress;

                    ProgressReporter.Report(thisProgress);
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
                    TextReader.Dispose();
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
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }


        #endregion
    }
}

using System;
using System.IO;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    internal abstract class BlockReader<T> : IDisposable
    {
        protected TextReader TextReader { get; set; }
        protected IProgress<float> ProgressReporter { get; set; }
        private float LastProgress { get; set; }
        private float FileLength { get; set; }
        public bool StreamEnd { get; protected set; }

        public BlockReader(string filePath, int bufferSize = 128, IProgress<float> reportProgress = null)
        {
            this.ProgressReporter = reportProgress;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"Reader of type {this.GetType().Name} was passed empty file path");
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"Reader of type {this.GetType().Name} can not be created with file that does not exist");
            }

            StreamReader fileStream = new StreamReader(File.OpenRead(filePath), Encoding.UTF8, true, bufferSize);

            this.TextReader = fileStream;
            this.FileLength = fileStream.BaseStream.Length;
        }

        public void ReportProgress()
        {
            if (this.ProgressReporter != null)
            {
                float thisProgress = (float)(Math.Truncate((this.TextReader as StreamReader).BaseStream.Position / this.FileLength * 100.0));

                if (this.LastProgress != thisProgress)
                {
                    //This is a dumb way to set this but this calculation is expensive
                    if (thisProgress == 100f)
                    {
                        this.StreamEnd = true;
                    }

                    this.LastProgress = thisProgress;

                    this.ProgressReporter.Report(thisProgress);
                }
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.TextReader.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                this.disposedValue = true;
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
            this.Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}
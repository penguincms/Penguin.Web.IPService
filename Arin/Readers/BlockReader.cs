using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public abstract class BlockReader<T> : IDisposable where T : class
    {
        protected StreamReader FileReader { get; set; }
        public BlockReader(string filePath, int bufferSize = 128)
        {
            OnDeck = new ConcurrentQueue<T>();

            MoreToGo = true;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"Reader of type {this.GetType().Name} was passed empty file path");
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"Reader of type {this.GetType().Name} can not be created with file that does not exist");
            }

            FileReader = new StreamReader(File.OpenRead(filePath), Encoding.UTF8, true, bufferSize);

        }

        BackgroundWorker DeckFiller { get; set; }
        protected bool MoreToGo { get; set; }

        protected abstract void DeckFiller_DoWork(object sender, DoWorkEventArgs e);

        public IEnumerable<T> Blocks()
        {

            void CheckWorker()
            {
                if (DeckFiller is null)
                {
                    DeckFiller = new BackgroundWorker();
                    DeckFiller.DoWork += DeckFiller_DoWork;
                }

                if (!DeckFiller.IsBusy)
                {
                    DeckFiller.RunWorkerAsync();
                }
            }

            while (MoreToGo || OnDeck.Count > 0)
            {

                if (OnDeck.Count < 100 && MoreToGo)
                {
                    CheckWorker();
                }

                T thisBlock = null;

                if (OnDeck.TryDequeue(out thisBlock))
                {
                    yield return thisBlock;
                } 
                
            }

        }

        protected ConcurrentQueue<T> OnDeck { get; set; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FileReader.Dispose();
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

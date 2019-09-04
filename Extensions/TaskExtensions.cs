using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Penguin.Web.IPServices.Extensions
{
    public static class TaskExtensions
    {
        public static void WhileNext<T>(this Func<Task<T>> Next, Action<T> Do) where T : class
        {
            T o = Next.Invoke().Result;

            if (o is null)
            {
                return;
            }

            Task<T> n = Next.Invoke();

            while (!(o is null))
            {

                Do.Invoke(o);

                o = n.Result;

                n = Next.Invoke();
            }
        }

        public static IEnumerable<T> YieldNext<T>(this Func<Task<T>> Next, Func<T, bool> Do) where T : class
        {
            T o = Next.Invoke().Result;

            if (o is null)
            {
                yield break;
            }

            Task<T> n = Next.Invoke();

            while (!(o is null))
            {

                if (Do.Invoke(o))
                {
                    yield return o;
                }

                o = n.Result;

                n = Next.Invoke();
            }
        }

        public static IEnumerable<T> YieldNext<T>(this Func<Task<T>> Next) where T : class
        {
            T o = Next.Invoke().Result;

            if (o is null)
            {
                yield break;
            }

            Task<T> n = Next.Invoke();

            while (!(o is null))
            {

                yield return o;

                o = n.Result;

                n = Next.Invoke();
            }
        }

    }
}

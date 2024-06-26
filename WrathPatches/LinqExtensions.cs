using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WrathPatches
{
    public static class LinqExtensions
    {
        public static IEnumerable<(int index, T item)> Indexed<T>(this IEnumerable<T> source)
        {
            var index = 0;
            foreach (var item in source)
                yield return (index++, item);
        }

        public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, int length, IEnumerable<Func<T, bool>> predicateSequence)
        {
            var i = 0;
            foreach (var result in predicateSequence.Zip(source, (f, x) => f(x)))
            {
                if (!result) return source.Skip(1).FindSequence(length, predicateSequence);

                i++;

                if (i >= length) return source.Take(i);
            }

            return Enumerable.Empty<T>();
        }

        public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicateSequence) =>
            source.FindSequence(predicateSequence.Count(), predicateSequence);

        public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, int length, Func<IEnumerable<T>, bool> predicate)
        {
            var subSeq = source.Take(length);
            if (subSeq.Count() < length) return Enumerable.Empty<T>();

            if (predicate(subSeq)) return subSeq;

            return source.Skip(1).FindSequence(length, predicate);
        }

        public static IEnumerable<IEnumerable<T>> Windowed<T>(this IEnumerable<T> source, int windowSize)
        {
            var buffer = new T[windowSize * 2];

            var e = source.GetEnumerator();

            var currentIndex = 0;

            IEnumerable<T> yieldWindow(int startIndex)
            {
                return buffer.Skip(startIndex).Take(windowSize);
            }

            while (e.MoveNext())
            {
                buffer[currentIndex] = e.Current;

                if (currentIndex >= windowSize)
                {
                    yield return yieldWindow(currentIndex - windowSize);
                }

                currentIndex++;

                if (currentIndex >= buffer.Length)
                {
                    var newBuffer = new T[windowSize * 2];
                    Array.Copy(buffer, windowSize, newBuffer, 0, windowSize);
                    buffer = newBuffer;
                    currentIndex = windowSize;
                }
            }

            if (currentIndex >= windowSize)
            {
                yield return yieldWindow(currentIndex - windowSize);
            }
            else
            {
                yield return buffer.Take(currentIndex);
            }
        }

        public static IEnumerable<(T, T)> Pairwise<T>(this IEnumerable<T> source)
        {
            foreach (var window in source.Windowed(2))
            {
                T x, y;

                var e = window.GetEnumerator();
                if (e.MoveNext())
                {
                    x = e.Current;

                    if (e.MoveNext())
                    {
                        y = e.Current;

                        yield return (x, y);
                    }
                }
            }
        }

        public static IEnumerable<(A, B)> Zip<A, B>(this IEnumerable<A> first, IEnumerable<B> second)
        {
            return first.Zip(second, (a, b) => (a, b));
            //var enumeratorA = first.GetEnumerator();
            //var enumeratorB = second.GetEnumerator();

            //while (enumeratorA.MoveNext() && enumeratorB.MoveNext())
            //    yield return (enumeratorA.Current, enumeratorB.Current);
        }
    }
}

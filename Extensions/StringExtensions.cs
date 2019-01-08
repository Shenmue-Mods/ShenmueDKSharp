using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Compares strings with culture and case sensitivity.
        /// </summary>
        /// <param name="str">Main string to check in.</param>
        /// <param name="toCheck">Substring to check for in Main String.</param>
        /// <param name="CompareType">Type of comparison.</param>
        /// <returns>True if toCheck found in str, false otherwise.</returns>
        public static bool Contains(this String str, string toCheck, StringComparison CompareType)
        {
            return str.IndexOf(toCheck, CompareType) >= 0;
        }

        /// <summary>
        /// Tests if <paramref name="item"/> is contained within <paramref name="enumerable"/>.
        /// </summary>
        /// <param name="enumerable">Enumerable to check.</param>
        /// <param name="item">Item to search for.</param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static bool Contains(this IEnumerable<string> enumerable, string item, StringComparison comparisonType)
        {
            return enumerable.Contains(item, new StringCaselessComparer(comparisonType));
        }

        class StringCaselessComparer : IEqualityComparer<string>
        {
            static StringComparison ComparisonType = StringComparison.OrdinalIgnoreCase;

            public StringCaselessComparer(StringComparison comparisonType)
            {
                ComparisonType = comparisonType;
            }

            public bool Equals(string x, string y)
            {
                return String.Equals(x, y, ComparisonType);
            }

            public int GetHashCode(string obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}

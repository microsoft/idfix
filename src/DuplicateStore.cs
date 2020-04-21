using System.Collections.Generic;

namespace IdFix
{
    class DuplicateStore
    {
        /// <summary>
        /// Key = attribute name
        /// Value = List of values found for that attribute
        /// </summary>
        private static Dictionary<string, List<string>> _lookup;

        public static bool IsDuplicate(string attributeName, string attributeValue)
        {
            if (DuplicateStore._lookup.ContainsKey(attributeName) && DuplicateStore._lookup[attributeName].Contains(attributeValue))
            {
                return true;
            }

            if (!DuplicateStore._lookup.ContainsKey(attributeName))
            {
                DuplicateStore._lookup.Add(attributeName, new List<string>());
            }

            DuplicateStore._lookup[attributeName].Add(attributeValue);
            return false;
        }

        public static void Reset()
        {
            DuplicateStore._lookup = new Dictionary<string, List<string>>();
        }
    }
}

using System.Collections.Generic;

namespace IdFix
{
    class DuplicateStore
    {
        /// <summary>
        /// Key = attribute name
        /// Value = List of values found for that attribute
        /// </summary>
        private static Dictionary<string, List<string>> _quickLookup;

        public static bool IsDuplicate(string attributeName, string attributeValue)
        {
            if (DuplicateStore._quickLookup.ContainsKey(attributeName) && DuplicateStore._quickLookup[attributeName].Contains(attributeValue))
            {
                return true;
            }

            if (!DuplicateStore._quickLookup.ContainsKey(attributeName))
            {
                DuplicateStore._quickLookup.Add(attributeName, new List<string>());
            }

            DuplicateStore._quickLookup[attributeName].Add(attributeValue);
            return false;
        }

        public static void Reset()
        {
            DuplicateStore._quickLookup = new Dictionary<string, List<string>>();
        }
    }
}

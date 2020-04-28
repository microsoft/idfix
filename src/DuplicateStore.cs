using System.Collections.Generic;

namespace IdFix
{
    /// <summary>
    /// Used to track duplicates in memory
    /// </summary>
    class DuplicateStore
    {
        /// <summary>
        /// Key = attribute name
        /// Value = List of values found for that attribute
        /// </summary>
        private static Dictionary<string, List<string>> _lookup;

        /// <summary>
        /// Determines if a given value is a duplicate for a given attribute
        /// </summary>
        /// <param name="attributeName">Name of the attribute whose value we are checking</param>
        /// <param name="attributeValue">Vlaue of the attribute we expect to be unique</param>
        /// <returns>True if the value is a duplicate, false otherwise</returns>
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

        /// <summary>
        /// Resets this store clearing all recorded values
        /// </summary>
        public static void Reset()
        {
            DuplicateStore._lookup = new Dictionary<string, List<string>>();
        }
    }
}

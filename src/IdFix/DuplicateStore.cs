using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;

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
        /// Key = attribute name
        /// Value = Dictionary of the attribute values and the first LDAP entry with that value
        /// </summary>
        private static Dictionary<string, Dictionary<string, SearchResultEntry>> _originalEntryLookup;

        /// <summary>
        /// Determines if a given value is a duplicate for a given attribute
        /// </summary>
        /// <param name="attributeName">Name of the attribute whose value we are checking</param>
        /// <param name="attributeValue">Value of the attribute we expect to be unique</param>
        /// <param name="entry">LDAP entry being checked</param>
        /// <returns>True if the value is a duplicate, false otherwise</returns>
        public static bool IsDuplicate(string attributeName, string attributeValue, SearchResultEntry entry)
        {
            if (!DuplicateStore._originalEntryLookup.ContainsKey(attributeName))
            {
                DuplicateStore._originalEntryLookup.Add(attributeName, new Dictionary<string, SearchResultEntry>());
            }

            if (DuplicateStore._originalEntryLookup.ContainsKey(attributeName) && !DuplicateStore._originalEntryLookup[attributeName].Keys.Contains(attributeValue, StringComparer.InvariantCultureIgnoreCase))
            {
                DuplicateStore._originalEntryLookup[attributeName].Add(attributeValue, entry);
            }

            if (DuplicateStore._lookup.ContainsKey(attributeName) && DuplicateStore._lookup[attributeName].Contains(attributeValue, StringComparer.InvariantCultureIgnoreCase))
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
            DuplicateStore._originalEntryLookup = new Dictionary<string, Dictionary<string, SearchResultEntry>>();
        }

        /// <summary>
        /// Gets the original (first) LDAP entry with the value matching the attribute
        /// </summary>
        /// <param name="attributeName">The name of the attribute</param>
        /// <param name="attributeValue">The attribute value</param>
        /// <returns>The original (first) LDAP entry</returns>
        public static SearchResultEntry GetOriginalSearchResultEntry(string attributeName, string attributeValue)
        {
            var dic = DuplicateStore._originalEntryLookup[attributeName];
            var key = dic.Keys.First(k => string.Compare(k, attributeValue, true) == 0);
            return dic[key];
        }
    }
}

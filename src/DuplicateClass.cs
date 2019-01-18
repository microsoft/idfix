// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IdFix
{
    class DuplicateClass
    {
        public string distinguishedName;
        public string objectClass;
        public string attribute;
        public string value;
        public string update;

        public DuplicateClass(string distinguishedName, string objectClass, string attribute, string value, string update)
        {
            this.distinguishedName = distinguishedName;
            this.objectClass = objectClass;
            this.attribute = attribute;
            this.value = value;
            this.update = update;
        }
    }
}

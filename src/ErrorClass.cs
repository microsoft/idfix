// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IdFix
{
    class ErrorClass
    {
        public string distinguishedName;
        public string objectClass;
        public string attribute;
        public string type;
        public string value;
        public string update;

        public ErrorClass(string distinguishedName, string objectClass, string attribute, string type, string value, string update)
        {
            this.distinguishedName = distinguishedName;
            this.objectClass = objectClass;
            this.attribute = attribute;
            this.type = type;
            this.value = value;
            this.update = update;
        }
            
    }
}

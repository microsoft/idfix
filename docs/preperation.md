# Preparation

Depending on the number of objects in the on-premises Active Directory, there may be a large number of objects to synchronize. Even a low failure rate can result in a large number of objects that must be manually corrected. This can significantly delay a deployment and increase project expense.  

The remediation effort is focused on directory synchronization errors which may be raised even if the on-premises environment seems to be operating normally.  Remember that the directory synchronization tools check for values that could potentially cause issues with cloud services that may not cause issues in the on-premises environment.

## Functionality

> The Administrator using the tool should understand the implications of modifying directory objects and attributes.

IdFix queries all domains in the currently authenticated forest and displays object attribute values which would be reported as errors by the  supported directory synchronization tool.  The datagrid supports the ability to scroll, sort, and edit those objects in a resulting table to produce compliant values.  Confirmed values can then be applied to the forest with the ability to undo updates.   Transaction rollback is supported.

In the case of invalid characters, a suggested “fix” is displayed where it can be determined from the existing value.  Changes are applied only to records for which the customer has set an ACTION value.   Customer confirmation of each change is enforced.

> Suggested values for formatting errors start with the removal of invalid characters and then the value must be updated by the user.  It is beyond the scope of this utility to determine what the user really wanted when a mistake in formatting is detected.

Not all objects should be made available for editing as some could cause harm to the source environment; e.g. critical system objects. These objects are excluded from the IdFix datagrid.  Well Known Exclusions as defined by the Deployment Guide are supported. 

Data can be exported into CSV or LDF format for offline editing or investigation.  Save to File is supported.

Import of CSV is supported.  There are caveats with this feature.  The function relies upon the distinguishedName and attribute to determine the value to update.  The best way to do this is to export from a query and change the Update.  Keep the other columns as they were and do not introduce escape characters into the values.

Since IdFix makes changes in the customer environment, logging is included.  Verbose logging is enabled by default.

Support for both Multi-Tenant and Dedicated versions of Office 365 are enabled in this release.  The rule sets are selected via the Settings icon on the menu. 

## Requirements

### Hardware Requirements
A physical or virtual machine is required in order to run IdFix.  The computer should meet the following specifications:
- 4 GB ram (minimum)
- 2 GB of hard disk space (minimum)

### Software Requirements

|Software|Description
|----|--------------------------
|Operating System|The application has been tested on Windows Server 2008 R2 and Windows 7 for x64 bit versions|
|.NET Framework 4.0|.NET Framework 4.0 or higher must be installed on the workstation running the application.|
|Active Directory|Queries are via native LDAP and have been tested with Windows Server 2008 R2, but all versions should be expected to work.|
|Exchange Server|The messaging attributes retrieved are version independent and should work with Exchange 2003 or newer.|
|Permissions|The application runs in the context of the authenticated user which means that it will query the authenticated forest and must have rights to read the directory.  If you wish to apply changes to the directory the authenticated user needs write permission to the desired objects.|

> Note that IdFix does not need to be installed on the Exchange or Active Directory server.  It merely needs to be installed on a workstation in the forest and have access to a Global Catalog server.

	
### Identity Management Systems Conflicts

It is important that any identity management system in the on-premises Active Directory environment be evaluated to determine if it creates any conflicts with IdFix.  The risk is after correcting an error, an on-premises identity management system may update the attribute again, returning it to its original error state.  Before implementing directory synchronization, it may be necessary to review or modify portions of existing identity management systems if they are repeatedly generating invalid attribute values. 

##	Active Directory Impacts

This section describes the updates that may be applied to attributes in the customer's on-premises Active Directory environment.

### Multi-tenant

Attributes that may be updated

- displayName
- givenName
- mail
- mailNickName
- proxyAddresses
- sAMAccountName
- sn
- targetAddress
- userPrincipalName

### Attribute Synchronization Rules
See the following support article for information on the attributes that can be included in synchronization.

[List of attributes that are synchronized to Office 365 and attributes that are written back to the on-premises Active Directory Domain Services](http://support.microsoft.com/kb/2256198) 

### Active Directory Attribute Values

IdFix checks several Active Directory attributes for the types of errors included in [Prepare for directory synchronization to Microsoft 365](https://docs.microsoft.com/en-us/microsoft-365/enterprise/prepare-for-directory-synchronization?view=o365-worldwide).

## Dedicated

Attributes that may be updated

- displayName
- mail
- mailNickName
- proxyAddresses
- targetAddress

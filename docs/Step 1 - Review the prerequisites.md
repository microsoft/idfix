# Step 1: Review the prerequisites

Ensure that the following prerequisites are in place:

## Minimum hardware requirements:

A physical or virtual machine is required to run   IdFix. The computer should minimally   meet the following specifications: 4 GB RAM and 2 GB of hard disk space.

## Minimum software requirements:

### Operating system

Windows Server 2008 R2 and Windows 7 for x64 bit versions or later versions of Windows and Windows Server.
.NET Framework 4.0: .NET Framework 4.5.2 or later must be installed on the workstation running the application.

### Windows Server Active Directory

Windows Server 2008 R2 or later versions. Don’t install and run the IdFix tool on a domain controller. The IdFix tool will, however, function correctly on an Azure AD Connect server even after directory synchronization has already occurred.

### Exchange Server 

The messaging attributes retrieved are version independent and should work with Exchange 2003 or later.

### Permissions

The application runs in the context of the authenticated user, which means that it will query the authenticated forest and must have rights to read the directory. If you want to apply changes to the directory, the authenticated user needs read/write permission to the desired objects.

Note: IdFix does not need to be installed on the Exchange or Active Directory server. It merely needs to be installed on a workstation in the forest and have access to a global catalog server. 

Important: It is important that any identity management system in the on-premises Active Directory environment be evaluated to determine if it creates any conflicts with IdFix. The risk is that after correcting an error, an on-premises identity management system might update the attribute again, returning it to its original error state. Before implementing directory synchronization, it might be necessary to review or modify portions of existing identity management systems if they are repeatedly generating invalid attribute values.

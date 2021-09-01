# Query and fix invalid object attributes with the IdFix tool

 ![IdFix logo](img/IdFixLogo.png)

## What is the IdFix tool?

Microsoft is working to reduce the time required to remediate identity issues when onboarding to Microsoft 365. A portion of this effort is intended to address the time involved in remediating the Windows Server Active Directory (Windows Server AD)   errors reported by the directory synchronization tools such as Azure AD Connect and Azure AD Connect cloud sync. The focus of IdFix is to enable you to accomplish this task in a simple, expedient fashion.

The IdFix tool provides you the ability to query, identify, and remediate the majority of object synchronization errors in your Window’s Server AD forests in preparation for deployment to Microsoft 365. The utility does not fix all errors, but it does find and fix the majority. This remediation will then allow you to successfully synchronize users, contacts, and groups from on-premises Active Directory into Microsoft 365.
Note: IdFix might identify   errors beyond those that emerge during synchronization. The most common example is compliance with rfc 2822 for smtp addresses. Although invalid attribute values can be synchronized to the cloud, the product group recommends that these errors be corrected.

## How it works    

IdFix queries all domains in the currently authenticated forest and displays object attribute values that would be reported as errors by the supported directory synchronization tool. The DataGrid view supports the ability to scroll, sort, and edit those objects in a resulting table to produce compliant values. Confirmed values can then be applied to the forest with the ability to undo updates. Transaction rollback is supported.

In the case of invalid characters, a suggested “fix” is displayed where it can be determined from the existing value. Changes are applied only to records for which you have set an ACTION value. Confirmation of each change is enforced.

Suggested values for formatting errors start with the removal of invalid characters and then the value must be updated by you. It is beyond the scope of this utility to determine what you wanted when a mistake in formatting is detected.

Data can be exported into CSV or LDF format for offline editing or investigation. Save to File is supported.

Because IdFix makes changes in your on-premises environment, logging is included. Verbose logging is enabled by default.


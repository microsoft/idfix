# IdFix : Directory Synchronization Error Remediation Tool

IdFix is used to perform discovery and remediation of identity objects and their attributes in an on-premises Active Directory environment in preparation for migration to Azure Active Directory. IdFix is intended for the Active Directory administrators responsible for directory synchronization with Azure Active Directory.

The purpose of IdFix is to reduce the time involved in remediating the Active Directory errors reported by Azure AD Connect. Our focus is on enabling the customer to accomplish the task in a simple expedient fashion without relying upon subject matter experts. 

The Microsoft Office 365 IdFix tool provides the customer with the ability to identify and remediate object errors in their Active Directory in preparation for deployment to Azure Active Directory or Office 365. They will then be able to successfully synchronize users, contacts, and groups from the on-premises Active Directory into Azure Active Directory.

## ClickOnce Launch

You can _**[launch](https://raw.githubusercontent.com/Microsoft/idfix/master/publish/setup.exe)**_ the application using the ClickOnce installer. Download and run the setup.exe file to install IdFix on your machine.

If you can't launch the application, check the registry key mentioned here: https://github.com/microsoft/idfix/issues/20#issuecomment-704497252

## Alternate MSI Installation

If running the ClickOnce application is not desirable or is not possible in your environment, you can install it using one of the MSI's located at: https://github.com/microsoft/idfix/tree/master/MSIs

Note that only the ClickOnce application is self-updating to the latest version.

## Documentation

Please see [the docs for details](https://microsoft.github.io/idfix) on using IdFix. If you see any gaps or issues [please let us know](https://github.com/microsoft/idfix/issues).

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.


-----
> Part of the Microsoft FastTrack Open Source Software initiative. For full details, please see https://github.com/microsoft/fasttrack.


# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## 2.6.0.3 - 2024-12-03

- Added error suppression during duplicate detection to resolve error: "Sequence contains no matching element". (#73)

## 2.6.0.2 - 2022-03-15
- User can now select AuthType to resolve error message: "The server does not support the control. The control is critical." (#17) Try selecting NTLM or Basic for AuthType on the Settings page if you get this error.

## 2.6.0.1 - 2022-01-25

- Fixed bug in ClickOnce application where domains.txt did not contain the list of valid top level domains (#68)
- Fixed bug in MSI application where domains.txt was not copied to the installation folder (#69)

## 2.6.0.0 - 2022-01-24

- Added CommonName (CN) attribute to results for easier sorting (#28)
- targetAddress is allowed to be blank when homeMdb is populated for dedicated tenants (#39)
- Apostrophe is an allowed character again (#46)
- Added .africa to list of valid top level domains (#59)
- You can now add to the list of valid top level domains by editing the domains.txt file located in the same folder as IdFix.exe (#59)
- Fixed case-sensitive check for non-replicated attributes (#63)

## 2.5.0.0 - 2022-01-07

- Fixed bug in proxyAddress duplicate detection when values differ only in casing (#29, #40)
- When a duplicate LDAP attribute is detected, all affected accounts are displayed. (#40)

## 2.4.0.0 - 2021-10-09

- When connecting to a Global Catalog Server on port 3268 and the attributes for validation are not replicated, a warning is displayed
- When connecting using LDAP port (389) or secure LDAP port (636), a warning is displayed indicating that only the local domain will be checked and not the entire forest
- Added MSI installation package for users that cannot use the ClickOnce application (#20, #32, #35, #36, #41, #42) NOTE: The MSI installation is not self-updating and will not check for updates
- A 64-bit operating system is now required to run IdFix
- Added .swiss to list of valid top level domains (#38)

## 2.3.0.0 - 2020-08-14

- Fixed false errors about parenthesis ( ) characters in x500 Addresses (#16)
- Fixed Search Base not working (#25)

## 2.2.0.0 - 2020-04-30

- Full refactoring of existing code
- Fixed issue with tld errors being misreported
- Fixed issue with valid x400 & x500 addresses being flagged as invalid
- Added check for (') char in proxy address and UPN

## 2.1.3.0 - 2020-04-06

- Added Alternate ID option in the settings that permit to skip checks on UserPrincipalName attribute
- Added .gmbh to list of valid domain extensions

## 2.0.2.0 - 2020-01-22

- Added .cloud to the list of tld's supported by the application (#5)
- Fixed credentials bug (#6)

## 2.0.2.0 - 2019-10-16

- Added .edu.au to the list of tld's supported by the application. (#1)

## 2.0.0.0 - 2018-12-14

First open source release from GitHub!

## 2018-10-23

Initial commit from original Microsoft internal vsts source

# Step 3: Query and fix invalid attributes

![Screen shot of the tool running](IdFixblank.png)

1.	Log on to the Windows machine where you installed the IdFix tool using an account that has read/write permissions to your on-premises Active Directory objects.
Directory synchronization rule sets are different depending on which version of Microsoft 365 is in use. Use Settings to choose between running the Multi-Tenant or Dedicated/ITAR   rule sets to detect attribute values known to cause directory synchronization errors relevant to the version of Office 365 in use.
2.	The scope of the query can be limited by selecting Settings and entering a valid directory path in the Filter   field to use as a start for the subtree search. Only one starting point can be designated at a time.
The subtree point will be used for all successive queries until changed. Deleting the value will reset the query to the whole forest. The value must be entered in the format OU=myOu,DC=Contoso,DC=com.

![Settings page](IdFixSettings.png)

3.	Select Query to query for objects containing invalid attributes that will cause directory synchronization errors.

IdFix queries all objects with a filter for applicable attributes. IdFix updates the status line on the bottom of the DataGrid view and writes all values to the log.

If you don’t want to continue, you can select Cancel to terminate a running query.

![IdFix running query](IdFixQuery.png)

4.	IdFix applies rules against the required AD attributes to determine which objects must be remediated and presents you with any detected error conditions.
    
    IdFix displays items with information related to the object in question and the error conditions. Objects are identified by the distinguishedName field with the associated         error type and value that is in error.

5.	Where feasible, IdFix presents a recommendation for corrective data in the UPDATE column. Recommendations are based on a “best effort” approach for the specific object in question. Because recommendations are object specific, they are not checked against the existing data set and might introduce additional errors.

6.	For certain types of errors (duplicates and format errors), a recommendation for correction is not provided. Corrective information must be manually entered to correct the issue.

7.	In the event that multiple errors are associated with a single attribute, errors are combined into a single line item.

8.	If a blank DataGrid is displayed after execution, then no errors were returned. This is a good thing.

9.	To correct the object attribute values, select one of the following options from the ACTION dropdown list:

|||
|-|-|
|COMPLETE|The original value is acceptable and should not be changed despite being identified as being in an error state.  For example, two users may have a proxyAddress identified as duplicate.  Only one can use the value for mail delivery.  The user with the correct value should be marked as COMPLETE, while the other user is marked as REMOVE.|
|REMOVE|The attribute value will be cleared from the source object.  In the case of a multi-valued attribute; e.g. proxyAddresses, only the individual value shown will be cleared.|
|EDIT|The information in the UPDATE column will be used to modify the attribute value for the selected object.  In many cases, a valid update value has been predetermined.  In these cases, you can mark the ACTION as EDIT and go on to the next error. If the predetermined update value is not desired, you can manually input the new value.|
|UNDO|This value is only shown if the user has loaded a previously saved Update file.  The sole operation that can be executed is to restore the original value.|
|FAIL|This value is only shown if an update value has an unknown conflict with the directory rules.  In this case, you may attempt to edit the value again. It may be necessary to analyze the values in the object using ADSIEDIT.|

> Only errors where you have selected an action will  be considered for update. To reiterate: unless a specific choice is made in the ACTION column  , IdFix will not perform any operation on the error.   

10.	The option to Accept all   suggested updates is available.

11.	After selecting the ACTION for one or more errors, select Apply to write the values to Active Directory. Successful writes are indicated by displaying “COMPLETE” in the ACTION column.

12.	IdFix writes all UPDATE transactions to a transaction log. The following is an example.

```
7/22/2021 6:36:44 AM INITIALIZED - IDFIX VERSION 2.0.0 - MULTI-TENANT
7/22/2021 6:36:47 AM QUERY AD
7/22/2021 6:36:47 AM FOREST:E2K10.COM SERVER:DC1.E2K10.COM FILTER:(|(OBJECTCATEGORY=PERSON)(OBJECTCATEGORY=GROUP))
7/22/2021 6:36:47 AM PLEASE WAIT WHILE THE LDAP CONNECTION IS ESTABLISHED.
7/22/2021 6:36:49 AM QUERY COUNT: 140  ERROR COUNT: 29  DUPLICATE CHECK COUNT: 191
7/22/2021 6:36:49 AM ELAPSED TIME: AD QUERY - 00:00:02.3890432
7/22/2021 6:36:49 AM WRITE SPLIT FILES
7/22/2021 6:36:49 AM MERGE SPLIT FILES
7/22/2021 6:36:49 AM COUNT DUPLICATES
7/22/2021 6:36:49 AM WRITE FILTERED DUPLICATE OBJECTS
7/22/2021 6:36:49 AM READ FILTERED DUPLICATE OBJECTS
7/22/2021 6:36:49 AM READ ERROR FILE
7/22/2021 6:36:49 AM ELAPSED TIME: DUPLICATE CHECKS - 00:00:00.0780785
7/22/2021 6:36:49 AM POPULATING DATAGRID
7/22/2021 6:36:50 AM ELAPSED TIME: POPULATE DATAGRIDVIEW - 00:00:00.0780785
7/22/2021 6:36:50 AM QUERY COUNT: 140  ERROR COUNT: 53
7/22/2021 6:37:34 AM APPLY PENDING
7/22/2021 6:37:34 AM UPDATE: [CN=USER000001,OU=E2K10OU1,DC=E2K10,DC=COM][USER][MAILNICKNAME][CHARACTER][USER?^|000001][USER000001][EDIT]
7/22/2021 6:37:34 AM UPDATE: [CN=USER000008,OU=E2K10OU1,DC=E2K10,DC=COM][USER][TARGETADDRESS][DUPLICATE][SMTP:USER000008@CUSTOMER.COM][][REMOVE]
7/22/2021 6:37:34 AM COMPLETE
7/22/2021 6:37:40 AM LOADING UPDATES
7/22/2021 6:37:40 AM ACTION SELECTION
7/22/2021 6:37:57 AM APPLY PENDING
7/22/2021 6:37:57 AM UPDATE: [CN=USER000001,OU=E2K10OU1,DC=E2K10,DC=COM][USER][MAILNICKNAME][CHARACTER][USER?^|000001][USER000001][UNDO]
7/22/2021 6:37:57 AM UPDATE: [CN=USER000008,OU=E2K10OU1,DC=E2K10,DC=COM][USER][TARGETADDRESS][DUPLICATE][SMTP:USER000008@CUSTOMER.COM][][UNDO]
7/22/2021 6:37:57 AM COMPLETE
```
13.	In the event of an unwanted correction, you may perform a transaction update undo one level deep per UPDATE transaction.

    a.	Apply generates a LDF file for the transactions that are applied.

    b.	To Undo a transaction, select the LDF file that contains the appropriate transaction and reload it into the table.

Note: IdFix can’t track updates to objects or attributes that occur outside the application. If you and someone else edit the same attribute, then the last change is the one committed to the object.

14.	You can Export what’s in the table to review with others before taking corrective action, or to use as the source of a later bulk import using a separate utility like CSVDE or LDIFDE. Testing is strongly recommended, no matter which tool you use.

15.	You also can Import data from a CSV file to allow offline manual edits to be applied. Be very careful with manually edited files and use an Exported CSV file as a template. Testing is strongly recommended and there is no guarantee that what you do offline will be correctly recognized by IdFix.

16.	If the query returns more than 50,000 errors, the menu items Next Block and Previous Block are displayed. The number of errors that can be displayed on the screen at one time is limited to avoid application exceptions resulting from exceeding physical memory.


> You may always submit suggestions for improvement or support requests via the [issues list](https://github.com/Microsoft/idfix/issues).

##	Explanations of errors
For details on the errors that apply to each attribute see the Errors supported by the IdFix tool section in the Appendix.

|||
|------|-----|
|Character|The Value contains a character which is invalid.  The suggested Update will show the value with the character removed.|
|Format|The Value violates the format requirements for the attribute usage.  The suggested Update will show the Value with any invalid characters removed.  If there are no invalid characters the Update and Value will appear the same.  It is up to the user to determine what they really want in the Update.  For example SMTP addresses must comply with rfc 2822 and mailNickName cannot start or end with a period.|
|TopLevelDomain|This applies to values subject to rfc2822 formatting.  If the top level domain is not internet routable then this will be identified as an error.  For example a smtp address ending in .local is not internet routable and would cause this error.|
|DomainPart|This applies to values subject to rfc2822 formatting.  If the domain portion of the value is invalid beyond the top level domain routing this will be generated.|
|LocalPart|This applies to values subject to rfc2822 formatting.  If the local portion of the value is invalid this will be generated.|
|Length|The Value violates the length limit for the attribute.   This is most commonly encountered when the schema has been altered.  The suggested Update will truncate the value to the attribute standard length.|
|Duplicate|The Value has a duplicate within the scope of the query.  All duplicate values will be displayed as errors.  The user can Edit or Remove values to eliminate duplication.|
|Blank|The Value violates the null restriction for attributes to be synchronized.  Only a few values must contain a value.  The suggested Update will leverage other attribute values in order to generate a likely substitute.|
|MailMatch|This applies to Dedicated only.  The Value does not match the mail attribute.  The suggested Update will be the mail attribute value prefixed by “SMTP:”.|

# Frequently Asked Questions

## Feedback 
Bug reports and desired feature requests can be submitted to issues list where it will reviewed. We also happily accept pull requests containing new functionality or fixes.


## Performance 
IdFix performance will vary based on the hardware utilized and the network latency to the Active Directory global catalog (GC) server.  Machines should have the minimum RAM specified and will benefit by using faster hard drives since temporary files are written to disk during the AD Query.  High latency connections to a DC are discouraged and the best performance will be experienced by running the application on a DC.

## Number of errors shown
Customers with more than 50,000 errors returned faced the possibility of exceeding physical memory in attempting to display all data at one time.  To alleviate this issue, errors are broken into blocks of 50,000.  Exceeding the block size enables the More Errors options of View Next Block and View Previous Block.

## Temporary files
Large volumes of data may be parsed in the search for duplicate values.  For instance a user may have up to six (6) attributes that must be checked and the proxyAddresses field can have many values.  The duplicate check count may routinely exceed five (5) times the number of objects returned. For this reason, data must be written to disk to avoid the out of memory exception on most workstations.  Do not delete the temporary files while running.  This will trigger an exception and may cause unpredictable results.

## Directory Exceptions 
There are 60 separate LDAP return codes and four (4) types of directory exceptions that can occur when contacting a directory server.  These should be gracefully handled with a message box and an error written to the log.  Client-server timeout is one example where the condition may be sporadic.  To alleviate this issue, the default LDAP timeout interval has been increased from 30 seconds to two (2) minutes.  This is a server side limitation, and the application respects all server side limits.  If this should occur, then wait for a period of time when the GC is not so heavily utilized and/or launch the application from a lower latency location. For example, directly on a global catalog server.

## Don’t see updates in other domains 
If the response to an update was COMPLETE, then the value has been applied to the directory. However, you may need to wait for replication to complete.  Attempting to apply the update multiple times in a row may result in an exception stating the server is unwilling to process the request.  Run Query AD again and you should observe the error has been resolved. 

## FAIL (ACTION) 
Extensive efforts have been made to make the schema and value limits of the Active Directory unobtrusive.  With that caveat, it is still possible for the value entered in the UPDATE column to conflict with a directory rule within AD.  If the ACTION value on a row turns to FAIL then there is an unknown conflict between the UPDATE column and the attribute values stored in the directory.  These conflicts will require that the attribute values currently stored be examined more closely and may require ADSIEDIT in order to resolve.

## Double Byte Characters 
IdFix has not been localized and double byte characters have not been tested in the application.  Please send any errors of this type to IdFixSupport@Microsoft.com for further investigation.

## Filtering 
The application allows the user to specify a directory branch to use to begin the subtree search for errors.  Multiple subtrees are not supported and the start point will be used by the Query function until changed in the Filter Subtree dialog.  Filtering of values within the column is not supported as they can already be sorted.  Selecting Import to use a manually edited file will reset the filter to the whole forest for the Query option.

## Sorting 
The data columns can be sorted by clicking on the column header as is standard in dataGridView UX behavior.  Clicking again will reverse the sort.

## Export Data 
Exporting of data is facilitated via the Export icon.  

## Import Data 
The ability to Import a CSV file is now supported.  This feature is a use at your own risk option.  While we’ve created error and format handling functions in order to help populate the datagrid correctly there is no way to insure that incorrect errors are not introduced by manually editing the file.
Consider these best practice recommendations:
- Use Excel to view and edit the source file.
- Do not change the number of columns, headings, or any field values other than Update or Action.
- Do not save the file with any format other than CSV.
- Do not use escape characters in the field values.
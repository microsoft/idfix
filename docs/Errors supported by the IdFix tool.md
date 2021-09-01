# Errors supported by the IdFix tool  


## Multi-Tenant Errors

### All objects

Well known exclusions

- Admini*
- CAS_{*
- DiscoverySearchMailbox*
- FederatedEmail*
- Guest*
- HTTPConnector*
- krbtgt*
- iusr_*
- iwam*
- msol*
- support_*
- SystemMailbox*
- WWIOadmini*
- *$
- distinguishedName contains “\0ACNF:”
- contains IsCriticalSystemObject

### mail
- no duplicates 

### mailNickName
- may not begin with a period
- no duplicates

### proxyAddresses
- invalid chars whitespace < > ( ) ; , [ ] “
- rfc2822 & routable namespace (smtp only)
- no duplicates
- single value maximum number of characters: 256

### sAMAccountName (only if no userPrincipalName value)
- invalid chars  [ \ “ | , / : < > + = ; ? * ]
- no duplicates
- maximum number of characters: 20
### targetAddress
- invalid chars whitespace \ < > ( ) ; , [ ] “
- rfc2822 & routable namespace (smtp only)
- no duplicates 
- maximum number of characters: 255
### userPrincipalName
- invalid chars whitespace \ % & * + / = ?  { } | < > ( ) ; : , [ ] “ and umlaut
- rfc2822 & routable namespace format
- no duplicates 
- maximum number of characters: 113
- less than 64 before @
- less than 48 after @

## Dedicated Errors

### All objects

Well known exclusions

- Admini*
- CAS_{*
- DiscoverySearchMailbox*
- FederatedEmail*
- Guest*
- HTTPConnector*
- krbtgt*
- iusr_*
- iwam*
- msol*
- support_*
- SystemMailbox*
- WWIOadmini*
- *$
- distinguishedName contains “\0ACNF:”
- contains IsCriticalSystemObject

### displayName
- not blank (group)
- no leading or trailing white space
- less than 256

### mail
- no white space
- rfc2822 & routable namespace
- no duplicates 
- less than 256

### mailNickName
- not blank (contact and user)
- invalid chars whitespace \ ! # $ % & * + / = ? ^ ` { } | ~ < > ( ) ' ; : , [ ] " @
- may not begin or end with a period
- less than 64

### proxyAddresses
- DEPRECATED - no leading or trailing white space
- rfc2822 & routable namespace (smtp only)
- no duplicates
- single value maximum number of characters: 256

### targetAddress
- not blank (contact and user without homeMdb)
- DEPRECATED - invalid chars whitespace
- rfc2822 & routable namespace (smtp only)
- DEPRECATED - no duplicates 
- less than 256
- value = mail (contact and user [if no homeMdb])

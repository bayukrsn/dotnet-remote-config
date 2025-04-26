# dotnet-remote-config

A dotnet webapi with remote config on vault without appsettings file, it will fetched when the app starting up. Vault can be used as a centralized config for multiple services across multiple nodes.
No more pain, no more suffering anymore.

Ask your devops engineer to put this on environment variable before starting up the app:

export VAULT_ADDR="your_vault_address"

export VAULT_TOKEN="your_vault_token"

Vault Secret Path:
![alt text](https://github.com/bayukrsn/dotnet-remote-config/blob/main/vaultpath.png?raw=true)

The result:
![alt text](https://github.com/bayukrsn/dotnet-remote-config/blob/main/fetchedsecret.png?raw=true)



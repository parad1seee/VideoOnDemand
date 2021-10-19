# VideoOnDemand Backend

List of supported OS for deploying:
https://github.com/dotnet/core/blob/master/release-notes/5.0/5.0-supported-os.md
The best option for AWS EC2 instance is Ubuntu

## Requirements

.Net Core SDK 5.0.0+

.Net Core Runtime:
	  Microsoft.AspNetCore.App 5.0.0+

## Database

MSSQL Server 2019+ 
	  

Nginx and Supervisor are preferred in Linux systems

## Building

Install the dependencies and start the server:
```
$ cd VideoOnDemand
$ dotnet restore
$ dotnet publish
```


Update the code:
```
$ supervisorctl stop VideoOnDemand
$ git pull origin {branch}
$ dotnet publish
$ supervisorctl start VideoOnDemand

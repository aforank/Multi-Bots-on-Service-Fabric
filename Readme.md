# Prerequisites

Prior to starting these labs, you must have the following operating system and software configured on your local machine:

**Operating System**

- 64-bit Windows 10 Operating System or Windows Server 2016
    - [download](https://www.microsoft.com/windows/get-windows-10)
- Microsoft .NET Framework 4.6.2 or higher <sup>1</sup>
    - [download](https://www.microsoft.com/en-us/download/details.aspx?id=53344)

**Software**

| Software | Download Link |
| --- | --- |
| Service Fabric SDK 3.1.274 | [/download.microsoft.com/service-fabric-sdk](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started)
| Service Fabric Tools 3.1.274 | [/download.microsoft.com/service-fabric-tools](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started)
| Bot Builder SDK 3.15 or higher | [/download.microsoft.com/bot-builder-sdk](https://docs.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-overview?view=azure-bot-service-3.0)
| Visual Studio 2017 Enterprise | [/code.visualstudio.com/download](https://www.visualstudio.com/downloads) |

---

# Labs

- [Pre-lab: Subscribe for Azure subscription and VSTS subscription](01-getting_started.md)
- [Lab 1: Set up of Resources - Service Fabric Cluster, Api Management and Bot Channel Registration](02%20-setup-resources-and-environment.md)
- [Lab 2: Download code template and configure bots to be published in Fabric cluster with Actor Programming model](03-code-excercise.md)
- [Post-lab: Cleaning Up](04-cleaning_up.md)

---

# Notes

1. If you are unsure of what version of the .NET Framework you have installed on your local machine, you can visit the following link to view instructions on determining your installed version: <https://docs.microsoft.com/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed>.
2. If you are unsure of how to create a bot project template, you can visit the following link to view instructions: <https://docs.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-quickstart?view=azure-bot-service-3.0>.
3. If you are unsure of how to create a service fabric project template, you can visit the following link to view instructions: <https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/service-fabric/service-fabric-create-your-first-application-in-visual-studio.md>.
4. If you are unsure of how to create a web api project template, you can visit the following link to view instructions: <https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-2.1>.
5. If you are unsure of how to deploy an application with CI/CD to a Service Fabric cluster, you can visit the following link to view instructions: <https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-tutorial-deploy-app-with-cicd-vsts>.

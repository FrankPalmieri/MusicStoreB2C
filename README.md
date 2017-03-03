# MusicStoreB2C

This repository pulls code from a number of different places. First off the solution is setup for Visual Studio 2017 RC.

This link documents the basics of getting the web app up and running with Azure AD B2C

[An ASP.NET Core web app with Azure AD B2C](https://azure.microsoft.com/en-us/resources/samples/active-directory-dotnet-webapp-openidconnect-aspnetcore-b2c/)

The code to connect to Azure AD B2C above is merged into [MusicStore (Sample ASP.NET Core application)](https://github.com/aspnet/MusicStore).  The implementation cuts out the ASP.Net Identity code originally in this sample as well as the support for that in StartupOpenIDConnect.cs. That code is still in the repository and put off to the side as MS* view folders and CS files (i.e. MSAccount, MSManage, MS\*Controller.cs, Orig\*.cs, etc.) - referring to that code or putting them back in place should work but YMMV. There are also a few fixes from the original Azure AD B2C code above to correct issues with sign out working properly, etc. from issues logged against the sample.  The sources of those changes are noted in comments in the code.

Next we wanted to add in a WebAPI so I used parts of this sample [Building Your First Web API with ASP.NET Core MVC and Visual Studio](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api) - which is the ToDo service used in various samples to test that out.  I added some code modeled on this [Getting Started with Filters](https://github.com/ardalis/GettingStartedWithFilters) to get the API to correctly return error codes directly instead of running through the Error Page functionality built in to the website. I also updated the service code to support partial updates with PATCH. 

We also wanted to pull some information from the AD B2C directory such as group membership to support admin functionality and to get (and eventually set) a thumbnail image for the user.  So the code calls the Graph API.  Getting that working is a bit complicated. This article [Azure AD B2C: Use the Graph API](https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-devquickstarts-graph-dotnet) walks through getting the Graph API client setup with the correct permissions. Code from the B2CGraphClient was pulled in to accomplish that. Because the code checks group membership on login to setup the appropriate OAuth claim, this needs to get setup to function properly.

We need to have our Windows applications (Win32) call into the Web API.  I pulled together a few different things to get that working. The basics of this are described here [Azure AD B2C: Build a Windows desktop app](https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-devquickstarts-native-dotnet). Some of the code and more importantly the setup also comes from this article [Calling a ASP.NET Core Web API from a WPF application using Azure AD](https://azure.microsoft.com/en-us/resources/samples/active-directory-dotnet-native-aspnetcore/), particularly using Powershell to setup the appropriate permissions. The main code of the MusicStoreClient comes from the ToDoListClient from [Integrating Azure AD into a Windows desktop application](https://github.com/Azure-Samples/active-directory-dotnet-native-desktop)

Last, I wanted to be able to have "local" configuration for the Azure AD B2C so that I would not copy the configuration into GitHub so I added support (config and code) for using config.local.json in the Web application and App.local.config in the Win32 Application and put the configuration I wanted in those files and modified .gitignore to ignore any files matching in *.local.json and *.local.config which will hopefully support that.



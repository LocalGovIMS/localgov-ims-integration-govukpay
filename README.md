# localgov-ims-integration-govukpay
LocalGov IMS codebase for a GOV.UK Pay integration

To build this solution you will need to add the LocalGov IMS package feed as a package source to your development environment

An example of how to do that on a device running the Windows operating system and using Visual Studio:

1. Create a GitHub Personal Access Token (PAT) which has read access to LocalGov IMS NuGet package feed:
  - Visit GitHub
	- Navigate to your account > Settings > Developer Settings > Personal Access Tokens
	- Create a new token and ensure it has read access to pacakges.
		(If you plan on publishing package updates you may want to consider giving the token write access too)
2. Open the Developer Powershell from Visual Studio (Tools > Command Line > Developer Powershell)
3. Add the LocalGov IMS package source:
	Run: dotnet nuget add source "https://nuget.pkg.github.com/LocalGovIMS/index.json" --name "LocalGov IMS" --username [YOUR GITHUB USERNAME] --password [YOUR PAT]
	
Note: You may need to restart Visual Studio for the changes to take effect

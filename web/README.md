### Easiest way to run the site
* Run the Web project in Visual Studio to run a minimal C# server to serve the static site, a conversion endpoint and swagger

### To use with Azure functions 
* You can change the api source to any source set up in [ClientSettings.json](https://github.com/icsharpcode/CodeConverter/blob/master/web/src/ClientSettings.json). e.g. append `?apisource=HostedFunc` to the url
* You can change the default api source in [index.html](https://github.com/icsharpcode/CodeConverter/blob/master/web/index.html)

#### Examples
* In live: https://icsharpcode.github.io/CodeConverter?apisource=HostedFunc
* Local debugging:
  * Host the static site by running `npm start` in the web folder
  * Start the Func project in Visual Studio
  * Visit http://localhost:5173/?apiSource=LocalFunc
* Run `npm run build` to create a dist folder ready for deployment on any other static host


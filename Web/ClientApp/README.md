Easiest way to run the site:
* Run the Web project in Visual Studio to run a minimal C# server to serve the static site, a conversion endpoint and swagger
* You can change the api source to any source set up in [ClientSettings.json](https://github.com/icsharpcode/CodeConverter/blob/master/Web/ClientApp/src/ClientSettings.json). e.g. append `?apisource=HostedFunc` to the url
* You can change the default api source in [index.html](https://github.com/icsharpcode/CodeConverter/blob/master/Web/ClientApp/public/index.html#L3)

Example of use with Azure functions:
* In live: https://codeconverter.icsharpcode.net?apisource=HostedFunc
* Local debugging:
  * Host the static site by running `npm start` in the Web/ClientApp folder
  * Start the Func project in Visual Studio
  * Visit http://localhost:3000/?apiSource=LocalFunc
* Run `npm run build` to create a dist folder ready for deployment on any other static host

See https://create-react-app.dev/docs/getting-started/ for details on customizing

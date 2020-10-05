Easiest way to run the site:
* Run the Web project in Visual Studio to run a minimal C# server to serve the static site, a conversion endpoint and swagger

Combining with Azure functions:
* Either run the Func project locally, or elsewhere
* In ClientSettings.json's set the conversion endpoint url
* Then in the ClientApp folder, either:
  * Run `npm start` to start a local development server hosting the static files
  * Run `npm run build` to create a dist folder ready for deployment on any other static host

See https://create-react-app.dev/docs/getting-started/ for details on customizing
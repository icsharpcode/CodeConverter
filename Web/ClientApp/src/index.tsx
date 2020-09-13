import React, { Component }  from 'react';
import * as ReactDOM from "react-dom";
import { Router } from "react-router";
import { createBrowserHistory } from "history";
import App from "./App";
import registerServiceWorker from "./registerServiceWorker";

// Create browser history to use in the Redux store
const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href") as string;
const history = createBrowserHistory({ basename: baseUrl });

ReactDOM.render(
    <Router history={history}>
        <App/>
    </Router>,
    document.getElementById("root"));

registerServiceWorker();
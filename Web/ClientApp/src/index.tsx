import 'bootstrap/dist/css/bootstrap.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import registerServiceWorker from './registerServiceWorker';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href') || undefined;

const container = document.getElementById('root');
const root = createRoot(container!);

root.render(
    <BrowserRouter basename={baseUrl}>
        <App />
    </BrowserRouter>);

registerServiceWorker();
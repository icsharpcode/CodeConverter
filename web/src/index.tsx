import 'bootstrap/dist/css/bootstrap.css';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';

// Set the API source in the document head for the API module to read
const headElement = document.head as HTMLElement & { dataset: DOMStringMap };
headElement.dataset['apisource'] = import.meta.env.VITE_DEFAULT_APISOURCE || 'LocalWeb';

const baseUrl = document.getElementsByTagName('base')[0]?.getAttribute('href') || undefined;

const container = document.getElementById('root');
const root = createRoot(container!);
let normalizedBaseUrl = baseUrl;
if (normalizedBaseUrl && normalizedBaseUrl.endsWith('/') && normalizedBaseUrl !== '/') {
    normalizedBaseUrl = normalizedBaseUrl.substring(0, normalizedBaseUrl.length - 1);
}

root.render(
    <StrictMode>
        <BrowserRouter basename={baseUrl}>
            <App />
        </BrowserRouter>
    </StrictMode>
);


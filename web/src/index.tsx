import 'bootstrap/dist/css/bootstrap.css';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';

// Set the API source in the document head for the API module to read
const headElement = document.head as HTMLElement & { dataset: DOMStringMap };
headElement.dataset['apisource'] = import.meta.env.VITE_DEFAULT_APISOURCE || 'LocalWeb';


const container = document.getElementById('root');
const root = createRoot(container!);
// Derive basename from BASE_URL (set at build time)
const baseUrl = import.meta.env.BASE_URL;
const normalizedBaseUrl = baseUrl && baseUrl.endsWith('/') && baseUrl !== '/' ? baseUrl.substring(0, baseUrl.length - 1) : baseUrl;

root.render(
    <StrictMode>
        <BrowserRouter basename={normalizedBaseUrl}>
            <App />
        </BrowserRouter>
    </StrictMode>
);


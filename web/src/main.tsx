import 'bootstrap/dist/css/bootstrap.css';
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.tsx'

// Set the API source in the document head for the API module to read
const headElement = document.head as HTMLElement & { dataset: DOMStringMap };
headElement.dataset['apisource'] = import.meta.env.VITE_DEFAULT_APISOURCE || 'LocalWeb';

const baseUrl = document.getElementsByTagName('base')[0]?.getAttribute('href') || undefined;

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter basename={baseUrl}>
      <App />
    </BrowserRouter>
  </StrictMode>,
)

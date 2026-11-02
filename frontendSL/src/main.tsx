import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App.tsx';
import { AuthProvider } from './auth/AuthContext.tsx';
import { PreferenciasProvider } from './preferencias/PreferenciasContext.tsx';
import './index.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <PreferenciasProvider>
        <AuthProvider>
          <App />
        </AuthProvider>
      </PreferenciasProvider>
    </BrowserRouter>
  </StrictMode>,
);

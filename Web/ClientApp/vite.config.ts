import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import fs from 'fs'
import path from 'path'

// Try to load HTTPS certificates if they exist
const getHttpsConfig = () => {
  const certPath = process.env.SSL_CRT_FILE || path.resolve(__dirname, 'aspnetcore-https.pem');
  const keyPath = process.env.SSL_KEY_FILE || path.resolve(__dirname, 'aspnetcore-https.key');
  
  if (fs.existsSync(certPath) && fs.existsSync(keyPath)) {
    return {
      key: fs.readFileSync(keyPath),
      cert: fs.readFileSync(certPath),
    };
  }
  
  return undefined;
};

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  base: process.env.VITE_PUBLIC_URL || '/',
  server: {
    port: 44463,
    https: getHttpsConfig(),
    strictPort: true,
  },
  build: {
    outDir: 'build',
  },
})


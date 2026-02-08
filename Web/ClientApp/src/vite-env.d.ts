/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_DEFAULT_APISOURCE: string
  readonly VITE_PUBLIC_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

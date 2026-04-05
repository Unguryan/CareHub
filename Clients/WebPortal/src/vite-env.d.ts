/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GATEWAY_URL: string
  readonly VITE_OIDC_CLIENT_ID: string
  readonly VITE_OIDC_CLIENT_SECRET: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

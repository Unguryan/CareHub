# CareHub clients (Phase 12)

## Web Portal (`WebPortal/`)

- **Dev server:** `npm install` then `npm run dev` (Vite default [http://localhost:5173](http://localhost:5173)).
- **API entry:** set `VITE_GATEWAY_URL` in `.env.development` (default `http://localhost:53615` — HTTP URL from `Gateway/CareHub.Gateway/Properties/launchSettings.json`).
- **Auth:** OpenIddict token endpoint via gateway: `POST /connect/token` (client `carehub-web` seeded in Identity).

## Desktop (`Desktop/`)

- **Run:** `dotnet run --project Desktop/CareHub.Desktop/CareHub.Desktop.csproj`
- **Gateway:** optional env `CAREHUB_GATEWAY_URL` (default `http://localhost:53615`).
- **Packaging:** `dotnet publish -c Release -r win-x64 --self-contained false` (framework-dependent) or add `-p:SelfContained=true` for a self-contained build.

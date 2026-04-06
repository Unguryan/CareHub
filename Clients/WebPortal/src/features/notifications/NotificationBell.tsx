import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../../auth/useAuth'

type Toast = { id: number; title: string; body: string }

export function NotificationBell() {
  const { accessToken, gatewayUrl } = useAuth()
  const [toasts, setToasts] = useState<Toast[]>([])
  const [open, setOpen] = useState(false)
  const nextId = useRef(1)

  useEffect(() => {
    if (!accessToken) return

    const getLiveAccessToken = () =>
      (window as unknown as { __carehub_access?: string }).__carehub_access ??
      accessToken

    const hub = `${gatewayUrl}/hubs/notifications`
    const connection = new HubConnectionBuilder()
      .withUrl(hub, {
        accessTokenFactory: () => getLiveAccessToken(),
        skipNegotiation: false,
        transport: undefined,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on(
      'ReceiveNotification',
      (dto: { title: string; body: string }) => {
        const id = nextId.current++
        setToasts((t) =>
          [{ id, title: dto.title, body: dto.body }, ...t].slice(0, 8),
        )
      },
    )

    void (async () => {
      try {
        await connection.start()
        if (connection.state === HubConnectionState.Connected) await connection.invoke('Join')
      } catch (err) {
        // Keep this visible in dev: negotiate/auth issues are otherwise hard to diagnose.
        console.warn('Notification hub connection failed', err)
      }
    })()

    return () => {
      void connection.stop()
    }
  }, [accessToken, gatewayUrl])

  return (
    <div className="relative">
      <button
        type="button"
        className="relative rounded-md border border-slate-300 px-3 py-1.5 text-sm hover:bg-slate-50"
        aria-expanded={open}
        aria-haspopup="true"
        onClick={() => setOpen((o) => !o)}
      >
        Alerts
        {toasts.length > 0 ? (
          <span className="absolute -right-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-teal-700 px-1 text-[10px] text-white">
            {toasts.length > 9 ? '9+' : toasts.length}
          </span>
        ) : null}
      </button>
      {open ? (
        <div
          className="absolute right-0 z-20 mt-2 w-80 rounded-lg border border-slate-200 bg-white py-2 shadow-lg"
          role="menu"
        >
          <div className="border-b border-slate-100 px-3 pb-2 text-xs font-medium text-slate-500">
            Recent notifications
          </div>
          {toasts.length === 0 ? (
            <p className="px-3 py-4 text-sm text-slate-500">No items yet.</p>
          ) : (
            <ul className="max-h-72 overflow-auto">
              {toasts.map((t) => (
                <li key={t.id} className="border-b border-slate-50 px-3 py-2 text-sm last:border-0">
                  <div className="font-medium text-slate-800">{t.title}</div>
                  <div className="text-slate-600">{t.body}</div>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  )
}

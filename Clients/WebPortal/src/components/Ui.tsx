export function Loading({ label = 'Loading…' }: { label?: string }) {
  return (
    <p className="text-sm text-slate-500" role="status">
      {label}
    </p>
  )
}

export function ErrorAlert({ message }: { message: string }) {
  return (
    <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-800" role="alert">
      {message}
    </div>
  )
}

export function EmptyState({ message }: { message: string }) {
  return <p className="text-sm text-slate-500">{message}</p>
}

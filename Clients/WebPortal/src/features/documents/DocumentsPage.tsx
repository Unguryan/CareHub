import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { apiFetch, apiJson } from '../../api/client'
import { ErrorAlert, Loading, EmptyState } from '../../components/Ui'

type ListResponse = {
  page: number
  pageSize: number
  total: number
  items: {
    id: string
    kind: string
    fileName: string
    entityType: string
    entityId: string
    createdAt: string
  }[]
}

export function DocumentsPage() {
  const [entityType, setEntityType] = useState('Appointment')
  const [entityId, setEntityId] = useState('')

  const query = useQuery({
    queryKey: ['documents', entityType, entityId],
    enabled: !!entityId,
    queryFn: () =>
      apiJson<ListResponse>(
        `/api/documents?entityType=${encodeURIComponent(entityType)}&entityId=${encodeURIComponent(entityId)}`,
      ),
  })

  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold text-slate-900">Documents</h2>
      <p className="mb-4 text-sm text-slate-600">
        List PDFs for an entity you are allowed to access. Download opens in a new tab using your
        current session.
      </p>
      <div className="mb-4 flex flex-wrap gap-4">
        <div>
          <label className="mb-1 block text-sm font-medium" htmlFor="doc-etype">
            Entity type
          </label>
          <input
            id="doc-etype"
            className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            value={entityType}
            onChange={(e) => setEntityType(e.target.value)}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium" htmlFor="doc-eid">
            Entity ID
          </label>
          <input
            id="doc-eid"
            className="rounded-md border border-slate-300 px-3 py-2 text-sm font-mono"
            value={entityId}
            onChange={(e) => setEntityId(e.target.value)}
            placeholder="GUID"
          />
        </div>
      </div>
      {!entityId ? <EmptyState message="Enter an entity ID to search." /> : null}
      {query.isPending && entityId ? <Loading /> : null}
      {query.isError ? <ErrorAlert message={(query.error as Error).message} /> : null}
      {query.data && query.data.items.length === 0 ? (
        <EmptyState message="No documents for this entity." />
      ) : null}
      {query.data && query.data.items.length > 0 ? (
        <ul className="divide-y divide-slate-100 rounded-lg border border-slate-200 bg-white">
          {query.data.items.map((d) => (
            <li key={d.id} className="flex items-center justify-between px-4 py-3 text-sm">
              <div>
                <div className="font-medium">{d.fileName}</div>
                <div className="text-xs text-slate-500">
                  {d.kind} · {new Date(d.createdAt).toLocaleString()}
                </div>
              </div>
              <button
                type="button"
                className="text-teal-800 underline"
                onClick={async () => {
                  const res = await apiFetch(`/api/documents/${d.id}`)
                  if (!res.ok) return
                  const blob = await res.blob()
                  const url = URL.createObjectURL(blob)
                  const a = document.createElement('a')
                  a.href = url
                  a.download = d.fileName
                  a.click()
                  URL.revokeObjectURL(url)
                }}
              >
                Download
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  )
}

import { useQuery } from '@tanstack/react-query'
import { apiJson } from '../../api/client'
import { ErrorAlert, Loading, EmptyState } from '../../components/Ui'

type Row = {
  id: string
  recordedAt: string
  actionType: string
  actorUserId: string | null
  entityType: string | null
  entityId: string | null
  branchId: string | null
}

type ListResponse = {
  items: Row[]
  hasMore: boolean
}

export function AuditPage() {
  const query = useQuery({
    queryKey: ['audit-logs'],
    queryFn: () => apiJson<ListResponse>('/api/audit-logs?pageSize=50'),
  })

  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold text-slate-900">Audit log</h2>
      {query.isPending ? <Loading /> : null}
      {query.isError ? <ErrorAlert message={(query.error as Error).message} /> : null}
      {query.data && query.data.items.length === 0 ? (
        <EmptyState message="No audit entries." />
      ) : null}
      {query.data && query.data.items.length > 0 ? (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="px-3 py-2 font-medium">Time</th>
                <th className="px-3 py-2 font-medium">Action</th>
                <th className="px-3 py-2 font-medium">Actor</th>
                <th className="px-3 py-2 font-medium">Entity</th>
              </tr>
            </thead>
            <tbody>
              {query.data.items.map((r) => (
                <tr key={r.id} className="border-t border-slate-100">
                  <td className="px-3 py-2 font-mono text-xs">
                    {new Date(r.recordedAt).toLocaleString()}
                  </td>
                  <td className="px-3 py-2">{r.actionType}</td>
                  <td className="px-3 py-2 font-mono text-xs">
                    {r.actorUserId?.slice(0, 8) ?? '—'}…
                  </td>
                  <td className="px-3 py-2">
                    {r.entityType ?? '—'} {r.entityId?.slice(0, 8) ?? ''}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}

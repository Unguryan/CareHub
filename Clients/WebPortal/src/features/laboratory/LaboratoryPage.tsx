import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { apiFetch, apiJson } from '../../api/client'
import { useRole } from '../../auth/AuthContext'
import { ErrorAlert, Loading, EmptyState } from '../../components/Ui'

type LabOrder = {
  id: string
  appointmentId: string
  patientId: string
  doctorId: string
  branchId: string
  status: number
  resultSummary: string | null
}

export function LaboratoryPage() {
  const qc = useQueryClient()
  const canMutate = useRole(['Admin', 'LabTechnician', 'Manager'])
  const [resultFor, setResultFor] = useState<string | null>(null)
  const [summary, setSummary] = useState('')

  const listQuery = useQuery({
    queryKey: ['lab-orders'],
    queryFn: () => apiJson<LabOrder[]>('/api/lab-orders'),
  })

  const receive = useMutation({
    mutationFn: (id: string) =>
      apiFetch(`/api/lab-orders/${id}/receive-sample`, { method: 'POST' }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['lab-orders'] }),
  })

  const result = useMutation({
    mutationFn: ({ id, summary }: { id: string; summary: string }) =>
      apiFetch(`/api/lab-orders/${id}/result`, {
        method: 'POST',
        body: JSON.stringify({ summary }),
      }),
    onSuccess: () => {
      setResultFor(null)
      setSummary('')
      void qc.invalidateQueries({ queryKey: ['lab-orders'] })
    },
  })

  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold text-slate-900">Laboratory orders</h2>
      {listQuery.isPending ? <Loading /> : null}
      {listQuery.isError ? (
        <ErrorAlert message={(listQuery.error as Error).message} />
      ) : null}
      {listQuery.data && listQuery.data.length === 0 ? (
        <EmptyState message="No lab orders." />
      ) : null}
      {listQuery.data && listQuery.data.length > 0 ? (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="px-3 py-2 font-medium">Status</th>
                <th className="px-3 py-2 font-medium">Patient</th>
                <th className="px-3 py-2 font-medium">Result</th>
                <th className="px-3 py-2 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {listQuery.data.map((o) => (
                <tr key={o.id} className="border-t border-slate-100">
                  <td className="px-3 py-2">{o.status}</td>
                  <td className="px-3 py-2 font-mono text-xs">{o.patientId.slice(0, 8)}…</td>
                  <td className="px-3 py-2">{o.resultSummary ?? '—'}</td>
                  <td className="px-3 py-2">
                    {canMutate && o.status === 0 ? (
                      <button
                        type="button"
                        className="mr-2 text-teal-800 underline"
                        onClick={() => receive.mutate(o.id)}
                      >
                        Receive sample
                      </button>
                    ) : null}
                    {canMutate && o.status === 1 ? (
                      <button
                        type="button"
                        className="text-teal-800 underline"
                        onClick={() => setResultFor(o.id)}
                      >
                        Enter result
                      </button>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
      {resultFor ? (
        <div className="fixed inset-0 z-30 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-sm rounded-lg bg-white p-6 shadow-xl">
            <h3 className="mb-3 font-semibold">Lab result summary</h3>
            <textarea
              className="mb-4 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              rows={4}
              value={summary}
              onChange={(e) => setSummary(e.target.value)}
            />
            <div className="flex justify-end gap-2">
              <button
                type="button"
                className="rounded-md border border-slate-300 px-3 py-2 text-sm"
                onClick={() => setResultFor(null)}
              >
                Cancel
              </button>
              <button
                type="button"
                className="rounded-md bg-teal-700 px-3 py-2 text-sm text-white"
                onClick={() => result.mutate({ id: resultFor, summary })}
              >
                Save
              </button>
            </div>
          </div>
        </div>
      ) : null}
      {receive.isError ? <ErrorAlert message={(receive.error as Error).message} /> : null}
      {result.isError ? <ErrorAlert message={(result.error as Error).message} /> : null}
    </div>
  )
}

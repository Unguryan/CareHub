import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { apiFetch, apiJson } from '../../api/client'
import { useRole } from '../../auth/AuthContext'
import { ErrorAlert, Loading, EmptyState } from '../../components/Ui'

type Invoice = {
  id: string
  appointmentId: string
  patientId: string
  branchId: string
  amount: number
  currency: string
  status: number
  createdAt: string
}

export function BillingPage() {
  const qc = useQueryClient()
  const canPay = useRole(['Admin', 'Accountant'])
  const [refundFor, setRefundFor] = useState<string | null>(null)
  const [reason, setReason] = useState('')

  const listQuery = useQuery({
    queryKey: ['invoices'],
    queryFn: () => apiJson<Invoice[]>('/api/invoices'),
  })

  const pay = useMutation({
    mutationFn: (id: string) => apiFetch(`/api/invoices/${id}/pay`, { method: 'POST' }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['invoices'] }),
  })

  const refund = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      apiFetch(`/api/invoices/${id}/refund`, {
        method: 'POST',
        body: JSON.stringify({ reason }),
      }),
    onSuccess: () => {
      setRefundFor(null)
      setReason('')
      void qc.invalidateQueries({ queryKey: ['invoices'] })
    },
  })

  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold text-slate-900">Invoices</h2>
      {listQuery.isPending ? <Loading /> : null}
      {listQuery.isError ? (
        <ErrorAlert message={(listQuery.error as Error).message} />
      ) : null}
      {listQuery.data && listQuery.data.length === 0 ? (
        <EmptyState message="No invoices." />
      ) : null}
      {listQuery.data && listQuery.data.length > 0 ? (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="px-3 py-2 font-medium">Amount</th>
                <th className="px-3 py-2 font-medium">Status</th>
                <th className="px-3 py-2 font-medium">Patient</th>
                <th className="px-3 py-2 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {listQuery.data.map((inv) => (
                <tr key={inv.id} className="border-t border-slate-100">
                  <td className="px-3 py-2">
                    {inv.amount} {inv.currency}
                  </td>
                  <td className="px-3 py-2">{inv.status}</td>
                  <td className="px-3 py-2 font-mono text-xs">{inv.patientId.slice(0, 8)}…</td>
                  <td className="px-3 py-2">
                    {canPay && inv.status === 0 ? (
                      <button
                        type="button"
                        className="mr-2 text-teal-800 underline"
                        onClick={() => pay.mutate(inv.id)}
                      >
                        Pay
                      </button>
                    ) : null}
                    {canPay && inv.status === 1 ? (
                      <button
                        type="button"
                        className="text-red-700 underline"
                        onClick={() => setRefundFor(inv.id)}
                      >
                        Refund
                      </button>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
      {refundFor ? (
        <div
          className="fixed inset-0 z-30 flex items-center justify-center bg-black/40 p-4"
          role="dialog"
        >
          <div className="w-full max-w-sm rounded-lg bg-white p-6 shadow-xl">
            <h3 className="mb-3 font-semibold">Refund invoice</h3>
            <label className="mb-1 block text-sm" htmlFor="refund-reason">
              Reason
            </label>
            <textarea
              id="refund-reason"
              className="mb-4 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
            <div className="flex justify-end gap-2">
              <button
                type="button"
                className="rounded-md border border-slate-300 px-3 py-2 text-sm"
                onClick={() => setRefundFor(null)}
              >
                Cancel
              </button>
              <button
                type="button"
                className="rounded-md bg-red-700 px-3 py-2 text-sm text-white"
                onClick={() => refund.mutate({ id: refundFor, reason })}
              >
                Submit refund
              </button>
            </div>
          </div>
        </div>
      ) : null}
      {pay.isError ? <ErrorAlert message={(pay.error as Error).message} /> : null}
      {refund.isError ? <ErrorAlert message={(refund.error as Error).message} /> : null}
    </div>
  )
}

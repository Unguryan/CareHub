import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { apiJson } from '../../api/client'
import { useRole } from '../../auth/AuthContext'
import { ErrorAlert, Loading } from '../../components/Ui'

type Tab = 'visits' | 'revenue' | 'workload' | 'cancellations'

export function ReportsPage() {
  const canGlobal = useRole(['Admin', 'Auditor'])
  const [tab, setTab] = useState<Tab>('visits')
  const [global, setGlobal] = useState(false)

  const range = useMemo(() => {
    const to = new Date()
    const from = new Date()
    from.setUTCDate(from.getUTCDate() - 30)
    return {
      from: from.toISOString(),
      to: to.toISOString(),
    }
  }, [])

  const qs = new URLSearchParams({
    from: range.from,
    to: range.to,
    maxRows: '100',
  })
  if (canGlobal && global) qs.set('global', 'true')

  const visits = useQuery({
    queryKey: ['reports', 'visits', range.from, range.to, global],
    enabled: tab === 'visits',
    queryFn: () => apiJson<unknown>(`/api/reports/visits?${qs}`),
  })

  const revenue = useQuery({
    queryKey: ['reports', 'revenue', range.from, range.to, global],
    enabled: tab === 'revenue',
    queryFn: () => apiJson<unknown>(`/api/reports/revenue?${qs}`),
  })

  const workload = useQuery({
    queryKey: ['reports', 'workload', range.from, range.to, global],
    enabled: tab === 'workload',
    queryFn: () => apiJson<unknown>(`/api/reports/workload?${qs}`),
  })

  const cancellations = useQuery({
    queryKey: ['reports', 'cancellations', range.from, range.to, global],
    enabled: tab === 'cancellations',
    queryFn: () => apiJson<unknown>(`/api/reports/cancellations?${qs}`),
  })

  const active =
    tab === 'visits'
      ? visits
      : tab === 'revenue'
        ? revenue
        : tab === 'workload'
          ? workload
          : cancellations

  return (
    <div>
      <h2 className="mb-2 text-xl font-semibold text-slate-900">Reports</h2>
      <p className="mb-4 text-sm text-slate-600">
        Last 30 days (UTC range). Branch scoping follows your role; admins and auditors may enable
        global view when permitted by the API.
      </p>
      {canGlobal ? (
        <label className="mb-4 flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={global}
            onChange={(e) => setGlobal(e.target.checked)}
          />
          Global (all branches)
        </label>
      ) : null}
      <div className="mb-4 flex flex-wrap gap-2 border-b border-slate-200">
        {(
          [
            ['visits', 'Visits'],
            ['revenue', 'Revenue'],
            ['workload', 'Workload'],
            ['cancellations', 'Cancellations'],
          ] as const
        ).map(([k, label]) => (
          <button
            key={k}
            type="button"
            className={`border-b-2 px-3 py-2 text-sm font-medium ${
              tab === k
                ? 'border-teal-700 text-teal-900'
                : 'border-transparent text-slate-600'
            }`}
            onClick={() => setTab(k as Tab)}
          >
            {label}
          </button>
        ))}
      </div>
      {active.isPending ? <Loading /> : null}
      {active.isError ? <ErrorAlert message={(active.error as Error).message} /> : null}
      {active.data ? (
        <pre className="max-h-[32rem] overflow-auto rounded-lg bg-slate-900 p-4 text-xs text-slate-100">
          {JSON.stringify(active.data, null, 2)}
        </pre>
      ) : null}
    </div>
  )
}

import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { apiJson } from '../../api/client'
import { ErrorAlert, Loading, EmptyState } from '../../components/Ui'
import { useRole } from '../../auth/useAuth'
import type { Patient } from './types'

export function PatientsPage() {
  const [q, setQ] = useState('')
  const canEdit = useRole(['Admin', 'Receptionist'])
  const query = useQuery({
    queryKey: ['patients', q],
    queryFn: () => {
      const params = new URLSearchParams()
      if (q.trim()) params.set('q', q.trim())
      return apiJson<Patient[]>(`/api/patients?${params}`)
    },
  })

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-end justify-between gap-4">
        <h2 className="text-xl font-semibold text-slate-900">Patients</h2>
        {canEdit ? (
          <Link
            to="/patients/new"
            className="rounded-md bg-teal-700 px-3 py-2 text-sm font-medium text-white hover:bg-teal-800"
          >
            New patient
          </Link>
        ) : null}
      </div>
      <div className="mb-4">
        <label htmlFor="patient-q" className="sr-only">
          Search patients
        </label>
        <input
          id="patient-q"
          className="w-full max-w-md rounded-md border border-slate-300 px-3 py-2 text-sm"
          placeholder="Search by name or phone…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
      </div>
      {query.isPending ? <Loading /> : null}
      {query.isError ? (
        <ErrorAlert message={(query.error as Error).message || 'Failed to load patients'} />
      ) : null}
      {query.data && query.data.length === 0 ? <EmptyState message="No patients found." /> : null}
      {query.data && query.data.length > 0 ? (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="px-4 py-2 font-medium">Name</th>
                <th className="px-4 py-2 font-medium">Phone</th>
                <th className="px-4 py-2 font-medium">Branch</th>
                <th className="px-4 py-2 font-medium" />
              </tr>
            </thead>
            <tbody>
              {query.data.map((p) => (
                <tr key={p.id} className="border-t border-slate-100">
                  <td className="px-4 py-2">
                    {p.firstName} {p.lastName}
                  </td>
                  <td className="px-4 py-2">{p.phoneNumber}</td>
                  <td className="px-4 py-2 font-mono text-xs">{p.branchId.slice(0, 8)}…</td>
                  <td className="px-4 py-2 text-right">
                    <Link className="text-teal-800 underline" to={`/patients/${p.id}`}>
                      Open
                    </Link>
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

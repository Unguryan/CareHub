import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { apiFetch, apiJson } from '../../api/client'
import { useRole } from '../../auth/useAuth'
import { ErrorAlert, Loading, EmptyState } from '../../components/Ui'

const statusLabel: Record<number, string> = {
  0: 'Scheduled',
  1: 'Checked in',
  2: 'Completed',
  3: 'Cancelled',
}

type Appointment = {
  id: string
  patientId: string
  doctorId: string
  branchId: string
  scheduledAt: string
  durationMinutes: number
  status: number
  requiresLabWork: boolean
}

export function AppointmentsPage() {
  const qc = useQueryClient()
  const canBook = useRole(['Admin', 'Receptionist', 'Manager'])
  const canCheckIn = useRole(['Admin', 'Receptionist'])
  const canComplete = useRole(['Admin', 'Doctor'])
  const [showCreate, setShowCreate] = useState(false)

  const listQuery = useQuery({
    queryKey: ['appointments'],
    queryFn: () => apiJson<Appointment[]>('/api/appointments'),
  })

  const checkIn = useMutation({
    mutationFn: (id: string) => apiFetch(`/api/appointments/${id}/checkin`, { method: 'POST' }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['appointments'] }),
  })

  const complete = useMutation({
    mutationFn: (id: string) =>
      apiFetch(`/api/appointments/${id}/complete`, {
        method: 'POST',
        body: JSON.stringify({ requiresLabWork: false }),
      }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['appointments'] }),
  })

  const cancel = useMutation({
    mutationFn: (id: string) =>
      apiFetch(`/api/appointments/${id}/cancel`, {
        method: 'POST',
        body: JSON.stringify({ reason: 'Cancelled from portal' }),
      }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['appointments'] }),
  })

  return (
    <div>
      <div className="mb-4 flex items-center justify-between gap-4">
        <h2 className="text-xl font-semibold text-slate-900">Appointments</h2>
        {canBook ? (
          <button
            type="button"
            className="rounded-md bg-teal-700 px-3 py-2 text-sm font-medium text-white hover:bg-teal-800"
            onClick={() => setShowCreate(true)}
          >
            New appointment
          </button>
        ) : null}
      </div>
      {showCreate ? (
        <CreateAppointmentForm
          onClose={() => setShowCreate(false)}
          onCreated={() => {
            setShowCreate(false)
            void qc.invalidateQueries({ queryKey: ['appointments'] })
          }}
        />
      ) : null}
      {listQuery.isPending ? <Loading /> : null}
      {listQuery.isError ? (
        <ErrorAlert message={(listQuery.error as Error).message} />
      ) : null}
      {listQuery.data && listQuery.data.length === 0 ? (
        <EmptyState message="No appointments." />
      ) : null}
      {listQuery.data && listQuery.data.length > 0 ? (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="px-3 py-2 font-medium">When</th>
                <th className="px-3 py-2 font-medium">Patient</th>
                <th className="px-3 py-2 font-medium">Doctor</th>
                <th className="px-3 py-2 font-medium">Status</th>
                <th className="px-3 py-2 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {listQuery.data.map((a) => (
                <tr key={a.id} className="border-t border-slate-100">
                  <td className="px-3 py-2 font-mono text-xs">
                    {new Date(a.scheduledAt).toLocaleString()}
                  </td>
                  <td className="px-3 py-2 font-mono text-xs">{a.patientId.slice(0, 8)}…</td>
                  <td className="px-3 py-2 font-mono text-xs">{a.doctorId.slice(0, 8)}…</td>
                  <td className="px-3 py-2">{statusLabel[a.status] ?? a.status}</td>
                  <td className="px-3 py-2">
                    <div className="flex flex-wrap gap-2">
                      {canCheckIn && a.status === 0 ? (
                        <button
                          type="button"
                          className="text-xs text-teal-800 underline"
                          onClick={() => checkIn.mutate(a.id)}
                        >
                          Check in
                        </button>
                      ) : null}
                      {canComplete && a.status === 1 ? (
                        <button
                          type="button"
                          className="text-xs text-teal-800 underline"
                          onClick={() => complete.mutate(a.id)}
                        >
                          Complete
                        </button>
                      ) : null}
                      {canBook && a.status !== 3 && a.status !== 2 ? (
                        <button
                          type="button"
                          className="text-xs text-red-700 underline"
                          onClick={() => cancel.mutate(a.id)}
                        >
                          Cancel
                        </button>
                      ) : null}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
      {checkIn.isError ? (
        <ErrorAlert message={(checkIn.error as Error).message} />
      ) : null}
      {complete.isError ? (
        <ErrorAlert message={(complete.error as Error).message} />
      ) : null}
      {cancel.isError ? <ErrorAlert message={(cancel.error as Error).message} /> : null}
    </div>
  )
}

function CreateAppointmentForm({
  onClose,
  onCreated,
}: {
  onClose: () => void
  onCreated: () => void
}) {
  const [patientId, setPatientId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [branchId, setBranchId] = useState('00000000-0000-0000-0000-000000000001')
  const [scheduledAt, setScheduledAt] = useState('')
  const [duration, setDuration] = useState(30)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      const res = await apiFetch('/api/appointments', {
        method: 'POST',
        body: JSON.stringify({
          patientId,
          doctorId,
          branchId,
          scheduledAt: new Date(scheduledAt).toISOString(),
          durationMinutes: duration,
        }),
      })
      if (res.status === 403) {
        setError('You do not have permission to book appointments.')
        return
      }
      if (res.status === 409) {
        setError('Conflict: slot or overlap issue.')
        return
      }
      if (!res.ok) throw new Error(await res.text())
      onCreated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div
      className="fixed inset-0 z-30 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
    >
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <h3 className="mb-4 text-lg font-semibold">New appointment</h3>
        <form onSubmit={onSubmit} className="flex flex-col gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ap-pt">
              Patient ID (GUID)
            </label>
            <input
              id="ap-pt"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm font-mono"
              value={patientId}
              onChange={(e) => setPatientId(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ap-dr">
              Doctor ID (GUID)
            </label>
            <input
              id="ap-dr"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm font-mono"
              value={doctorId}
              onChange={(e) => setDoctorId(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ap-br">
              Branch ID
            </label>
            <input
              id="ap-br"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm font-mono"
              value={branchId}
              onChange={(e) => setBranchId(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ap-when">
              Start (local)
            </label>
            <input
              id="ap-when"
              type="datetime-local"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              value={scheduledAt}
              onChange={(e) => setScheduledAt(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ap-dur">
              Duration (minutes)
            </label>
            <input
              id="ap-dur"
              type="number"
              min={15}
              step={5}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              value={duration}
              onChange={(e) => setDuration(Number(e.target.value))}
            />
          </div>
          {error ? <ErrorAlert message={error} /> : null}
          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
              onClick={onClose}
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={busy}
              className="rounded-md bg-teal-700 px-3 py-2 text-sm font-medium text-white disabled:opacity-60"
            >
              Create
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

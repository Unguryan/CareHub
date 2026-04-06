import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
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

type PatientOption = {
  id: string
  firstName: string
  lastName: string
  phoneNumber: string
  branchId: string
}

type DoctorOption = {
  id: string
  firstName: string
  lastName: string
  specialty: string
  branchId: string
  isActive: boolean
}

export function AppointmentsPage() {
  const qc = useQueryClient()
  const [searchParams] = useSearchParams()
  const canBook = useRole(['Admin', 'Receptionist', 'Manager'])
  const canCheckIn = useRole(['Admin', 'Receptionist'])
  const canComplete = useRole(['Admin', 'Doctor'])
  const [showCreate, setShowCreate] = useState(false)
  const presetPatientId = searchParams.get('patientId') ?? ''

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
          presetPatientId={presetPatientId}
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
  presetPatientId,
  onClose,
  onCreated,
}: {
  presetPatientId?: string
  onClose: () => void
  onCreated: () => void
}) {
  const patientsQuery = useQuery({
    queryKey: ['appointment-patients'],
    queryFn: () => apiJson<PatientOption[]>('/api/patients'),
  })
  const doctorsQuery = useQuery({
    queryKey: ['appointment-doctors'],
    queryFn: () => apiJson<DoctorOption[]>('/api/doctors'),
  })

  const patientOptions = useMemo(() => patientsQuery.data ?? [], [patientsQuery.data])
  const doctorOptions = (doctorsQuery.data ?? []).filter((d) => d.isActive)
  const specialties = useMemo(
    () => [...new Set(doctorOptions.map((d) => d.specialty).filter(Boolean))].sort(),
    [doctorOptions],
  )

  const [patientId, setPatientId] = useState(presetPatientId ?? '')
  const [doctorId, setDoctorId] = useState('')
  const [patientQuery, setPatientQuery] = useState('')
  const [doctorQuery, setDoctorQuery] = useState('')
  const [specialty, setSpecialty] = useState('')
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [scheduledAt, setScheduledAt] = useState('')
  const [duration, setDuration] = useState(30)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [selectedSlot, setSelectedSlot] = useState<string>('')

  const selectedPatient = patientOptions.find((p) => p.id === patientId)
  const selectedDoctor = doctorOptions.find((d) => d.id === doctorId)
  const branchId =
    selectedDoctor?.branchId ??
    selectedPatient?.branchId ??
    '00000000-0000-0000-0000-000000000001'

  const filteredPatients = useMemo(() => {
    const q = patientQuery.trim().toLowerCase()
    if (!q) return patientOptions
    return patientOptions.filter((p) =>
      `${p.firstName} ${p.lastName} ${p.phoneNumber}`.toLowerCase().includes(q),
    )
  }, [patientOptions, patientQuery])

  const filteredDoctors = useMemo(() => {
    const q = doctorQuery.trim().toLowerCase()
    return doctorOptions.filter((d) => {
      if (specialty && d.specialty !== specialty) return false
      if (!q) return true
      return `${d.firstName} ${d.lastName} ${d.specialty}`.toLowerCase().includes(q)
    })
  }, [doctorOptions, doctorQuery, specialty])

  const slotsQuery = useQuery({
    queryKey: ['appointment-slots', doctorId, date],
    enabled: !!doctorId && !!date,
    queryFn: () =>
      apiJson<Array<{ slotTime: string }>>(
        `/api/doctors/${doctorId}/slots?date=${encodeURIComponent(date)}`,
      ),
  })

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      const localWhen = selectedSlot || scheduledAt
      const normalizedLocal = localWhen.length === 16 ? `${localWhen}:00` : localWhen
      const res = await apiFetch('/api/appointments', {
        method: 'POST',
        body: JSON.stringify({
          patientId,
          doctorId,
          branchId,
          // Backend treats this as clinic-local wall-clock time.
          scheduledAt: normalizedLocal,
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

  function onPatientChange(nextPatientId: string) {
    setPatientId(nextPatientId)
  }

  function onDoctorChange(nextDoctorId: string) {
    setDoctorId(nextDoctorId)
    setSelectedSlot('')
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
          {!presetPatientId ? (
            <div>
              <label className="mb-1 block text-sm font-medium" htmlFor="ap-pt-search">
                Patient
              </label>
              <input
                id="ap-pt-search"
                className="mb-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                placeholder="Search patient by name or phone..."
                value={patientQuery}
                onChange={(e) => setPatientQuery(e.target.value)}
              />
              <select
                id="ap-pt"
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                value={patientId}
                onChange={(e) => onPatientChange(e.target.value)}
                required
              >
                <option value="">Select patient...</option>
                {filteredPatients.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.firstName} {p.lastName} - {p.phoneNumber}
                  </option>
                ))}
              </select>
            </div>
          ) : (
            <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm">
              Patient: {selectedPatient ? `${selectedPatient.firstName} ${selectedPatient.lastName}` : presetPatientId}
            </div>
          )}
          <div>
            <label className="mb-1 block text-sm font-medium">Category</label>
            <select
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              value={specialty}
              onChange={(e) => setSpecialty(e.target.value)}
            >
              <option value="">All specialties</option>
              {specialties.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ap-dr-search">
              Doctor
            </label>
            <input
              id="ap-dr-search"
              className="mb-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              placeholder="Search doctor by name or specialty..."
              value={doctorQuery}
              onChange={(e) => setDoctorQuery(e.target.value)}
            />
            <select
              id="ap-dr"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              value={doctorId}
              onChange={(e) => onDoctorChange(e.target.value)}
              required
            >
              <option value="">Select doctor...</option>
              {filteredDoctors.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.firstName} {d.lastName} - {d.specialty}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ap-date">
              Date
            </label>
            <input
              id="ap-date"
              type="date"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              value={date}
              onChange={(e) => {
                setDate(e.target.value)
                setSelectedSlot('')
              }}
              required
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">
              Available times for selected doctor
            </label>
            {slotsQuery.isPending ? <Loading label="Loading slots..." /> : null}
            {slotsQuery.data && slotsQuery.data.length > 0 ? (
              <div className="flex max-h-32 flex-wrap gap-2 overflow-auto rounded-md border border-slate-200 p-2">
                {slotsQuery.data.map((s) => {
                  const time = s.slotTime.slice(0, 5)
                  const value = `${date}T${time}`
                  const active = selectedSlot === value
                  return (
                    <button
                      key={s.slotTime}
                      type="button"
                      className={`rounded-md border px-2 py-1 text-xs ${active ? 'border-teal-700 bg-teal-700 text-white' : 'border-slate-300'}`}
                      onClick={() => {
                        setSelectedSlot(value)
                        setScheduledAt(value)
                      }}
                    >
                      {time}
                    </button>
                  )
                })}
              </div>
            ) : (
              <p className="text-xs text-slate-500">No slots for this doctor/date.</p>
            )}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium" htmlFor="ap-when">
              Start (manual override)
            </label>
            <input
              id="ap-when"
              type="datetime-local"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              value={scheduledAt}
              onChange={(e) => {
                setScheduledAt(e.target.value)
                setSelectedSlot('')
              }}
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
          {patientsQuery.isError ? (
            <ErrorAlert message={(patientsQuery.error as Error).message} />
          ) : null}
          {doctorsQuery.isError ? (
            <ErrorAlert message={(doctorsQuery.error as Error).message} />
          ) : null}
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

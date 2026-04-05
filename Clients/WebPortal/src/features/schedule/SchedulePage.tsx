import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { apiJson } from '../../api/client'
import { ErrorAlert, Loading, EmptyState } from '../../components/Ui'

type Doctor = {
  id: string
  firstName: string
  lastName: string
  specialty: string
  branchId: string
  isActive: boolean
}

type Slot = { slotTime: string }

export function SchedulePage() {
  const [doctorId, setDoctorId] = useState<string>('')
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))

  const doctorsQuery = useQuery({
    queryKey: ['doctors'],
    queryFn: () => apiJson<Doctor[]>('/api/doctors'),
  })

  const doctors = useMemo(() => doctorsQuery.data ?? [], [doctorsQuery.data])
  const resolvedDoctorId =
    doctorId !== '' ? doctorId : doctors.length > 0 ? doctors[0].id : ''

  const slotsQuery = useQuery({
    queryKey: ['slots', resolvedDoctorId, date],
    enabled: !!resolvedDoctorId && !!date,
    queryFn: () =>
      apiJson<Slot[]>(
        `/api/doctors/${resolvedDoctorId}/slots?date=${encodeURIComponent(date)}`,
      ),
  })

  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold text-slate-900">Schedule</h2>
      {doctorsQuery.isPending ? <Loading /> : null}
      {doctorsQuery.isError ? (
        <ErrorAlert message={(doctorsQuery.error as Error).message} />
      ) : null}
      <div className="mb-4 flex flex-wrap gap-4">
        <div>
          <label htmlFor="doctor" className="mb-1 block text-sm font-medium text-slate-700">
            Doctor
          </label>
          <select
            id="doctor"
            className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            value={resolvedDoctorId}
            onChange={(e) => setDoctorId(e.target.value)}
          >
            <option value="">Select…</option>
            {doctors.map((d) => (
              <option key={d.id} value={d.id}>
                {d.firstName} {d.lastName} — {d.specialty}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="slot-date" className="mb-1 block text-sm font-medium text-slate-700">
            Date
          </label>
          <input
            id="slot-date"
            type="date"
            className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
        </div>
      </div>
      {slotsQuery.isPending ? <Loading label="Loading slots…" /> : null}
      {slotsQuery.isError ? (
        <ErrorAlert message={(slotsQuery.error as Error).message} />
      ) : null}
      {slotsQuery.data && slotsQuery.data.length === 0 ? (
        <EmptyState message="No available slots for this day." />
      ) : null}
      {slotsQuery.data && slotsQuery.data.length > 0 ? (
        <ul className="flex flex-wrap gap-2">
          {slotsQuery.data.map((s) => (
            <li
              key={s.slotTime}
              className="rounded-md border border-teal-200 bg-teal-50 px-3 py-1.5 font-mono text-sm text-teal-900"
            >
              {s.slotTime}
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  )
}

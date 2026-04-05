import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { apiFetch, apiJson } from '../../api/client'
import { useRole } from '../../auth/AuthContext'
import { ErrorAlert, Loading, EmptyState } from '../../components/Ui'
import type { Patient } from './types'

export function PatientDetailPage() {
  const { id } = useParams()
  const qc = useQueryClient()
  const canEdit = useRole(['Admin', 'Receptionist'])
  const [tab, setTab] = useState<'profile' | 'history'>('profile')

  const patientQuery = useQuery({
    queryKey: ['patient', id],
    enabled: !!id && id !== 'new',
    queryFn: () => apiJson<Patient>(`/api/patients/${id}`),
  })

  const historyQuery = useQuery({
    queryKey: ['patient-history', id],
    enabled: !!id && id !== 'new' && tab === 'history',
    queryFn: () => apiJson<unknown[]>(`/api/patients/${id}/history`),
  })

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    phoneNumber: '',
    email: '',
    dateOfBirth: '',
  })

  useEffect(() => {
    if (patientQuery.data) {
      const p = patientQuery.data
      setForm({
        firstName: p.firstName,
        lastName: p.lastName,
        phoneNumber: p.phoneNumber,
        email: p.email ?? '',
        dateOfBirth: p.dateOfBirth,
      })
    }
  }, [patientQuery.data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      const res = await apiFetch(`/api/patients/${id}`, {
        method: 'PUT',
        body: JSON.stringify({
          firstName: form.firstName,
          lastName: form.lastName,
          phoneNumber: form.phoneNumber,
          email: form.email || null,
          dateOfBirth: form.dateOfBirth,
        }),
      })
      if (!res.ok) throw new Error(await res.text())
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['patient', id] })
      void qc.invalidateQueries({ queryKey: ['patients'] })
    },
  })

  if (id === 'new') {
    return <PatientCreatePage />
  }

  if (patientQuery.isPending) return <Loading />
  if (patientQuery.isError) {
    return <ErrorAlert message={(patientQuery.error as Error).message} />
  }
  if (!patientQuery.data) return <EmptyState message="Patient not found." />

  const p = patientQuery.data

  return (
    <div>
      <div className="mb-4">
        <Link to="/patients" className="text-sm text-teal-800 underline">
          ← Patients
        </Link>
      </div>
      <h2 className="mb-4 text-xl font-semibold text-slate-900">
        {p.firstName} {p.lastName}
      </h2>
      <div className="mb-4 flex gap-2 border-b border-slate-200">
        <button
          type="button"
          className={`border-b-2 px-3 py-2 text-sm font-medium ${
            tab === 'profile'
              ? 'border-teal-700 text-teal-900'
              : 'border-transparent text-slate-600'
          }`}
          onClick={() => setTab('profile')}
        >
          Profile
        </button>
        <button
          type="button"
          className={`border-b-2 px-3 py-2 text-sm font-medium ${
            tab === 'history'
              ? 'border-teal-700 text-teal-900'
              : 'border-transparent text-slate-600'
          }`}
          onClick={() => setTab('history')}
        >
          History
        </button>
      </div>

      {tab === 'profile' ? (
        <div className="max-w-lg rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
          {canEdit ? (
            <form
              onSubmit={(e: FormEvent) => {
                e.preventDefault()
                saveMutation.mutate()
              }}
              className="flex flex-col gap-3"
            >
              <Field
                label="First name"
                value={form.firstName}
                onChange={(v) => setForm((f) => ({ ...f, firstName: v }))}
              />
              <Field
                label="Last name"
                value={form.lastName}
                onChange={(v) => setForm((f) => ({ ...f, lastName: v }))}
              />
              <Field
                label="Phone"
                value={form.phoneNumber}
                onChange={(v) => setForm((f) => ({ ...f, phoneNumber: v }))}
              />
              <Field
                label="Email"
                value={form.email}
                onChange={(v) => setForm((f) => ({ ...f, email: v }))}
              />
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700" htmlFor="dob">
                  Date of birth
                </label>
                <input
                  id="dob"
                  type="date"
                  className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                  value={form.dateOfBirth}
                  onChange={(e) => setForm((f) => ({ ...f, dateOfBirth: e.target.value }))}
                  required
                />
              </div>
              {saveMutation.isError ? (
                <ErrorAlert message={(saveMutation.error as Error).message} />
              ) : null}
              <button
                type="submit"
                disabled={saveMutation.isPending}
                className="rounded-md bg-teal-700 px-3 py-2 text-sm font-medium text-white hover:bg-teal-800 disabled:opacity-60"
              >
                Save
              </button>
            </form>
          ) : (
            <dl className="grid grid-cols-1 gap-2 text-sm">
              <div>
                <dt className="text-slate-500">Phone</dt>
                <dd>{p.phoneNumber}</dd>
              </div>
              <div>
                <dt className="text-slate-500">Email</dt>
                <dd>{p.email ?? '—'}</dd>
              </div>
              <div>
                <dt className="text-slate-500">Date of birth</dt>
                <dd>{p.dateOfBirth}</dd>
              </div>
            </dl>
          )}
        </div>
      ) : (
        <div>
          {historyQuery.isPending ? <Loading /> : null}
          {historyQuery.isError ? (
            <ErrorAlert message={(historyQuery.error as Error).message} />
          ) : null}
          {historyQuery.data ? (
            <pre className="overflow-auto rounded-lg bg-slate-900 p-4 text-xs text-slate-100">
              {JSON.stringify(historyQuery.data, null, 2)}
            </pre>
          ) : null}
        </div>
      )}
    </div>
  )
}

function Field({
  label,
  value,
  onChange,
}: {
  label: string
  value: string
  onChange: (v: string) => void
}) {
  const id = label.replace(/\s/g, '-').toLowerCase()
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-slate-700" htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required
      />
    </div>
  )
}

function PatientCreatePage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    phoneNumber: '',
    email: '',
    dateOfBirth: '',
  })
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      const res = await apiFetch('/api/patients', {
        method: 'POST',
        body: JSON.stringify({
          firstName: form.firstName,
          lastName: form.lastName,
          phoneNumber: form.phoneNumber,
          email: form.email || null,
          dateOfBirth: form.dateOfBirth,
        }),
      })
      if (res.status === 409) {
        setError('Phone number already in use.')
        return
      }
      if (!res.ok) throw new Error(await res.text())
      const created = (await res.json()) as Patient
      void qc.invalidateQueries({ queryKey: ['patients'] })
      void navigate(`/patients/${created.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div>
      <div className="mb-4">
        <Link to="/patients" className="text-sm text-teal-800 underline">
          ← Patients
        </Link>
      </div>
      <h2 className="mb-4 text-xl font-semibold text-slate-900">New patient</h2>
      <form
        onSubmit={onSubmit}
        className="max-w-lg flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm"
      >
        <Field
          label="First name"
          value={form.firstName}
          onChange={(v) => setForm((f) => ({ ...f, firstName: v }))}
        />
        <Field
          label="Last name"
          value={form.lastName}
          onChange={(v) => setForm((f) => ({ ...f, lastName: v }))}
        />
        <Field
          label="Phone"
          value={form.phoneNumber}
          onChange={(v) => setForm((f) => ({ ...f, phoneNumber: v }))}
        />
        <Field
          label="Email"
          value={form.email}
          onChange={(v) => setForm((f) => ({ ...f, email: v }))}
        />
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700" htmlFor="dob-new">
            Date of birth
          </label>
          <input
            id="dob-new"
            type="date"
            className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            value={form.dateOfBirth}
            onChange={(e) => setForm((f) => ({ ...f, dateOfBirth: e.target.value }))}
            required
          />
        </div>
        {error ? <ErrorAlert message={error} /> : null}
        <button
          type="submit"
          disabled={busy}
          className="rounded-md bg-teal-700 px-3 py-2 text-sm font-medium text-white hover:bg-teal-800 disabled:opacity-60"
        >
          Create
        </button>
      </form>
    </div>
  )
}

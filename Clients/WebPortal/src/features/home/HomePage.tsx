import { Link } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'

export function HomePage() {
  const { roles } = useAuth()
  return (
    <div>
      <h2 className="mb-2 text-xl font-semibold text-slate-900">Welcome</h2>
      <p className="mb-4 text-slate-600">
        You are signed in with roles:{' '}
        <span className="font-medium text-slate-800">
          {roles.length ? roles.join(', ') : '—'}
        </span>
      </p>
      <p className="text-sm text-slate-600">
        Use the sidebar to open modules. All data access is still enforced by the API; the UI only
        hides navigation you are unlikely to need.
      </p>
      <ul className="mt-6 list-inside list-disc text-sm text-teal-800">
        <li>
          <Link className="underline hover:text-teal-950" to="/patients">
            Patients
          </Link>
        </li>
        <li>
          <Link className="underline hover:text-teal-950" to="/appointments">
            Appointments
          </Link>
        </li>
        <li>
          <Link className="underline hover:text-teal-950" to="/reports">
            Reports
          </Link>
        </li>
      </ul>
    </div>
  )
}

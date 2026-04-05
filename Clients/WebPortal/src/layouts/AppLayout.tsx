import { NavLink, Outlet } from 'react-router-dom'
import { useAuth, useRole } from '../auth/useAuth'
import { NotificationBell } from '../features/notifications/NotificationBell'

type NavItem = { to: string; label: string; roles: string[] }

const navItems: NavItem[] = [
  { to: '/', label: 'Home', roles: ['__any__'] },
  {
    to: '/patients',
    label: 'Patients',
    roles: [
      'Admin',
      'Doctor',
      'Manager',
      'Receptionist',
      'Auditor',
      'CallCenter',
      'LabTechnician',
      'Accountant',
    ],
  },
  {
    to: '/schedule',
    label: 'Schedule',
    roles: [
      'Admin',
      'Doctor',
      'Manager',
      'Receptionist',
      'Auditor',
      'CallCenter',
      'LabTechnician',
      'Accountant',
    ],
  },
  {
    to: '/appointments',
    label: 'Appointments',
    roles: ['Admin', 'Doctor', 'Manager', 'Receptionist', 'Auditor'],
  },
  {
    to: '/billing',
    label: 'Billing',
    roles: ['Admin', 'Accountant', 'Manager', 'Auditor'],
  },
  {
    to: '/laboratory',
    label: 'Laboratory',
    roles: ['Admin', 'Doctor', 'Manager', 'Auditor', 'LabTechnician'],
  },
  { to: '/audit', label: 'Audit', roles: ['Admin', 'Auditor'] },
  {
    to: '/documents',
    label: 'Documents',
    roles: [
      'Admin',
      'Manager',
      'Accountant',
      'Doctor',
      'LabTechnician',
      'Receptionist',
    ],
  },
  { to: '/reports', label: 'Reports', roles: ['Admin', 'Manager', 'Auditor'] },
]

function canSee(item: NavItem, roles: string[]) {
  if (item.roles.includes('__any__')) return true
  return item.roles.some((r) => roles.includes(r))
}

export function AppLayout() {
  const { logout, roles, accessToken } = useAuth()
  const isAdmin = useRole(['Admin'])

  return (
    <div className="flex min-h-screen">
      <aside className="w-56 shrink-0 border-r border-slate-200 bg-white p-4 shadow-sm">
        <div className="mb-6 font-semibold text-teal-800">CareHub</div>
        <nav className="flex flex-col gap-1" aria-label="Main">
          {navItems
            .filter((item) => canSee(item, roles))
            .map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  [
                    'rounded-md px-3 py-2 text-sm font-medium',
                    isActive
                      ? 'bg-teal-700 text-white'
                      : 'text-slate-700 hover:bg-slate-100',
                  ].join(' ')
                }
              >
                {item.label}
              </NavLink>
            ))}
        </nav>
        {isAdmin && (
          <p className="mt-6 text-xs text-slate-500">
            Admin tools for users and branches are not exposed in this MVP UI;
            use internal APIs or a future release.
          </p>
        )}
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
          <h1 className="text-lg font-semibold text-slate-800">Web Portal</h1>
          <div className="flex items-center gap-3">
            {accessToken ? <NotificationBell /> : null}
            <button
              type="button"
              className="rounded-md border border-slate-300 px-3 py-1.5 text-sm hover:bg-slate-50"
              onClick={() => logout()}
            >
              Log out
            </button>
          </div>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

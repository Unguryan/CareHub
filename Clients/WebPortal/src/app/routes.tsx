import type { ReactNode } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth, useRole } from '../auth/AuthContext'
import { AppLayout } from '../layouts/AppLayout'
import { AppointmentsPage } from '../features/appointments/AppointmentsPage'
import { AuditPage } from '../features/audit/AuditPage'
import { BillingPage } from '../features/billing/BillingPage'
import { DocumentsPage } from '../features/documents/DocumentsPage'
import { HomePage } from '../features/home/HomePage'
import { LaboratoryPage } from '../features/laboratory/LaboratoryPage'
import { LoginPage } from '../features/auth/LoginPage'
import { PatientDetailPage } from '../features/patients/PatientDetailPage'
import { PatientsPage } from '../features/patients/PatientsPage'
import { ReportsPage } from '../features/reports/ReportsPage'
import { SchedulePage } from '../features/schedule/SchedulePage'

function ProtectedRoute({
  children,
  roles,
}: {
  children: ReactNode
  roles: string[]
}) {
  const { isAuthenticated } = useAuth()
  const ok = useRole(roles)
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!ok) return <Navigate to="/" replace />
  return <>{children}</>
}

function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  return <>{children}</>
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <AppLayout />
          </RequireAuth>
        }
      >
        <Route index element={<HomePage />} />
        <Route
          path="patients"
          element={
            <ProtectedRoute
              roles={[
                'Admin',
                'Doctor',
                'Manager',
                'Receptionist',
                'Auditor',
                'CallCenter',
                'LabTechnician',
                'Accountant',
              ]}
            >
              <PatientsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="patients/:id"
          element={
            <ProtectedRoute
              roles={[
                'Admin',
                'Doctor',
                'Manager',
                'Receptionist',
                'Auditor',
                'CallCenter',
                'LabTechnician',
                'Accountant',
              ]}
            >
              <PatientDetailPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="schedule"
          element={
            <ProtectedRoute
              roles={[
                'Admin',
                'Doctor',
                'Manager',
                'Receptionist',
                'Auditor',
                'CallCenter',
                'LabTechnician',
                'Accountant',
              ]}
            >
              <SchedulePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="appointments"
          element={
            <ProtectedRoute
              roles={[
                'Admin',
                'Doctor',
                'Manager',
                'Receptionist',
                'Auditor',
              ]}
            >
              <AppointmentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="billing"
          element={
            <ProtectedRoute roles={['Admin', 'Accountant', 'Manager', 'Auditor']}>
              <BillingPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="laboratory"
          element={
            <ProtectedRoute
              roles={[
                'Admin',
                'Doctor',
                'Manager',
                'Auditor',
                'LabTechnician',
              ]}
            >
              <LaboratoryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="audit"
          element={
            <ProtectedRoute roles={['Admin', 'Auditor']}>
              <AuditPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="documents"
          element={
            <ProtectedRoute
              roles={[
                'Admin',
                'Manager',
                'Accountant',
                'Doctor',
                'LabTechnician',
                'Receptionist',
              ]}
            >
              <DocumentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="reports"
          element={
            <ProtectedRoute roles={['Admin', 'Manager', 'Auditor']}>
              <ReportsPage />
            </ProtectedRoute>
          }
        />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

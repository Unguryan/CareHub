# CareHub

## Overview

**CareHub** is an internal enterprise platform designed to manage the operational workflows of clinic networks, medical centers, and private hospitals.

It is not a public patient-facing website. Instead, it is an internal system for clinic staff, including:
- reception teams
- doctors
- laboratory staff
- accountants
- administrators
- branch managers
- auditors

The main purpose of CareHub is to centralize daily clinic operations in one system and provide a structured, secure, and scalable environment for working with patients, appointments, billing, laboratory workflows, notifications, documents, and audit logs.

---

## Main Idea

CareHub is designed as an enterprise-grade solution for organizations that operate multiple clinics or medical branches.

Such organizations usually have:
- multiple locations
- many employees with different responsibilities
- a constant flow of patients
- strict requirements for security and access control
- a need for both web and desktop applications
- complex operational workflows that should be separated into business domains

The platform should help staff efficiently manage patients, appointments, invoices, and documents, while managers and administrators can monitor clinic activity through a centralized web portal.

---

## Client Applications

### Web Portal

The web application is intended for:
- administrators
- doctors
- clinic managers
- accountants
- auditors

Main capabilities:
- view patient profiles
- manage appointments
- review doctor schedules
- view invoices and payment statuses
- manage users, roles, and permissions
- access audit logs
- review reports and analytics

### Desktop Client

The desktop application is intended for:
- reception staff
- front-desk teams
- call-center operators
- laboratory teams

Main capabilities:
- quick patient search and registration
- appointment booking
- patient check-in
- printing tickets, referrals, and invoices
- working with local devices such as printers and scanners
- supporting fast daily operational workflows

The desktop application is especially valuable for heavy operational use, where speed, quick actions, printing, and local device integration are important.

---

## Architecture

CareHub is built as a **microservices-based architecture** with centralized authentication and authorization using **IdentityServer / OpenID Connect / OAuth2**.

Each service is responsible for its own business domain.

---

## Core Services

### Identity Service

Responsible for authentication and authorization.

Main responsibilities:
- user login
- access token and refresh token management
- roles
- claims
- permissions
- access policies
- secure authentication for both web and desktop clients

This service acts as the central security layer of the platform.

---

### Patient Service

Responsible for patient master data.

Main responsibilities:
- storing patient personal information
- contact details
- patient identifier
- registration history
- branch-related patient information

This service acts as the main source of truth for patient records.

---

### Appointment Service

Responsible for appointment workflows.

Main responsibilities:
- creating appointments
- rescheduling
- cancellation
- patient check-in
- visit completion
- appointment status lifecycle management

This service handles the business process of patient visits from booking to completion.

---

### Staff / Schedule Service

Responsible for doctors, schedules, and availability.

Main responsibilities:
- doctor profiles
- work shifts
- available time slots
- room assignments
- specialties
- branch assignments
- doctor availability validation

This service provides scheduling data required by the Appointment Service.

---

### Billing Service

Responsible for financial operations.

Main responsibilities:
- invoice creation
- payment tracking
- invoice statuses
- refunds
- handling billing events after completed visits

This service manages the financial side of clinic operations.

---

### Laboratory Service

Responsible for laboratory workflows.

Main responsibilities:
- lab order creation
- sample receiving
- status tracking
- result entry
- completed analysis processing

This service allows the platform to support diagnostic and laboratory processes.

---

### Notification Service

Responsible for sending notifications based on business events.

Examples:
- appointment created
- appointment cancelled
- invoice generated
- lab result ready

This service is event-driven and reacts to changes happening in other services.

---

### Audit Service

Responsible for tracking critical user activity.

Main responsibilities:
- login/logout tracking
- patient creation logs
- appointment modification logs
- invoice payment actions
- important business data change history

This service is important for enterprise-level traceability, security, and compliance.

---

### Document Service

Responsible for storing and generating documents.

Main responsibilities:
- referrals
- invoices
- lab result files
- attachments
- printable templates
- generated PDF files

This service centralizes file and document handling across the platform.

---

### Reporting Service

Responsible for analytics and reporting.

Main responsibilities:
- visit statistics
- doctor workload
- revenue reports
- cancellation reports
- branch-level reporting

This service provides management visibility into operational and financial performance.

---

## How the System Works

A typical workflow in CareHub looks like this:

1. A staff member signs in through the **Identity Service**.
2. After successful authentication, the client application receives an access token and a set of permissions.
3. The receptionist searches for a patient in the **Patient Service**.
4. If the patient does not exist, a new patient profile is created.
5. An appointment is created through the **Appointment Service**.
6. The **Appointment Service** checks doctor availability through the **Schedule Service**.
7. Once the appointment is successfully created, a business event is published.
8. The **Notification Service** sends the required notification.
9. The **Audit Service** records the user action.
10. After the visit is completed, the **Billing Service** creates an invoice.
11. When payment is completed, the invoice status is updated and the result is reflected in billing, audit, and reporting.

This flow demonstrates how separate business domains work together through APIs and events.

---

## Why This Project Is Strong

CareHub is a strong enterprise project idea because it combines several important architectural and technical aspects in one solution:
- web and desktop clients in the same ecosystem
- microservices architecture
- centralized authentication and authorization
- role-based and permission-based access control
- event-driven communication between services
- realistic business workflows
- audit and traceability
- scalability for multi-branch organizations

It is not just a CRUD application. It represents a realistic business platform with multiple user roles, multiple operational flows, and clear service boundaries.

---

## Business Value

CareHub helps organizations:
- centralize clinic operations
- improve patient flow handling
- speed up reception and front-desk work
- improve billing and payment visibility
- strengthen security and access control
- keep track of critical user actions
- reduce operational fragmentation between departments
- build a single internal platform for medical operational management
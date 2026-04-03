-- Creates all service databases on the shared PostgreSQL instance.
-- carehub_identity is created by the POSTGRES_DB env var in docker-compose.yml.
CREATE DATABASE carehub_patient;
CREATE DATABASE carehub_appointment;
CREATE DATABASE carehub_schedule;
CREATE DATABASE carehub_billing;
CREATE DATABASE carehub_laboratory;
CREATE DATABASE carehub_notification;
CREATE DATABASE carehub_audit;
CREATE DATABASE carehub_document;
CREATE DATABASE carehub_reporting;

# ADR 0003: PostgreSQL is the system-of-record database

Status: Accepted

## Decision
Use PostgreSQL for projects, immutable AI analysis records, approvals, audit events, and future integration metadata.

## Rationale
The product requires relational workflow integrity while also storing structured AI JSON. PostgreSQL provides both, and preserves a straightforward path to Supabase-hosted PostgreSQL and pgvector.

## Constraints
- Domain state remains owned by the Core API.
- External agents do not write directly to product tables.
- Tenant/workspace scoping will be mandatory before multi-user deployment.
- Schema migrations replace EnsureCreated before production deployment.

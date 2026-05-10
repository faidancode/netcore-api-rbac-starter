# Implementation Plan

This document tracks the current implementation status against `docs/checklist.md` and breaks the remaining work into phased delivery.

## Current Status

### Implemented

- The API contract is consistent through `Response<T>` with `Success`, `Message`, `Code`, `Data`, `Meta`, and `Errors`.
- Versioning is in place through `api/v{version:apiVersion}` routes and `AddApiVersioning`.
- Authentication uses JWT access tokens plus opaque refresh tokens stored in the database.
- Authorization uses RBAC with permission-based policies.
- Request IDs are generated in middleware and pushed into log context.
- Idempotency keys are handled through middleware and Redis-backed cache/lock behavior.
- Request body and query validation use FluentValidation.
- Basic path-parameter validation exists through route constraints and explicit `Guid.Empty` checks.
- Pagination, filtering, sorting, and pagination metadata are available on list endpoints.
- Error handling is centralized through `ExceptionMiddleware` and `AppException`.
- Logging uses Serilog with `RequestId` and `UserId` in context.
- Database transactions are used in complex write flows, especially in `RolesService` and `EmployeesService`.
- Configuration is environment-driven through `.env` and environment variables, with startup validation for critical values.
- `/health` and `/ready` endpoints are available, including database and Redis checks.
- Swagger/OpenAPI and a Postman collection are available.
- Unit and integration tests exist for auth, roles, users, employees, departments, positions, and dashboard.
- Secure refresh-token cookie support is in place, while still keeping the body-based flow for compatibility.
- Rate limiting is enforced per IP globally, per authenticated user on protected routes, and per-login for sensitive auth requests.
- A global request timeout is configured through middleware and can be tuned via `RequestTimeoutSeconds`.
- Documentation now includes request/response examples, auth instructions, and a basic error-code reference.

### Partially Implemented

- Request IDs are logged, but they are not yet propagated through external async/event systems because event processing is still in-process.
- Audit logging exists, but it is currently concentrated on employee-related events instead of every critical business action.
- Transaction boundaries are solid in some services, but not yet standardized across every write operation.
- Deployment readiness is helped by Docker and Compose files, but port/env handling can still be tightened further.

### Not Implemented

- Async/Kafka-style event processing with event IDs, broker headers, consumer idempotency, and replay safety.
- Audit logs for all critical business actions across every module.
- A complete endpoint-by-endpoint request/response example catalog and exhaustive error-code reference.

## Phased Plan

### Phase 1: Contract, Validation, and Documentation

- Keep the API response envelope consistent across all controllers.
- Standardize validation rules for request bodies, query parameters, path parameters, and boundary values.
- Audit list endpoints so pagination, filtering, sorting, and metadata behave consistently.
- Document request/response shapes and error-code behavior more explicitly.
- Goal: the API is easy to consume, and invalid input is rejected consistently before business logic runs.

### Phase 2: Security and Abuse Protection

- Add rate-limiting policies for IP-based and authenticated-user-based traffic.
- Revisit the refresh-token flow and decide whether browser-safe cookie storage is required.
- Add a global HTTP timeout policy and make cancellation propagation consistent end to end.
- Harden sensitive endpoints such as login, refresh, and expensive write operations.
- Goal: reduce abuse surface and make sensitive flows more resilient.

### Phase 3: Observability, Audit, and Data Consistency

- Expand audit logging to other critical business actions beyond employees.
- Make transaction boundaries explicit wherever partial writes would be risky.
- Standardize before/after state payloads for audit records.
- Decide whether request context should be carried more formally into internal event payloads.
- Goal: increase traceability and reduce the risk of inconsistent writes.

### Phase 4: Async/Event and Deployment Readiness

- Decide whether in-process events are sufficient or whether a broker/message bus is needed.
- If a broker is introduced, add event IDs, request-id propagation, consumer idempotency, and safe replay handling.
- Tighten port and environment handling for local, Docker, and production deployments.
- Add clearer startup/readiness logging if needed.
- Goal: the system is ready for a heavier deployment model without major rework.

## Priority Notes

- Phase 1 and Phase 2 are the highest priorities because they affect API usability and security first.
- Phase 3 is important for production maturity, especially if audit trails become operationally significant.
- Phase 4 can wait unless there is a real need for broker-driven event processing.

This checklist is used as a guide for building a REST API that is
**secure, consistent, easy to debug, and production-ready**
without overengineering.

---

## 1. API Design & Contract

- [x] Consistent response format (success & error)
- [x] Clear separation between HTTP status & business error code
- [ ] Backward compatible changes (additive, no breaking rename)
- [x] Versioning strategy (`/v1`, header, etc)

---

## 2. Authentication & Authorization

- [x] Authentication (JWT / session / token based)
- [x] Role-based authorization (RBAC)
- [x] Token expiration handling
- [x] Refresh token mechanism
- [x] Secure cookie / header usage (HttpOnly, Secure)

---

## 3. Request Tracing & Safety

- [x] Request-ID generated at edge (middleware)
- [x] Request-ID propagated through context, logs, and async events
- [x] Idempotency-Key for sensitive POST operations
- [x] Idempotent handling on duplicate requests

---

## 4. Input Validation

- [x] Validation for request body
- [x] Validation for query parameters
- [x] Validation for path parameters
- [x] Enum and boundary validation (status, qty, limit, etc)
- [x] Reject invalid input early (before service logic)

---

## 5. Pagination, Filtering, Sorting

- [x] Pagination (`page` / `limit` or cursor-based)
- [x] Filtering by common fields
- [x] Sorting (`sort_by`, `order`)
- [x] Pagination metadata in response

---

## 6. Error Handling

- [x] Centralized error mapping
- [x] No internal error leakage to client
- [x] Meaningful business error codes
- [x] Consistent error response structure

---

## 7. Logging & Observability

- [x] Structured logging (JSON / key-value)
- [x] Log levels (INFO, WARN, ERROR)
- [x] Logs include request_id and user_id
- [x] Errors logged with sufficient context

---

## 8. Transaction & Data Consistency

- [x] Database transaction for write operations
- [x] Clear transaction boundaries
- [x] Rollback on failure
- [x] No partial write on error

---

## 9. Async / Kafka / Event Processing

- [ ] Event contains unique event ID
- [ ] Request-ID propagated in message headers
- [ ] Idempotent consumer handling
- [ ] Safe retry and replay handling
- [ ] No duplicate business effect on re-consume

---

## 10. Rate Limiting & Abuse Protection

- [x] Rate limit per IP
- [x] Rate limit per authenticated user
- [x] Protection for sensitive endpoints (login, checkout)

---

## 11. Timeout & Context Propagation

- [x] HTTP request has timeout
- [x] Context propagated to service and repository
- [x] Database queries respect context
- [x] Async operations respect cancellation

---

## 12. Audit Log (Business Level)

- [x] Audit log for critical actions
- [x] Who performed the action
- [x] What action was performed
- [x] Timestamp recorded
- [x] Before / after state (optional)

---

## 13. Configuration & Secrets

- [x] Configuration via environment variables
- [x] Required config validated on startup
- [x] No hardcoded secrets
- [x] Fail fast on missing critical config

---

## 14. Healthcheck & Readiness

- [x] `/health` endpoint
- [x] `/ready` endpoint
- [x] Database connectivity check
- [x] Optional: cache / broker readiness

---

## 15. Documentation

- [x] API documentation (Swagger / OpenAPI)
- [x] Request and response examples
- [x] Error code list
- [x] Authentication instructions

---

## 16. Testing Strategy

- [x] Unit test for service layer
- [x] Handler/controller test (success & failure)
- [x] Authentication & authorization test
- [x] Idempotency test
- [ ] Async / consumer test (if applicable)

---

## 17. Deployment Readiness

- [x] Graceful shutdown
- [x] Proper port and env handling
- [x] Docker-friendly configuration
- [ ] Startup logs clearly indicate readiness

---

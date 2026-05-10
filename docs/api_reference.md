# API Reference

This document captures the common API contract, authentication flow, and error-code conventions used by the service.

## Base URL and Versioning

- Base path: `/api/v1`
- Versioning is embedded in the route, for example:
  - `/api/v1/auth/login`
  - `/api/v1/users`
  - `/api/v1/employees`

## Authentication

- Login with `POST /api/v1/auth/login`.
- Use the returned access token in the `Authorization` header:

```http
Authorization: Bearer <access_token>
```

- Refresh tokens are returned by login and must be sent to `POST /api/v1/auth/refresh`.
- Protected endpoints require a valid access token plus the relevant permission policy.

## Standard Response Shape

Success response:

```json
{
  "success": true,
  "message": "User created successfully.",
  "code": null,
  "data": {
    "id": "8c3d5d9a-6f4a-4b9a-a1d8-6f3d9c8a1234",
    "name": "Jane Doe"
  },
  "meta": null,
  "errors": null
}
```

Error response:

```json
{
  "success": false,
  "message": "User with id '...' was not found.",
  "code": "USER_NOT_FOUND",
  "data": null,
  "meta": null,
  "errors": null
}
```

Validation response:

```json
{
  "success": false,
  "message": "Validation failed",
  "code": null,
  "data": null,
  "meta": null,
  "errors": {
    "email": ["Email is required."]
  }
}
```

## Pagination Shape

List endpoints return `data` as the current page items and `meta` as pagination information:

```json
{
  "success": true,
  "message": null,
  "code": null,
  "data": [],
  "meta": {
    "page": 1,
    "limit": 10,
    "total": 42,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "errors": null
}
```

## Common Error Codes

- `UNAUTHORIZED`
- `FORBIDDEN`
- `CONFLICT`
- `NOT_FOUND`
- `INTERNAL_ERROR`
- Resource-specific variants such as:
  - `USER_NOT_FOUND`
  - `ROLE_NOT_FOUND`
  - `DEPARTMENT_NOT_FOUND`
  - `POSITION_NOT_FOUND`

## Example Request Patterns

Create user:

```http
POST /api/v1/users
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "name": "Jane Doe",
  "email": "jane@example.com",
  "password": "Password123!",
  "roleId": "..."
}
```

Paginated list:

```http
GET /api/v1/users?page=1&limit=10&sort=createdAt:desc
Authorization: Bearer <access_token>
```


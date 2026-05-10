# Phase 2 Implementation

Phase 2 focused on security hardening and abuse protection.

## What Was Added or Improved

- Global request timeout support was added through middleware.
- Rate limiting was expanded to cover:
  - per-IP traffic at the application level
  - per-authenticated-user traffic on protected routes
  - login requests as a sensitive endpoint
- Refresh tokens now set an `HttpOnly` and `Secure` cookie on login and refresh.
- The refresh endpoint can use the cookie when the request body does not provide a token.
- Protected controllers were annotated with per-user rate limiting metadata so the limiter can evaluate authenticated identity.
- The environment defaults now include `RequestTimeoutSeconds`.

## What Changed in Practice

- Clients still receive refresh tokens in the response body for backward compatibility.
- Browser clients can now rely on the cookie-based refresh flow instead of exposing the token to JavaScript.
- Requests that run too long are cut off with a `408 Request Timeout` response.
- The API now has a stronger first line of defense against burst traffic and repeated abuse.

## Tips and Tricks

- Keep the global IP limiter generous enough that it blocks abuse without punishing normal shared-network traffic.
- Use the per-user limiter for authenticated routes so a single account cannot monopolize the API.
- Keep login and refresh limits tighter than normal business endpoints because they are common abuse targets.
- Preserve backward compatibility when introducing secure cookies by keeping the body token path available during the transition.
- If you add another sensitive endpoint later, give it explicit rate limiting instead of relying only on the global limiter.
- For timeout-sensitive code, continue passing `CancellationToken` into repository and external calls so the timeout actually has an effect.

## Checklist Items Covered in Phase 2

- Secure cookie / header usage
- Rate limit per IP
- Rate limit per authenticated user
- Protection for sensitive endpoints
- HTTP request timeout

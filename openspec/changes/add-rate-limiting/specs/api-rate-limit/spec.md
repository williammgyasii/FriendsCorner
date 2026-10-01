## ADDED Requirements

### Requirement: Account posts are rate limited by IP

The API Worker MUST rate-limit `POST /account` before forwarding to the room container. The limit MUST be 10 requests per 60 seconds per client IP. When over limit, the Worker MUST respond with HTTP 429 and an empty body.

#### Scenario: Under the account limit

- **WHEN** a client sends fewer than 11 `POST /account` requests within 60 seconds from the same IP
- **THEN** each request is forwarded to the room container

#### Scenario: Over the account limit

- **WHEN** a client sends an 11th `POST /account` within 60 seconds from the same IP
- **THEN** the Worker responds with 429 and does not forward the request

### Requirement: Room creation is rate limited by IP

The API Worker MUST rate-limit `POST /rooms` at 20 requests per 60 seconds per client IP. Over limit MUST return 429 with an empty body.

#### Scenario: Over the room limit

- **WHEN** a client exceeds 20 `POST /rooms` within 60 seconds from the same IP
- **THEN** the Worker responds with 429

### Requirement: Billing posts are rate limited except webhooks

The API Worker MUST rate-limit `POST /billing` at 20 requests per 60 seconds per client IP. `POST /billing/webhook` MUST NOT be rate limited.

#### Scenario: Webhook is not throttled

- **WHEN** Stripe posts to `/billing/webhook`
- **THEN** the Worker forwards the request without applying the billing rate limit

### Requirement: TURN credentials are rate limited by IP

The API Worker MUST rate-limit `GET /turn` at 60 requests per 60 seconds per client IP. Over limit MUST return 429 with an empty body.

#### Scenario: Over the TURN limit

- **WHEN** a client exceeds 60 `GET /turn` requests within 60 seconds from the same IP
- **THEN** the Worker responds with 429

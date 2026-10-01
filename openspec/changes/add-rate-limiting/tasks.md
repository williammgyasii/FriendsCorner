## 1. Rate limit policy

- [x] 1.1 Write failing edge tests: `POST /account` returns 429 after 10 calls from one IP; `POST /billing/webhook` is never limited; `GET /account` is not limited. Run and show failure.
- [x] 1.2 Add `rateLimit.ts` with IP extraction and limit checks so those tests pass. Run edge tests.

## 2. Wire the API worker

- [x] 2.1 Write a failing edge test that the API fetch path calls rate limiting before the container. Run and show failure.
- [x] 2.2 Add `ratelimits` bindings to `edge/api/wrangler.jsonc` and call checks from `serve.ts`. Run edge tests.

## 3. Verify

- [x] 3.1 Run the full edge test suite.

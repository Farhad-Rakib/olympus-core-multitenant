# API Testing Instructions

This guide explains how to obtain an authentication token and use it to test other protected endpoints in the API. These steps assume you are using a tool like Postman, Insomnia, or `curl`.

## 1. Register a New User (if needed)

**Endpoint:** `POST /api/auth/register`

**Body Example:**
```json
{
  "fullName": "Test User",
  "email": "testuser@example.com",
  "password": "YourPassword123!",
  "roles": ["User"]
}
```

- If registration is successful, you will receive user details in the response.

## 2. Obtain an Access Token

**Endpoint:** `POST /api/auth/login`

**Body Example:**
```json
{
  "email": "testuser@example.com",
  "password": "YourPassword123!"
}
```

- The response will include an `accessToken` and a `refreshToken`.
- Copy the `accessToken` for use in subsequent requests.

## 3. Use the Access Token to Call Protected Endpoints

For any endpoint that requires authentication:
- Add an `Authorization` header to your request:

```
Authorization: Bearer <accessToken>
```

**Example with `curl`:**
```
curl -H "Authorization: Bearer <accessToken>" https://your-api-url/api/protected-endpoint
```

**Example with Postman:**
- Go to the "Authorization" tab.
- Select "Bearer Token" and paste your `accessToken`.

## 4. Refresh the Access Token (if expired)

**Endpoint:** `POST /api/auth/refresh-token`

**Body Example:**
```json
{
  "refreshToken": "<refreshToken>"
}
```

- The response will include a new `accessToken` and `refreshToken`.

## 5. Revoke a Refresh Token (Logout)

**Endpoint:** `POST /api/auth/revoke-refresh-token`

**Body Example:**
```json
{
  "refreshToken": "<refreshToken>"
}
```

- This will invalidate the refresh token.

## 6. Test Other Endpoints

- Repeat step 3 for any other protected endpoints, always including the `Authorization: Bearer <accessToken>` header.
- If you receive a 401 Unauthorized error, your token may be expired or invalid—refresh it as in step 4.

---

**Note:** Replace endpoint URLs and request bodies as needed for your API. Always use valid credentials and tokens.

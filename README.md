# CAS Login Back-End

Authentication and account-management API for CAS clients. The API issues two JWTs:

- **SSO token** — valid for 8 hours and used only to switch business entities.
- **System token** — valid for 1 hour and required by protected API endpoints.

The client selects a business entity by its numeric `businessEntityId`. The API resolves its name and redirect URL from `Tbl_BusinessEntity`; clients must not send a business-entity name.

## Run locally

### Prerequisites

- .NET SDK 10.0
- Access to the SQL Server database

Configure the connection and JWT settings in `appsettings.json` or user secrets:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SERVER;Database=DATABASE;Integrated Security=True;Encrypt=True;Trust Server Certificate=True"
  },
  "JWT": {
    "Key": "a-long-random-signing-key",
    "Issuer": "CAS.Api",
    "Audience": "CAS.Clients",
    "ExpirationMinutes": 60,
    "SsoExpirationHours": 8
  }
}
```

Run the project:

```powershell
dotnet run
```

Swagger UI is available at `/swagger` in the Development environment.

## Authentication

Protected endpoints require the system JWT:

```http
Authorization: Bearer {jwtToken}
```

An SSO token cannot access protected endpoints. It is only accepted by `POST /api/auth/switch`.

The system JWT contains the account, role, business-entity ID/name, and `RedirectUrl` claims. The redirect URL is data returned from the authorized `Tbl_BusinessEntity.URL` row; clients should use it only after successful authentication.

## Response envelope

All successful responses use this shape:

```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": {}
}
```

Errors are returned by the exception middleware:

```json
{
  "statusCode": 400,
  "success": false,
  "message": "Registration validation failed.",
  "errors": ["Email is required."]
}
```

Common status codes are `400` (validation), `401` (invalid token, credentials, or access), `404` (not found), and `500`.

## Endpoint summary

| Method | Endpoint | Authentication | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | None | Authenticate and obtain SSO/System tokens. |
| POST | `/api/auth/switch` | SSO token | Obtain a system token for another entity. |
| POST | `/api/auth/validate` | SSO or System token | Validate a token. |
| POST | `/api/account/register` | None | Create an account and login record. |
| GET | `/api/account/profile` | System token | Get the current profile. |
| PUT | `/api/account/profile` | System token | Update the current profile names. |
| POST | `/api/account/change-password` | System token | Change the current password. |
| POST | `/api/account/forgot-password` | None | Start the reset flow. |
| POST | `/api/account/reset-password` | None | Reset a password. |
| GET | `/api/businessentity` | System token | List business entities. |
| GET | `/api/businessentity/{id}` | System token | Get one business entity. |
| GET | `/api/businessentity/my-entities` | System token | List entities assigned to the caller. |
| GET | `/api/role` | System token | List roles. |
| GET | `/api/role/{id}` | System token | Get a role. |
| GET | `/api/role/account/{businessEntityId}` | System token | Get the caller's role for an entity. |

## Authentication endpoints

### `POST /api/auth/login`

Authenticates credentials and verifies the user has a role in the supplied business entity.

Request:

```json
{
  "email": "user@example.com",
  "password": "P@ssw0rd!",
  "businessEntityId": 1
}
```

Success response (`200`):

```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "ssoToken": "eyJ...",
    "jwtToken": "eyJ...",
    "accountId": 42,
    "email": "user@example.com",
    "fullNameEn": "Jane Doe",
    "fullNameAr": "جين دو",
    "role": "Administrator",
    "businessEntityId": 1,
    "businessEntityName": "CAS",
    "redirectUrl": "https://cas.example.com",
    "ssoExpiresAt": "2026-08-11T18:00:00Z",
    "jwtCreatedAt": "2026-08-11T10:00:00Z",
    "jwtExpiresAt": "2026-08-11T11:00:00Z"
  }
}
```

`redirectUrl` is an empty string when the selected entity has no URL configured.

### `POST /api/auth/switch`

Exchanges an SSO token for a new system token scoped to another entity to which the account is assigned.

Request headers:

```http
Authorization: Bearer {ssoToken}
```

Request:

```json
{
  "businessEntityId": 2
}
```

Success response (`200`):

```json
{
  "success": true,
  "message": "Token exchanged successfully.",
  "data": {
    "jwtToken": "eyJ...",
    "role": "Instructor",
    "businessEntityId": 2,
    "businessEntityName": "Learning Portal",
    "redirectUrl": "https://learning.example.com",
    "jwtCreatedAt": "2026-08-11T10:05:00Z",
    "jwtExpiresAt": "2026-08-11T11:05:00Z"
  }
}
```

### `POST /api/auth/validate`

Validates either an SSO or system JWT.

Request headers:

```http
Authorization: Bearer {token}
```

Success response (`200`):

```json
{
  "success": true,
  "message": "Token validation completed successfully.",
  "data": {
    "isValid": true,
    "isExpired": false,
    "tokenType": "System",
    "accountId": 42,
    "createdAt": "2026-08-11T10:00:00Z"
  }
}
```

For SSO tokens, `createdAt` is `null` because SSO tokens do not carry that claim.

## Account endpoints

### `POST /api/account/register`

Creates an active `Account_Info` profile and associated login credentials.

```json
{
  "nationalId": "29801011234567",
  "email": "user@example.com",
  "password": "P@ssw0rd!",
  "confirmPassword": "P@ssw0rd!",
  "fullNameEn": "Jane Doe",
  "fullNameAr": "جين دو"
}
```

Success response (`201`):

```json
{
  "success": true,
  "message": "Account registered successfully.",
  "data": {
    "accountId": 42,
    "email": "user@example.com",
    "nationalId": "29801011234567",
    "phone": null,
    "city": null,
    "fullNameEn": "Jane Doe",
    "fullNameAr": "جين دو",
    "isActive": true,
    "createdAt": "2026-08-11",
    "statusId": 1,
    "statusName": "Active",
    "governoratesId": null,
    "governorateNameEn": null,
    "governorateNameAr": null
  }
}
```

### `GET /api/account/profile`

Requires a system token. Returns the same profile object shown in the registration response.

### `PUT /api/account/profile`

Requires a system token. Only the caller's English and Arabic names are updated.

```json
{
  "fullNameEn": "Jane A. Doe",
  "fullNameAr": "جين أ. دو"
}
```

Success response (`200`) contains the updated profile object.

### `POST /api/account/change-password`

Requires a system token.

```json
{
  "currentPassword": "P@ssw0rd!",
  "newPassword": "N3wP@ssw0rd!",
  "confirmPassword": "N3wP@ssw0rd!"
}
```

Success response (`200`):

```json
{
  "success": true,
  "message": "Password changed successfully.",
  "data": null
}
```

### `POST /api/account/forgot-password`

```json
{
  "email": "user@example.com"
}
```

The endpoint always returns `200` with a generic message to avoid exposing whether an email exists. Email delivery is currently a placeholder; no reset token is issued yet.

### `POST /api/account/reset-password`

```json
{
  "email": "user@example.com",
  "resetToken": "token-from-reset-email",
  "newPassword": "N3wP@ssw0rd!",
  "confirmPassword": "N3wP@ssw0rd!"
}
```

The current implementation accepts the `resetToken` field but does not validate it yet. Do not expose this endpoint publicly until token generation, delivery, storage, and validation are implemented.

## Business entity endpoints

All business-entity endpoints require a system token.

### `GET /api/businessentity`

Lists `Tbl_BusinessEntity` rows.

### `GET /api/businessentity/{id}`

Gets an entity by `Tbl_BusinessEntity.ID`.

### `GET /api/businessentity/my-entities`

Lists only entities assigned to the authenticated account.

Entity response example:

```json
{
  "success": true,
  "message": "Business entities retrieved successfully.",
  "data": [
    {
      "id": 1,
      "name": "CAS",
      "description": "CAS",
      "redirectUrl": "https://cas.example.com",
      "isActive": true
    }
  ]
}
```

`isActive` is currently returned as `true` because `Tbl_BusinessEntity` does not expose an active-status column.

## Role endpoints

All role endpoints require a system token.

### `GET /api/role`

Lists all roles.

### `GET /api/role/{id}`

Gets a role by ID.

### `GET /api/role/account/{businessEntityId}`

Gets the authenticated account's role in the specified business entity.

Role response example:

```json
{
  "success": true,
  "message": "Role retrieved successfully.",
  "data": {
    "roleId": 3,
    "name": "Instructor",
    "description": "Learning Portal"
  }
}
```

## Business-entity resolution

`Tbl_BusinessEntity` provides the canonical entity data:

| Column | API use |
| --- | --- |
| `ID` | `businessEntityId` accepted by login, switch, and account-role endpoints. |
| `BusinessEntity` | Resolved name returned in responses and used to match existing `AccountRoles.BusinessEntityName`. |
| `URL` | Returned as `redirectUrl` and embedded in system JWTs. |
| `OrderNo` | Stored by the table but not currently returned or used by the API. |

This preserves compatibility with the existing `AccountRoles.BusinessEntityName` schema while ensuring client requests use the stable numeric entity ID.

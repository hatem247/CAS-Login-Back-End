# CAS Login Backend

<p align="center">
  <img src="https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core">
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server">
  <img src="https://img.shields.io/badge/JWT-000000?style=for-the-badge" alt="JWT">
  <img src="https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black" alt="Swagger">
</p>

<p align="center">
  A centralized authentication and authorization API for applications that need secure identity management, business-entity switching, and role-aware access control.
</p>

<p align="center">
  <a href="#features">Features</a> ·
  <a href="#architecture">Architecture</a> ·
  <a href="#getting-started">Getting Started</a> ·
  <a href="#api-overview">API Overview</a>
</p>

---

## Overview

**CAS Login Backend** is an ASP.NET Core Web API designed to provide centralized authentication and authorization for multiple connected applications.

The API supports two JWT-based security contexts:

* **SSO Token** — identifies the authenticated account and remains valid for 8 hours.
* **System Token** — provides application context for a selected business entity and remains valid for 1 hour.

Business entities are selected using their numeric `businessEntityId`. The backend resolves the corresponding entity information from the database and returns the authorized context to the client.

---

## Features

* 🔐 JWT authentication and authorization
* 🎫 SSO and system-token authentication contexts
* 🏢 Business-entity switching
* 👤 Account registration and profile management
* 🔑 Password change support with BCrypt hashing
* 🛡️ Role-aware access control
* ⚡ Login rate limiting
* 📚 Swagger / OpenAPI documentation
* 🗄️ SQL Server integration through Entity Framework Core
* 🚨 Centralized exception handling
* 🌐 Forwarded-header support for reverse proxies
* 📦 Consistent API response envelopes

---

## Authentication Flow

```text
Client
  │
  ▼
Login
  │
  ├── Validate credentials
  ├── Validate business entity
  ├── Resolve role
  └── Issue tokens
        │
        ├── SSO Token
        │
        └── System Token
                │
                ▼
        Protected API Requests
                │
                ▼
        Role / Entity Context
```

### Token Lifetimes

| Token        | Purpose                              | Lifetime |
| ------------ | ------------------------------------ | -------: |
| SSO Token    | Account identity and session context |  8 hours |
| System Token | Business-entity scoped access        |   1 hour |

Protected endpoints accept:

```http
Authorization: Bearer <token>
```

---

## Security

### Login Rate Limiting

The login endpoint is limited to:

```text
5 requests / 15 minutes / client IP
```

The sixth request returns:

```http
429 Too Many Requests
```

with a rate-limit response.

### Password Security

Passwords are handled using **BCrypt** hashing.

### Configuration Security

Sensitive configuration values should be supplied through:

* Environment variables
* .NET User Secrets
* Deployment-specific configuration

Never commit production connection strings or JWT signing keys.

---

## Architecture

The project follows a practical layered organization:

```text
CAS-Login-Back-End/
├── Controllers/
├── Data/
├── Exceptions/
├── Middleware/
├── Models/
├── Services/
├── Properties/
├── Program.cs
├── appsettings.json
└── README.md
```

### Main Responsibilities

| Area          | Responsibility                               |
| ------------- | -------------------------------------------- |
| `Controllers` | HTTP endpoints and request handling          |
| `Services`    | Business logic and authentication operations |
| `Data`        | Entity Framework Core database access        |
| `Models`      | Request, response, and domain models         |
| `Middleware`  | Cross-cutting HTTP processing                |
| `Exceptions`  | Centralized application exceptions           |
| `Properties`  | Development and launch configuration         |

---

## Technology Stack

| Technology            | Purpose                    |
| --------------------- | -------------------------- |
| C#                    | Primary language           |
| ASP.NET Core          | Web API framework          |
| .NET 10               | Runtime / target framework |
| Entity Framework Core | ORM                        |
| SQL Server            | Database                   |
| JWT Bearer            | Authentication             |
| BCrypt                | Password hashing           |
| Swagger / OpenAPI     | API documentation          |

---

## Getting Started

### Prerequisites

Install:

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* SQL Server
* Git

### Clone

```bash
git clone https://github.com/hatem247/CAS-Login-Back-End.git
cd CAS-Login-Back-End
```

### Configuration

Configure the application using your local environment or .NET User Secrets.

Required settings include:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  },
  "JWT": {
    "Key": "YOUR_JWT_SIGNING_KEY",
    "Issuer": "CAS.Api",
    "Audience": "CAS.Clients",
    "ExpirationMinutes": 60,
    "SsoExpirationHours": 8
  },
  "LoginRateLimiting": {
    "MaxRequests": 5,
    "Window": "00:15:00"
  }
}
```

### Run

```bash
dotnet restore
dotnet build
dotnet run
```

Swagger is available at:

```text
/swagger
```

when running in the Development environment.

---

## API Overview

### Authentication

| Method | Endpoint             | Access        |
| ------ | -------------------- | ------------- |
| `POST` | `/api/auth/login`    | Public        |
| `POST` | `/api/auth/switch`   | Authenticated |
| `POST` | `/api/auth/validate` | Authenticated |

### Account

| Method | Endpoint                       | Access        |
| ------ | ------------------------------ | ------------- |
| `POST` | `/api/account/register`        | Public        |
| `GET`  | `/api/account/profile`         | Authenticated |
| `PUT`  | `/api/account/profile`         | Authenticated |
| `POST` | `/api/account/change-password` | Authenticated |

### Business Entities

| Method | Endpoint                          | Access        |
| ------ | --------------------------------- | ------------- |
| `GET`  | `/api/businessentity`             | Authenticated |
| `GET`  | `/api/businessentity/{id}`        | Authenticated |
| `GET`  | `/api/businessentity/my-entities` | Authenticated |

### Roles

| Method | Endpoint                               | Access        |
| ------ | -------------------------------------- | ------------- |
| `GET`  | `/api/role`                            | Authenticated |
| `GET`  | `/api/role/{id}`                       | Authenticated |
| `GET`  | `/api/role/account/{businessEntityId}` | Authenticated |

---

## Response Format

Successful responses follow a consistent envelope:

```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": {}
}
```

Errors follow:

```json
{
  "statusCode": 400,
  "success": false,
  "message": "Validation failed.",
  "errors": [
    "Example validation error."
  ]
}
```

---

## Example

### Login Request

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "email": "user@example.com",
  "password": "P@ssw0rd!",
  "businessEntityId": 1
}
```

### Login Response

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
    "role": "Administrator",
    "businessEntityId": 1,
    "businessEntityName": "CAS",
    "redirectUrl": "https://example.com"
  }
}
```

---

## Reverse Proxy Deployment

When deployed behind a reverse proxy or load balancer, configure trusted forwarded headers so client IP detection remains correct for login rate limiting.

Trusted proxy configuration should be deployment-specific and must not trust arbitrary forwarded headers.

---

## Project Structure

```text
Controllers/
Data/
Exceptions/
Middleware/
Models/
Services/
Program.cs
```

The project is intentionally organized around clear responsibilities while keeping the codebase straightforward to maintain.

---

## Status

**Active Development**

This project is being developed as a reusable centralized authentication and authorization backend for connected applications.

---

## Author

**Hatem Medhat**

[GitHub](https://github.com/hatem247) · [LinkedIn](https://www.linkedin.com/in/hatem--medhat/)

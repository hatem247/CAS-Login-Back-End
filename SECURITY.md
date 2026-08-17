# Security Policy

## Supported Versions

Security fixes are applied to the actively maintained version of the project.

| Version        | Supported |
| -------------- | --------- |
| Latest         | ✅         |
| Older versions | ❌         |

## Reporting a Vulnerability

Please do not report security vulnerabilities through public GitHub issues.

For security-related concerns involving this project, contact:

**Email:** [hatemmedhat247@gmail.com](mailto:hatemmedhat247@gmail.com)

When reporting a vulnerability, include:

* A clear description of the issue
* Steps to reproduce the vulnerability
* Potential security impact
* Any relevant logs, screenshots, or proof of concept

Please avoid sharing credentials, private keys, production connection strings, or other sensitive information in the report.

## Security Practices

This project is designed around common API security practices including:

* JWT-based authentication
* Role and business-entity authorization
* BCrypt password hashing
* Login rate limiting
* Secure externalized configuration
* Centralized exception handling
* Swagger/OpenAPI documentation for API testing

Production deployments should store secrets and connection strings outside the repository using secure environment configuration or secret-management systems.

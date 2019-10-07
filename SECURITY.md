# Security Policy

## Reporting a Vulnerability

**Do not open public GitHub issues for security vulnerabilities.**

If you discover a security vulnerability in DotNet API Gateway, please report it responsibly through one of the following channels:

### Option 1: GitHub Private Vulnerability Reporting (Recommended)

Use GitHub's built-in private vulnerability reporting:
- Navigate to: https://github.com/sarmkadan/dotnet-api-gateway/security/advisories/new
- Provide details about the vulnerability
- GitHub will create a private discussion with the maintainers

### Option 2: Email

Send a detailed report to: **rutova2@gmail.com**

Include:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if available)

## Response Timeline

We are committed to addressing security vulnerabilities promptly:

- **Acknowledgment**: Within 48 hours of receiving the report
- **Assessment**: Within 1 week of receiving the report
- **Resolution**: Depends on complexity; we aim for timely patches

## Supported Versions

Security updates are provided for:

| Version | Status      | End of Support |
|---------|-------------|----------------|
| 1.x     | Supported   | Active         |
| < 1.0   | Unsupported | N/A            |

## Security Best Practices

When using DotNet API Gateway, follow these security best practices:

### Authentication & Authorization

- Always enable JWT validation for sensitive routes
- Use strong, randomly generated secrets for JWT signing
- Implement proper role-based access control (RBAC)
- Regularly rotate authentication credentials
- Never commit secrets to version control

### Configuration

- Use environment variables for sensitive configuration (API keys, secrets)
- Keep `appsettings.json` with secure defaults
- Restrict access to configuration files in production
- Use HTTPS in production
- Implement proper TLS/SSL certificate management

### API Gateway Setup

- Enable rate limiting to prevent abuse
- Use circuit breakers to prevent cascading failures
- Implement request validation
- Monitor and log suspicious activity
- Keep the gateway updated with the latest patches

### Dependency Management

- Keep .NET SDK and dependencies up to date
- Regularly review and update NuGet packages
- Monitor security advisories for dependencies
- Use `dotnet list package --outdated` to find updates

### Network & Infrastructure

- Run the gateway behind a firewall
- Implement proper network segmentation
- Use secrets management systems (e.g., Azure Key Vault, HashiCorp Vault)
- Monitor gateway logs for suspicious patterns
- Implement rate limiting at the network level

## Disclosures

We follow responsible disclosure practices. Security advisories will be published:
- After a fix has been released
- When giving affected users time to update (typically 90 days from patch release)
- On the [GitHub Security Advisories](https://github.com/sarmkadan/dotnet-api-gateway/security/advisories) page

## Third-Party Security Scanning

DotNet API Gateway is regularly scanned using:
- GitHub's built-in security scanning
- Dependabot for dependency vulnerabilities
- Static analysis tools

## Questions?

If you have questions about security practices or need clarification, please contact the maintainers at rutova2@gmail.com.

---

Thank you for helping keep DotNet API Gateway secure!

# Security Policy

The **KUKULCAN.SharedKernel.i18n** project takes security seriously.

If you discover a security vulnerability, please report it responsibly and **do not disclose it publicly** until it has been investigated and resolved.

---

# Supported Versions

The following table indicates which versions currently receive security updates.

| Version   | Supported  |
|-----------|:----------:|
| 1.0.x     |     ✅     |
| &lt; 1.0  |     ❌     |

Only the latest stable released version is guaranteed to receive security fixes.

Beta, alpha and release candidate versions are intended for evaluation and testing purposes and may contain known issues that will be addressed before the final release.

---

# Reporting a Vulnerability

If you believe you have discovered a security vulnerability, **please do not create a public GitHub Issue**.

Instead, report it privately using the following e-mail address:

**jpardo.kukulcan@gmail.com**

Please include as much information as possible:

- Framework version.
- .NET version.
- Operating system.
- Detailed description of the vulnerability.
- Steps required to reproduce the issue.
- Proof of concept (if available).
- Expected impact.
- Suggested mitigation (optional).

Providing a small reproducible sample greatly helps the investigation process.

---

# Responsible Disclosure

We kindly ask all security researchers to follow responsible disclosure principles.

Please:

- Do not publicly disclose the vulnerability before a fix has been released.
- Allow the maintainers reasonable time to investigate the issue.
- Avoid publishing exploit code while the vulnerability is under investigation.
- Coordinate disclosure with the project maintainers whenever possible.

Responsible disclosure helps protect every application depending on **KUKULCAN.SharedKernel.i18n**.

---

# What Should Be Reported

Examples of security-related issues include, but are not limited to:

- Remote Code Execution (RCE)
- Privilege Escalation
- Authentication bypass
- Authorization bypass
- Arbitrary file access
- Information disclosure
- Injection vulnerabilities
- Unsafe serialization
- Denial of Service caused by framework behaviour
- Dependency-related security vulnerabilities
- Cryptographic weaknesses
- Unexpected execution paths affecting application security

---

# What Should NOT Be Reported

The following are generally **not considered security vulnerabilities**:

- Coding style issues
- Documentation errors
- XML documentation mistakes
- Feature requests
- General API design discussions
- Performance improvements
- Unit test failures
- Compiler warnings without security implications
- Breaking-change proposals

Those topics should be reported using the normal GitHub Issues workflow.

---

# Security Response Process

Every security report follows the same investigation process.

## 1. Acknowledgement

A confirmation of receipt will normally be sent within a reasonable time after receiving the report.

---

## 2. Investigation

The maintainers will:

- Verify the reported issue.
- Assess the potential impact.
- Identify affected versions.
- Evaluate possible mitigations.
- Determine the appropriate severity level.

---

## 3. Resolution

If the vulnerability is confirmed:

- A fix will be implemented.
- Appropriate tests will be added.
- Documentation will be updated if necessary.

Depending on severity, the fix may be released as:

- Patch release
- Minor release
- Major release

---

## 4. Public Disclosure

Once a fix has been published, the vulnerability may be publicly disclosed together with the corresponding release notes and security advisory.

Whenever possible, affected users should have sufficient time to update before public disclosure.

---

# Security Design Principles

The architecture of **KUKULCAN.SharedKernel.i18n** has been intentionally designed to reduce common sources of vulnerabilities.

Core architectural principles include:

- Immutable Value Objects
- Strongly Typed Identifiers
- Explicit validation
- Null-safe APIs
- Minimal public API surface
- Separation of concerns
- Framework independence
- Encapsulation of business rules
- Deterministic behaviour
- Minimal external dependencies

Although these principles improve robustness, they do **not** eliminate the need for secure application design.

Applications built on top of the Shared Kernel remain responsible for their own authentication, authorisation, encryption and infrastructure security.

---

# Third-Party Dependencies

The Shared Kernel intentionally minimises third-party dependencies.

Whenever an external dependency is introduced, it will be evaluated according to:

- Security history
- Maintenance status
- Community adoption
- Long-term viability
- Licence compatibility

Dependencies with known critical vulnerabilities will be updated or replaced as soon as reasonably possible.

---

# Secure Development Practices

Project development follows modern secure development practices, including:

- Nullable Reference Types enabled
- XML documentation for every public API
- Immutable domain models
- Strong encapsulation
- Deterministic unit testing
- Static code analysis
- Continuous Integration builds
- Semantic Versioning
- Architecture-first design

---

# Security Updates

Security fixes will always be documented through one or more of the following channels:

- CHANGELOG.md
- GitHub Releases
- Release Notes

Whenever possible, security updates will preserve backward compatibility.

---

# Contact

Responsible disclosure reports should be sent exclusively to:

**jpardo.kukulcan@gmail.com**

Please **do not** report security vulnerabilities through public GitHub Issues or Discussions.

---

# Private Vulnerability Reporting

If the GitHub repository has **Private Vulnerability Reporting** enabled, that mechanism should be preferred over public communication channels.

This allows confidential communication between security researchers and the project maintainers while a fix is being prepared.

---

# Final Notes

Security is considered a fundamental architectural concern within **KUKULCAN.SharedKernel.i18n**.

Every responsible security report is appreciated and contributes to improving the quality, reliability and long-term stability of the framework.

Thank you for helping keep **KUKULCAN.SharedKernel.i18n** secure for the entire community.

# Security policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately:

- GitHub **Security → Report a vulnerability** (private advisory) on this repo, or
- email **llewellynbooth1@gmail.com** with `SECURITY` in the subject.

Please include steps to reproduce and the impact you observed. I'll acknowledge
within a few days. This is a personal portfolio project, not a commercial service —
there is no bounty, but credit is given for valid reports.

## Scope

In scope: the static site, the Azure Functions API (`/api/getResumeFunction`,
`/api/contact`, `/api/health`), the CI/CD workflows, and anything in this repo.

Out of scope: third-party services the site links to (Credly, OneDrive, LinkedIn,
GitHub), and denial-of-service testing against the live endpoints.

## Handling

- Dependencies are watched by Dependabot (`.github/dependabot.yml`).
- Static analysis runs on every push and PR via CodeQL (`.github/workflows/codeql.yml`).
- The threat model is at [`docs/threat-model.md`](docs/threat-model.md).
- Secrets are never committed; CI authenticates to Azure with OIDC, not stored
  credentials (see [`docs/adr/0003-cicd-oidc.md`](docs/adr/0003-cicd-oidc.md)).

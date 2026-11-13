# Exam Management Application - Project Operating Guide

## Team workflow

Read team/README.md and use the task ID, assignee, reviewer, affected files and acceptance criteria from team/PHAN_CONG_NHOM_THEO_MANG.xlsx. Coordinate changes to shared shell, DTOs, services and migrations. Keep proposed behavior distinct from tested implementation.

## Source and priority

This repository implements the C# course project requirements in
`De tai mon Ngon ngu lap trinh CSharp - 2026 (1).pdf` (20 September 2026).
Treat those requirements as acceptance criteria for all architectural and
implementation decisions. The working topic is **an online multiple-choice
examination management system**; confirm with the project owner before
changing the topic scope.

## Non-negotiable delivery architecture

The finished system must contain two C# applications that serve the same
business domain and use the same Microsoft SQL Server business data:

1. **Desktop administration application** for Windows: `Exam.Desktop`, using
   WinForms. The owner approved this choice on 24 October 2026 (DECISIONS.md).
   Preserve the existing WinUI 3 `App1` as historical reference.
2. **User-facing web application** in C#. Prefer ASP.NET Core MVC, Razor Pages,
   or Blazor. It must be responsive.
3. **Shared backend and data access**. Use ASP.NET Core Web API where it makes
   the desktop/web integration clear; use EF Core with SQL Server and a layered
   architecture. Do not create separate databases or duplicate business rules
   for Web and Desktop.

Approved solution layout:

```text
Exam Management Application.slnx
|- Exam.Desktop          # WinForms, JWT API client
|- Exam.Web              # MVC/Razor/Bootstrap and API in one host
|- Exam.Core             # DTOs and shared business types
|- Exam.Services         # all use cases and business rules
|- Exam.Data             # Identity, EF Core, SQL Server, migrations
`- Exam.Tests            # xUnit and isolated SQL Server tests
```

Keep the structure proportionate. Do not add a project merely to imitate a
large-system architecture.

## Mandatory functional baseline

### Desktop administrator

- Require login, authentication, and role-based authorization before any
  administrative feature.
- Provide add, view, edit, delete, and search flows for the required business
  information and categories.
- Keep screens clear, usable, and resilient to normal validation/errors.

### Web user application

- Provide a home page, navigation/menu, information categories, search,
  detail pages, and the topic-specific business flows.
- Keep layouts usable on common phone and desktop widths.

### Data and quality

- Model the SQL Server schema with appropriate primary/foreign keys and at
  least third normal form (3NF) unless an intentional, documented exception is
  necessary.
- Apply OOP, separation of UI/business/data responsibilities, validation,
  exception handling, and dependency injection where useful.
- Keep all credentials and connection strings out of source control. Use local
  configuration or user secrets for development.
- Every implemented feature must be explainable by a group member. Record and
  cite any externally adapted code, library, or design source.

## Required evidence before claiming a milestone complete

1. Build and run the relevant Desktop, API, and Web components.
2. Demonstrate that an admin change is visible to the Web application through
   the same SQL Server data.
3. Verify authorization, validation/error behavior, and the main business flow.
4. Check Web responsive layout and Desktop usability visually; a successful
   build alone is insufficient.
5. Preserve screenshots, test notes, schema/migration evidence, and an
   explanation of each member's contribution for the demo and report.

## Report and assessment guardrails

The report must include: cover page, table of contents, introduction/objective,
system survey, system analysis (functions and layouts), system/database design,
achieved results, conclusion/future development, and references.

Do not treat optional technology as a substitute for required features. The
brief limits the maximum score if any of the following is missing:

| Missing condition | Maximum score |
| --- | ---: |
| Web application | 6.0 |
| Desktop administration application | 6.0 |
| Shared Web/Desktop business data | 7.0 |
| SQL Server | 6.5 |
| Runnable/demoable product | 5.0 |
| Only UI, no main business functionality | 4.0 |
| Source code submission | 4.0 |

## Working conventions for agents

- Before adding a feature, identify its Web screen, Desktop screen, API/use
  case, authorization rule, and persisted data.
- Prefer small vertical slices that prove Desktop -> API -> SQL Server -> Web
  integration over isolated mock UIs.
- State clearly whether a result is build-tested, database-tested, API-tested,
  or visually tested. Never infer one from another.
- Keep documentation and diagrams synchronized with implemented behavior.
- Preserve App1; the assessed clients are now Exam.Desktop and Exam.Web.
- Current acceptance source: docs/YEU_CAU_DU_AN_DE_TAI_18.md and DECISIONS.md.
- Target role mapping is Student/Lecturer/Administrator; lecturer owns exam-session
  workflows within assigned subjects. Current source still has ExamOfficer;
  migrate it under QT01. Identity remains Users/Roles/UserRoles.
- Approved scope includes xUnit and basic CSV/XLSX result export; other optional
  features remain deferred. Never claim unexecuted runtime or visual checks.

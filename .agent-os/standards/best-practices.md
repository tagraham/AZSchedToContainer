# Development Best Practices

## Context

Global development guidelines for Agent OS projects.

<conditional-block context-check="core-principles">
IF this Core Principles section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Core Principles already in context"
ELSE:
  READ: The following principles

## Core Principles

### Keep It Simple
- Implement code in the fewest lines possible
- Avoid over-engineering solutions
- Choose straightforward approaches over clever ones

### Optimize for Readability
- Prioritize code clarity over micro-optimizations
- Write self-documenting code with clear variable names
- Add comments for "why" not "what"

### Code Consistency
- **Follow existing project patterns** - Don't introduce new styles in established codebases
- **When in Rome, do as Romans do** - Match the existing code style, even if it differs from these guidelines
- **Gradual improvement** - Only refactor patterns when touching code for other reasons
- **Team over individual preference** - Consistency trumps personal coding style preferences
- **Document deviations** - If project patterns differ from these guidelines, document why

### DRY (Don't Repeat Yourself)
- Extract repeated business logic to private methods
- Extract repeated UI markup to reusable components
- Create utility functions for common operations

### YAGNI with Strategic Planning
- **You Aren't Gonna Need It**: Don't build features until they're needed
- **But plan for phases**: When requirements show clear phases, design interfaces that won't require rework
- Build what you need now, architect for what you know is coming
- Avoid gold-plating, but don't paint yourself into corners

### Security First
- **Security is not optional** - All code must run securely
- Validate all inputs at system boundaries
- Follow principle of least privilege
- Assume all external data is malicious until proven otherwise
- Use security-focused libraries and frameworks

### File Structure
- Keep files focused on a single responsibility
- Group related functionality together
- Use consistent naming conventions
</conditional-block>

<conditional-block context-check="architecture-patterns">
IF this Architecture Patterns section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Architecture Patterns already in context"
ELSE:
  READ: The following patterns

## Architecture Patterns

### Default Pattern Hierarchy
Follow this preference order unless language/framework recommends otherwise:

1. **Clean Code Principles** (Always apply)
2. **Onion Architecture** (Preferred for complex applications)
3. **MVC Pattern** (Standard fallback)
4. **Language-specific patterns** (When framework recommends better alternatives)

### Clean Code Rules
- Write functions that do one thing well
- Use meaningful names that express intent
- Keep functions small (ideally < 20 lines)
- Avoid deep nesting (max 3 levels)
- Return early to reduce complexity
- Use pure functions when possible

### Onion Architecture (Preferred)
**Layer Structure**: Presentation → Application → Domain → Infrastructure
- Keep domain layer free of external dependencies
- Make dependencies point inward only
- Use dependency injection for external services
- Define interfaces in inner layers, implement in outer layers

### MVC Pattern (Fallback)
- Keep controllers thin (orchestration only)
- Put business logic in models or services
- Make views stateless when possible
- Use consistent naming conventions
</conditional-block>

<conditional-block context-check="component-portability">
IF this Component Portability section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Component Portability guidelines already in context"
ELSE:
  READ: The following guidelines

## Component Portability

### Design for Reusability
- Create components with clear, minimal interfaces
- Use dependency injection for external services
- Make components configurable through props/parameters
- Avoid hardcoded dependencies
- Design stateless components when possible

### Database Access
- **Never use inline SQL** - Always use query builders or ORMs
- **Language-specific recommendations**:
  - JavaScript/TypeScript: Knex.js, Prisma, TypeORM
  - C#/.NET: Entity Framework, LINQ to SQL
  - Python: SQLAlchemy, Django ORM
  - Java: Hibernate, MyBatis, JOOQ
  - Go: GORM, Squirrel
  - Rust: Diesel, SQLx
- Abstract database operations behind repository interfaces
- Use parameterized queries to prevent SQL injection

### Framework Agnostic Design
- Extract business logic into plain functions/classes
- Use standard language features over framework-specific ones
- Create wrapper functions for framework-specific functionality
- Design domain models independent of persistence mechanisms
</conditional-block>

<conditional-block context-check="security-guidelines">
IF this Security Guidelines section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Security guidelines already in context"
ELSE:
  READ: The following security requirements

## Security Guidelines

### Input & Data Security
- Validate all inputs at system boundaries
- Use parameterized queries/ORM (never inline SQL)
- Prevent XSS with output encoding and CSP
- Implement CSRF protection
- Encrypt sensitive data at rest and in transit

### Authentication & Authorization
- Enforce strong password requirements
- Implement proper session management (httpOnly, secure, sameSite)
- Use role-based access control
- Never trust client-side authentication checks
- Support multi-factor authentication when possible

### Infrastructure Security
- **Local development**: HTTP acceptable (localhost only)
- **All online environments**: HTTPS mandatory
- **Cloudflare**: Use for SSL termination, security headers, and performance
- Configure security headers (HSTS, CSP, etc.) - Cloudflare can handle many
- Implement rate limiting (Cloudflare provides this)
- Set up logging and monitoring
- Keep dependencies updated
- Use environment variables for secrets (never hardcode)
- Configure applications to detect proxy headers (X-Forwarded-Proto)

### Error Handling
- Never expose internal details in error messages
- Log errors securely for debugging
- Fail securely (deny by default)
</conditional-block>

<conditional-block context-check="phase-development">
IF this Phase Development section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Phase Development guidelines already in context"
ELSE:
  READ: The following phase planning approach

## Phase Development Strategy

### YAGNI with Strategic Planning
- **Current Phase**: Build only what's needed now
- **Known Phases**: Design interfaces that won't require rework for planned phases
- **Unknown Future**: Don't over-engineer for hypothetical requirements

### Common Phase Evolution Patterns
Plan interfaces for these predictable transitions:
- **Authentication**: Basic → OAuth → Enterprise SSO
- **Payments**: Single provider → Multiple providers → Recurring billing
- **Notifications**: Email → Multi-channel → Personalized delivery
- **Data Storage**: Simple tables → Audit logs → Event sourcing
- **APIs**: Internal → Public → Rate limiting & analytics
- **Permissions**: Basic roles → Fine-grained → Dynamic permissions

### Implementation Strategy
- Use feature flags for phase rollouts
- Document known phases in interface comments
- Design APIs with versioning in mind
- Create abstraction layers for known evolution points
- Plan database schema for growth (without over-engineering)
</conditional-block>

<conditional-block context-check="project-consistency">
IF this Project Consistency section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Project Consistency guidelines already in context"
ELSE:
  READ: The following consistency requirements

## Project Consistency

### Existing Codebase Rules
- **Analyze existing patterns first** - Before writing new code, understand how the project currently handles similar functionality
- **Match existing style** - Use the same naming conventions, file organization, and architectural patterns already in use
- **Don't introduce competing patterns** - Avoid creating the 5th way to handle the same thing
- **Respect established conventions** - Even if they differ from your preferences or these guidelines

### When Working on Established Projects
- **PHP projects**: Follow existing PSR standards usage, framework patterns (Laravel/Symfony style), and naming conventions
- **JavaScript projects**: Match existing module patterns (CommonJS/ES6), testing frameworks, and build tools
- **Any language**: Observe existing error handling, logging, configuration, and database access patterns

### Gradual Improvement Strategy
- **Boy Scout Rule**: Leave code cleaner than you found it, but don't rewrite everything
- **Opportunistic refactoring**: Improve patterns only when modifying code for feature work
- **Discuss major changes**: Get team agreement before introducing significantly different approaches
- **Document evolution**: When patterns do change, update documentation and communicate to team

### Red Flags - Stop and Discuss
- Creating a new way to handle authentication when one exists
- Introducing a different ORM/database pattern alongside existing one
- Adding a new HTTP client when project already has one
- Using different testing patterns in same test suite
- Creating new configuration method when project has established approach
</conditional-block>

<conditional-block context-check="deployment-environments">
IF this Deployment Environments section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Deployment Environments already in context"
ELSE:
  READ: The following environment guidelines

## Deployment Environments

### Standard Environment Pattern
- **Local Development**: Native or Docker development environment (HTTP acceptable)
- **Test Environment**: Azure VM (or other cloud) with Docker containers (HTTPS required)
- **Production Environment**: Cloud-hosted containers with orchestration (HTTPS required)
- **CDN/Proxy**: Cloudflare for SSL termination, caching, and security

### HTTPS Strategy
- **Local**: HTTP acceptable for development (localhost, 127.0.0.1)
- **All online environments**: HTTPS mandatory (test, staging, production)
- **Cloudflare**: Handle SSL certificates, security headers, and performance optimization
- **Application**: Configure to work behind SSL-terminating proxy

### Container Strategy
- **Containerize all applications** for consistency across environments
- Use Docker for local development when possible (matches test/prod)
- Design applications to be container-friendly (12-factor app principles)
- Keep containers lightweight and single-purpose

### Environment Configuration
- Use environment variables for all configuration differences
- Configure apps to detect proxy headers (X-Forwarded-Proto, X-Forwarded-For)
- Never hardcode environment-specific values
- Use `.env` files for local development (never commit to git)
- Use cloud configuration services for test/production secrets

### Development Workflow
- Code locally (HTTP acceptable for localhost)
- Test in containerized cloud environment with HTTPS before production
- Ensure parity between local, test, and production environments
- Use infrastructure as code (Terraform, ARM templates, etc.)

### Container Best Practices
- Use multi-stage builds to minimize image size
- Run containers as non-root users
- Use health checks for container monitoring
- Tag images with semantic versions, not `latest`
- Scan images for security vulnerabilities
- Configure apps to work behind reverse proxy (Cloudflare)
</conditional-block>

<conditional-block context-check="dependencies" task-condition="choosing-external-library">
IF current task involves choosing an external library:
  IF Dependencies section already read in current context:
    SKIP: Re-reading this section
    NOTE: "Using Dependencies guidelines already in context"
  ELSE:
    READ: The following guidelines
ELSE:
  SKIP: Dependencies section not relevant to current task

## Dependencies

### Choose Libraries Wisely
When adding third-party dependencies:
- Select the most popular and actively maintained option
- Check the library's GitHub repository for:
  - Recent commits (within last 6 months)
  - Active issue resolution
  - Number of stars/downloads
  - Clear documentation
- Consider bundle size impact for frontend projects
- Prefer standard library solutions when available
- Use peer dependencies when possible to reduce duplication
</conditional-block>

<conditional-block context-check="language-specific-adaptations">
IF current task involves specific programming language:
  READ: Apply these language-specific adaptations to above patterns

## Language-Specific Adaptations

### When to Deviate from Default Patterns

- **React/Next.js**: Use React's component patterns over traditional MVC, leverage hooks for state management
- **Node.js/Express**: Use Express middleware patterns, leverage event-driven architecture
- **Python/Django**: Follow Django's MVT pattern, use built-in features as intended
- **Go**: Use interface-based composition, follow small interface convention, leverage concurrency patterns
- **Rust**: Use ownership system for memory safety, leverage trait system, follow Result/Option patterns
- **C#/.NET**: Use LINQ extensively, follow async/await patterns, leverage dependency injection container
</conditional-block>

## Quick Reference

### Security Checklist
- [ ] All inputs validated and sanitized
- [ ] No inline SQL (use ORM/query builder)
- [ ] HTTPS enforced on online environments
- [ ] Secrets in environment variables
- [ ] Error messages don't expose internals
- [ ] Authentication/authorization on all protected endpoints

### Code Quality Checklist
- [ ] Functions do one thing well
- [ ] Names are clear and descriptive
- [ ] Dependencies point inward (Onion Architecture)
- [ ] Business logic separated from framework code
- [ ] Database access abstracted behind repositories
- [ ] Components designed for reusability

### Project Consistency Checklist
- [ ] Analyzed existing codebase patterns before writing new code
- [ ] Matched existing naming conventions and file organization
- [ ] Used same architectural patterns as rest of project
- [ ] Didn't introduce competing ways to handle same functionality
- [ ] Discussed any major pattern changes with team
- [ ] Left code cleaner than found it (Boy Scout Rule)

### Environment Checklist
- [ ] Application runs consistently in containers
- [ ] All config externalized to environment variables
- [ ] No hardcoded environment-specific values
- [ ] Local development matches test/prod architecture
- [ ] Health checks implemented for container monitoring
- [ ] Images tagged with semantic versions
- [ ] HTTPS enforced on all online environments
- [ ] Application configured to work behind Cloudflare proxy
- [ ] Proxy headers (X-Forwarded-Proto) properly detected
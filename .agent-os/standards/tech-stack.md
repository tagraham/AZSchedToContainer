# Tech Stack

## Context

Global tech stack defaults for Agent OS projects, overridable in project-specific `.agent-os/product/tech-stack.md`.


- **Backend Framework**: Node.js with Express or Fastify
- **Language**: TypeScript 5.3+
- **Node Version**: 20 LTS (Active until April 2026)
- **Frontend Framework**: Vue 3.4+ or React 18+
- **Build Tool**: Vite 5.0+
- **Import Strategy**: ES modules
- **Package Manager**: npm or pnpm
- **Primary Database**: PostgreSQL 16+
- **ORM/Query Builder**: Prisma, TypeORM, or Knex.js (TypeScript), Hibernate/MyBatis/JOOQ (Java)
- **CSS Framework**: Bootstrap 5.3+ (preferred) or TailwindCSS 3.4+
- **UI Components**: Vuetify/PrimeVue (Vue), Material-UI/Ant Design (React), React-Bootstrap/Vue-Bootstrap
- **Font Provider**: Google Fonts or system fonts
- **Font Loading**: Self-hosted for performance and GDPR compliance
- **Icons**: Lucide, Bootstrap Icons, or Heroicons
- **Application Hosting**: Azure App Service (preferred), AWS Elastic Beanstalk, or Google App Engine
- **Hosting Region**: Primary region based on user base
- **Database Hosting**: Containerized PostgreSQL (startup default), Azure Database/AWS RDS/Google Cloud SQL (scale-up)
- **Database Backups**: Manual scripts (containers), Daily automated (managed services)
- **Asset Storage**: Azure Blob Storage (preferred), Amazon S3, or Google Cloud Storage
- **CDN**: Azure CDN (preferred), CloudFront, or Cloud CDN
- **Asset Access**: Private with signed URLs
- **CI/CD Platform**: GitHub Actions
- **CI/CD Trigger**: Push to main/staging branches
- **Tests**: Run before deployment
- **Production Environment**: main branch
- **Staging Environment**: staging branch

## Database Strategy Notes

- **Startup Approach**: Begin with containerized PostgreSQL (~$10-50/month)
- **Load Capacity**: Handles <100k users, <10GB data, thousands of concurrent connections
- **Migration Trigger**: Move to managed services when scaling demands or budget allows
- **Managed Service Costs**: Azure Database (~$50-200/month), AWS RDS (~$40-180/month), Google Cloud SQL (~$45-190/month), Digital Ocean (~$35-160/month)

## Cloud Provider Priorities

1. **Azure** (Current preference) - Enterprise integration, good scaling
2. **AWS** (Best ecosystem) - Most comprehensive services, competitive at scale
3. **Google Cloud** (AI/ML focus) - Best for data analytics, sustained use discounts
4. **Digital Ocean** (Budget option) - Simple pricing, good for small-medium projects
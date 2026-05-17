# Shorth

High-throughput URL shortener built with ASP.NET Core, PostgreSQL, Redis, and React.

Shorth converts long URLs into compact Base62 slugs, resolves redirects through a Redis-backed fast path, and records click analytics asynchronously through background workers.

## Functions

- Create anonymous or authenticated short links.
- Resolve short links with Redis caching and HTTP redirects.
- View authenticated "My Links" with pagination.
- View link analytics, including total clicks, daily clicks, unique visitors, and top countries.
- Register with email/password and verify email by OTP.
- Sign in or sign up with Google OAuth2.
- Manage profile information, avatar, and password.
- Upload profile images directly to S3 through presigned POST.
- Generate QR codes for created links.
- Protect anonymous link creation with Cloudflare Turnstile.
- Send transactional emails through an outbox + worker flow.
- Process click events asynchronously through SQS workers.

## Tech Stack

### Backend

- .NET 10
- ASP.NET Core Web API
- Clean Architecture-style projects: `Api`, `Application`, `Domain`, `Infrastucture`, `Worker`
- PostgreSQL
- Entity Framework Core / Npgsql
- Redis distributed cache
- SQS background queues
- S3 for object storage
- Google OAuth2
- Resend email service

### Frontend (Vibe coded)

- Vite
- React
- TypeScript
- Vanilla CSS
- Phosphor Icons

## Cloud And Infrastructure

- **Frontend:** S3 + CloudFront
- **Backend:** EC2 + Docker Compose
- **Reverse proxy:** nginx
- **Database:** PostgreSQL
- **Cache:** Redis
- **Queues:** SQS
- **Object storage:** S3
- **CDN for uploads:** CloudFront
- **Secrets:** AWS Secrets Manager
- **Email:** Resend
- **Container registry:** ECR
- **CI/CD:** GitHub Actions

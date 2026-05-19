# Nexora.Web

A multi-tenant business management web application built with ASP.NET Core MVC on .NET 10. Each organization gets its own isolated workspace with product, order, customer, and inventory management — plus a public-facing landing page.

## Features

- **Authentication** — Register, login, email confirmation, forgot/reset password (ASP.NET Core Identity)
- **Multi-tenancy** — Data is scoped per Organization; users belong to an org with an Owner or Staff role
- **Products & Categories** — CRUD with image uploads, organized by category
- **Orders** — Create orders with multiple line items; stock movements tracked automatically
- **Customers** — Customer directory tied to the organization
- **Inventory** — Stock movement history per product
- **Imports** — Bulk order import via Excel (ClosedXML)
- **Dashboard** — Overview stats
- **Contact Messages** — Landing page contact form; messages land in an inbox inside the app
- **Email** — Transactional email via SMTP (confirmation, password reset)
- **Subscriptions** — Subscription plan model (Starter plan seeded by default)
- **Soft deletes** — All entities use `IsDeleted` with global query filters
- **Public landing page** — Marketing site served from `wwwroot/play`

## Tech Stack

- **Framework:** ASP.NET Core MVC, .NET 10
- **Database:** SQL Server — EF Core 10, code-first migrations
- **Auth:** ASP.NET Core Identity with email confirmation
- **Excel import:** ClosedXML
- **Email:** SMTP via `SmtpEmailSender`

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or SQL Server Express

## Getting Started

1. **Clone**

   ```bash
   git clone https://github.com/URLeeo/Nexora.Web.git
   cd Nexora.Web
   ```

2. **Configure the connection string** in `Nexora.Web/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=NexoraAppDB;Trusted_Connection=True;Encrypt=False;"
   }
   ```

3. **Configure SMTP** (required for email confirmation and password reset):

   ```json
   "Smtp": {
     "Host": "smtp.gmail.com",
     "Port": 587,
     "User": "your@email.com",
     "Password": "your-app-password",
     "FromEmail": "your@email.com",
     "FromName": "Nexora",
     "EnableSsl": true
   }
   ```

4. **Run**

   ```bash
   dotnet run --project Nexora.Web/Nexora.Web.csproj
   ```

   The database is created and migrated automatically on first run. The `Owner`/`Staff` roles and a `Starter` subscription plan are seeded.

## Project Structure

```
Nexora.Web/
├── Controllers/        # Account, Dashboard, Products, Orders, Customers, …
├── Data/
│   ├── Entities/       # EF Core entity models
│   ├── AppDbContext.cs
│   └── DbSeeder.cs
├── Models/             # View models
├── Services/Email/     # SMTP email service
├── Views/              # Razor views
├── Migrations/
└── wwwroot/
    ├── admin/          # Admin panel static assets
    ├── play/           # Public landing page assets
    └── uploads/        # Uploaded product images
```

## Roles

| Role | Description |
|------|-------------|
| `Owner` | Full access to the organization |
| `Staff` | Limited access within the organization |

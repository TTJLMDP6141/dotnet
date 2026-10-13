# Urban Services — ASP.NET Core MVC

A compact ASP.NET Core MVC version of the Urban Services home-service booking app. It uses Razor views, Bootstrap, ASP.NET Core Identity for account/password/role management, Entity Framework Core, and a persistent SQLite database.

## Requirements

- .NET 8 SDK
- Internet access for the first NuGet restore and Bootstrap CDN assets

## Run locally

Open a terminal in this folder (the folder containing `UrbanServicesMvc.csproj`) and run:

```bash
dotnet restore
dotnet run
```

The application listens on `http://localhost:5080` by default. If a `PORT` environment variable is set, it listens on `0.0.0.0:$PORT`. On first launch, it creates `urbanservices.db`, Identity role tables, and the sample service catalog.

### Enable the first admin account

Admin registration requires a private invite code. Set `ADMIN_REGISTRATION_CODE` in the environment before starting the app. Do not put the value in source control.

PowerShell:

```powershell
$env:ADMIN_REGISTRATION_CODE = "choose-a-private-invite-code"
dotnet run
```

macOS/Linux:

```bash
export ADMIN_REGISTRATION_CODE='choose-a-private-invite-code'
dotnet run
```

Open **Create account**, choose **Administrator**, and enter the invite code. The app does not ship with a default admin password. Sign in using the **Administrator** account type.

## Roles and functionality

- **Customer:** create an account, browse/search services, view service details and reviews, book a time slot, view/cancel eligible bookings, update profile, and review a completed booking once.
- **Provider:** create an account and provider profile, view assigned bookings, accept/reject pending requests, start and complete work, and update availability/profile details.
- **Admin:** register with the invite code, view dashboard counts and recent bookings, manage customer/provider directories, assign providers and update booking statuses, and add/edit/delete services. Services already used by bookings cannot be deleted; mark them inactive instead.
- **All roles:** role-aware sign-in, sign-out, and authorization checks. Selecting a different account type at login does not grant that role.

## Booking lifecycle

1. Customers request an active service, a future date, an available time, a phone number, and a service address. New bookings are `PENDING`.
2. An admin assigns a provider.
3. The provider accepts or rejects a pending request. Accepted work can move to `IN_PROGRESS`, then `COMPLETED`.
4. Customers can cancel `PENDING` or `ACCEPTED` bookings and review a completed booking once.

## Database and security notes

- SQLite is the lightweight file-database equivalent used here; H2 is a Java database and is not used by ASP.NET Core.
- The database file is local and ignored by Git. Back it up before deleting or replacing it.
- Passwords are managed and hashed by ASP.NET Core Identity. Forms use antiforgery protection and account creation validates roles and input.
- `ADMIN_REGISTRATION_CODE` is intentionally not included in this ZIP. Configure it through Replit Secrets or a local environment variable.
- This is a demonstration app. Add verified email/password recovery, production rate limiting, deployment-specific HTTPS settings, backups, and monitoring before handling real users.

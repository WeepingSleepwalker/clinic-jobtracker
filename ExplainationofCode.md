📝 Code & Docker Architecture
Backend (clinic-jobtracker/JobTracker.Api)

    Tech: .NET 9 Minimal APIs, EF Core with SQLite.

    Purpose: Handles core business logic:

    Patients (create/list)

    Appointments (book, check conflicts)

    Doctor schedule (daily agenda)

    Invoices (create, pay, rollback demo)

    Background job queue for bulk billing.

    Persistence: SQLite database (app.db), stored in /app/data inside the container (mounted to _data/api on host).

API Dockerfile flow:

    Uses dotnet/sdk:9.0 to build & publish.

    Copies build into dotnet/aspnet:9.0 runtime image.

    Exposes port 5002, runs the app.

    Connection string points to /app/data/app.db.

    Frontend (clinic-frontend)

    Tech: Angular 20.

    Purpose: Provides a simple UI for:

    Creating patients.

    Scheduling appointments with doctors.

    Viewing doctor schedules.

    Viewing and paying invoices.

Environment config: In Docker, apiBase is set to /api.
The Angular app doesn’t need to know the backend host/port directly.

Frontend Dockerfile flow:

Uses node:20-alpine to install deps and build Angular into static files (dist/.../browser).

    Uses nginx:alpine to serve those static files.

    Copies in a custom nginx.conf:

    Serves Angular app under /.

    Proxies /api requests to the backend service (api:5002).

    Supports Angular’s client-side routing fallback (/index.html).

    Exposes port 80.

    Docker Compose (docker-compose.yml)

    Defines two services:

    api (backend) → built from clinic-jobtracker/JobTracker.Api/Dockerfile.

    web (frontend) → built from clinic-frontend/Dockerfile.

Networking:

    Both services share the same internal network.

    Frontend proxies /api → backend container at http://api:5002.

    Ports:

    Backend exposed at http://localhost:5002 (Swagger available).

    Frontend exposed at http://localhost:8080.

    Run flow

    docker compose up builds & runs both services.

    User opens http://localhost:8080:

    Angular (NGINX) serves the UI.

    When Angular calls /api/..., NGINX forwards that to the backend service on port 5002.

    Backend reads/writes data from SQLite (./_data/api/app.db).
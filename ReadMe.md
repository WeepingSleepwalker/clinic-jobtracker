Clinic JobTracker
A simplified clinic scheduling + billing system built with .NET 9 (minimal APIs) and Angular 20.Core features:
* 👩‍⚕️ Appointment Scheduling – patients can book visits with doctors
* 📅 Doctor Schedule Management – doctors view daily agenda
* 💳 Billing & Invoicing – generate + pay invoices for visits
* 🔄 Transaction safety – rollback demo for failed billing
* ⚙️ Background job queue for bulk invoice updates

🚀 Backend (API)
Run locally

dotnet restore
dotnet build
dotnet run --project JobTracker.Api
* API runs on http://localhost:5002
* Swagger UI: http://localhost:5002/swagger
DB
* Uses SQLite (file app.db) by default
* In tests → in-memory SQLite
Key Endpoints
* POST /api/v1/patients – create patient
* POST /api/v1/appointments – book appointment
* GET /api/v1/doctors/{id}/agenda?date=YYYY-MM-DD – doctor schedule
* POST /api/v1/invoices – generate invoice
* POST /api/v1/appointments/{id}/complete-and-invoice – complete visit + billing (transactional)
* POST /api/v1/jobs/billing – bulk invoice job (with rollback demo)

🎨 Frontend (Angular)
Run locally

cd clinic-frontend
npm install
ng serve
* App runs on http://localhost:4200
* Make sure src/environments/environment.ts has:

export const environment = {
  apiBase: 'http://localhost:5002'
};
Pages
* Patients – create/list patients
* Appointments – schedule new appointments
* Doctor Schedule – view agenda by date/doctor
* Invoices – pay & see invoice status

✅ Tests
Uses xUnit + WebApplicationFactory with in-memory SQLite.
Run tests

dotnet test JobTracker.Api.Tests -v n
Coverage
* Appointment creation (happy path, past time, double-book, unknown patient/doctor)
* Rollback demo (complete-and-invoice failure keeps data consistent)

🛠 Design Notes
* Layering:
    * Domain → entities
    * Infrastructure → EF Core DbContext + services + jobs
    * Api → minimal APIs
    * Application → extension point for business logic (lightweight for now)
* Error handling: centralized with Angular HTTP interceptor
* Background jobs: simple in-memory queue + hosted worker, can be swapped for real broker
* Extensibility: manager/repository layers omitted for simplicity, but API vs. Jobs separation shows modularity
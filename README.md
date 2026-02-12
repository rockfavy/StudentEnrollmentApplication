## Student Course Enrollment System

This is a small end‑to‑end system I built to demonstrate how I approach designing and implementing real‑world applications. It uses a **Blazor WebAssembly** frontend and a **minimal API** backend to manage student course enrollments, and it’s intentionally structured to be easy to read, extend, and discuss in interviews or code reviews.

The focus is on clear behavior and clean architecture, not on heavy infrastructure or cloud setup, so you can clone it and start exploring in a few minutes.

### Quick Start

#### Prerequisites

- **.NET 8.0 SDK or later** – you can grab it from the official .NET download page.  
- **A code editor or IDE** – Visual Studio, VS Code, or Rider all work fine.

#### Getting up and running

1. **Clone the repo**

   ```bash
   git clone <repository-url>
   cd StudentEnrollmentApplication
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Start the API**

   ```bash
   cd src/StudentCourseEnrollment.Api
   dotnet run
   ```

   When the API is running you’ll get:

   - Swagger UI at `https://localhost:<api-port>/swagger`  
   - JSON endpoints under `/api/*`

4. **Start the frontend**

   In a **separate terminal**:

   ```bash
   cd src/StudentCourseEnrollment.Frontend
   dotnet run
   ```

   Then open the URL printed in the console (typically something like `https://localhost:5001`).

5. **Log in as the seeded admin user**

   On first run, the API seeds an admin account so you can explore the full feature set right away:

   - **Email:** `admin@example.com`  
   - **Password:** `Admin123!`

   Use this account to manage courses and see the admin‑only parts of the UI.

### Environment & configuration

For demo and interview purposes I keep the environment deliberately simple:

- **In‑memory database** – no SQL Server or external database required.  
- **JWT‑based authentication** using symmetric keys.  
- **No external identity providers** (no Azure AD / Entra ID, no social logins), and no production‑specific hosting logic.

Configuration values (for example, JWT settings) live in `appsettings.*.json` and are bound using the options pattern. If a critical configuration value is missing, the app fails fast at startup rather than failing later at runtime.

---

## Project structure

Here’s how I’ve organized the solution so it’s easy to navigate and extend.

### Root directory

```
StudentEnrollmentApplication/
├── README.md                          # You’re here
├── StudentCourseEnrollment.sln        # Visual Studio solution
├── global.json                        # .NET SDK selection (optional)
├── src/                               # Application source
│   ├── StudentCourseEnrollment.Api/       # Backend API (Minimal API)
│   ├── StudentCourseEnrollment.Frontend/  # Frontend (Blazor WebAssembly)
│   ├── StudentCourseEnrollment.Shared/    # Shared DTOs and models
│   └── tests/                           # Test projects
│       ├── StudentCourseEnrollment.Tests.Integration/
│       └── StudentCourseEnrollment.Tests.Unit/
└── StudentCourseEnrollment.slnLaunch.user # Local IDE launch settings
```

### Source code (`src/`)

#### `StudentCourseEnrollment.Api/`
This is the minimal API backend:
- **Features/** – feature‑based organization (Auth, Courses, Enrollments).  
- **Models/** – entity models such as `Student`, `Course`, and `Enrollment`.  
- **Data/** – database context plus seeding logic.  
- **Program.cs** – entry point and application wiring.

#### `StudentCourseEnrollment.Frontend/`
This is the Blazor WebAssembly frontend:
- **Components/** – Razor components (pages, shared components, layouts).  
- **Clients/** – typed API client interfaces and implementations.  
- **Services/** – services such as the authentication state provider.  
- **Helpers/** – small utilities (for example, JWT token parsing).  
- **wwwroot/** – static assets like CSS and HTML.

#### `StudentCourseEnrollment.Shared/`
This project contains types that are shared between the API and the frontend:
- **DTOs/** – data transfer objects used by the API and UI.  
- **Role.cs** – role enumeration for authorization.

#### `tests/`
I’ve split tests into unit and integration projects:
- **StudentCourseEnrollment.Tests.Integration/** – integration tests that hit the API.  
- **StudentCourseEnrollment.Tests.Unit/** – fast unit tests around core logic.

---

## Features

Here’s what the system currently supports:

- **Student registration and login** using JWT‑based authentication.  
- **Course browsing** with capacity and current enrollment counts.  
- **Course enrollment** for students, with safeguards around capacity and duplicate enrollments.  
- **Enrollment management** so students can review and deregister from their own courses.  
- **Admin course management** to create, update, and delete courses (with protections when enrollments already exist).  
- **Role‑based access control** so student and admin capabilities are clearly separated in both the API and the UI.

---

## Testing

I’ve included both unit and integration tests so you can see how I approach testing different layers.

Run **all tests**:

```bash
dotnet test
```

Run **specific test projects**:

```bash
# Unit tests
dotnet test src/tests/StudentCourseEnrollment.Tests.Unit

# Integration tests
dotnet test src/tests/StudentCourseEnrollment.Tests.Integration
```

---

## Hosted version

If you’d like to try the hosted production version of this app, send me an email at:

**rock.learn@favysoft.com**

Please include your name, email address, and a short note about why you’d like access (for example: interview preparation, code review, evaluation, etc.).

---

## License

I haven’t attached a formal license yet. If you’re interested in using this project beyond personal learning or interview review, reach out and we can clarify usage.

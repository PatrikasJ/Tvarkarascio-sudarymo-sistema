# Employee Scheduling API

The application runs in a Docker container. It supports multiple clients. Currently 2 sample clients are supported, identified by ClientId.
Each HTTP request must include X-Client-Id header with value 1 or 2.

## Requirements
* Docker Desktop
* Docker Compose

## First time setup

1. Clone the repository.
2. Open terminal in repository root.
3. Copy .env.example to .env:
```bash
Copy-Item .env.example .env
```
4. Replace placeholder JWT_KEY with a secret of at least 32 bytes.

## Starting the application

Run: 
```bash
docker compose up --build
```
Docker Compose builds the the API image in a container,
creates persistent SQLite storage, and starts the application.

API is available at http://localhost:8080

## Available commands
* Check if API is responding
```bash
curl.exe http://localhost:8080/health
```
* Stop application
```bash
docker compose down
```
* Stop application and delete stored database
```bash
docker compose down -v
```
* Run automated tests (Requires .NET SDK to be installed locally)
```bash
dotnet test
```

## API Endpoints

| Method | Endpoint             | Description                               |
|--------|----------------------|-------------------------------------------|
| POST   | /auth/login          | User login                                |
| GET    | /auth/me             | Check logged in user                      |
| POST   | /schedules/generate  | Generate and save schedule                |
| GET    | /schedules/{id}      | Retrieve schedule by id                   |
| GET    | /schedules           | Retrieve all schedules for current client |
| GET    | /settings/scheduling | Retrieve scheduling settings              |
| PUT    | /settings/scheduling | Edit scheduling settings                  |

### Example requests:
```bash
POST http://localhost:5269/schedules/generate
Authorization: Bearer YOUR_TOKEN_HERE
X-Client-Id: 1
Content-Type: application/json

{
  "periodStart": "2026-11-16T00:00:00",
  "periodEnd": "2026-11-17T00:00:00"
}
```
Response contains generated schedule ID, client ID, selected Period, shift assignments and unfilled shift IDs.

### Demo accounts
The following accounts are available when running the app in the configured Development environment:

| Email               | Role   | Organization |
|---------------------|--------|--------------|
| admin1@example.com  | Admin  | 1            |
| viewer1@example.com | Viewer | 1            |
| admin2@example.com  | Admin  | 2            |

Development password: password123

# Running the app locally
You can run the app locally, without Docker.
## Prerequisites
- .NET SDK 8.0 
- Internet connection for first NuGet package restore

## Running the application

```bash
dotnet restore
dotnet run --project src/Scheduler.Api
```
Running the API gives web address (ex. 'http://localhost:5000'). 
Database file 'scheduler.db' will be created and filled with sample data automatically.

## Running tests:

```bash
dotnet test
```
There are appriopriate tests for the algorithm itself and for api integration.
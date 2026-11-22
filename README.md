# Employee Scheduling API

The application runs in a Docker container. It supports multiple clients. 
Demo database contains 2 sample organizations, identified by ClientId.

## Requirements
* Docker Desktop
* Docker Compose

## First time setup

1. Clone the repository.
2. Open terminal in repository root.
3. Copy .env.example to .env:
```powershell
Copy-Item .env.example .env
```
4. Replace placeholder JWT_KEY with a secret of at least 32 bytes.

## Starting the application

Run: 
```bash
docker compose up --build
```
Docker Compose builds the API image in a container,
creates persistent SQLite storage, and starts the application.

API is available at http://localhost:8080

## Available commands
* Check if API is respondingq
```powershell
curl.exe http://localhost:8080/health
```
* Stop application
```bash
docker compose down
```
* Stop application and permanently delete stored database 
```bash
docker compose down -v
```
* Run automated tests (Requires .NET SDK to be installed locally)
```bash
dotnet test
```

## API Endpoints

### Authentication and organization access

The API uses JWT Bearer authentication.

Users log in through POST /auth/login using their email and password. The API returns an access token that must be included in subsequent protected requests:
```http
Authorization: Bearer YOUR_TOKEN_HERE
```
Scheduling and settings endpoints also require the X-Client-Id header to identify the organization:
```http
X-Client-Id: 1
```
The server verifies that the authenticated user belongs to the requested organization.
Changing the header does not grant access to another organization.

Requests without valid authentication return 401 Unauthorized.
Authenticated users attempting to access an unauthorized organization or perform a forbidden action receive 403 Forbidden.


| Method | Endpoint             | Description                               | Required Role      |
|--------|----------------------|-------------------------------------------|--------------------|
| POST   | /auth/login          | User login                                | None               |
| GET    | /auth/me             | Check logged in user                      | Any logged-in user |
| POST   | /schedules/generate  | Generate and save schedule                | Admin              |
| GET    | /schedules/{id}      | Retrieve schedule by id                   | Admin or Viewer    |
| GET    | /schedules           | Retrieve all schedules for current client | Admin or Viewer    |
| GET    | /settings/scheduling | Retrieve scheduling settings              | Admin or Viewer    |
| PUT    | /settings/scheduling | Edit scheduling settings                  | Admin              |
| GET    | /health              | Check whether API is responding           | None               |

### Example requests
#### Login
```http
POST http://localhost:8080/auth/login
Content-Type: application/json

{
  "email": "admin1@example.com",
  "password": "password123"
}
```
#### Generate schedule
```http
POST http://localhost:8080/schedules/generate
Authorization: Bearer YOUR_TOKEN_HERE
X-Client-Id: 1
Content-Type: application/json

{
  "periodStart": "2026-11-16T00:00:00",
  "periodEnd": "2026-11-17T00:00:00"
}
```
#### Get all schedules for the users organization
```http
GET http://localhost:8080/schedules
Authorization: Bearer YOUR_TOKEN_HERE
X-Client-Id: 1
```
#### Change scheduling settings
```http
PUT http://localhost:8080/settings/scheduling
Authorization: Bearer YOUR_TOKEN_HERE
X-Client-Id: 1
Content-Type: application/json

{
  "maxShiftsPerEmployee": 5
}
```

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
dotnet run --project Scheduler.Api
```
Running the API gives web address (ex. 'http://localhost:5000'). 
Database file 'scheduler.db' will be created and filled with sample data automatically.
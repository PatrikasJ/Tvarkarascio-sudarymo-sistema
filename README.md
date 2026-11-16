# Employee Scheduling API

The application supports multiple clients. Currently 2 sample clients are supported, identified by ClientId.
Each HTTP request must include X-Client-Id header with value 1 or 2.

# API Endpoints

| Method | Endpoint            | Description                               |
|--------|---------------------|-------------------------------------------|
| POST   | /schedules/generate | Generate and save schedule                |
| GET    | /schedules/{id}     | Retrieve schedule by id                   |
| GET    | /schedules          | Retrieve all schedules for current client |

Example request:

POST http://localhost:5269/schedules/generate
Content-Type: application/json
X-Client-Id: 1

{
  "periodStart": "2026-11-16T00:00:00",
  "periodEnd": "2026-11-17T00:00:00"
}

Response contains generated schedule ID, client ID, selected Period, shift assignments and unfilled shift IDs.

# Prerequisites
- .NET SDK 8.0 
- Internet connection for first NuGet package restore

# Running the application

```bash
dotnet restore
dotnet run --project src/Scheduler.Api
```
Running the API gives web address (ex. 'http://localhost:5000'). Database file 'scheduler.db' will be created and filled with sample data automatically.

# Running tests:

```bash
dotnet test
```
There are appriopriate tests for the algorithm itself and for api integration.

---

# Live Trivia Game

Live Trivia is a **real-time multiplayer trivia platform** where hosts create quiz rooms and players join from their own devices to answer questions live. The game is designed for synchronized gameplay and supports category voting, real-time answer submission, and live score updates. It can be used for casual play, learning general knowledge, or small competitive events.

The backend is built with **C# and ASP.NET Core (.NET 9)** using **SignalR** for real-time communication and **PostgreSQL** for data persistence. The frontend is built with **React (JavaScript)**.

Authentication is handled using **JWT**, passwords are securely hashed, and data access is managed through **Entity Framework Core**. Core logic and API behavior are covered with **xUnit** tests.

The project emphasizes **clean architecture, server-authoritative game state, and reliable real-time synchronization**.

---

## Tech Stack

* **Backend:** C#, .NET 9, ASP.NET Core, SignalR, Entity Framework Core
* **Database:** PostgreSQL
* **Frontend:** React, JavaScript, HTML5, CSS3
* **Authentication:** JWT, Password Hashing
* **Testing:** xUnit (Unit & Integration Tests)
* **Real-time Communication:** WebSockets via SignalR
* **Deployment:** Docker (optional)

---

## Features

* **Real-time Multiplayer Rooms** – Hosts create rooms and players join via code or link. Supports public/private rooms with optional passwords.
* **Category Voting System** – Players vote on available trivia categories before the game starts. A category is selected based on voting results.
* **Answer Submission** – Players submit answers from their own devices. Answers are locked once submitted or when the question advances.
* **User Authentication** – Registered user accounts using JWT-based authentication with securely hashed passwords.
* **Live Leaderboard** – Scores update in real time during gameplay. Maximum 100 points per game are awarded based on speed and accuracy of answers.
* **Global Leaderboards** – Public leaderboards showing top players overall and by category.
* **Public Player Statistics** – Publicly accessible player statistics including accuracy, categories played, and overall performance.
* **Game Summary** – End-of-game overview showing final rankings and question results.
* **Planned Features** – Team-based gameplay with shared scores and other collaborative modes.

> **Note:** Points are **purely time-based**, up to 100 per game; question difficulty does **not** affect scoring.

---

## Architecture

The application follows a **clean architecture** approach:

* **Controllers** – Handle HTTP requests and validation
* **Services** – Core business logic and game rules
* **Repositories** – Data access via Entity Framework Core
* **Hubs** – SignalR hubs for real-time communication
* **Models / DTOs** – Data representation and transfer
* **Tests** – Unit, integration, and controller tests

Game state is managed **server-side** and synchronized to connected clients via SignalR to ensure consistency and prevent client-side manipulation.

---

## File Structure

```text
live_trivia/
├── README.md                  # Project documentation
├── live_trivia.sln            # .NET solution file
├── frontend/                  # React frontend application
│   ├── public/                # Static assets (HTML, images, icons)
│   └── src/                   # React source code (components, pages, utils)
│
├── LiveTriviaBackend/         # ASP.NET Core backend
│   ├── Program.cs             # Application entry point
│   ├── appsettings*.json      # Configuration (DB, JWT, logging)
│   ├── Controllers/           # HTTP API endpoints
│   ├── Services/              # Core business logic
│   ├── Repositories/          # Data access layer
│   ├── Models/                # Domain and entity models
│   ├── DTOs/                  # Data Transfer Objects
│   ├── Hubs/                  # SignalR real-time hubs
│   ├── Data/                  # Database context and EF Core setup
│   ├── Migrations/            # EF Core migrations
│   └── Properties/            # Launch and environment settings
│
└── LiveTriviaBackend.Tests/   # Test project
    ├── ControllerTests/       # API controller tests
    ├── ServiceTests/          # Business logic tests
    ├── HubTests/              # SignalR hub tests
    └── Integration/           # End-to-end integration tests
```

---

## Getting Started

### Prerequisites

* .NET 9 SDK
* PostgreSQL
* Node.js
* npm or yarn

### Database Setup

1. Ensure PostgreSQL is running.
2. Create the database:

```bash
sudo -u postgres createdb livetrivia
# Or using psql:
psql -U postgres -c "CREATE DATABASE livetrivia;"
```

3. Update `LiveTriviaBackend/appsettings.json` with your database credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=livetrivia;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Key": "your-secret-here",
    "Issuer": "live-trivia",
    "Audience": "live-trivia-users"
  }
}
```

### Backend Setup

```bash
cd LiveTriviaBackend
dotnet restore
dotnet run
```

Backend runs at: `http://localhost:5216`

### Frontend Setup

```bash
cd frontend
npm install
npm start
```

Frontend runs at: `http://localhost:3000`

---

## Usage

### Creating a Game

1. Register or log in as a host
2. Create a game room and configure settings
3. Share the room link or code with players

### Joining a Game

1. Enter the room link or code
2. Choose a nickname or log in
3. Vote on categories (if enabled)
4. Answer questions in real time

Live leaderboard updates during the game and **final results are shown at the end**.

---

## API Documentation

Swagger UI is available at `/swagger`.

### Authorization

Most API endpoints require a valid JWT token in the `Authorization` header.

#### Public Endpoints

* `POST /auth/register`
* `POST /auth/login`
* `GET /games/active`
* Leaderboard endpoints
* `GET /questions`, `GET /questions/random`, `GET /questions/category/{category}`
* Statistics endpoints

### Key Endpoints

| Method | Endpoint               | Description       |
| -----: | ---------------------- | ----------------- |
|   POST | /auth/register         | User registration |
|   POST | /auth/login            | User login        |
|    GET | /games/{roomId}        | Get game details  |
|   POST | /games/{roomId}        | Create game       |
| DELETE | /games/{roomId}        | Delete game       |
|   POST | /games/{roomId}/start  | Start game        |
|   POST | /games/{roomId}/next   | Next question     |
|   POST | /games/{roomId}/join   | Join game         |
|   POST | /games/{roomId}/answer | Submit answer     |
|    GET | /leaderboard/top       | Top players       |
|    GET | /statistics/player     | Player statistics |

---

## Development Notes

### Granting Admin Access (Development Only)

```sql
UPDATE "Users"
SET "IsAdmin" = true
WHERE "Username" = 'your-username';
```

---

## Testing

```bash
cd LiveTriviaBackend.Tests
dotnet test
```

Includes **unit, integration, and controller tests**. Coverage reports are generated in the `coverage` directory.

---
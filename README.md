# GoodDeeds-Initial
GoodDeeds – Initial Project Challenge You have been hired by a new software company called GoodDeeds. GoodDeeds has a simple mission: Make it easier for people to help other people. Your team has been asked to design and develop a software product that helps connect people who want to help with people or organizations that need help.

# Startup
## Development
To start up the whole program in development mode so you may make live changes and debug the program, do as follows:
- Start Docker
- Navigate to ../(project_file)/Docker
- Run `docker compose -f docker-compose.dev.yml up -d` in the `Docker` folder. This initializes and runs both databases.
- Open an IDE to debug .NET solutions.
- Open API solution.
- Run API in HTTP mode.
- Open Webapp in whatever editor you choose.
- Ensure `Node` and `npm` are installed on your machine.
- Run `npm install` then `npm run dev` in terminal within the `WebUi` folder to start the Vite dev server.

Now you are set to debug and run the application.

## Production/Presentation
To start the program with the purpose of presenting the application:
- Start Docker
- Run `docker compose -f Docker/PresentationCompose.yaml up --build` withen the `Docker` folder. This will initialize and run both databases, as well as start up the api and webapp and expose them on ports 5160 and 5173.

Now the program is fully up and running. If first time running, give db time to get initialized by API.

# Technologies Used

## API
- .NET 10 (C#)
- Entity Framework
- Dependency Injection
- Model, Service, Controller API architecture.

## Database
- Postgres: SQL database for long lived information.
- Redis: In Memory Data Cache for short lived information accessed quickly.
### Entity Framework Within Database
Entity framework is used to initialize the whole database from C# classes. In the API there is a folder called `Data`. In that folder, there is a class called `AppDbContext.cs` this file contains the classes that will be initialized in the DB and the relations of those classes.

Entity framework, on first boot, will run `migrations` on the database to initialize the tables. From then on, when interacting with the db to pull information, you can use Entity Framework to treat the DB as a standard Object and run methods on it to return `Entities`. Check the Service layer for this implimentation.

## WebUi/Frontend
- React.js

## Deployment
- Docker
- Docker Compose

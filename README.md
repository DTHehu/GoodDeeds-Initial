# GoodDeeds-Initial
GoodDeeds – Initial Project Challenge You have been hired by a new software company called GoodDeeds. GoodDeeds has a simple mission: Make it easier for people to help other people. Your team has been asked to design and develop a software product that helps connect people who want to help with people or organizations that need help.

# Startup
## Development
To start up the whole program in development mode so you may make live changes and debug the program, do as follows:
- Start Docker
- Run `docker compose -f docker-compose.dev.yml up -d` in the `Docker` folder. This initializes and runs both databases.
- Open an IDE to debug .NET solutions.
- Open API solution.
- Run API in HTTP mode.
- Open Webapp in whatever editor you choose.
- Ensure `Node` and `npm` are installed on your machine.
- Run `npm run dev` in terminal withen the `WebUi` folder to start up the webui in dev mode.

Now you are set to debug and run the application.

## Production/Presentation
To start the program with the purpose of presenting the application:
- Start Docker
- Run `docker compose -f Docker/PresentationCompose.yaml up --build` withen the `Docker` folder. This will initialize and run both databases, as well as start up the api and webapp and expose them on ports 5160 and 5173.

Now the program is fully up and running. If first time running, give db time to get initialized by API.
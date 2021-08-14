# Random CPF scoring API

Test case for an API using VS-Code remote containers for development and docker for running the app, based on SAFE Template

## Starting the application

If you already have Docker installed, running `docker-compose up` is enough to have a frontend running on `http://localhost:8085/`

## Developing

I recommend using the [Remote - Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension as it will already download most of the need requisites for developing. After that and opening the workspace inside the container you can restore the added dotnet tools by running:

```bash
dotnet tool restore
```

To concurrently run the server and the client components in watch mode use the following command:

```bash
dotnet run
```

Then open `http://localhost:8080` in your browser.

To run concurrently server and client tests in watch mode (you can run this command in parallel to the previous one in new terminal):

```bash
dotnet run -- RunTests
```

Client tests are available under `http://localhost:8081` in your browser and server tests are running in watch mode in console.

## Used libraries (outside SAFE Template)

- ThrowawayDb: For creating dummy databases for tests to run
- DbUp: For handling the database migrations
- Npgsql: For connecting on the PostgreSQL database
- Thoth.Fetch: For making HTTP requests
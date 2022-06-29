# Introduction

The repository holds a solution containing a console app that fetches weather data from an external data
source and persists it on a connected storage.

# Build & Run

To build the app, run the following command.
```shell
dotnet build .\src\app\SunshineExpress.WeatherApp.csproj
```

To run the app with a certain list of cities to fetch the weather, run the following commands.
```shell
cd src\app\bin\Debug\net6.0
.\SunshineExpress.WeatherApp.exe --cities Vilnius,Kaunas,Klaipeda
```
*Note: The app must be run in the same folder where the `appSettings.json` is located*

The list of cities specified as the `--cities` argument must be comma-separated. It gets validated to ensure only supported cities are provided.

# Repository structure

The following graph describes the meaning of the folder stucture inside the repository.

- `src` contains all the source code.
  - `app` contains the code for the console app used to run the service.
  - `lib` contains all the reusable library-like code.
    - `service` contains the source code for the weather service.
    - `cache` contains an example implementation of the `SunshineExpress.Service.Contract.ICache` cache that is required to run the weather service.
    - `client` contains an example implementation of the `SunshineExpress.Service.Contract.ISourceClient` data source client that is required to run the weather service.
    - `storage` contains multiple example implementations of the `SunshineExpress.Service.Contract.IStorageClient` storage client, one of which is required to run the weather service.
      - `blob` contains an example implementation of Azure Blob storage used as the storage for the weather service.
      - `file` contains an example implementation of local file system used as the storage for the weather service.
  - `test` contains the unit test projects.
  
  
# Customizing
The weather service can run with custom implementations of `ICache`, `ISourceClient`, `IStorageClient`, yet exactly one implementation of each is required for the service to run.
Please check the `src/app/Program.cs` to get the idea of how the wiring up is performed on the startup of the program.

# Logging
By default, logging is set to be stored in a text file `[app base dir]/logs/[Date].log`, the location can be changed
in the `appSettings.json` file.
Different kind of logging can be set up by adjusting the Host configuration in the `src/app/Program.cs` file.
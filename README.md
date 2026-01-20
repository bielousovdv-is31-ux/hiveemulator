# Hive Emulator

- [Hive Emulator](#hive-emulator)
  - [About](#about)
  - [Installation](#installation)
    - [Redis](#redis)
    - [Map Component](#map-component)
    - [Communiction Control](#communiction-control)
    - [Hive Mind](#hive-mind)
  - [Usage](#usage)
  - [Build](#build)
    - [Map Clinet](#map-clinet)
    - [Communiction Control](#communiction-control-1)
    - [Hive Mind](#hive-mind-1)
    - [Communiction Control](#communiction-control-2)

## About
This is a demo project used in the Uni DevOps course

## Installation

### Redis
```bash
docker run --name redis -d -p 6379:6379 redis
```

### Map Component
```bash
cd src/MapClient

npm install

npm run dev
```

### Communiction Control
```bash
cd src/CommunicationControl

dotnet run  --project DevOpsProject/DevOpsProject.CommunicationControl.API.csproj
```

### Hive Mind
```bash
cd src/CommunicationControl

dotnet run  --project DevOpsProject.HiveMind.API/DevOpsProject.HiveMind.API.csproj
```

### Drone
```bash
cd src/CommunicationControl

$env:IP_ADDRESS = "127.0.0.1"
$env:UDP_PORT = 5382
dotnet run --no-launch-profile --project DevOpsProject.Drone.API/DevOpsProject.Drone.API.csproj --urls="http://localhost:5282" -- DroneInitialState:Id="1"
```

You can deploy the second drone in another terminal window in the following way:
```bash
cd src/CommunicationControl

$env:IP_ADDRESS = "127.0.0.1"
$env:UDP_PORT = 5383
dotnet run --no-launch-profile --project DevOpsProject.Drone.API/DevOpsProject.Drone.API.csproj --urls="http://localhost:5283" -- DroneInitialState:Id="2" DroneInitialState:Type="Striker" DroneInitialState:Location:Latitude=48.7 DroneInitialState:Location:Longitude=38

```


## Usage

1. Map Control is available at http://localhost:3000
2. Redis - Get available keys:
   ```bash
        docker exec -it redis redis-cli
        keys *
        get [hiveKey]
    ```

3. Communication Control Swagger: http://localhost:8080

## Build

### Map Clinet
cd src/MapClient

npm install
npm run build

### Communiction Control
cd src/CommunicationControl
dotnet publish -p:PublishProfile=FolderProfile --artifacts-path=build/CommunicationControl DevOpsProject/DevOpsProject.CommunicationControl.API.csproj 

### Hive Mind
### Communiction Control
cd src/CommunicationControl
dotnet publish -p:PublishProfile=FolderProfile --artifacts-path=build/HiveMind DevOpsProject/DevOpsProject.HiveMind.API.csproj
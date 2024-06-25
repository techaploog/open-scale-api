# Scale API for FPN

## Setup Instructions

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.

### Clone the Repository

```bash
git clone https://github.com/techaploog/fpn-scale-api
cd fpn-scale-api
```

### Restore Dependencies

```bash
dotnet restore
```

### Build project

```bash
dotnet build
```

### Publich for Windows x64.

```bash
dotnet publish -c Release -r win-x64 --self-contained
# builded path: .\fpn-scale-api\bin\Release\net8.0\win-x64\publish
```

### Configuration File (config.txt)

```text
PortMapping=uuid1:COM5,uuid2:COM6                   # Map UUIDs to COM ports
SampleSize=20                                       # Number of samples to collect
TimeoutMilliseconds=5000                            # Timeout in milliseconds
DefaultBaudRate=2400                                # Default baud rate for serial communication
ErrorTolerance=0.2                                  # Tolerance for sampling errors
StandardDataPattern=^=([+-]?\d+\.\d+)\((\w+)\)$     # Regex pattern for parsing scale data
HttpUrl=http://0.0.0.0:5000                         # Base URL for the API

```

### Running the Application

```bash
cd .\fpn-scale-api\bin\Release\net8.0\win-x64\publish
.\fpn-scale-api.exe
```

### Get Data from scale

```bash
curl http://localhost:5000/scale/uuid1
```

### Health check for scale

```bash
curl http://localhost:5000/scale/uuid1/health
```

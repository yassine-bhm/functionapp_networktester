# Network Connectivity Test - Azure Function

An Azure Function HTTP Trigger designed to test network connectivity and SSL/TLS connections.

## Features

✅ **HTTP Trigger** - Triggered by HTTP GET request  
✅ **Flex Consumption Plan** - Optimized deployment  
✅ **HTML Output** - Formatted and easily readable results  
✅ **Flexible Configuration** - Via environment variables or query parameters  
✅ **Comprehensive Diagnostics** - DNS resolution, TCP, SSL/TLS and server greeting  

## Prerequisites

- .NET 8.0 SDK
- Azure CLI
- Azure Storage Account (for local development)

## Installation and Local Execution

### 1. Install Dependencies

```bash
dotnet restore
```

### 2. Configure local.settings.json

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "SERVER_FQDN": "imap.gmail.com",
        "SERVER_PORT": "993"
    }
}
```

### 3. Start the Function Locally

```bash
func start
```

## Usage

### HTTP Request

**GET** `http://localhost:7071/api/ConnectivityTest`

**Query Parameters (optional):**
```
server=imap.gmail.com
port=993
```

**Examples:**
```bash
# With default values
http://localhost:7071/api/ConnectivityTest

# With custom parameters
http://localhost:7071/api/ConnectivityTest?server=imap.gmail.com&port=993

# Different server
http://localhost:7071/api/ConnectivityTest?server=smtp.gmail.com&port=465
```

### Success Response (200)

Returns a styled HTML page with:
- Test summary (server, port, status)
- Complete formatted output with all test steps
- Dark mode theme with syntax highlighting

### Error Response (500)

Returns an HTML page with:
- Error details
- Test output before failure
- Stack trace information

## Output Details

The HTML output includes:
- ✅ DNS resolution with resolved IP addresses
- ✅ TCP connection establishment
- ✅ SSL/TLS handshake details (protocol, cipher, certificate info)
- ✅ Server greeting response
- ✅ Complete test summary

## Deployment

### 1. Build the Project

```bash
dotnet publish -c Release -o ./publish
```

### 2. Create an Azure Function App (Flex Consumption Plan)


## Environment Variables Configuration

```bash
az functionapp config appsettings set \
  --name <FunctionAppName> \
  --resource-group <ResourceGroupName> \
  --settings SERVER_FQDN="imap.gmail.com" SERVER_PORT="993"
```

## Architecture

```
ConnectivityTest HTTP Trigger (GET)
    ↓
    → DNS Resolution
    → TCP Connection
    → SSL/TLS Handshake
    → Read Server Greeting
    ↓
HTML Formatted Response
```

## Troubleshooting

### Connection Issues?
- Check `SERVER_FQDN` and `SERVER_PORT` parameters
- Verify firewall/NSG rules
- Ensure the target port is accessible

### Function Not Starting?
- Verify .NET 8.0 SDK is installed
- Check Azure Functions Core Tools version
- Review local.settings.json configuration

## Technology Stack

- **Runtime:** .NET 8.0 (Isolated Worker Model)
- **Framework:** Azure Functions v4
- **Output:** HTML with dark mode styling
- **Plan:** Flexible Consumption Plan

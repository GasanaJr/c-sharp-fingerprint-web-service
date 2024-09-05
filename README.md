# Fingerprint Web Service

## Overview

This project is a C#-based web service designed to integrate with fingerprint sensors to capture fingerprints and send them to a designated web application. The service includes an installer that ensures the service runs automatically on startup.

### Features
- **Fingerprint Capture:** Captures fingerprints from compatible sensors (e.g., Z9500).
- **Web Integration:** Sends captured fingerprint data to the specified web application for processing and storage.
- **Automatic Startup:** The service runs automatically at system startup after installation.

## Prerequisites

- **.NET Framework 4.x / .NET 6+** (depending on your project)
- **Fingerprint Sensor Drivers:** Ensure the drivers for your fingerprint sensor (e.g., Z9500) are installed and correctly configured.
- **Administrator Rights:** Required to install the service and configure it to run at startup.
- **IIS / Web Server** (Optional for local hosting of the web service)

## Installation

### 1. Download the Installer
- Download the setup executable from the [Releases](#) section.

### 2. Install the Service
- Run the installer as an administrator.
- The installer will:
  - Install the fingerprint capture web service.
  - Configure the service to run automatically at system startup.

### 3. Start the Service
- After installation, the service will start automatically.
- You can also manually start/stop the service using the **Services** management tool (`services.msc`).

### 4. Sensor Configuration
- Ensure the fingerprint sensor is connected and the appropriate drivers are installed.
- Test that the sensor is correctly capturing fingerprints.

## Configuration

The service configuration (e.g., web application endpoint) can be found in the `appsettings.json` or `config.xml` file located in the installation directory.

### Example Configuration (`appsettings.json`):
```json
{
  "WebAppUrl": "https://your-webapp-url.com/api/fingerprint",
  "SensorTimeout": 30
}

# 🚀 Quiz Task Application Setup Guide

This document explains how to set up and run the **Quiz Task** application using Docker, the standard approach for clean environments like Ubuntu.

## Prerequisites

You must have **Docker** or **Docker Desktop** installed on your system.

## ⚙️ Configuration Setup (MongoDB)

The application requires MongoDB connection details. Configuration uses ASP.NET Core's environment variable mechanism, which **overrides** defaults found in `appsettings.json`.

### Option A: Recommended (Environment Variables via Docker)

This is the preferred method for production as it keeps sensitive data out of static files. You will pass these variables during the `docker run` command.

| Environment Variable | Description |
| :--- | :--- |
| `DatabaseSettings__ConnectionString` | The full MongoDB connection string (e.g., MongoDB Atlas URL). |
| `DatabaseSettings__DatabaseName` | The name of the database to use. |

### Option B: Local Defaults (`appsettings.json` Modification)

If you need to set local defaults:

1.  **Rename** `appsettings.Example.json` to **`appsettings.json`**.
2.  **Edit** `appsettings.json` and replace the placeholder values under `DatabaseSettings` with your local connection details.

---

## 📦 Running the Application with Docker

Follow these steps from the root directory of your project (where this `README.md` and the `Dockerfile` reside).

### Step 1: Build the Docker Image

This command compiles the .NET application inside Docker and creates the portable image named `quiz-task-app`.

```sh
docker build -t quiz-task-app .
```

To run the container with environment variables, use the following command template:
```sh
docker run -d -p 8080:8080 --name quiz-app-instance -e "DatabaseSettings__ConnectionString=mongodb+srv://your_user:your_password@your_cluster_url" -e "DatabaseSettings__DatabaseName=YourDatabaseName" quiz-task-app
```


Then on your browser go to:
```sh
localhost:8080/Test/list
```

If the application is on anorher machine replace <localhost> with the Ip addess of that machine

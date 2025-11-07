## User Management Web API

This project is a simple and efficient ASP.NET Core Web API built using the Minimal API approach. It provides full CRUD functionality for managing users, with a focus on clean architecture, performance, and middleware customization. The API uses an in-memory dictionary for fast user lookups and includes input validation to ensure data integrity.

### 🔐 Authentication & Middleware

The project features custom middleware for:
- **Global error handling**: Catches and formats unhandled exceptions.
- **Selective authentication**: Protects sensitive endpoints using a custom attribute and token-based access.
- **Request logging**: Logs HTTP method, path, and response status for each request.

Built-in middleware such as **response compression** is also included to enhance performance and security. Swagger is enabled for API documentation and testing.

### 📦 Features

- Minimal API structure with clean routing
- Custom middleware for error handling, logging, and authentication
- Response compression for faster performance
- Swagger UI for interactive API testing
- Pagination support for listing users
- `.http` file included for testing all endpoints
- `/crash` endpoint to simulate server-side errors

### 🧪 Testing

A `UserRequests.http` file is included with ready-to-run REST requests to test:
- Public and protected endpoints
- Validation errors
- Unauthorized access
- Internal server error handling

### 🤖 Powered by Microsoft Copilot

Microsoft Copilot was used throughout development to enhance code quality, optimize middleware ordering, and follow best practices for performance and scalability.

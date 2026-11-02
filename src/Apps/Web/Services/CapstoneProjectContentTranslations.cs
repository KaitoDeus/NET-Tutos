namespace NET_Tutos.Services;

public static class CapstoneProjectContentTranslations
{
    private static readonly Dictionary<string, (string RequirementsEn, string FullContentEn)> EnglishProjectContent = new(StringComparer.OrdinalIgnoreCase)
    {
        ["xay-dung-restful-web-api-quan-ly-thu-vien-sach"] = (
            RequirementsEn: @"### 🎯 Project Objectives
Build a comprehensive RESTful Web API system supporting borrowing/return workflows and book library inventory management. This project helps you master building enterprise-grade REST APIs, querying and managing data with EF Core, and documenting endpoints with Swagger/OpenAPI.

---

### 📋 Functional Requirements
1. **Books Catalog Management (Books CRUD)**:
   - `GET /api/books`: Retrieve list of books (supports pagination with `pageIndex`, `pageSize`, filtering by `genre`, and search by `title`).
   - `GET /api/books/{id}`: View book details including author info and real-time inventory availability.
   - `POST /api/books`: Add a new book (Input validation: Title cannot be empty, valid publication year, unit price > 0).
   - `PUT /api/books/{id}`: Update book information.
   - `DELETE /api/books/{id}`: Delete a book (only allowed if no active borrow records exist for this book).
2. **Member Management (Members)**:
   - Register new library members with phone number, email, and library card code.
3. **Borrowing Transactions (Borrow & Return)**:
   - `POST /api/borrow`: Create a book borrow transaction. Automatically decrement `AvailableCopies` inventory.
   - `POST /api/return`: Record book return and calculate overdue late fees if applicable.

---

### 🛡️ Technical Rubrics & Clean Code Standards
- [ ] **Data Transfer Objects (DTOs)**: Never expose Database Entities directly from Controllers. Use dedicated DTOs for Requests and Responses.
- [ ] **Repository Pattern & DI**: Decouple data access via `IBookRepository` and register Scoped Services in `Program.cs`.
- [ ] **Global Error Handling**: Centralize exception handling via `UseExceptionHandler` or custom middleware, returning standard `ProblemDetails` (RFC 7807).
- [ ] **API Documentation**: Integrate Swagger / OpenAPI with full descriptions and response code annotations for each endpoint.",

            FullContentEn: @"# Capstone Implementation Guide: Book Library RESTful API

Welcome to your first real-world capstone project on the path to becoming an **ASP.NET Core Backend Developer**!

In this project, you will act as a backend engineer responsible for designing the complete API system for a school library management application.

### 1. Recommended Project Architecture
Organize your solution with clean layered architecture:
- `LibraryApi.Domain`: Core Entities (`Book`, `Author`, `BorrowRecord`).
- `LibraryApi.Infrastructure`: `LibraryDbContext`, EF Core Migrations configuration, and data Repositories.
- `LibraryApi.Application`: Service Interfaces, DTOs, Business Validators, and AutoMapper profiles.
- `LibraryApi.Web`: ASP.NET Core Web API Controllers, Swagger OpenAPI config, and middleware pipeline.

### 2. Submission Steps
1. Create a new GitHub repository (set to Public).
2. Write clean source code with a thorough `README.md` documenting setup instructions, database connection strings, and Swagger UI screenshots.
3. Enter your GitHub repository URL into the submission panel on the right, along with an architectural summary of your design choices."
        ),

        ["he-thong-dat-hang-mini-clean-architecture-cqrs"] = (
            RequirementsEn: @"### 🎯 Project Objectives
Build an enterprise-grade Ordering Module using Clean Architecture. The project focuses on Separation of Concerns, automated Unit Testing, and high architectural scalability.

---

### 📋 Functional Requirements
1. **Cart & Product Inventory**:
   - Verify stock availability before confirming orders.
   - Calculate total order amount, VAT tax, and discount coupons.
2. **Order Processing Workflows**:
   - `CreateOrderCommand`: Create a new order with `Pending` status.
   - `CancelOrderCommand`: Cancel an order and restore inventory if the order has not yet shipped.
   - `GetOrderByIdQuery` & `GetOrdersByCustomerQuery`: Optimize read queries using EF Core `AsNoTracking`.
3. **Domain Events & Side Effects**:
   - Upon successful order placement: Raise an `OrderCreatedDomainEvent` to trigger confirmation notifications and inventory reservations.

---

### 🛡️ Technical Rubrics & Clean Code Standards
- [ ] **Clean Architecture Layers**: Independently separate 4 layers: `Domain`, `Application`, `Infrastructure`, `WebApi`. The Domain layer must have zero dependencies on 3rd-party libraries.
- [ ] **CQRS Pattern**: Separate write operations (Commands) and read queries (Queries) utilizing MediatR handlers.
- [ ] **Validation Pipeline**: Implement automatic input validation using MediatR Pipeline Behaviors integrated with FluentValidation.
- [ ] **Unit Tests**: Minimum of 5 Unit Tests covering domain pricing rules and order cancellation logic with `xUnit` and `Moq` (Test coverage $\ge 70\%$).",

            FullContentEn: @"# Capstone Implementation Guide: Ordering System Clean Architecture

This capstone is a comprehensive test of modern enterprise software architecture: **Clean Architecture & Domain-Driven Design (DDD)**.

### 1. Key Architectural Milestones
- **Domain-Centric Design**: Core business rules and entities reside strictly in the Domain layer without infrastructure pollution.
- **Fail-Fast Pipeline**: Intercept validation failures early in MediatR pipeline behaviors before requests ever reach the database or core handlers.
- **Unit Testing First**: Safeguard purchasing logic, discount calculations, and inventory rollbacks with automated test suites.

### 2. Submission Instructions
1. Initialize your project from clean architecture templates or build from scratch.
2. Push your source code with unit test results and architecture diagrams to a Public GitHub repository.
3. Submit the repository link with a brief explanation of how you organized CQRS commands and domain events."
        ),

        ["nen-tang-dau-gia-truc-tuyen-realtime-signalr"] = (
            RequirementsEn: @"### 🎯 Project Objectives
Build a high-performance real-time online auction platform. Hundreds of concurrent bidders can participate in live auctions, see instantaneous bid increments without page reloads, and rely on automated session settlement when time expires.

---

### 📋 Functional Requirements
1. **Live Auction Room**:
   - SignalR Hub group connection partitioned by `AuctionRoom_{id}`.
   - Real-time countdown timer synchronized with sub-second accuracy between the server clock and all connected clients.
2. **Real-Time Bidding Workflows**:
   - Users submit bids $\ge$ current highest bid + minimum bid increment.
   - Broadcast updated price and winning bidder information to all room participants with $< 100\text{ms}$ latency.
3. **Automated Session Finalization (Background Worker Service)**:
   - Leverage `.NET BackgroundService` running periodic checks to detect expired auctions, transition statuses to `Ended`, declare the winner, and dispatch notifications.

---

### 🛡️ Technical Rubrics & Clean Code Standards
- [ ] **Concurrency Control**: Enforce `[Timestamp]` / `RowVersion` concurrency tokens in EF Core to handle race conditions when concurrent bids arrive within the same millisecond.
- [ ] **SignalR Groups & Connection Management**: Safe join/leave room management and accurate live participant count tracking.
- [ ] **Background Processing**: Decouple auction expiration polling into an independent Hosted Service outside HTTP web request threads.
- [ ] **Resilience**: Implement SignalR automatic reconnection policies (`withUrl().withAutomaticReconnect()`) for fluctuating network environments.",

            FullContentEn: @"# Capstone Implementation Guide: Live Auction Real-time Platform

This advanced capstone project tests your ability to engineer real-time systems combining multithreaded backend pipelines, WebSocket communication via SignalR, and financial-grade database concurrency control.

### 1. Architectural Highlights
- **High Concurrency Bidding**: Use Optimistic Concurrency Control to guarantee that only valid, non-stale bids are accepted into the ledger.
- **Resilient WebSockets**: Seamlessly recover active state and countdown timers on client reconnection.
- **Worker Services**: Manage time-sensitive auction completions via Hosted BackgroundServices.

### 2. Submission Instructions
1. Push your solution along with setup scripts and demo video/screenshots to a Public GitHub repo.
2. Include explanations of your Concurrency Exception handling strategy in your submission notes."
        )
    };

    public static string? GetRequirementsMarkdownEn(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        if (EnglishProjectContent.TryGetValue(slug, out var content))
            return content.RequirementsEn;
        return null;
    }

    public static string? GetFullContentMarkdownEn(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        if (EnglishProjectContent.TryGetValue(slug, out var content))
            return content.FullContentEn;
        return null;
    }
}

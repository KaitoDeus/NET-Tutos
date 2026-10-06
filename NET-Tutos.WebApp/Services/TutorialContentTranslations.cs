namespace NET_Tutos.Services;

public static class TutorialContentTranslations
{
    private static readonly Dictionary<string, string> EnglishMarkdown = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong"] = @"# .NET Ecosystem Overview & Environment Setup

## 1. What is .NET?
**.NET** is a free, cross-platform, open-source developer platform created and maintained by Microsoft. With .NET, you can build diverse kinds of applications: Web, Mobile, Desktop, Cloud, Gaming, IoT, and Artificial Intelligence (AI).

### Distinguishing Common Terminology:
- **.NET Framework (up to 4.8)**: The legacy Windows-only implementation. Feature development has concluded; Microsoft maintains it for security updates only.
- **.NET Core (1.0 - 3.1)**: Microsoft's cross-platform revolution, running natively across Windows, Linux, and macOS.
- **Modern .NET (.NET 5, 6, 7, 8, 9, 10...)**: The unified platform combining all .NET workloads. The name was officially streamlined to **.NET** followed by the version number.

```
┌─────────────────────────────────────────────────────────────┐
│                      .NET Applications                      │
│  Web (ASP.NET) │ Desktop (WPF/MAUI) │ Cloud │ Mobile │ AI   │
├─────────────────────────────────────────────────────────────┤
│         Base Class Library (BCL) & Common APIs              │
├─────────────────────────────────────────────────────────────┤
│             Common Language Runtime (CLR / CoreCLR)         │
│          (GC Memory Management, JIT Compiler, Threads)      │
├─────────────────────────────────────────────────────────────┤
│             Operating Systems: Windows │ Linux │ macOS      │
└─────────────────────────────────────────────────────────────┘
```

## 2. Core Components of .NET
1. **CLR (Common Language Runtime)**: The execution engine and virtual machine that executes .NET code, handling automatic **Garbage Collection (GC)**, Just-In-Time (JIT) compilation from Intermediate Language (IL) to native machine code, and type safety guarantees.
2. **BCL (Base Class Library)**: The rich, standard class library providing out-of-the-box support for strings, file I/O, networking, collections, cryptography, multithreading, and more.
3. **C# Language**: A modern, type-safe, object-oriented programming language, the most prominent language used on .NET.

## 3. Setting Up the Development Environment
To get started, you will need:
1. **.NET SDK**: Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).
2. **IDE / Code Editor**:
   - **Visual Studio Community**: Comprehensive IDE with rich visual tooling.
   - **VS Code**: Lightweight and modular, paired with the **C# Dev Kit** extension.
   - **Antigravity / JetBrains Rider**: Advanced modern IDEs with powerful AI integrations.

### Verify Installation in Terminal / PowerShell:
```bash
# Check installed SDK version
dotnet --version

# View detailed runtime and environment information
dotnet --info
```

### Creating Your First Project (Console App):
```bash
# Create a new Console application
dotnet new console -n HelloWorld

# Navigate into project directory
cd HelloWorld

# Run application
dotnet run
```

### First `Program.cs` Source Code:
```csharp
// Top-level statements in modern C#
Console.WriteLine(""Hello, .NET World! Welcome to your learning journey!"");

int currentYear = DateTime.Now.Year;
Console.WriteLine($""Current year is: {currentYear}"");
```

> **Tip**: Always prioritize using LTS (Long Term Support) releases like .NET 8 or modern releases like .NET 10 to ensure the highest performance and latest language features.",

        ["cu-phap-csharp-nen-tang-bien-kieu-du-lieu-luong-dieu-khien"] = @"# Core C# Syntax: Variables, Data Types & Control Flow

## 1. Data Types and Variable Declarations
C# is a **statically typed** language. Every variable must have a declared type before use.

### Primitive Data Types:
- Integers: `int` (32-bit), `long` (64-bit), `short` (16-bit), `byte` (8-bit)
- Floating-point: `double` (scientific standard), `float`, `decimal` (128-bit high precision, mandatory for financial/monetary calculations)
- Characters & Strings: `char` ('A'), `string` (""Hello"")
- Booleans: `bool` (`true` or `false`)

### Declaration Examples:
```csharp
int age = 22;
double gpa = 8.75;
decimal tuitionFee = 15000000.50m; // Note the 'm' suffix
bool isStudent = true;
string fullName = ""Alex Johnson"";

// Using the 'var' keyword - Compiler infers type (Type Inference)
var city = ""Seattle"";  // string
var quantity = 100;    // int
```

### Nullable Value Types:
In C#, value types cannot be `null` by default. Append `?` to declare them as nullable:
```csharp
int? testScore = null; // Allows absence of a value
if (testScore.HasValue)
{
    Console.WriteLine($""Score: {testScore.Value}"");
}
else
{
    Console.WriteLine(""No test score recorded yet."");
}

// Null-coalescing operator (??)
int finalScore = testScore ?? 0; // Default to 0 if null
```

## 2. Conditional Control Flow

### `if - else` Statements:
```csharp
int score = 85;

if (score >= 90)
{
    Console.WriteLine(""Excellent"");
}
else if (score >= 80)
{
    Console.WriteLine(""Good"");
}
else
{
    Console.WriteLine(""Average / Pass"");
}
```

### Switch Expressions (Modern C#):
```csharp
string grade = score switch
{
    >= 90 => ""Excellent"",
    >= 80 => ""Good"",
    >= 65 => ""Fair"",
    >= 50 => ""Average"",
    _ => ""Poor"" // Default case
};

Console.WriteLine($""Academic grade: {grade}"");
```

## 3. Loops in C#

```csharp
// 1. Classic for loop
for (int i = 1; i <= 5; i++)
{
    Console.WriteLine($""Iteration: {i}"");
}

// 2. Foreach loop iterating collections
string[] languages = { ""C#"", ""F#"", ""VB.NET"", ""SQL"" };
foreach (var item in languages)
{
    Console.WriteLine($""Language: {item}"");
}

// 3. while and do-while loops
int count = 0;
while (count < 3)
{
    Console.WriteLine($""Count: {count}"");
    count++;
}
```

> **Key Rule**: Always use `decimal` for monetary values in .NET to prevent floating-point rounding errors associated with `double`/`float`.",

        ["cau-truc-du-lieu-xu-ly-chuoi-list-dictionary-stringbuilder"] = @"# Data Structures & Strings in .NET: List, Dictionary, Array & StringBuilder

## 1. Fixed Arrays vs Dynamic Collections (`List<T>`)

### Static Arrays (Array):
Arrays have a fixed size upon initialization and provide $O(1)$ indexed access:
```csharp
int[] numbers = new int[3] { 10, 20, 30 };
Console.WriteLine(numbers[0]); // 10
Console.WriteLine($""Length: {numbers.Length}"");
```

### Dynamic Lists (`List<T>`):
In production, you will use `List<T>` (from `System.Collections.Generic`) most frequently due to its flexible dynamic resizing:
```csharp
List<string> studentNames = new List<string>();

// Adding items
studentNames.Add(""Alex Johnson"");
studentNames.Add(""Sarah Connor"");
studentNames.Add(""David Miller"");

// Membership checks and removals
if (studentNames.Contains(""Alex Johnson""))
{
    Console.WriteLine(""Student found in roster!"");
}

studentNames.Remove(""David Miller"");
Console.WriteLine($""Total students: {studentNames.Count}"");
```

## 2. Key-Value Dictionaries (`Dictionary<TKey, TValue>`)
Store key-value pairs utilizing an underlying Hash Table with an average lookup complexity of $O(1)$:
```csharp
Dictionary<string, string> glossary = new Dictionary<string, string>
{
    { ""OOP"", ""Object-Oriented Programming"" },
    { ""DI"", ""Dependency Injection"" },
    { ""EF"", ""Entity Framework"" }
};

// Add item
glossary[""CLR""] = ""Common Language Runtime"";

// Safe lookup with TryGetValue
if (glossary.TryGetValue(""OOP"", out string? definition))
{
    Console.WriteLine($""OOP stands for: {definition}"");
}
```

## 3. Optimizing String Manipulation with `StringBuilder`
In C#, `string` is **immutable**. Every time you concatenate strings using the `+` operator, a new string allocation occurs on the Heap, putting unnecessary pressure on the Garbage Collector.

When concatenating many strings in loops, always use `StringBuilder`:
```csharp
using System.Text;

var sb = new StringBuilder();
for (int i = 1; i <= 1000; i++)
{
    sb.Append($""Line {i}; "");
}

string result = sb.ToString();
```

> **Best Practice**: For a small number of concatenations (< 4 items), use String Interpolation `$""{a} {b}""`. In repetitive loops, always use `StringBuilder`.",

        ["4-tru-cot-lap-trinh-huong-doi-tuong-oop-trong-csharp"] = @"# The 4 Pillars of Object-Oriented Programming (OOP) in C#

Object-Oriented Programming (OOP) is the foundational architectural paradigm of C# and the .NET ecosystem.

## 1. Encapsulation
Hiding internal object state and exposing safe methods and properties for access and mutation:

```csharp
public class BankAccount
{
    // Private backing field
    private decimal _balance;

    // Public property with validation
    public decimal Balance
    {
        get { return _balance; }
        private set 
        { 
            if (value >= 0) _balance = value; 
        }
    }

    public string AccountNumber { get; }

    public BankAccount(string accNum, decimal initialBalance)
    {
        AccountNumber = accNum;
        _balance = initialBalance > 0 ? initialBalance : 0;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException(""Deposit amount must be greater than zero."");
        _balance += amount;
    }
}
```

## 2. Inheritance
Permits a derived child class to inherit fields, properties, and behaviors from a base parent class, promoting code reuse:

```csharp
// Base class
public class Person
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public void Introduce()
    {
        Console.WriteLine($""Hello, I am {FullName}"");
    }
}

// Derived child class
public class Student : Person
{
    public string StudentId { get; set; } = string.Empty;
    public double Gpa { get; set; }
}
```

## 3. Polymorphism
Permits objects of different classes to respond differently to the same method invocation. In C#, use `virtual` in the base class and `override` in derived classes:

```csharp
public class Animal
{
    public virtual void MakeSound()
    {
        Console.WriteLine(""Generic animal sound..."");
    }
}

public class Dog : Animal
{
    public override void MakeSound()
    {
        Console.WriteLine(""Woof woof!"");
    }
}

public class Cat : Animal
{
    public override void MakeSound()
    {
        Console.WriteLine(""Meow meow!"");
    }
}

// Utilizing polymorphism:
List<Animal> animals = new() { new Dog(), new Cat(), new Animal() };
foreach (var animal in animals)
{
    animal.MakeSound(); // Each animal executes its specific behavior
}
```

## 4. Abstraction
Focuses on essential capabilities rather than low-level implementation details, achieved through **Abstract Classes** or **Interfaces**:

```csharp
public abstract class PaymentProcessor
{
    public abstract void ProcessPayment(decimal amount);
}

public class StripePaymentProcessor : PaymentProcessor
{
    public override void ProcessPayment(decimal amount)
    {
        Console.WriteLine($""Processing Stripe payment of: ${amount:N2} via API gateway."");
    }
}
```",

        ["interface-va-generics-trong-csharp-hien-dai"] = @"# Interfaces & Generics in Modern C#

## 1. What is an Interface?
An **Interface** represents a contractual agreement. Any class implementing an interface pledges to provide concrete implementations for the declared methods, properties, and events.

### Advantages of Interfaces:
- Allows a class to implement multiple interfaces (overcoming C#'s single class inheritance limitation).
- Enforces loose coupling, acting as the foundation of **Dependency Injection (DI)**.

```csharp
public interface INotificationService
{
    void Send(string recipient, string message);
}

public class EmailNotificationService : INotificationService
{
    public void Send(string recipient, string message)
    {
        Console.WriteLine($""Sending Email to {recipient}: {message}"");
    }
}

public class SmsNotificationService : INotificationService
{
    public void Send(string recipient, string message)
    {
        Console.WriteLine($""Sending SMS to {recipient}: {message}"");
    }
}
```

## 2. Generics in C#
Generics allow you to define classes, interfaces, and methods with placeholder type parameters (typically denoted as `T`).

### Benefits of Generics:
1. **Type Safety**: Enforces compile-time type verification.
2. **Performance**: Eliminates **Boxing / Unboxing** overhead between value types and reference types.
3. **Reusability**: Write once, operate over any type cleanly.

```csharp
// Generic Repository Pattern
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task DeleteAsync(int id);
}

// Generic API Response Wrapper
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = ""Success"") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Message = error, Data = default };
}
```",

        ["linq-language-integrated-query-va-lambda-expressions"] = @"# LINQ & Lambda Expressions from A to Z

## 1. What is LINQ?
**LINQ (Language Integrated Query)** is an exceptionally powerful .NET capability that allows you to write consistent queries across diverse data sources: in-memory collections (`IEnumerable<T>`), relational databases (`IQueryable<T>` with EF Core), XML, or JSON.

## 2. Most Common LINQ Operators

Suppose we have a list of products:
```csharp
public record Product(int Id, string Name, decimal Price, string Category, int StockQuantity);

var products = new List<Product>
{
    new(1, ""Dell XPS Laptop"", 32000000m, ""Laptop"", 5),
    new(2, ""MacBook Pro M3"", 45000000m, ""Laptop"", 3),
    new(3, ""Logitech MX Mouse"", 2100000m, ""Accessories"", 20),
    new(4, ""Keychron Mechanical Keyboard"", 1800000m, ""Accessories"", 0),
    new(5, ""Dell 4K Monitor"", 12500000m, ""Monitor"", 8)
};
```

### 1. `Where`: Filter Data by Condition
```csharp
// Find laptop products priced over 30,000,000
var laptops = products.Where(p => p.Category == ""Laptop"" && p.Price > 30000000m).ToList();
```

### 2. `Select`: Project (Transform/Map) Data
```csharp
// Get list of capitalized product names
var names = products.Select(p => p.Name.ToUpper()).ToList();

// Create Anonymous Type or DTO projection
var summary = products.Select(p => new { p.Name, FormattedPrice = $""{p.Price:N0} đ"" }).ToList();
```

### 3. `OrderBy` & `OrderByDescending`: Sorting
```csharp
// Sort products by price in descending order
var sortedByPrice = products.OrderByDescending(p => p.Price).ToList();
```

### 4. `FirstOrDefault` & `SingleOrDefault`: Retrieve Single Element
```csharp
// Retrieve product with Id = 2, returns null if not found
var product = products.FirstOrDefault(p => p.Id == 2);
```

### 5. `Any` & `All`: Logical Verification
```csharp
// Check if any product is out of stock (StockQuantity == 0)
bool hasOutOfStock = products.Any(p => p.StockQuantity == 0); // true

// Check if all products have price > 0
bool allPricesValid = products.All(p => p.Price > 0); // true
```

### 6. `GroupBy`: Grouping Data
```csharp
var categoryGroups = products.GroupBy(p => p.Category)
    .Select(g => new
    {
        Category = g.Key,
        ProductCount = g.Count(),
        TotalInventoryValue = g.Sum(p => p.Price * p.StockQuantity)
    });
```

## 3. The Concept of Deferred Execution
LINQ queries **do not execute immediately** upon declaration. They only execute when you actually iterate through them (e.g. `foreach` loop) or invoke materialization methods such as `.ToList()`, `.ToArray()`, `.Count()`, or `.FirstOrDefault()`.
This deferred behavior is crucial when working with Entity Framework Core, as it allows composable query construction and translates optimal, single SQL commands to the Database Server.",

        ["lap-trinh-bat-dong-bo-async-await-trong-dotnet"] = @"# Asynchronous Programming with Async / Await in .NET

## 1. Why Asynchronous Programming?
When an application performs time-consuming I/O operations (database queries, 3rd party HTTP API calls, reading files from disk):
- **Synchronous**: The thread is **blocked** and sits idle waiting, unable to handle other requests. In web servers, this causes Thread Pool Starvation.
- **Asynchronous**: The thread is immediately released back to the Thread Pool to process other incoming user requests. When the I/O completes, a thread resumes execution of the remaining code.

## 2. Syntax: `async` and `await`
```csharp
public async Task<string> FetchDataFromApiAsync(string url)
{
    using var client = new HttpClient();
    
    // 'await' releases the current thread while waiting for the network response
    string result = await client.GetStringAsync(url);
    
    return result;
}
```

## 3. Essential Rules:
1. **Naming Convention**: Suffix async methods with `Async` (e.g. `GetUsersAsync()`).
2. **Return Types**:
   - `Task`: For async methods returning no value (equivalent to `void`).
   - `Task<T>`: For async methods returning data of type `T`.
   - **Avoid `async void`**: Except for UI desktop event handlers, `async void` cannot be awaited and unhandled exceptions cannot be caught via `try-catch`.
3. **Avoid `.Result` and `.Wait()`**: Always use `await`. Blocking on `.Result` can cause thread deadlocks!",

        ["entity-framework-core-code-first-dbcontext-migrations"] = @"# Entity Framework Core: Code-First, DbContext & Migrations

## 1. What is Entity Framework Core (EF Core)?
EF Core is the official lightweight, extensible **Object-Relational Mapper (ORM)** for .NET by Microsoft. It acts as a bridge translating C# domain objects (Classes/Entities) to relational database tables (SQL Server, PostgreSQL, SQLite, MySQL,...).

## 2. The Code-First Approach
In Code-First, you define your Entities and DbContext in C#, then utilize EF Core's **Migrations** tool to automatically scaffold schema, keys, constraints, and indexes into the database.

### Step 1: Declare the Entity
```csharp
public class Product
{
    public int Id { get; set; } // Recognized as Primary Key by convention
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    
    // Navigation Property
    public Category? Category { get; set; }
}
```

### Step 2: Create the `DbContext`
```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
}
```

### Step 3: Register DbContext in `Program.cs`
```csharp
// Configure SQL Server connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));
```

### Step 4: CLI Migration Commands
```bash
# Generate a new migration capturing schema changes
dotnet ef migrations add InitialCreate

# Apply schema migrations to the database
dotnet ef database update
```

## 3. Basic CRUD Operations with EF Core
```csharp
// 1. CREATE (Insert)
var newProduct = new Product { Name = ""Mechanical Keyboard"", Price = 120.00m, CategoryId = 1 };
await context.Products.AddAsync(newProduct);
await context.SaveChangesAsync();

// 2. READ (Query)
var products = await context.Products
    .AsNoTracking() // Boost read performance when entities are not modified
    .Where(p => p.Price > 50.00m)
    .ToListAsync();

// 3. UPDATE (Modify)
var productToEdit = await context.Products.FindAsync(1);
if (productToEdit != null)
{
    productToEdit.Price = 110.00m;
    await context.SaveChangesAsync();
}

// 4. DELETE (Remove)
var productToDelete = await context.Products.FindAsync(2);
if (productToDelete != null)
{
    context.Products.Remove(productToDelete);
    await context.SaveChangesAsync();
}
```",

        ["kien-truc-aspnet-core-mvc-programcs-middleware-controller"] = @"# Comprehensive ASP.NET Core MVC Architecture

## 1. The MVC Pattern (Model - View - Controller)
- **Model**: Represents application domain data and business logic (Entities, ViewModels).
- **View**: Presentation layer rendering HTML/CSS/JavaScript to the browser (`.cshtml` files with Razor engine).
- **Controller**: Handles incoming HTTP requests, orchestrates business services, and returns appropriate Views or JSON responses.

```
       HTTP Request
Browser  ───────►  [Routing / Middleware Pipeline]
                       │
                       ▼
                 [Controller]  ◄───►  [Service / DbContext]
                       │
                       ▼
                 [Razor View]  ───►  Render HTML  ───►  Browser
```

## 2. Request Lifecycle & Middleware Pipeline in `Program.cs`
Middleware components assemble into an execution pipeline sequentially handling incoming requests and outgoing responses.

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register Services (Dependency Injection Container)
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(...);

var app = builder.Build();

// 2. Configure Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(""/Home/Error"");
    app.UseHsts();
}

app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
app.UseStaticFiles();       // Serve static files (css, js, images in wwwroot)
app.UseRouting();           // Route matching
app.UseAuthentication();    // Identify caller (Who is this?)
app.UseAuthorization();     // Permissions check (What can they do?)

// 3. Map Controller Routes
app.MapControllerRoute(
    name: ""default"",
    pattern: ""{controller=Home}/{action=Index}/{id?}"");

app.Run();
```

## 3. Creating Controllers and Actions
```csharp
public class ProductsController : Controller
{
    private readonly AppDbContext _context;

    // Inject DbContext via Constructor
    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Products
    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.ToListAsync();
        return View(products); // Returns Views/Products/Index.cshtml
    }

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(); // Returns HTTP 404
        }
        return View(product);
    }
}
```",

        ["xay-dung-restful-web-api-chuan-voi-aspnet-core"] = @"# Building Standard RESTful Web APIs with ASP.NET Core

## 1. RESTful API Design Principles
- Use plural nouns for resource URI paths (e.g., `/api/products`, `/api/users`).
- Adhere strictly to standard HTTP Verbs:
  - `GET`: Retrieve resource (Idempotent, Safe).
  - `POST`: Create a new resource.
  - `PUT`: Replace an entire resource.
  - `PATCH`: Partially update a resource.
  - `DELETE`: Remove a resource.
- Return appropriate HTTP Status Codes:
  - `200 OK`: Successful response with payload.
  - `201 Created`: Resource successfully created (includes `Location` header).
  - `204 No Content`: Successful request with no payload (typical for `DELETE`).
  - `400 Bad Request`: Invalid client payload or validation failure.
  - `404 Not Found`: Resource does not exist.
  - `500 Internal Server Error`: Unhandled server exception.

## 2. Writing a Standard `ApiController` in C#
```csharp
[ApiController]
[Route(""api/[controller]"")]
public class ProductsApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return Ok(await _context.Products.AsNoTracking().ToListAsync());
    }

    [HttpGet(""{id}"")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound(new { message = $""Product #{id} not found."" });
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var product = new Product { Name = dto.Name, Price = dto.Price };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpDelete(""{id}"")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
```",

        ["dependency-injection-va-nguyen-ly-solid-trong-aspnet-core"] = @"# Dependency Injection (DI) & SOLID Principles in ASP.NET Core

## 1. The 3 Service Lifetimes in .NET DI Container

When registering services in the DI container (`builder.Services`), you have 3 lifetime options:

| Lifetime | How It Works | Typical Use Case |
|---|---|---|
| **Transient** (`AddTransient`) | A new instance is created **every time it is requested** | Lightweight, stateless services |
| **Scoped** (`AddScoped`) | Created **once per HTTP Request**. All dependencies in the same request share this instance | `DbContext`, Repositories, Unit of Work |
| **Singleton** (`AddSingleton`) | Created **once across the entire application lifecycle** | In-memory cache, global ApplicationConfig, Background Workers |

```csharp
// Registering services in Program.cs
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITutorialService, TutorialService>();
builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
```

## 2. The 5 SOLID Principles
1. **S - Single Responsibility Principle**: A class should have only one reason to change. Separate emailing, pricing, and database persistence into distinct classes!
2. **O - Open/Closed Principle**: Open for extension, but closed for modification. Add new features by extending interfaces rather than modifying battle-tested code.
3. **L - Liskov Substitution Principle**: Derived subtypes must be substitutable for their base types without altering correctness.
4. **I - Interface Segregation Principle**: Many client-specific interfaces are better than one general-purpose fat interface.
5. **D - Dependency Inversion Principle**: High-level modules should depend on abstractions (interfaces), not low-level concrete implementations."
    };

    public static string? GetContentMarkdownEn(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var canonical = LocalizationHelper.GetCanonicalSlug(slug);
        if (EnglishMarkdown.TryGetValue(canonical, out var md)) return md;
        return EnglishMarkdown.TryGetValue(slug, out md) ? md : null;
    }
}
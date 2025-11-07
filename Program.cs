
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;



var builder = WebApplication.CreateBuilder(args);

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

app.UseResponseCompression();  //for speed enhancing and better performance

// Error Handling Middleware (First in pipeline) to catches exceptions early
//ensures it wraps everything that follows — including authentication, logging, and endpoint execution
app.Use(async (context, next) =>
{
    try
    {
        await next();  // proceed to next middleware in pipline
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var errorResponse = new { error = "Internal server error.", details = ex.Message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
});



// Selective Authentication Middleware (Second)
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    var requiresAuth = endpoint?.Metadata.GetMetadata<RequireAuthAttribute>() != null;

    if (requiresAuth)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token) || token != "my-secret-token")
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized" }));
            return;
        }
    }

    await next();
});



// Logging Middlware (Last in pipeline): records all requests and responses
//Logs the full lifecycle of the request and Ensures that logs reflect the outcome after authentication and error handling
app.Use(async (context, next) =>
{
    var requestPath = context.Request.Path;
    var method = context.Request.Method;

    await next();

    var statusCode = context.Response.StatusCode;
    Console.WriteLine($"[{method}] {requestPath} => {statusCode}");
});



// Enable Swagger middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Use Dictionary for fast lookups by ID instead of list
var users = new Dictionary<int, User>();
var nextId = 1;


// Validate user input
bool IsValidUser(User user, out Dictionary<string, string[]> errors)
{
    errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(user.Name))
        errors["Name"] = new[] { "Name is required." };

    if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
        errors["Email"] = new[] { "Valid email is required." };

    return errors.Count == 0;
}

//GET All users (Public)
app.MapGet("/users", (int? page, int? pageSize) =>
{
    int currentPage = page.GetValueOrDefault(1);
    int size = pageSize.GetValueOrDefault(10);

    if (currentPage <= 0 || size <= 0)
        return Results.BadRequest("Page and pageSize must be positive integers.");

    var skip = (currentPage - 1) * size;
    var pagedUsers = users.Values.Skip(skip).Take(size).ToList();

    var response = new
    {
        TotalCount = users.Count,
        Page = currentPage,
        PageSize = size,
        Users = pagedUsers
    };

    return Results.Ok(response);
})
.WithName("GetAllUsers")
.WithTags("Users");




// GET: User by ID and gives error message when user not found
//GET: User by ID (Public)
app.MapGet("/users/{id}", (int id) =>
{
    if (!users.TryGetValue(id, out var user))
        return Results.NotFound($"User with ID {id} not found.");

    return Results.Ok(user);
})
.WithName("GetUserById")
.WithTags("Users");



// POST: create new user with validation
//POST: Create user (Protected)
app.MapPost("/users", (User user) =>
{
    if (!IsValidUser(user, out var errors))
        return Results.ValidationProblem(errors);

    user.Id = nextId++;
    users[user.Id] = user;
    return Results.Created($"/users/{user.Id}", user);
})
.WithMetadata(new RequireAuthAttribute())
.WithName("CreateUser")
.WithTags("Users");



 //Update user with validation

 //Update user (Protected)
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    if (!users.TryGetValue(id, out var existingUser))
        return Results.NotFound($"User with ID {id} not found.");

    if (!IsValidUser(updatedUser, out var errors))
        return Results.ValidationProblem(errors);

    existingUser.Name = updatedUser.Name;
    existingUser.Email = updatedUser.Email;
    return Results.NoContent();
})
.WithMetadata(new RequireAuthAttribute())
.WithName("UpdateUser")
.WithTags("Users");




// Remove user with validation

// DELETE: Remove user (Protected)
app.MapDelete("/users/{id}", (int id) =>
{
    if (!users.Remove(id))
        return Results.NotFound($"User with ID {id} not found.");

    return Results.NoContent();
})
.WithMetadata(new RequireAuthAttribute())
.WithName("DeleteUser")
.WithTags("Users");



// GET: Crash test (for error handling)
app.MapGet("/crash", () =>
{
    throw new Exception("Simulated server crash.");
});


app.Run();


// User class
public class User
{
    public int Id { get; set; }
    required public string Name { get; set; }
    required public string Email { get; set; }
}

//Custom Attribute for Protected Endpoints
class RequireAuthAttribute : Attribute { }


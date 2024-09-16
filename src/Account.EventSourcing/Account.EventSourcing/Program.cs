using Account.EventSourcing.Grains.Abstraction;
using Account.EventSourcing.Models;
using Functional;
using Functional.DotNet;
using Functional.DotNet.Monad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Orleans;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add Orleans services
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    // Configure storage providers
    siloBuilder.AddMemoryGrainStorage("accountStore");
    siloBuilder.AddMemoryGrainStorage("eventStore");

});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank Account API", Version = "v1" });
});

var app = builder.Build();

// Extension method for mapping Result to HTTP responses
static IResult ToHttpResult<T>(Result<T> result)
{
    return result.IsSuccess
        ? Results.Ok(result.Data)
        : Results.BadRequest(new { Error = result.Message });
}

// Map endpoints
app.MapPost("/accounts/{accountId}/deposit", async (
    [FromRoute] string accountId,
    [FromBody] decimal amount,
    IGrainFactory grainFactory) =>
{
    var accountGrain = grainFactory.GetGrain<IAccountGrain>(accountId);

    return await accountGrain.Deposit(amount).Map(
        Completed: _ => Results.Ok($"Deposited {amount} to account {accountId}."),
        Faulted: error => Results.BadRequest(new { Error = error })
    );
});

app.MapPost("/accounts/{accountId}/withdraw", async (
    [FromRoute] string accountId,
    [FromBody] decimal amount,
    IGrainFactory grainFactory) =>
{
    var accountGrain = grainFactory.GetGrain<IAccountGrain>(accountId);

    var result = await accountGrain.Withdraw(amount).Map(
        Completed: _ => Results.Ok($"Withdrew {amount} from account {accountId}."),
        Faulted: error => Results.BadRequest(new { Error = error })
    );
});

app.MapPost("/accounts/{fromAccountId}/transfer/{toAccountId}", async (
    [FromRoute] string fromAccountId,
    [FromRoute] string toAccountId,
    [FromBody] decimal amount,
    IGrainFactory grainFactory) =>
{
    var fromAccountGrain = grainFactory.GetGrain<IAccountGrain>(fromAccountId);

    return 
    await fromAccountGrain.Transfer(toAccountId, amount).Map(
        Completed: _ => Results.Ok($"Transferred {amount} from account {fromAccountId} to {toAccountId}."),
        Faulted: error => Results.BadRequest(new { Error = error })
    );
});

app.MapGet("/accounts/{accountId}/balance", async (
    [FromRoute] string accountId,
    IGrainFactory grainFactory) =>
{
    var accountGrain = grainFactory.GetGrain<IAccountGrain>(accountId);

    return await accountGrain.GetBalance().Map(
        Completed: balance => Results.Ok(new { AccountId = accountId, Balance = balance }),
        Faulted: error => Results.BadRequest(new { Error = error })
    );
});

app.MapGet("/accounts/{accountId}/transactions", async (
    [FromRoute] string accountId,
    IGrainFactory grainFactory) =>
{
    var accountGrain = grainFactory.GetGrain<IAccountGrain>(accountId);

    var result = await accountGrain.GetTransactionHistory().Map(
        Completed: transactions => Results.Ok(transactions),
        Faulted: error => Results.BadRequest(new { Error = error })
    );

    return result;
});

app.UseSwagger();


app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank Account API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root (http://localhost:<port>/)
});


app.Run();
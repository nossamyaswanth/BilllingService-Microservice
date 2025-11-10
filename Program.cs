using BillingService.Data;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BillingService.Data;
using BillingService.Models;
using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Globalization;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BillingDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapGet("/v1/bills", async (BillingDb db, [FromQuery] string? status) =>
{
    var query = db.Bills.AsQueryable();

    if (!string.IsNullOrWhiteSpace(status))
        query = query.Where(b => b.Status == status);

    var bills = await query
        .OrderByDescending(b => b.CreatedAt)
        .Select(b => new
        {
            b.BillId,
            b.AppointmentId,
            b.PatientId,
            b.AmountTotal,
            b.Status,
            b.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(bills);
});

app.MapGet("/v1/bills/{id:long}", async (BillingDb db, long id) =>
{
    var bill = await db.Bills
        .Include(b => b.LineItems)
        .FirstOrDefaultAsync(b => b.BillId == id);

    return bill is null ? Results.NotFound(new { message = "Bill not found" }) : Results.Ok(bill);
});

app.MapPost("/v1/bills", async (BillingDb db, [FromBody] CreateBillRequest request) =>
{
    if (request.LineItems == null || request.LineItems.Count == 0)
        return Results.BadRequest(new { message = "LineItems are required" });

    decimal subtotal = request.LineItems.Sum(li => li.Quantity * li.UnitPrice);
    decimal taxPercent = request.TaxPercent ?? 5m;
    decimal taxAmount = Math.Round(subtotal * taxPercent / 100m, 2);
    decimal total = subtotal + taxAmount;

    var bill = new Bill
    {
        AppointmentId = request.AppointmentId,
        PatientId = request.PatientId,
        AmountSubtotal = subtotal,
        TaxPercent = taxPercent,
        TaxAmount = taxAmount,
        AmountTotal = total,
        Status = "OPEN",
        LineItems = request.LineItems.Select(li => new BillLineItem
        {
            Type = li.Type,
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice
        }).ToList()
    };

    db.Bills.Add(bill);
    await db.SaveChangesAsync();

    return Results.Created($"/v1/bills/{bill.BillId}", new { bill.BillId, bill.AmountTotal, bill.Status });
});

app.MapPost("/v1/bills/{id:long}/mark-paid", async (BillingDb db, long id, MarkPaidRequest request) =>
{
    var bill = await db.Bills.FirstOrDefaultAsync(b => b.BillId == id);

    if (bill is null)
        return Results.NotFound(new { message = "Bill not found" });

    if (bill.Status != "OPEN")
        return Results.BadRequest(new { message = $"Cannot pay bill in {bill.Status} status" });

    if (bill.AmountTotal != request.Amount)
        return Results.BadRequest(new { message = "Amount mismatch" });

    bill.Status = "PAID";
    bill.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(new { bill.BillId, bill.Status });
});

app.MapPost("/v1/bills/{id:long}/void", async (BillingDb db, long id) =>
{
    var bill = await db.Bills.FirstOrDefaultAsync(b => b.BillId == id);

    if (bill is null)
        return Results.NotFound(new { message = "Bill not found" });

    if (bill.Status == "PAID")
        return Results.BadRequest(new { message = "Cannot void a paid bill" });

    bill.Status = "VOID";
    bill.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(new { bill.BillId, bill.Status });
});

SeedBillingData(app); // Seed initial data

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDb>();

    try
    {
        db.Database.Migrate(); // applies migrations if DB exists
    }
    catch (SqlException ex)
    {
        if (ex.Number == 4060) // Database does not exist
        {
            // Connect to 'master' and create it dynamically
            var masterConn = db.Database.GetDbConnection().ConnectionString
                .Replace("Database=BillingDb", "Database=master");

            using (var masterConnection = new SqlConnection(masterConn))
            {
                masterConnection.Open();
                using (var cmd = new SqlCommand("CREATE DATABASE BillingDb", masterConnection))
                    cmd.ExecuteNonQuery();
            }

            db.Database.Migrate();
        }
        else throw;
    }
}

app.Run();

static void SeedBillingData(IApplicationBuilder app)
{
    using var scope = app.ApplicationServices.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BillingDb>();

    if (db.Bills.Any()) return; // Prevent duplicate import

    using var reader = new StreamReader("Seeds/hms_bills.csv");
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    var records = csv.GetRecords<BillingCsv>();

    foreach (var r in records)
{
    // ✅ Parse date with correct format
    var date = DateTime.ParseExact(
        r.created_at.Trim(),
        new[] { "dd/MM/yy H:mm", "dd/MM/yy HH:mm", "dd/MM/yyyy H:mm", "dd/MM/yyyy HH:mm" },
        CultureInfo.InvariantCulture,
        DateTimeStyles.None
    );

    // ✅ Create bill object
    var bill = new Bill
    {
        PatientId = r.patient_id,
        AppointmentId = r.appointment_id,
        AmountTotal = r.amount,
        Status = r.status,
        CreatedAt = date,
        UpdatedAt = date
    };

    db.Bills.Add(bill);
}

    db.SaveChanges();
}

class BillingCsv
{
    public int patient_id { get; set; }
    public int appointment_id { get; set; }
    public decimal amount { get; set; }
    public string status { get; set; } = "";
    public string created_at { get; set; } = "";
}
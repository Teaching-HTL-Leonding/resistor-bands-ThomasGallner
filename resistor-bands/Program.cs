using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<CalcResistorValueService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Resistor Color Coding", Version = "v1" });

    var locationOfExecutable = Assembly.GetExecutingAssembly().Location;
    var execFileNameWithoutExtension = Path.GetFileNameWithoutExtension(locationOfExecutable);
    var execFilePath = Path.GetDirectoryName(locationOfExecutable);
    var xmlFilePath = Path.Combine(execFilePath!, $"{execFileNameWithoutExtension}.xml");

    options.IncludeXmlComments(xmlFilePath);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
var service = app.Services.CreateScope().ServiceProvider.GetRequiredService<CalcResistorValueService>();

var colors = new ConcurrentDictionary<string, ColorDetails>()
{
    ["black"] = new(0, 1, 0),
    ["brown"] = new(1, 10, 1),
    ["red"] = new(2, 100, 2),
    ["orange"] = new(3, 1_000, 0),
    ["yellow"] = new(4, 10_000, 0),
    ["green"] = new(5, 100_000, 0.5),
    ["blue"] = new(6, 1_000_000, 0.25),
    ["violet"] = new(7, 10_000_000, 0.1),
    ["grey"] = new(8, 100_000_000, 0.05),
    ["white"] = new(9, 1_000_000_000, 0),
    ["gold"] = new(0, 0.1, 5),
    ["silver"] = new(0, 0.01, 10),
};


app.MapGet("/colors", () => colors.Keys)
    .WithTags("Colors")
    .WithOpenApi(o =>
    {
        o.Summary = "Returns all colors for bands on resistors";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "A list of all colors";

        return o;
    });

app.MapGet("/colors/{color}", (string color) =>
{
    if (colors.TryGetValue(color, out ColorDetails? colorDetails))
    {
        return Results.Ok(colorDetails);
    }

    return Results.NotFound();
})
    .WithTags("Colors")
    .Produces<ColorDetails>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi(o =>
    {
        o.Summary = "Returns details for a color";
        o.Parameters[0].Description = "Color for which to get details";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Details for a color band";
        o.Responses[((int)StatusCodes.Status404NotFound).ToString()].Description = "Unknown color";

        return o;
    });

app.MapPost("/resistors/value-from-bands", (ResistorBands resistorBands) =>
{
    return service.CalcResistorValue(colors, resistorBands);
})
    .WithTags("Resistors")
    .Produces<ResistorValue>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithOpenApi(o =>
    {
        o.Summary = "Calculates the resistor value based on given color bands (using POST)";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Resistor value could be decoded correctly";
        o.Responses[((int)StatusCodes.Status400BadRequest).ToString()].Description = "The request body contains invalid data";

        return o;
    });

app.MapGet("/resistors/value-from-bands", (string firstBand, string secondBand, string? thirdBand, string multiplier, string tolerance) =>
{
    var resistorBands = new ResistorBands(firstBand, secondBand, thirdBand, multiplier, tolerance);
    return service.CalcResistorValue(colors, resistorBands);
})
    .WithTags("Resistors")
    .Produces<ResistorValue>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithOpenApi(o =>
    {
        o.Summary = "Calculates the resistor value based on given color bands (using GET)";
        o.Parameters[0].Description = "Color of the 1st band";
        o.Parameters[1].Description = "Color of the 2nd band";
        o.Parameters[2].Description = "Color of the 3rd band. Note that this band can be left out for 4-band-coded resistors";
        o.Parameters[3].Description = "Color of the multiplier band";
        o.Parameters[4].Description = "Color of the tolerance band";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Resistor value could be decoded correctly";
        o.Responses[((int)StatusCodes.Status400BadRequest).ToString()].Description = "The request body contains invalid data";

        return o;
    }); ;

app.Run();

record ColorDetails(int Value, double Multiplier, double Tolerance)
{
    /// <summary>
    /// Meaning of the value bands
    /// </summary>
    [Required]
    public int Value { get; init; } = Value;
    /// <summary>
    /// Meaning of the multiplier band
    /// </summary>
    [Required]
    public double Multiplier { get; init; } = Multiplier;
    /// <summary>
    /// Meaning of the tolerance band. Note that this band does not exist for some colors.
    /// </summary>
    public double Tolerance { get; init; } = Tolerance;
}

record ResistorBands(string FirstBand, string SecondBand, string? ThirdBand, string Multiplier, string Tolerance)
{
    /// <summary>
    /// Color of the 1st band
    /// </summary>
    [Required]
    public string FirstBand { get; init; } = FirstBand;
    /// <summary>
    /// Color of the 2nd band
    /// </summary>
    [Required]
    public string SecondBand { get; init; } = SecondBand;
    /// <summary>
    /// Color of the 3rd band. Note that this band can be left out for 4-band-coded resistors
    /// </summary>
    public string? ThirdBand { get; init; } = ThirdBand;
    /// <summary>
    /// Color of the multiplier band
    /// </summary>
    [Required]
    public string Multiplier { get; init; } = Multiplier;
    /// <summary>
    /// Color of the tolerance band
    /// </summary>
    [Required]
    public string Tolerance { get; init; } = Tolerance;
}

record ResistorValue(double ResistorVal, double Tolerance)
{
    /// <summary>
    /// Resistor value in Ohm
    /// </summary>
    [Required]
    public double ResistorVal { get; init; } = ResistorVal;
    /// <summary>
    /// Tolerance in percentage
    /// </summary>
    [Required]
    public double Tolerance { get; init; } = Tolerance;
}
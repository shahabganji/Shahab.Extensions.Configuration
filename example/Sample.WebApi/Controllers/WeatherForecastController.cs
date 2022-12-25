using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Sample.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly IOptionsSnapshot<Settings> _settingsSnapshot;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(
        IOptionsSnapshot<Settings> settingsSnapshot,
        ILogger<WeatherForecastController> logger)
    {
        _settingsSnapshot = settingsSnapshot;
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, _settingsSnapshot.Value.MaxNumberOfRecords).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}

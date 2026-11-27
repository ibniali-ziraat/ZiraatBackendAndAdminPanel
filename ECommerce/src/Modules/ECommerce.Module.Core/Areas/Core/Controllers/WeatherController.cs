using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels.Weather;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Authorize]
    [Route("api/weather")]
    public class WeatherController : ControllerBase
    {
        private static readonly string[] ForecastKnownKeys = { "lat", "lon", "package", "asl", "city", "timeZone", "timezone" };
        private static readonly string[] ImageKnownKeys = { "path", "lat", "lon" };
        private static readonly string[] WeatherApiKnownKeys = { "q", "days", "aqi", "alerts" };
        private static readonly string[] AlertKnownKeys = { "lat", "lon", "package", "asl", "city", "timeZone", "timezone" };

        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("forecast")]
        public async Task<IActionResult> Forecast([FromQuery] WeatherForecastRequest request)
        {
            try
            {
                var response = await _weatherService.GetMeteoblueForecastAsync(request, ExtraQuery(ForecastKnownKeys));
                return Content(response.Content, response.ContentType);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Weather provider request failed.", detail = ex.Message });
            }
        }

        [HttpGet("current")]
        public async Task<IActionResult> Current([FromQuery] WeatherForecastRequest request)
        {
            try
            {
                var summary = await _weatherService.GetCurrentSummaryAsync(request.Lat, request.Lon, request.City, request.Package);
                return Ok(summary);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Weather provider request failed.", detail = ex.Message });
            }
        }

        [HttpGet("image")]
        public async Task<IActionResult> Image([FromQuery] WeatherImageRequest request)
        {
            try
            {
                var response = await _weatherService.GetMeteoblueImageAsync(request, ExtraQuery(ImageKnownKeys));
                return File(response.Bytes, response.ContentType);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Weather image provider request failed.", detail = ex.Message });
            }
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> Alerts([FromQuery] WeatherForecastRequest request)
        {
            try
            {
                var alerts = await _weatherService.GetWeatherAlertsAsync(request.Lat, request.Lon, ExtraQuery(AlertKnownKeys));
                return Ok(new { data = alerts, timestamp = DateTimeOffset.UtcNow.ToString("O") });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Weather alert provider request failed.", detail = ex.Message });
            }
        }
        [HttpGet("weatherapi/forecast")]
        public async Task<IActionResult> WeatherApiForecast([FromQuery] WeatherApiForecastRequest request)
        {
            try
            {
                var response = await _weatherService.GetWeatherApiForecastAsync(request, ExtraQuery(WeatherApiKnownKeys));
                return Content(response.Content, response.ContentType);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "WeatherAPI request failed.", detail = ex.Message });
            }
        }

        private IDictionary<string, string> ExtraQuery(IEnumerable<string> knownKeys)
        {
            var known = new HashSet<string>(knownKeys, StringComparer.OrdinalIgnoreCase);
            return Request.Query
                .Where(x => !known.Contains(x.Key))
                .Where(x => x.Value != StringValues.Empty && !StringValues.IsNullOrEmpty(x.Value))
                .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        }
    }
}


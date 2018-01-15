using AarekhLabs.Api.Memcached.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Xml;

namespace AarekhLabs.Api.Memcached.Extensions
{
    internal static class MemcachedControllerExtensions
    {
        internal static IActionResult JsonFormatMismathErrorResult(this MemcachedController controller, string key, JsonReaderException ex, ILogger logger)
        {
            var error = new { error = "Format Mismatch", description = "Value in key:[" + key + "] is not json." };
            logger.LogDebug(ex, "Error : " + JsonConvert.SerializeObject(error));
            return controller.StatusCode(StatusCodes.Status400BadRequest, error);
        }

        internal static IActionResult XmlFormatMismathErrorResult(this MemcachedController controller, string key, XmlException ex, ILogger logger)
        {
            var error = new { error = "Format Mismatch", description = "Value in key:[" + key + "] is not xml." };
            logger.LogDebug(ex, "Error : " + JsonConvert.SerializeObject(error));
            return controller.StatusCode(StatusCodes.Status400BadRequest, error);
        }

        internal static IActionResult NullValueErrorResult(this MemcachedController controller, string key, ILogger logger)
        {
            var error = new { error = "Null Value", description = "Value passed in body for key:[" + key + "] is either null or invalid." };
            logger.LogDebug("Validation Error : " + JsonConvert.SerializeObject(error));
            return controller.StatusCode(StatusCodes.Status400BadRequest, error);
        }

        internal static IActionResult ApiErrorResult(this MemcachedController controller, ILogger logger)
        {
            var error = new { error = "Memcached Error", description = "Memcached server is either unavailable or operation failed." };
            logger.LogDebug("Error : " + JsonConvert.SerializeObject(error));
            return controller.StatusCode(StatusCodes.Status503ServiceUnavailable, error);
        }

        internal static IActionResult ApiErrorResult(this MemcachedController controller, Exception ex, ILogger logger)
        {
            var error = new { error = "Memcached Error", description = "Memcached server is either unavailable or operation failed." };
            logger.LogDebug(ex, "Error : " + JsonConvert.SerializeObject(error));
            return controller.StatusCode(StatusCodes.Status503ServiceUnavailable, error);
        }
    }
}

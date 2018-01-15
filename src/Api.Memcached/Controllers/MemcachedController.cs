using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using AarekhLabs.Api.Memcached.Extensions;
using AarekhLabs.Api.Memcached.Configuration;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AarekhLabs.Api.Memcached.Controllers
{
    [Route("api/memcached")]
    public class MemcachedController : Controller
    {
        private ILogger _logger;
        private IMemcachedClient _memcachedClient;

        public MemcachedController(IMemcachedClient memcachedClient, ILogger<MemcachedController> logger)
        {
            _memcachedClient = memcachedClient;
            _logger = logger;
            _memcachedClient.NodeFailed += _memcachedClient_NodeFailed;
        }

        private void _memcachedClient_NodeFailed(IMemcachedNode obj)
        {
            _logger.LogCritical("Memcached Node Failure - {Node} has failed to respond", obj.EndPoint);
        }

        /// <summary>
        /// Get value for the memcached key.
        /// </summary>
        /// <param name="key">Key stored in Memcached.</param>
        /// <param name="format">Format in which data should be returned. Supports returning data in xml | json | text format. </param>
        /// <returns>Returns value stored in Memcached based on the key and format provided. If the data stored is not in the right format, it will return format mismatch error.</returns>        
        [HttpGet("get/{key}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Get(string key, [FromQuery] string format = "")
        {
            try
            {
                string result = await _memcachedClient.GetValueAsync<string>(key);
                if (!string.IsNullOrEmpty(result))
                {
                    switch (format.ToLower())
                    {
                        case "json":
                            return Ok(JsonConvert.DeserializeObject(result));
                        case "xml":
                            result = WebUtility.HtmlDecode(result);
                            var xDoc = XDocument.Parse(result);
                            return new ContentResult() { Content = xDoc.Root.ToString(), ContentType = "application/xml", StatusCode = StatusCodes.Status200OK };
                        case "text":
                            return new ContentResult() { Content = result, ContentType = "plain/text", StatusCode = StatusCodes.Status200OK };
                        default:
                            return Ok(result);
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                return this.JsonFormatMismathErrorResult(key, ex, _logger);
            }
            catch (XmlException ex)
            {
                return this.XmlFormatMismathErrorResult(key, ex, _logger);
            }
            catch (Exception ex)
            {
                return this.ApiErrorResult(ex, _logger);
            }

            return NotFound();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("gets/{key}")]
        public IActionResult Gets(string key)
        {
            var value = _memcachedClient.GetWithCas(key);
            var result = new { value = value.Result, cas = value.Cas, statusCode = value.StatusCode };
            if (value.Result != null)
                return Ok(result);
            else
                return StatusCode(StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("add/{key}")]
        public async Task<IActionResult> Add(string key, [FromBody] AddCacheItem item)
        {
            if (item == null)
            {
                return this.NullValueErrorResult(key, _logger);
            }

            if (!item.Seconds.HasValue)
                item.Seconds = 0;

            var value = _memcachedClient.Get(key);
            if (value == null)
            {
                await _memcachedClient.AddAsync(key, item.Value, item.Seconds.Value);
                value = _memcachedClient.Get(key);
                if (value != null)
                {
                    return StatusCode(StatusCodes.Status201Created);
                }
                else
                {
                    return this.ApiErrorResult(_logger);
                }
            }
            else
            {
                var error = new { error = "Key Exists", description = "The key:["+ key +"] already exist." };
                return StatusCode(StatusCodes.Status400BadRequest, error);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("set/{key}")]
        public async Task<IActionResult> Store(string key, [FromQuery]string value)
        {
            bool result = await _memcachedClient.StoreAsync(StoreMode.Set, key, value, TimeSpan.Parse("00:00:00"));
            if (result)
                return StatusCode(StatusCodes.Status201Created);
            else
                return this.ApiErrorResult(_logger);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("store/{key}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult Store(string key, [FromBody] StoreCacheItem item)
        {
            if (item == null)
            {              
                return this.NullValueErrorResult(key, _logger);
            }

            if (!item.Cas.HasValue || item.Cas.Value == 0)
            {
                bool result = true;
                if (item.ValidFor.HasValue)
                {
                    result = _memcachedClient.Store(GetMode(item), key, item.Value, item.ValidFor.Value);

                }
                else if (item.ExpireAt.HasValue)
                {
                    result = _memcachedClient.Store(GetMode(item), key, item.Value, item.ExpireAt.Value);
                }
                else
                {
                    result = _memcachedClient.Store(GetMode(item), key, item.Value);
                }

                if (!result)
                    return StatusCode(StatusCodes.Status400BadRequest, result);

                return StatusCode(StatusCodes.Status201Created);
            }
            else
            {
                CasResult<bool> result = new CasResult<bool>();

                if (item.ValidFor.HasValue)
                {
                    result = _memcachedClient.Cas(GetMode(item), key, item.Value, item.ValidFor.Value, item.Cas.Value);
                }
                else if (item.ExpireAt.HasValue)
                {
                    result = _memcachedClient.Cas(GetMode(item), key, item.Value, item.ExpireAt.Value, item.Cas.Value);
                }
                else
                {
                    result = _memcachedClient.Cas(GetMode(item), key, item.Value, item.Cas.Value);
                }
                
                if (!result.Result)
                    return StatusCode(StatusCodes.Status400BadRequest, result);

                return StatusCode(StatusCodes.Status201Created, result);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("append/{key}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult Append(string key, [FromBody] CasCacheItem item = null)
        {
            var bytes = Encoding.ASCII.GetBytes(item.Value);

            if (item.Cas.HasValue && item.Cas.Value > 0)
            {
                var result = _memcachedClient.Append(key, item.Cas.Value, new ArraySegment<byte>(bytes, 0, bytes.Length));
                return Ok(result);
            }
            else
            {
                var result = _memcachedClient.Append(key, new ArraySegment<byte>(bytes, 0, bytes.Length));
                return Ok(result);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("prepend/{key}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult Prepend(string key, [FromBody] CasCacheItem item = null)
        {
            var bytes = Encoding.ASCII.GetBytes(item.Value);

            if (item.Cas.HasValue && item.Cas.Value > 0)
            {
                var result = _memcachedClient.Prepend(key, item.Cas.Value, new ArraySegment<byte>(bytes, 0, bytes.Length));
                return Ok(result);
            }
            else
            {
                var result = _memcachedClient.Prepend(key, new ArraySegment<byte>(bytes, 0, bytes.Length));
                return Ok(result);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("incr/{key}")]
        public IActionResult Increment(string key, [FromBody] IncrDecrCacheItem item)
        {
            if (item == null)
                item = new IncrDecrCacheItem();
            
            if (!item.Cas.HasValue || item.Cas.Value == 0)
            {
                ulong result;
                if (item.ValidFor.HasValue)
                {
                    result = _memcachedClient.Increment(key, item.DefaultValue.Value, item.Delta.Value, item.ValidFor.Value);
                }
                else if (item.ExpireAt.HasValue)
                {
                    result = _memcachedClient.Increment(key, item.DefaultValue.Value, item.Delta.Value, item.ExpireAt.Value);
                }
                else
                {
                    result = _memcachedClient.Increment(key, item.DefaultValue.Value, item.Delta.Value);
                }

                return Ok(result);
            }
            else
            {
                CasResult<ulong> value = new CasResult<ulong>();

                if (item.ValidFor.HasValue)
                {
                    value = _memcachedClient.Increment(key, item.DefaultValue.Value, item.Delta.Value, item.ValidFor.Value, item.Cas.Value);
                }
                else if (item.ExpireAt.HasValue)
                {
                    value = _memcachedClient.Increment(key, item.DefaultValue.Value, item.Delta.Value, item.ExpireAt.Value, item.Cas.Value);
                }
                else
                {
                    value = _memcachedClient.Increment(key, item.DefaultValue.Value, item.Delta.Value, item.Cas.Value);
                }

                var result = new { value = value.Result, cas = value.Cas, statusCode = value.StatusCode };

                return Ok(result);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost("decr/{key}")]
        public IActionResult Decrement(string key, [FromBody] IncrDecrCacheItem item)
        {
            if (item == null)
                item = new IncrDecrCacheItem();

            if (!item.Cas.HasValue || item.Cas.Value == 0)
            {
                ulong result;
                if (item.ValidFor.HasValue)
                {
                    result = _memcachedClient.Decrement(key, item.DefaultValue.Value, item.Delta.Value, item.ValidFor.Value);
                }
                else if (item.ExpireAt.HasValue)
                {
                    result = _memcachedClient.Decrement(key, item.DefaultValue.Value, item.Delta.Value, item.ExpireAt.Value);
                }
                else
                {
                    result = _memcachedClient.Decrement(key, item.DefaultValue.Value, item.Delta.Value);
                }

                return Ok(result);
            }
            else
            {
                CasResult<ulong> value = new CasResult<ulong>();

                if (item.ValidFor.HasValue)
                {
                    value = _memcachedClient.Decrement(key, item.DefaultValue.Value, item.Delta.Value, item.ValidFor.Value, item.Cas.Value);
                }
                else if (item.ExpireAt.HasValue)
                {
                    value = _memcachedClient.Decrement(key, item.DefaultValue.Value, item.Delta.Value, item.ExpireAt.Value, item.Cas.Value);
                }
                else
                {
                    value = _memcachedClient.Decrement(key, item.DefaultValue.Value, item.Delta.Value, item.Cas.Value);
                }

                var result = new { value = value.Result, cas = value.Cas, statusCode = value.StatusCode };

                return Ok(result);
            }
        }

        /// <summary>
        /// Remove key from Memcached.
        /// </summary>
        /// <param name="key">Key stored in Memcached.</param>
        /// <returns>Returns HTTP 204 if succeed, else HTTP 400.</returns>
        [HttpDelete("delete/{key}")]
        public async Task<IActionResult> Remove(string key)
        {
            try
            {
                var result = await _memcachedClient.RemoveAsync(key);
                if (result)
                {
                    return NoContent();
                }
                else
                {
                    return StatusCode(StatusCodes.Status400BadRequest, result);
                }
            }
            catch (Exception ex)
            {
                return this.ApiErrorResult(ex, _logger);
            }
        }

        /// <summary>
        /// Flush all the keys in Memcached.
        /// </summary>
        /// <returns>Returns HTTP 204.</returns>
        [HttpDelete("flushall")]
        public IActionResult FlushAll()
        {
            try
            {
                _memcachedClient.FlushAll();
                return NoContent();
            }
            catch (Exception ex)
            {
                return this.ApiErrorResult(ex, _logger);
            }
        }

        private StoreMode GetMode(StoreCacheItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Mode))
            {
                return StoreMode.Set;
            }

            switch (item.Mode.ToLower())
            {
                case "add": return StoreMode.Add;
                case "set": return StoreMode.Set;
                case "replace": return StoreMode.Replace;
                default: return StoreMode.Set;
            }
        }
    }
}

using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace AarekhLabs.Api.Memcached.Configuration
{
    public class CacheItem
    {
    }

    public class StringCacheItem : CacheItem
    {
        [JsonProperty(PropertyName = "value", Required = Required.Always, Order =1)]
        public string Value { get; set; }
    }   

    public class AddCacheItem : StringCacheItem
    {

        [JsonProperty(PropertyName = "seconds", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order =3)]
        public int? Seconds { get; set; }
    }

    public class CasCacheItem : StringCacheItem
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "cas", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order = 4)]
        public ulong? Cas { get; set; }
    }

    public class StoreCacheItem : StringCacheItem
    {
        public StoreCacheItem():base() 
        {
            Mode = "set";
        }

        [DefaultValue("set")]
        [JsonProperty(PropertyName = "mode", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order =3)]
        public string Mode { get; set; }

        [JsonProperty(PropertyName = "cas", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order = 4)]
        public ulong? Cas { get; set; }
    
        [JsonProperty(PropertyName = "validFor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order =4)]
        public TimeSpan? ValidFor { get; set; }

        [JsonProperty(PropertyName = "expireAt", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order =5)]
        public DateTime? ExpireAt { get; set; }
    }


    public class IncrDecrCacheItem 
    {
        public IncrDecrCacheItem()
        {
            if (!DefaultValue.HasValue)
                DefaultValue = 0;

            if (!Cas.HasValue)
                Cas = 0;

            if (!Delta.HasValue)
                Delta = 1;

        }

        [JsonProperty(PropertyName = "defaultValue", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order = 1)]
        public ulong? DefaultValue { get; set; }
        
        [JsonProperty(PropertyName = "delta", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order = 3)]
        public ulong? Delta { get; set; }
        
        [JsonProperty(PropertyName = "cas", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order = 4)]
        public ulong? Cas { get; set; }
              
        [JsonProperty(PropertyName = "validFor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order = 4)]
        public TimeSpan? ValidFor { get; set; }

        [JsonProperty(PropertyName = "expireAt", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, Order = 5)]
        public DateTime? ExpireAt { get; set; }
    }
}

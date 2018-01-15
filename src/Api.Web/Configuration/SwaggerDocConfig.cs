using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AarekhLabs.Memcached.Api.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    public class SwaggerDocConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public SwaggerDocConfig()
        {
            this.Info = new Info();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool EnableSwagger { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Info Info { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string IncludeXmlComments { get; set; }
    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreFileProcessing
{
    public class UploadMultipartModel
    {
        public IFormFile File { get; set; }
        public int SomeValue { get; set; }
    }
}

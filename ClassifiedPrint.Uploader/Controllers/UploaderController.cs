using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassifiedPrint.Uploader.Controllers
{
    public class UploadFilesController : Controller
    {
        private readonly IHostingEnvironment hostingEnvironment;
        public UploadFilesController(IHostingEnvironment environment)
        {
            hostingEnvironment = environment;
        }
        


    }
}
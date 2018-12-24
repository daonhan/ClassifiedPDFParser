using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ClassifiedPrint.Uploader.Models;
using ClassifiedPrint.Core.Repository;
using ClassifiedPrint.Core.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Xml.Serialization;
using iTextSharp.text.pdf;

using iTextSharp.text.pdf.parser;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace ClassifiedPrint.Uploader.Controllers
{
    public class HomeController : Controller
    {

        const float MIN_COL1 = 0;
        //const double MAX_COL1 = 200;
        const float MIN_COL2 = 158;
        //const double MAX_COL2 = 320.8188;
        const float MIN_COL3 = 322;
        //const double MAX_COL3 = 485.2282;
        const float MIN_COL4 = 481;
        //const double MAX_COL4 = 649.6376;
        const float MIN_COL5 = 636;
        private readonly Regex rx = new Regex(@"\t|\n|\r|\s+|\?|\(|\)|\-|\/|\,|\:|\.|\;|\[|\]|\–|\…|\""|\”");
        // filters control characters but allows only properly-formed surrogate sequences
        private readonly Regex invalidXMLChars = new Regex(
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|&#x([0-8BCEF]|1[0-9A-F]);|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
            RegexOptions.Compiled);
        private readonly INewspaperRespository service;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly IConfiguration configuration;
        private readonly ConcurrentDictionary<string, Newspaper> parsed  = new ConcurrentDictionary<string, Newspaper>();
        private readonly ConcurrentDictionary<int, Dictionary<int, StringBuilder>>  dPages = new ConcurrentDictionary<int, Dictionary<int, StringBuilder>>();
        public HomeController(INewspaperRespository repository, IHostingEnvironment environment, IConfiguration config)
        {
            hostingEnvironment = environment;
            service = repository;
            configuration = config;
        }
        public IActionResult Index()
        {
            //var ttt = service.FindAll();
            return View();
        }
        [HttpPost()]
        public async Task<IActionResult> Index(List<IFormFile> files)
        {
            if (files.Count != 2 || !files[0].ContentType.Contains("text/xml") || !files[1].ContentType.Contains("application/pdf"))
            {
                throw new InvalidDataException("Loại file không hợp lệ. Chỉ chấp nhận một cặp file có dạng .xml và .pdf");
            }

            string pdfFileName  = System.IO.Path.GetFileNameWithoutExtension(files[1].FileName);

            string[] strArr = pdfFileName.Split('_');
            if (strArr.Length != 3)
                throw new FormatException("Tên file không hợp lệ theo cấu trúc: {location}_{press}_{postdate}.pdf");

            string location = strArr[0].ToLower();
            var validLocations = new string[] { "hcm", "hn" };
            if(!validLocations.Contains( location.ToLower()))
                throw new FormatException("Báo chỉ đăng tại HCM hoặc HN");

            int press = int.Parse(strArr[1]);
            DateTime postDate;
            if(!DateTime.TryParseExact(strArr[2], "ddMMyyyy", null, DateTimeStyles.None, out postDate))
                throw new FormatException("Định dạng ngày trên file pdf không hợp lệ");

            long size = files.Sum(f => f.Length);

            // full path to file in temp location

            var uploads = System.IO.Path.Combine(hostingEnvironment.WebRootPath, "uploads");
            var filePath = System.IO.Path.GetTempFileName();
            
            foreach (var formFile in files)
            {

                if (formFile.Length > 0)
                {
                    filePath = System.IO.Path.Combine(uploads, formFile.FileName);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    using (var stream = new System.IO.FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                }
            }

            return RedirectToAction("ParsePdf", new { xmlFileName = files[0].FileName, pdfFileName = files[1].FileName, location, press, postDate });
        }

        public async Task<IActionResult> ParsePdf(string xmlFileName, string pdfFileName, string location, int press, DateTime postDate)
        {
            var areaId = location == "hcm" ? 2 : 1;
            //Stopwatch stopwatch = new Stopwatch();

            // Begin timing.
            //stopwatch.Start();

            var uploads = System.IO.Path.Combine(hostingEnvironment.WebRootPath, "uploads");
            var xmlFilePath = System.IO.Path.Combine(uploads, xmlFileName);
            var pdfFilePath = System.IO.Path.Combine(uploads, pdfFileName);
            if (!System.IO.File.Exists(xmlFilePath) || !System.IO.File.Exists(pdfFilePath))
            {
                throw new InvalidDataException("Thông tin cần phân tích không hợp lệ");
            }
            //var adfsa = new FileStream(xmlFilePath, FileMode.Open)
            //XmlSerializer xs = new XmlSerializer(typeof(MBP));
            MBP listPrints;// = (MBP)xs.Deserialize(new FileStream(xmlFilePath, FileMode.Open));
            XmlSerializer serializer = new XmlSerializer(typeof(MBP));
            using (StreamReader streamReader = new StreamReader(xmlFilePath, Encoding.UTF8, false))
            {
                string strXml = streamReader.ReadToEnd();
                strXml = Regex.Replace(strXml, @"&#x([0-8BCEF]|1[0-9A-F]);", "");
                using (var stringreader = new StringReader(strXml))
                {
                    listPrints = (MBP)serializer.Deserialize(stringreader);
                }
                //listPrints = (MBP)serializer.Deserialize(streamReader);
            }
            foreach (var print in listPrints.Items)
            {
                var bk2 = new MBN.Utils.Fonts(MBN.Utils.Fonts.ConvertType.UNICODE, MBN.Utils.Fonts.ConvertType.BK2);
                print.ContentBK2 = bk2.Convert(rx.Replace(print.Content, "").Trim());
            }
            //Open PDF document
            await ParserPdf(pdfFilePath);
            
            int maxDegreeOfParallelism = configuration["MaxDegreeOfParallelism"] != null ? int.Parse(configuration["MaxDegreeOfParallelism"]) : 2;
            await ParseContent(listPrints, maxDegreeOfParallelism, areaId, press, postDate);
                        
            var printIds = parsed.Keys.ToList();
            var unparsed = listPrints.Items.Where(p => !printIds.Contains(p.ContractNo)).ToList();
            if (unparsed.Count > 0)
            {
                Parallel.ForEach(dPages.Keys, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, (page) => {                    

                    foreach (var print in unparsed)
                    {
                        var currentPage = dPages[page];
                        foreach (int col in currentPage.Keys)
                        {
                            string rs = rx.Replace(currentPage[col].ToString(), "");
                            
                            int idx = rs.IndexOf(print.Phone, StringComparison.OrdinalIgnoreCase);
                            if (idx != -1)
                            {
                                if (!parsed.ContainsKey(print.ContractNo))
                                {
                                    parsed[print.ContractNo] = new Newspaper()
                                    {
                                        ClassifiedId = print.Id,
                                        ContractNo = print.ContractNo,
                                        Content = print.Content,
                                        Phone = print.Phone,
                                        BeginDate = print.BDate,
                                        EndDate = print.EDate,
                                        Col = col,
                                        Page = page,
                                        Created = print.Created,
                                        AreaId = areaId,
                                        PressNo = press,
                                        PostDate = postDate,

                                    };
                                    break;
                                }
                            }
                            else
                            {
                                var m = Regex.Match(print.ContentBK2, @"\+?[0-9]{10}");
                                if (m.Success)
                                {
                                    var mobile = m.Value;
                                    idx = rs.IndexOf(mobile);
                                    if (idx != -1 && !parsed.ContainsKey(print.ContractNo))
                                    {
                                        parsed[print.ContractNo] = new Newspaper()
                                        {
                                            ClassifiedId = print.Id,
                                            ContractNo = print.ContractNo,
                                            Content = print.Content,
                                            Phone = print.Phone,
                                            BeginDate = print.BDate,
                                            EndDate = print.EDate,
                                            Col = col,
                                            Page = page,
                                            Created = print.Created,
                                            AreaId = areaId,
                                            PressNo = press,
                                            PostDate = postDate,
                                        };

                                        break;
                                    }
                                }
                            }         
                        }
                    }
                });
            }
            //stopwatch.Stop();
            //Debug.WriteLine(stopwatch.Elapsed);


            //stopwatch.Reset();
            //stopwatch.Start();
            unparsed = listPrints.Items.Where(p => !parsed.Keys.Contains(p.ContractNo)).ToList();
            if (unparsed.Count > 0)
            {
                Parallel.ForEach(dPages.Keys, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, (page) => {

                    foreach (var print in unparsed)
                    {
                        var currentPage = dPages[page];
                        foreach (int col in currentPage.Keys)
                        {
                            string rs = rx.Replace(currentPage[col].ToString(), "");
                            string subContent = print.ContentBK2.Length > 20 ? print.ContentBK2.Substring(0, print.ContentBK2.Length - 20) : print.ContentBK2.Substring(0, print.ContentBK2.Length - 11);
                            int idx = rs.IndexOf(subContent, StringComparison.OrdinalIgnoreCase);
                            if (idx != -1)
                            {
                                if (!parsed.ContainsKey(print.ContractNo))
                                {
                                    parsed[print.ContractNo] = new Newspaper()
                                    {
                                        ClassifiedId = print.Id,
                                        ContractNo = print.ContractNo,
                                        Content = print.Content,
                                        Phone = print.Phone,
                                        BeginDate = print.BDate,
                                        EndDate = print.EDate,
                                        Col = col,
                                        Page = page,
                                        Created = print.Created,
                                        AreaId = areaId,
                                        PressNo = press,
                                        PostDate = postDate,

                                    };
                                    break;
                                }
                            }
                        }
                    }
                });
            }
            unparsed = listPrints.Items.Where(p => !parsed.Keys.Contains(p.ContractNo)).ToList();
            printIds = parsed.Keys.ToList();
            var newspapers = unparsed.Select(print => new Newspaper()
            {
                ClassifiedId = print.Id,
                ContractNo = print.ContractNo,
                Content = print.Content,
                Phone = print.Phone,
                BeginDate = print.BDate,
                EndDate = print.EDate,
                Col = 0,
                Page = 0,
                Created = print.Created,
                AreaId = areaId,
                PressNo = press,
                PostDate = postDate,
            }).ToList();

            await InsertIntoDb(newspapers);
            //Debug.WriteLine($"Finished using dapper. Elapsed: {stopwatch.Elapsed} ");
            //printIds = parsed.Keys.ToList();
            
            return View(new ParserContentResult() { Parsed = printIds.Count, TotalRecord = listPrints.Items.Count, UnParseds = unparsed.Select(p => string.Format("{0} - {1}",p.ContractNo, p.Content)).ToList() });
        }
        private async Task ParseContent(MBP listPrints, int maxDegreeOfParallelism, int areaId, int press, DateTime postDate)
        {
            Parallel.ForEach(dPages.Keys, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, (page) => {
                var ids = parsed.Keys;
                var prints = listPrints.Items.Where(p => !ids.Contains(p.ContractNo)).ToList();
                foreach (var print in prints)
                {
                    var currentPage = dPages[page];
                    foreach (int col in currentPage.Keys)
                    {
                        string rs = rx.Replace(currentPage[col].ToString(), "");

                        int idx = rs.IndexOf(print.ContentBK2, StringComparison.OrdinalIgnoreCase);
                        if (idx != -1)
                        {
                            parsed[print.ContractNo] = new Newspaper()
                            {
                                ClassifiedId = print.Id,
                                ContractNo = print.ContractNo,
                                Content = print.Content,
                                Phone = print.Phone,
                                BeginDate = print.BDate,
                                EndDate = print.EDate,
                                Col = col,
                                Page = page,
                                Created = print.Created,
                                AreaId = areaId,
                                PressNo = press,
                                PostDate = postDate,
                            };
                            break;
                        }
                        else
                        {
                            string contractNo = print.ContractNo.Replace("-", "");
                            idx = rs.IndexOf(contractNo, StringComparison.OrdinalIgnoreCase);
                            if (idx != -1)
                            {
                                parsed[print.ContractNo] = new Newspaper()
                                {
                                    ClassifiedId = print.Id,
                                    ContractNo = print.ContractNo,
                                    Content = print.Content,
                                    Phone = print.Phone,
                                    BeginDate = print.BDate,
                                    EndDate = print.EDate,
                                    Col = col,
                                    Page = page,
                                    Created = print.Created,
                                    AreaId = areaId,
                                    PressNo = press,
                                    PostDate = postDate,
                                };
                                break;
                            }
                        }
                    }
                }
            });
        }
        private async Task ParserPdf(string pdfFilePath)
        {
            using (PdfReader reader = new PdfReader(pdfFilePath))
            {
                for (int page = 1; page <= reader.NumberOfPages; page++)
                {
                    var its = new Core.LocationTextExtractionStrategyEx();
                    String str = PdfTextExtractor.GetTextFromPage(reader, page, its);

                    using (StringReader stringReader = new StringReader(str))
                    {
                        string line;

                        while ((line = stringReader.ReadLine()) != null)
                        {
                            if (line.StartsWith("#"))
                            {
                                int col = int.Parse(line.Substring(1, line.IndexOf(" ")).Trim());
                                var column = GetCulumn(col);
                                if (!dPages.ContainsKey(page))
                                    dPages[page] = new Dictionary<int, StringBuilder>();

                                if (!dPages[page].ContainsKey(column))
                                    dPages[page][column] = new StringBuilder();

                                dPages[page][column].Append(line.Substring(line.IndexOf(" ")));
                            }
                        }
                    }
                }
            }
        }
        private async Task InsertIntoDb(List<Newspaper> unparsed)
        {
            var sbSql = new StringBuilder();
            var parameters = new Dictionary<string, object>();
            int p = 1;
            foreach (var print in parsed.Values)
            {
                sbSql.Append($"INSERT INTO newspaper (classified_id,contract_no,area_id,press_no,post_date,begin_date,end_date,content,phone,created,col,page) VALUES(@p{p}1,@p{p}2,@p{p}3,@p{p}4,@p{p}5,@p{p}6,@p{p}7,@p{p}8,@p{p}9,@p{p}_10,@p{p}_11,@p{p}_12) " +
                    $"ON CONFLICT ON CONSTRAINT u_constraint_print DO UPDATE SET area_id=@p{p}3,press_no=@p{p}4,post_date=@p{p}5,begin_date=@p{p}6,end_date=@p{p}7,content=@p{p}8,phone=@p{p}9,created=@p{p}_10,col=@p{p}_11,page=@p{p}_12 ; ");
                parameters.Add($"@p{p}1", print.ClassifiedId);
                parameters.Add($"@p{p}2", print.ContractNo);
                parameters.Add($"@p{p}3", print.AreaId);
                parameters.Add($"@p{p}4", print.PressNo);
                parameters.Add($"@p{p}5", print.PostDate);
                parameters.Add($"@p{p}6", print.BeginDate);
                parameters.Add($"@p{p}7", print.EndDate);
                parameters.Add($"@p{p}8", print.Content);
                parameters.Add($"@p{p}9", print.Phone);
                parameters.Add($"@p{p}_10", print.Created);
                parameters.Add($"@p{p}_11", print.Col);
                parameters.Add($"@p{p}_12", print.Page);
                p++;
            }

            foreach (var print in unparsed)
            {
                sbSql.Append($"INSERT INTO newspaper (classified_id,contract_no,area_id,press_no,post_date,begin_date,end_date,content,phone,created,col,page) VALUES(@p{p}1,@p{p}2,@p{p}3,@p{p}4,@p{p}5,@p{p}6,@p{p}7,@p{p}8,@p{p}9,@p{p}_10,@p{p}_11,@p{p}_12) " +
                    $"ON CONFLICT ON CONSTRAINT u_constraint_print DO UPDATE SET area_id=@p{p}3,press_no=@p{p}4,post_date=@p{p}5,begin_date=@p{p}6,end_date=@p{p}7,content=@p{p}8,phone=@p{p}9,created=@p{p}_10,col=@p{p}_11,page=@p{p}_12 ; ");
                parameters.Add($"@p{p}1", print.ClassifiedId);
                parameters.Add($"@p{p}2", print.ContractNo);
                parameters.Add($"@p{p}3", print.AreaId);
                parameters.Add($"@p{p}4", print.PressNo);
                parameters.Add($"@p{p}5", print.PostDate);
                parameters.Add($"@p{p}6", print.BeginDate);
                parameters.Add($"@p{p}7", print.EndDate);
                parameters.Add($"@p{p}8", print.Content);
                parameters.Add($"@p{p}9", print.Phone);
                parameters.Add($"@p{p}_10", print.Created);
                parameters.Add($"@p{p}_11", print.Col);
                parameters.Add($"@p{p}_12", print.Page);
                p++;
            }
            
            var command = new Command(sbSql.ToString(), parameters);
            await  service.ExecuteBulkOperationAsync(command);
        }
        private int GetCulumn(int col)
        {
            int currentCol = 1;
            if (col < MIN_COL2)
                currentCol = 1;
            else if (col >= MIN_COL2 && col < MIN_COL3)
                currentCol = 2;
            else if (col >= MIN_COL3 && col < MIN_COL4)
                currentCol = 3;
            else if (col >= MIN_COL4 && col < MIN_COL5)
                currentCol = 4;
            else if (col >= MIN_COL5)
                currentCol = 5;
            return currentCol;
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

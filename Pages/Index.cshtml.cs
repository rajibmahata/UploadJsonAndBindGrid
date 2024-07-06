using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using System.Data;
using System.Text.Json.Serialization;
using UploadJsonAndBindGrid.Models;
using UploadJsonAndBindGrid.Utility;
using System.Linq;
using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace UploadJsonAndBindGrid.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private IHostingEnvironment _environment;
        [BindProperty]
        public DataTablesRequest DataTablesRequest { get; set; }
        public  IList<JsonModel> _jsonItems;

        [BindProperty]
        public IFormFile UploadJsonFile { get; set; }

        public string Message { get; set; }
        private static string filePath { get; set; }
        public IndexModel(ILogger<IndexModel> logger, IHostingEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task OnGetAsync()
        {

        }
        public async Task OnPostAsync()
        {

            if (UploadJsonFile == null || UploadJsonFile.Length == 0)
            {
                return;
            }

            string path = Path.Combine(_environment.ContentRootPath, "Uploads");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            _logger.LogInformation($"Uploading {UploadJsonFile.FileName}.");
            string targetFileName = $"{_environment.ContentRootPath}/{UploadJsonFile.FileName}";

            var file = Path.Combine(_environment.ContentRootPath, "Uploads", UploadJsonFile.FileName);
            using (var fileStream = new FileStream(file, FileMode.Create))
            {
                await UploadJsonFile.CopyToAsync(fileStream);
                this.Message += string.Format("<b>{0}</b> uploaded.<br />", UploadJsonFile.FileName);
            }

            filePath = file;

        }


        public async Task<JsonResult> OnPostViewAsync()
        {
            _jsonItems = JsonFileReader.Read<IList<JsonModel>>(filePath);

            if (_jsonItems != null && _jsonItems.Count > 0)
            {
                var recordsTotal = _jsonItems.Count();

                var jsonItemQuery = _jsonItems.AsQueryable();

                var searchText = DataTablesRequest.Search.Value?.ToUpper();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    jsonItemQuery = jsonItemQuery.Where(s =>
                        s.color.ToUpper().Contains(searchText) ||
                        s.value.ToUpper().Contains(searchText)
                    );
                }

                var recordsFiltered = jsonItemQuery.Count();

                var sortColumnName = DataTablesRequest.Columns.ElementAt(DataTablesRequest.Order.ElementAt(0).Column).Name;
                var sortDirection = DataTablesRequest.Order.ElementAt(0).Dir.ToLower();

                // using System.Linq.Dynamic.Core
                //   jsonItemQuery = jsonItemQuery.OrderBy($"{sortColumnName} {sortDirection}");

                var skip = DataTablesRequest.Start;
                var take = DataTablesRequest.Length;
                var data = jsonItemQuery
                    .Skip(skip)
                    .Take(take)
                    .ToList();

                return new JsonResult(new
                {
                    Draw = DataTablesRequest.Draw,
                    RecordsTotal = recordsTotal,
                    RecordsFiltered = recordsFiltered,
                    Data = data
                });
            } else
            {
                return new JsonResult(new
                {
                    Draw = DataTablesRequest.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<JsonModel>()
                });
            }
        }
    }
}

using DinkToPdf.Contracts;
using DinkToPdf;
using GoogleDriveClone.Data;
using GoogleDriveClone.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using SelectPdf;
using System.Drawing.Printing;
using Microsoft.AspNetCore.Html;


[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConverter _converter;

    public HomeController(ApplicationDbContext context, IConverter converter)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public IActionResult Index()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null)
        {
            return RedirectToAction("Login", "User");
        }
        
        var userId = int.Parse(userIdClaim.Value);
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        ViewData["UserName"] = user?.Name;
        var folders = _context.Folders.Where(f => f.CreatedBy == userId && f.ParentFolderId == null && f.IsActive).ToList();
        var files = _context.Files.Where(f => f.CreatedBy == userId && f.ParentFolderId == 0 && f.IsActive).ToList();

        var viewModel = new FolderFileViewModel
        {
            Folders = folders,
            Files = files
        };

        return View(viewModel);
    }

    public IActionResult ViewFolder(int folderId)
{
    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
    if (userIdClaim == null)
    {
        return RedirectToAction("Login", "User");
    }

    var userId = int.Parse(userIdClaim.Value);
    var folders = _context.Folders.Where(f => f.ParentFolderId == folderId && f.CreatedBy == userId && f.IsActive).ToList();
    var files = _context.Files.Where(f => f.ParentFolderId == folderId && f.CreatedBy == userId && f.IsActive).ToList();

    var viewModel = new FolderFileViewModel
    {
        Folders = folders,
        Files = files
    };

    ViewBag.ParentFolderId = folderId;
    return View(viewModel);
}


    [HttpPost]
    public IActionResult CreateFolder(string folderName, int? parentFolderId)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null)
        {
            return RedirectToAction("Login", "User");
        }
        try
        {
            if(folderName == null)
            {
                var userId = int.Parse(userIdClaim.Value);
                var folder = new Folder
                {
                    Name = "NewFolder",
                    ParentFolderId = parentFolderId,
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now,
                    IsActive = true
                };
                _context.Folders.Add(folder);
                _context.SaveChanges();
            }
            else
            {
                var userId = int.Parse(userIdClaim.Value);
                var folder = new Folder
                {
                    Name = folderName,
                    ParentFolderId = parentFolderId,
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now,
                    IsActive = true
                };
                _context.Folders.Add(folder);
                _context.SaveChanges();
            }
            
            
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            return Content($"Enter Folder Name");
        }
    }



  
    [HttpPost]
   
    public IActionResult DeleteFolder(int folderId)
    {
        var folder = _context.Folders.Where(f=>f.Id == folderId).FirstOrDefault();
        Console.WriteLine("folder", folder);
        if (folder != null)
        {
            folder.IsActive = false;
            var filesInFolder = _context.Files.Where(f => f.ParentFolderId == folderId).ToList();
            foreach (var file in filesInFolder)
            {
                file.IsActive = false;
            }
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteFile(int fileId)
    {
        var file = _context.Files.Where(f => f.Id == fileId).FirstOrDefault();
        Console.WriteLine("folder", file);
        if (file != null)
        {
            file.IsActive = false;
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }


    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile file, int parentFolderId)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "File is missing or empty.");
            return RedirectToAction("Index");
        }

        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null)
        {
            return RedirectToAction("Login", "User");
        }

        var userId = int.Parse(userIdClaim.Value);
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var newFile = new GoogleDriveClone.Models.File
        {
            Name = uniqueFileName,
            ParentFolderId = parentFolderId,
            FileExt = Path.GetExtension(file.FileName),
            FileSizeInKB = (int)(file.Length / 1024),
            CreatedBy = userId,
            UploadedOn = DateTime.Now,
            IsActive = true
        };
        _context.Files.Add(newFile);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }



    public IActionResult DownloadFile(int id)
{
     var file = _context.Files.FirstOrDefault(f => f.Id == id);
     if (file == null)
     {
         return NotFound();
     }
 
     var filePath = Path.Combine("wwwroot/files", file.Name);
     var fileBytes = System.IO.File.ReadAllBytes(filePath);
     var contentType = "application/octet-stream";
     return File(fileBytes, contentType, file.Name);
}

    private string GetContentType(string path)
    {
        var types = GetMimeTypes();
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return types.ContainsKey(ext) ? types[ext] : "application/octet-stream"; 
    }

    private Dictionary<string, string> GetMimeTypes()
    {
        return new Dictionary<string, string>
    {
        { ".txt", "text/plain" },
        { ".pdf", "application/pdf" },
        { ".doc", "application/vnd.ms-word" },
        { ".docx", "application/vnd.ms-word" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif", "image/gif" },
        { ".csv", "text/csv" }
    };
    }




    public IActionResult DownloadMetaAsPdf()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null)
        {
            return RedirectToAction("Login", "User");
        }

        var userId = int.Parse(userIdClaim.Value);
        HtmlToPdf converter = new SelectPdf.HtmlToPdf();

        try
        {
            var folders = _context.Folders.Where(f => f.CreatedBy == userId && f.IsActive).ToList();
            var files = _context.Files.Where(f => f.CreatedBy == userId && f.IsActive).ToList();

            var htmlContent = new StringBuilder();
            htmlContent.Append("<html>");
            htmlContent.Append("<head><title>Folder Meta Information</title></head>");
            htmlContent.Append("<body>");
            htmlContent.Append("<h1>Folder Meta Information</h1>");
            htmlContent.Append("<ul>");

            foreach (var folder in folders)
            {
                htmlContent.Append($"<li>Folder: {folder.Name} (FolderId: {folder.Id})</li>");
            }
            htmlContent.Append("<body>");
            htmlContent.Append("<h1>File Meta Information</h1>");
            htmlContent.Append("<ul>");
            foreach (var file in files)
            {
                htmlContent.Append($"<li>File: {file.Name} ({file.FileExt}, {file.FileSizeInKB} KB, parentFolder {file.ParentFolderId})</li>");
            }

            htmlContent.Append("</ul>");
            htmlContent.Append("</body>");
            htmlContent.Append("</html>");

            
            PdfDocument doc = converter.ConvertHtmlString(htmlContent.ToString(), "Http://stewart.pk");
            
            var bytes = doc.Save();
            
            
            doc.Close();

            
            return File(bytes, "application/pdf", "MetaInformation.pdf");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while generating the PDF: {ex.Message}");
            return Content($"An error occurred while generating the PDF: {ex.Message}");
        }
    }


}

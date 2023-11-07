using System.Net;
using Domain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/filesave", async (HttpRequest request, IEnumerable<IFormFile> files, IHostEnvironment env) =>
    {
        var maxAllowedFile = 3;
        long maxFileSize = 1024 * 1500;
        var filesProcessed = 0;

        // request.Scheme = "http"; request.Host = "localhost:5084";
        var resourcePath = new Uri($"{request.Scheme}://{request.Host}/");
        List<UploadResult> uploadResults = new();
        
        foreach(var file in files)
        {
           var uploadResult = new UploadResult();
           string trustedFileNameForFileStorage;
           var untrustedFileName = file.FileName;
           uploadResult.FileName = untrustedFileName;

           var trustedFileNameForDisplay = WebUtility.HtmlEncode(untrustedFileName);

           if (filesProcessed < maxAllowedFile)
           {
               if (file.Length == 0)
               {
                   Console.WriteLine($"File {trustedFileNameForDisplay} is empty (code : 1)");
                   uploadResult.ErrorCode = 1;
               }
               else if (file.Length > maxFileSize)
               {
                   Console.WriteLine($"File {trustedFileNameForDisplay} of {file.Length} bytes is too big than the limit {maxFileSize} bytes (code : 2)");
                   uploadResult.ErrorCode = 2;
               }
               else
               {
                   try
                   {
                       trustedFileNameForFileStorage = Path.GetRandomFileName();

                       var path = Path.Combine(
                           env.ContentRootPath,
                           env.EnvironmentName,
                           "unsafe_uploads",
                           trustedFileNameForFileStorage);

                       await using var fs = new FileStream(path, FileMode.Create);
                       await file.CopyToAsync(fs);

                       Console.WriteLine($"File {trustedFileNameForDisplay} uploaded successfully to path: {path}");
                       
                       uploadResult.Uploaded = true;    
                       uploadResult.StoredFileName = trustedFileNameForFileStorage;
                   }
                   catch (IOException e)
                   {
                       Console.WriteLine($"File {trustedFileNameForDisplay} error on upload (code : 3) : {e.Message}");
                       uploadResult.ErrorCode = 3;
                   }
               }
               
               filesProcessed++;
           }
           else
           {
               Console.WriteLine($"File {trustedFileNameForDisplay} not uploaded because exceed {maxAllowedFile} files");
               uploadResult.ErrorCode = 4;
           }
           
           uploadResults.Add(uploadResult);
        }
        
        return Results.Created(resourcePath, uploadResults);
    })
    .WithName("FileUpload")
    .WithOpenApi();

app.Run();


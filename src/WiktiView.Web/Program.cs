using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using WiktiView.Web.Configuration;
using WiktiView.Web.Hosting;
using WiktiView.Web.Indexing;
using WiktiView.Web.Search;
using WiktiView.Web.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DictionaryOptions>()
    .BindConfiguration(DictionaryOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart()
    .PostConfigure<IHostEnvironment>((options, environment) =>
    {
        options.DataFilePath = Path.GetFullPath(options.DataFilePath, environment.ContentRootPath);
        options.IndexCachePath = Path.GetFullPath(options.IndexCachePath, environment.ContentRootPath);
    });

builder.Services.AddSingleton<DictionaryIndex>();
builder.Services.AddSingleton<IndexCache>();
builder.Services.AddSingleton<WordReader>();
builder.Services.AddSingleton<WordSearchService>();
builder.Services.AddHostedService<IndexInitializationService>();

builder.Services.AddControllersWithViews();

// Render German/Turkish characters as-is instead of HTML entities (ä instead of &#xE4;).
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/Error");
app.UseRouting();

app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

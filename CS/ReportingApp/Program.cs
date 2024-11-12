using Azure.AI.OpenAI;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using DevExpress.Security.Resources;
using DevExpress.XtraReports.Web.Extensions;
using DevExpress.XtraReports.Web.WebDocumentViewer;
using ReportingApp;
using ReportingApp.Data;
using ReportingApp.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using DevExpress.AIIntegration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDevExpressControls();
builder.Services.AddScoped<ReportStorageWebExtension, CustomReportStorageWebExtension>();
builder.Services.AddMvc();
builder.Services.ConfigureReportingServices(configurator => {
    if(builder.Environment.IsDevelopment()) {
        configurator.UseDevelopmentMode();
    }
    configurator.ConfigureReportDesigner(designerConfigurator => {
        designerConfigurator.RegisterDataSourceWizardConnectionStringsProvider<CustomSqlDataSourceWizardConnectionStringsProvider>();
    });
    configurator.ConfigureWebDocumentViewer(viewerConfigurator => {
        viewerConfigurator.UseCachedReportSourceBuilder();
        viewerConfigurator.RegisterConnectionProviderFactory<CustomSqlDataConnectionProviderFactory>();
    });
});
var azureOpenAIClient = new AzureOpenAIClient(new Uri(EnvSettings.AzureOpenAIEndpoint),
    new System.ClientModel.ApiKeyCredential(EnvSettings.AzureOpenAIKey));
    
var chatClient = azureOpenAIClient.GetChatClient(EnvSettings.DeploymentName);

builder.Services.AddDbContext<ReportDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("ReportsDataConnectionString")));
builder.Services.AddSingleton(chatClient);
builder.Services.AddSingleton<IAIAssistantProvider, AIAssistantProvider>();
builder.Services.AddScoped<DocumentOperationService, AIDocumentOperationService>();
builder.Services.AddDevExpressAI(config =>
{
    config.RegisterOpenAIAssistants(azureOpenAIClient, EnvSettings.DeploymentName);
});

var app = builder.Build();
using(var scope = app.Services.CreateScope()) {
    var services = scope.ServiceProvider;
    services.GetService<ReportDbContext>().InitializeDatabase();
}
var contentDirectoryAllowRule = DirectoryAccessRule.Allow(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Content")).FullName);
AccessSettings.ReportingSpecificResources.TrySetRules(contentDirectoryAllowRule, UrlAccessRule.Allow());
DevExpress.XtraReports.Configuration.Settings.Default.UserDesignerOptions.DataBindingMode = DevExpress.XtraReports.UI.DataBindingMode.Expressions;
app.UseDevExpressControls();
System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
if(app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
} else {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

string contentPath = app.Environment.ContentRootPath;
AppDomain.CurrentDomain.SetData("DataDirectory", contentPath);

app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
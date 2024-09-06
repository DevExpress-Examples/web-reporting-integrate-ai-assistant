using System.Collections.Generic;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports.Web.ReportDesigner.Services;
using DevExpress.XtraReports.Web.WebDocumentViewer;
using Microsoft.AspNetCore.Mvc;

namespace ReportingApp.Controllers {
    public class HomeController : Controller {
        public IActionResult Index() {
            return View();
        }
        public IActionResult Error() {
            Models.ErrorModel model = new Models.ErrorModel();
            return View(model);
        }
        
        public IActionResult ReportDesigner(
            [FromServices] IReportDesignerModelBuilder reportDesignerModelBuilder, 
            [FromQuery] string reportName) {
            // Create a SQL data source with the specified connection string.
            SqlDataSource ds = new SqlDataSource("NWindConnectionString");
            // Create a SQL query to access the Products data table.
            SelectQuery query = SelectQueryFluentBuilder.AddTable("Products").SelectAllColumnsFromTable().Build("Products");
            ds.Queries.Add(query);
            ds.RebuildResultSchema();

            reportName = string.IsNullOrEmpty(reportName) ? "TestReport" : reportName;
            var designerModel = reportDesignerModelBuilder
                .Report(reportName)
                .DataSources(x => {
                    x.Add("Northwind", ds);
                })
                .BuildModel();
            return View(designerModel);
        }

        public IActionResult DocumentViewer(
            [FromServices] IWebDocumentViewerClientSideModelGenerator viewerModelGenerator,
            [FromQuery] string reportName) {
            reportName = string.IsNullOrEmpty(reportName) ? "TestReport" : reportName;
            var viewerModel = viewerModelGenerator.GetModel(reportName, CustomWebDocumentViewerController.DefaultUri);
            return View(viewerModel);
        }
    }
}
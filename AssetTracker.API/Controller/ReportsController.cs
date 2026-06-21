using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetTracker.API.Data;
using AssetTracker.API.Models;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AssetTracker.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Asset Manager")] // Admin and Asset Manager access
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/reports/checkout-history
        [HttpGet("checkout-history")]
        public async Task<ActionResult<IEnumerable<object>>> GetCheckoutHistory(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? category,
            [FromQuery] int? assetId,
            [FromQuery] int? userId)
        {
            var query = _context.Checkouts
                .Include(c => c.Asset)
                .Include(c => c.User)
                .AsQueryable();

            // Filter by date range
            if (fromDate.HasValue)
            {
                query = query.Where(c => c.CheckoutTimestamp.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(c => c.CheckoutTimestamp.Date <= toDate.Value.Date);
            }

            // Filter by category
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(c => c.Asset!.Category == category);
            }

            // Filter by specific asset
            if (assetId.HasValue && assetId.Value > 0)
            {
                query = query.Where(c => c.AssetID == assetId.Value);
            }

            // Filter by specific user
            if (userId.HasValue && userId.Value > 0)
            {
                query = query.Where(c => c.UserID == userId.Value);
            }

            var checkouts = await query
                .OrderByDescending(c => c.CheckoutTimestamp)
                .Select(c => new
                {
                    checkoutID = c.CheckoutID,
                    checkoutDate = c.CheckoutTimestamp,
                    assetName = c.Asset!.Name,
                    category = c.Asset.Category,
                    serialNumber = c.Asset.SerialNumber,
                    userFullName = c.UserFullName,
                    userEmail = c.UserEmail,
                    returnDate = c.ActualReturnDate,
                    status = c.Status
                })
                .ToListAsync();

            return Ok(checkouts);
        }

        // GET: api/reports/checkout-history/excel
        [HttpGet("checkout-history/excel")]
        public async Task<IActionResult> DownloadCheckoutHistoryExcel(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? category,
            [FromQuery] int? assetId,
            [FromQuery] int? userId)
        {
            var query = _context.Checkouts
                .Include(c => c.Asset)
                .Include(c => c.User)
                .AsQueryable();

            // Apply same filters
            if (fromDate.HasValue)
                query = query.Where(c => c.CheckoutTimestamp.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(c => c.CheckoutTimestamp.Date <= toDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
                query = query.Where(c => c.Asset!.Category == category);

            if (assetId.HasValue && assetId.Value > 0)
                query = query.Where(c => c.AssetID == assetId.Value);

            if (userId.HasValue && userId.Value > 0)
                query = query.Where(c => c.UserID == userId.Value);

            var checkouts = await query
                .OrderByDescending(c => c.CheckoutTimestamp)
                .ToListAsync();

            // Generate Excel file
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Checkout History");

            // Add headers
            worksheet.Cells[1, 1].Value = "Checkout Date";
            worksheet.Cells[1, 2].Value = "Asset Name";
            worksheet.Cells[1, 3].Value = "Category";
            worksheet.Cells[1, 4].Value = "Serial Number";
            worksheet.Cells[1, 5].Value = "User Full Name";
            worksheet.Cells[1, 6].Value = "User Email";
            worksheet.Cells[1, 7].Value = "Return Date";
            worksheet.Cells[1, 8].Value = "Status";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Add data
            int row = 2;
            foreach (var checkout in checkouts)
            {
                worksheet.Cells[row, 1].Value = checkout.CheckoutTimestamp.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cells[row, 2].Value = checkout.Asset?.Name ?? "N/A";
                worksheet.Cells[row, 3].Value = checkout.Asset?.Category ?? "N/A";
                worksheet.Cells[row, 4].Value = checkout.Asset?.SerialNumber ?? "N/A";
                worksheet.Cells[row, 5].Value = checkout.UserFullName;
                worksheet.Cells[row, 6].Value = checkout.UserEmail;
                worksheet.Cells[row, 7].Value = checkout.ActualReturnDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not Returned";
                worksheet.Cells[row, 8].Value = checkout.Status;
                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Convert to byte array
            var excelData = package.GetAsByteArray();

            // Return file
            var fileName = $"CheckoutHistory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: api/reports/checkout-history/pdf
        [HttpGet("checkout-history/pdf")]
        public async Task<IActionResult> DownloadCheckoutHistoryPdf(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? category,
            [FromQuery] int? assetId,
            [FromQuery] int? userId)
        {
            var query = _context.Checkouts
                .Include(c => c.Asset)
                .Include(c => c.User)
                .AsQueryable();

            // Apply same filters
            if (fromDate.HasValue)
                query = query.Where(c => c.CheckoutTimestamp.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(c => c.CheckoutTimestamp.Date <= toDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
                query = query.Where(c => c.Asset!.Category == category);

            if (assetId.HasValue && assetId.Value > 0)
                query = query.Where(c => c.AssetID == assetId.Value);

            if (userId.HasValue && userId.Value > 0)
                query = query.Where(c => c.UserID == userId.Value);

            var checkouts = await query
                .OrderByDescending(c => c.CheckoutTimestamp)
                .ToListAsync();

            // Generate PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header()
                        .Text("Checkout History Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    // Content
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Report info
                            column.Item().Text(text =>
                            {
                                text.Span("Generated: ").SemiBold();
                                text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"));

                                if (fromDate.HasValue || toDate.HasValue)
                                {
                                    text.Span(" | Date Range: ").SemiBold();
                                    text.Span($"{fromDate?.ToString("yyyy-MM-dd") ?? "All"} to {toDate?.ToString("yyyy-MM-dd") ?? "All"}");
                                }
                            });

                            // Table
                            column.Item().Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Checkout Date
                                    columns.RelativeColumn(3); // Asset Name
                                    columns.RelativeColumn(2); // Category
                                    columns.RelativeColumn(2); // Serial Number
                                    columns.RelativeColumn(3); // User Name
                                    columns.RelativeColumn(3); // User Email
                                    columns.RelativeColumn(2); // Return Date
                                    columns.RelativeColumn(1); // Status
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Checkout Date").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Asset Name").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Category").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Serial Number").SemiBold();
                                    header.Cell().Element(CellStyle).Text("User Name").SemiBold();
                                    header.Cell().Element(CellStyle).Text("User Email").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Return Date").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Status").SemiBold();

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold())
                                            .PaddingVertical(5)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Black);
                                    }
                                });

                                // Data rows
                                foreach (var checkout in checkouts)
                                {
                                    table.Cell().Element(CellStyle).Text(checkout.CheckoutTimestamp.ToString("yyyy-MM-dd HH:mm"));
                                    table.Cell().Element(CellStyle).Text(checkout.Asset?.Name ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(checkout.Asset?.Category ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(checkout.Asset?.SerialNumber ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(checkout.UserFullName);
                                    table.Cell().Element(CellStyle).Text(checkout.UserEmail);
                                    table.Cell().Element(CellStyle).Text(checkout.ActualReturnDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not Returned");
                                    table.Cell().Element(CellStyle).Text(checkout.Status);

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .PaddingVertical(5);
                                    }
                                }
                            });
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            // Generate PDF bytes
            var pdfData = document.GeneratePdf();

            // Return file
            var fileName = $"CheckoutHistory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            return File(pdfData, "application/pdf", fileName);
        }

        // GET: api/reports/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.Assets
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/reports/assets
        [HttpGet("assets")]
        public async Task<ActionResult<IEnumerable<object>>> GetAssets()
        {
            var assets = await _context.Assets
                .OrderBy(a => a.Name)
                .Select(a => new { id = a.AssetID, name = a.Name, serialNumber = a.SerialNumber })
                .ToListAsync();

            return Ok(assets);
        }

        // GET: api/reports/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users
                .Where(u => u.AccountStatus == "Active")
                .OrderBy(u => u.FullName)
                .Select(u => new { id = u.UserID, name = u.FullName, email = u.Email })
                .ToListAsync();

            return Ok(users);
        }
    }
}

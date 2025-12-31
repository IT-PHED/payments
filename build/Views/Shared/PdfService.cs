using PhedPay.Models;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace PhedPay.Services
{
    public class PdfService
    {
        // Colors sampled from the image
        private readonly string PhedBlue = "#3067BA";
        private readonly string BorderGreen = "#718E72";
        private readonly string TableHeaderPurple = "#8E9AFF";
        private readonly string LightRowBlue = "#E8EFFF";

        public byte[] GenerateOfficialReceipt(PhedReceiptItem receiptData)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
            var logoBytes = File.ReadAllBytes(logoPath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                    // --- 1. LOGO & CONTACT HEADER ---
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().PaddingTop(-10).Column(c =>
                        {
                            // Placeholder for the PHED Logo
                            c.Item().Width(120).Image(logoBytes);

                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Row(r => {
                                r.ConstantItem(60).Text("Address :").Bold();
                                r.RelativeItem().Text("#1 Moscow Road, Port Harcourt, Rivers state, Nigeria").Bold();
                            });
                            col.Item().Row(r => {
                                r.ConstantItem(85).Text("Customer Care :").Bold();
                                r.RelativeItem().Text("070022557433").Bold();
                            });
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // --- 2. BLUE RECEIPT BAR ---
                        col.Item().Background(PhedBlue).PaddingVertical(3).AlignCenter()
                            .Text("PAYMENT RECEIPT").FontColor(Colors.White).SemiBold().FontSize(11);

                        col.Item().Height(20);

                        // --- 3. MAIN BODY (Content + Sidebar) ---
                        col.Item().Row(row =>
                        {
                            // Left Side: Transaction Details
                            row.RelativeItem(3).Column(mainCol =>
                            {
                                // Info Box with Green Border
                                mainCol.Item().Border(1).BorderColor(BorderGreen).Padding(12).Column(details =>
                                {
                                    // Improved DetailRow Helper
                                    void DetailRow(string label, string value, bool isLast = false)
                                    {
                                        details.Item().PaddingVertical(3).Row(r =>
                                        {
                                            r.ConstantItem(100).Text(label).FontColor(Colors.Grey.Darken3).SemiBold();
                                            r.RelativeItem().Text(value ?? "-").Medium();
                                        });

                                        // Optional: Add a very thin separator if not the last item
                                        if (!isLast)
                                        {
                                           // Uncomment if you want subtle lines
                                            details.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                                        }
                                    }

                                    DetailRow("Date/Time :", receiptData.PAYMENTDATETIME);
                                    DetailRow("Account :", receiptData.CUSTOMER_NO);
                                    DetailRow("Meter No. :", receiptData.METER_NO);
                                    DetailRow("Address :", receiptData.ADDRESS);
                                    DetailRow("Tariff :", receiptData.TARIFF);
                                    DetailRow("eReceipt No. :", receiptData.RECEIPTNUMBER);
                                    DetailRow("Handled By :", "PHED", true);

                                    // Separator before Token
                                    details.Item().PaddingVertical(5).LineHorizontal(1).LineColor(BorderGreen);

                                    // Refined Token & Units Row
                                    details.Item().Row(r =>
                                    {
                                        r.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text("Token :").FontSize(8).FontColor(Colors.Grey.Medium).Bold();
                                            c.Item().PaddingTop(2).Text(receiptData.TOKENDESC).FontSize(11).Bold().FontColor(PhedBlue);
                                        });

                                        r.ConstantItem(80).Column(c =>
                                        {
                                            c.Item().Text("Units :").FontSize(8).FontColor(Colors.Grey.Medium).Bold();
                                            c.Item().PaddingTop(2).Text(receiptData.UNITSACTUAL).FontSize(11).Bold();
                                        });
                                    });
                                });

                                mainCol.Item().Height(20);

                                // Transaction Table (Remaining same as per instructions)
                                mainCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(30);
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(100);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(TableHeaderPurple).Padding(5).Text("S/N").FontColor(Colors.White);
                                        header.Cell().Background(TableHeaderPurple).Padding(5).Text("Transaction Details (Cash)").FontColor(Colors.White);
                                        header.Cell().Background(TableHeaderPurple).Padding(5).AlignRight().Text("Amount").FontColor(Colors.White);
                                    });

                                    if (receiptData.DETAILS != null)
                                    {
                                        int sn = 1;
                                        foreach (var item in receiptData.DETAILS)
                                        {
                                            var isEven = sn % 2 == 0;
                                            var bgColor = isEven ? Colors.White : Color.FromHex(LightRowBlue);

                                            table.Cell().Background(bgColor).Padding(5).Text(sn.ToString());
                                            table.Cell().Background(bgColor).Padding(5).Text(item.HEAD);
                                            table.Cell().Background(bgColor).Padding(5).AlignRight().Text(item.AMOUNT);
                                            sn++;
                                        }
                                    }
                                });
                            });

                            // Right Side: Sidebar Accent (Remaining same as per instructions)
                            row.ConstantItem(100).PaddingLeft(10).Column(sideCol =>
                            {
                                sideCol.Item().Height(250).Background(PhedBlue);
                                sideCol.Item().PaddingTop(10).AlignCenter().Width(80).Height(80).Placeholder(); // For QR Code
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated via PHED Payment Portal").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
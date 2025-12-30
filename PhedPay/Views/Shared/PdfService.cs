using PhedPay.Models;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PhedPay.Services
{
   

   
        public class PdfService
        {
            public byte[] GenerateOfficialReceipt(PhedReceiptItem receiptData)
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                        // --- HEADER ---
                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("PHED").Bold().FontSize(24).FontColor(Colors.Blue.Medium);
                                col.Item().Text("Port Harcourt Electricity Distribution").FontSize(10);
                                col.Item().Text("Payment Receipt").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken2);
                            });

                            row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy"));
                        });

                        // --- CONTENT ---
                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            // 1. Token Section
                            if (!string.IsNullOrEmpty(receiptData.TOKENDESC))
                            {
                                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                {
                                    c.Item().AlignCenter().Text("TOKEN").FontSize(12).Bold().FontColor(Colors.Grey.Darken2);
                                    c.Item().AlignCenter().Text(receiptData.TOKENDESC).FontSize(24).Bold().LetterSpacing(0.1f);
                                    c.Item().AlignCenter().Text($"Units: {receiptData.UNITSACTUAL} kwh").FontSize(12);
                                });
                            }

                            col.Item().Height(15);

                            // 2. Customer Details Table
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(120);
                                    columns.RelativeColumn();
                                });

                                void InfoRow(string label, string value)
                                {
                                    table.Cell().Text(label).Bold();
                                    table.Cell().Text(value ?? "-");
                                }

                                InfoRow("Receipt No:", receiptData.RECEIPTNUMBER);
                                InfoRow("Date:", receiptData.PAYMENTDATETIME);
                                InfoRow("Meter No:", receiptData.METER_NO);
                                InfoRow("Account No:", receiptData.CUSTOMER_NO);
                                InfoRow("Address:", receiptData.ADDRESS);
                                InfoRow("Tariff:", receiptData.TARIFF);
                            });

                            col.Item().Height(15);
                            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            col.Item().Height(15);

                            // 3. Payment Breakdown Table
                            col.Item().Text("Payment Breakdown").Bold().FontSize(12);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                                // --- FIXED HEADER ---
                                table.Header(header =>
                                {
                                    static IContainer CellStyle(IContainer container) =>
                                        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                                    header.Cell().Element(CellStyle).Text("Description").SemiBold();
                                    header.Cell().Element(CellStyle).AlignRight().Text("Amount").SemiBold();
                                });

                                if (receiptData.DETAILS != null)
                                {
                                    foreach (var item in receiptData.DETAILS)
                                    {
                                        table.Cell().Padding(5).Text(item.HEAD);
                                        table.Cell().Padding(5).AlignRight().Text(item.AMOUNT);
                                    }
                                }

                                table.Footer(footer =>
                                {
                                    footer.Cell().Padding(5).Text("Total Paid").Bold();
                                    footer.Cell().Padding(5).AlignRight().Text($"NGN {receiptData.AMOUNT}").Bold();
                                });
                            });

                            col.Item().Height(30);
                            col.Item().AlignCenter().Text("Thank you for using PHED.").FontSize(10).Italic();
                        });

                        // --- FOOTER ---
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Generated via PHED Payment Portal | Page ");
                            x.CurrentPageNumber();
                        });
                    });
                });

                return document.GeneratePdf();
            }
        }
    }





 

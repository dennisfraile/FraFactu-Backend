using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FraFactu.Infrastructure.Services;

public class AgingPdfExporter : IAgingPdfExporter
{
    public byte[] Generar(AgingReporteDto reporte)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        string M(decimal v) => v.ToString("N2");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(t => t.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Antigüedad de saldos").FontSize(14).Bold();
                    col.Item().Text($"Fecha de corte: {reporte.FechaCorte:dd/MM/yyyy}").FontSize(9);
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });

                    void HeaderCell(string s) => table.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(s).Bold();
                    HeaderCell("Cliente"); HeaderCell("Plan"); HeaderCell("Por vencer");
                    HeaderCell("1-30"); HeaderCell("31-60"); HeaderCell("61-90"); HeaderCell("+90"); HeaderCell("Total");

                    foreach (var cliente in reporte.Clientes)
                    {
                        foreach (var plan in cliente.Planes)
                        {
                            table.Cell().Padding(3).Text(cliente.ClienteNombre);
                            table.Cell().Padding(3).Text(plan.PlanId.ToString());
                            table.Cell().Padding(3).AlignRight().Text(M(plan.PorVencer));
                            table.Cell().Padding(3).AlignRight().Text(M(plan.D1a30));
                            table.Cell().Padding(3).AlignRight().Text(M(plan.D31a60));
                            table.Cell().Padding(3).AlignRight().Text(M(plan.D61a90));
                            table.Cell().Padding(3).AlignRight().Text(M(plan.Mas90));
                            table.Cell().Padding(3).AlignRight().Text(M(plan.Total));
                        }
                        table.Cell().ColumnSpan(2).Padding(3).Text($"Subtotal {cliente.ClienteNombre}").Italic();
                        table.Cell().Padding(3).AlignRight().Text(M(cliente.Subtotal.PorVencer)).Italic();
                        table.Cell().Padding(3).AlignRight().Text(M(cliente.Subtotal.D1a30)).Italic();
                        table.Cell().Padding(3).AlignRight().Text(M(cliente.Subtotal.D31a60)).Italic();
                        table.Cell().Padding(3).AlignRight().Text(M(cliente.Subtotal.D61a90)).Italic();
                        table.Cell().Padding(3).AlignRight().Text(M(cliente.Subtotal.Mas90)).Italic();
                        table.Cell().Padding(3).AlignRight().Text(M(cliente.Subtotal.Total)).Italic();
                    }

                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(3).Text("TOTAL GENERAL").Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text(M(reporte.Totales.PorVencer)).Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text(M(reporte.Totales.D1a30)).Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text(M(reporte.Totales.D31a60)).Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text(M(reporte.Totales.D61a90)).Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text(M(reporte.Totales.Mas90)).Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text(M(reporte.Totales.Total)).Bold();
                });

                page.Footer().AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
            });
        }).GeneratePdf();
    }
}

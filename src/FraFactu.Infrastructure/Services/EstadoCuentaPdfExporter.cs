using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FraFactu.Infrastructure.Services;

public class EstadoCuentaPdfExporter : IEstadoCuentaPdfExporter
{
    public byte[] GenerarPlan(EstadoCuentaPlanDto plan)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(t => t.FontSize(9));
                page.Content().Column(col =>
                {
                    CabeceraGeneral(col, plan.EmisorNombre, plan.ClienteNombre, plan.ClienteDocumento, plan.FechaReporte);
                    col.Item().PaddingTop(6);
                    ComponerPlan(col, plan);
                });
                page.Footer().AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
            });
        }).GeneratePdf();
    }

    public byte[] GenerarCliente(EstadoCuentaClienteDto cliente)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(t => t.FontSize(9));
                page.Content().Column(col =>
                {
                    CabeceraGeneral(col, cliente.EmisorNombre, cliente.ClienteNombre, cliente.ClienteDocumento, cliente.FechaReporte);
                    col.Item().Text($"Total: {cliente.MontoTotal:N2}   Pagado: {cliente.MontoPagado:N2}   Saldo: {cliente.SaldoAdeudado:N2}").Bold();
                    foreach (var plan in cliente.Planes)
                    {
                        col.Item().PaddingTop(8);
                        ComponerPlan(col, plan);
                    }
                });
                page.Footer().AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
            });
        }).GeneratePdf();
    }

    private static void CabeceraGeneral(ColumnDescriptor col, string emisor, string cliente, string documento, DateTime fecha)
    {
        col.Item().Text("Estado de cuenta").FontSize(14).Bold();
        col.Item().Text($"Emisor: {emisor}");
        col.Item().Text($"Cliente: {cliente} ({documento})");
        col.Item().Text($"Fecha: {fecha:dd/MM/yyyy}");
    }

    private static void ComponerPlan(ColumnDescriptor col, EstadoCuentaPlanDto plan)
    {
        string M(decimal v) => v.ToString("N2");
        col.Item().Text($"Plan #{plan.PlanId} — {plan.Condicion} — Estado: {plan.EstadoCobro}").Bold();
        col.Item().Text($"Total: {M(plan.MontoTotal)}   Pagado: {M(plan.MontoPagado)}   Saldo: {M(plan.SaldoAdeudado)}");
        col.Item().PaddingTop(3).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(3);
                c.RelativeColumn(2);
            });
            void H(string s) => table.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text(s).Bold();
            H("Cuota"); H("Vence"); H("Monto"); H("Estado"); H("Fecha pago"); H("DTE"); H("Mora");
            foreach (var c in plan.Cuotas)
            {
                table.Cell().Padding(2).Text(c.Numero.ToString());
                table.Cell().Padding(2).Text(c.FechaPactada.ToString("dd/MM/yyyy"));
                table.Cell().Padding(2).AlignRight().Text(M(c.Monto));
                table.Cell().Padding(2).Text(c.Estado);
                table.Cell().Padding(2).Text(c.FechaPago.HasValue ? c.FechaPago.Value.ToString("dd/MM/yyyy") : "");
                table.Cell().Padding(2).Text(c.CodigoGeneracion ?? "—");
                table.Cell().Padding(2).AlignRight().Text(M(c.InteresMora));
            }
        });
    }
}

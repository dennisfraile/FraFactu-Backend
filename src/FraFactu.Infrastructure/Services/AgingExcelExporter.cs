using ClosedXML.Excel;
using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.Interfaces;

namespace FraFactu.Infrastructure.Services;

public class AgingExcelExporter : IAgingExcelExporter
{
    public byte[] Generar(AgingReporteDto reporte)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Antiguedad");

        ws.Cell(1, 1).Value = "Antigüedad de saldos";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = $"Fecha de corte: {reporte.FechaCorte:dd/MM/yyyy}";

        int r = 4;
        string[] headers = { "Cliente", "Plan", "Por vencer", "1-30", "31-60", "61-90", "+90", "Total" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(r, i + 1).Value = headers[i];
        ws.Range(r, 1, r, headers.Length).Style.Font.Bold = true;
        ws.Range(r, 1, r, headers.Length).Style.Fill.BackgroundColor = XLColor.LightBlue;
        r++;

        foreach (var cliente in reporte.Clientes)
        {
            foreach (var plan in cliente.Planes)
            {
                ws.Cell(r, 1).Value = cliente.ClienteNombre;
                ws.Cell(r, 2).Value = plan.PlanId;
                ws.Cell(r, 3).Value = plan.PorVencer;
                ws.Cell(r, 4).Value = plan.D1a30;
                ws.Cell(r, 5).Value = plan.D31a60;
                ws.Cell(r, 6).Value = plan.D61a90;
                ws.Cell(r, 7).Value = plan.Mas90;
                ws.Cell(r, 8).Value = plan.Total;
                r++;
            }
            ws.Cell(r, 1).Value = $"Subtotal {cliente.ClienteNombre}";
            ws.Cell(r, 3).Value = cliente.Subtotal.PorVencer;
            ws.Cell(r, 4).Value = cliente.Subtotal.D1a30;
            ws.Cell(r, 5).Value = cliente.Subtotal.D31a60;
            ws.Cell(r, 6).Value = cliente.Subtotal.D61a90;
            ws.Cell(r, 7).Value = cliente.Subtotal.Mas90;
            ws.Cell(r, 8).Value = cliente.Subtotal.Total;
            ws.Range(r, 1, r, 8).Style.Font.Italic = true;
            r++;
        }

        ws.Cell(r, 1).Value = "TOTAL GENERAL";
        ws.Cell(r, 3).Value = reporte.Totales.PorVencer;
        ws.Cell(r, 4).Value = reporte.Totales.D1a30;
        ws.Cell(r, 5).Value = reporte.Totales.D31a60;
        ws.Cell(r, 6).Value = reporte.Totales.D61a90;
        ws.Cell(r, 7).Value = reporte.Totales.Mas90;
        ws.Cell(r, 8).Value = reporte.Totales.Total;
        ws.Range(r, 1, r, 8).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}

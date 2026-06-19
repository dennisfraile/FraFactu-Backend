using ClosedXML.Excel;
using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.Interfaces;

namespace FraFactu.Infrastructure.Services;

public class EstadoCuentaExcelExporter : IEstadoCuentaExcelExporter
{
    public byte[] GenerarPlan(EstadoCuentaPlanDto plan)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Estado de cuenta");
        int row = EscribirCabeceraGeneral(ws, 1, plan.EmisorNombre, plan.ClienteNombre, plan.ClienteDocumento, plan.FechaReporte);
        row++;
        row = EscribirPlan(ws, row, plan);
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenerarCliente(EstadoCuentaClienteDto cliente)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Estado de cuenta");
        int row = EscribirCabeceraGeneral(ws, 1, cliente.EmisorNombre, cliente.ClienteNombre, cliente.ClienteDocumento, cliente.FechaReporte);
        ws.Cell(row, 1).Value = $"Total: {cliente.MontoTotal:N2}   Pagado: {cliente.MontoPagado:N2}   Saldo: {cliente.SaldoAdeudado:N2}";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row += 2;
        foreach (var plan in cliente.Planes)
        {
            row = EscribirPlan(ws, row, plan);
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static int EscribirCabeceraGeneral(IXLWorksheet ws, int row, string emisor, string cliente, string documento, DateTime fecha)
    {
        ws.Cell(row, 1).Value = "Estado de cuenta"; ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = $"Emisor: {emisor}"; row++;
        ws.Cell(row, 1).Value = $"Cliente: {cliente} ({documento})"; row++;
        ws.Cell(row, 1).Value = $"Fecha: {fecha:dd/MM/yyyy}"; row++;
        return row;
    }

    private static int EscribirPlan(IXLWorksheet ws, int row, EstadoCuentaPlanDto plan)
    {
        ws.Cell(row, 1).Value = $"Plan #{plan.PlanId} — {plan.Condicion} — Estado: {plan.EstadoCobro}";
        ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = $"Total: {plan.MontoTotal:N2}   Pagado: {plan.MontoPagado:N2}   Saldo: {plan.SaldoAdeudado:N2}"; row++;

        string[] headers = { "Cuota", "Vence", "Monto", "Estado", "Fecha pago", "DTE", "Mora" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(row, i + 1).Value = headers[i];
        ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
        ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightBlue;
        row++;

        foreach (var c in plan.Cuotas)
        {
            ws.Cell(row, 1).Value = c.Numero;
            ws.Cell(row, 2).Value = c.FechaPactada.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = c.Monto;
            ws.Cell(row, 4).Value = c.Estado;
            ws.Cell(row, 5).Value = c.FechaPago.HasValue ? c.FechaPago.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(row, 6).Value = c.CodigoGeneracion ?? "—";
            ws.Cell(row, 7).Value = c.InteresMora;
            row++;
        }
        return row;
    }
}

using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace FraFactu.Tests.Services;

public class PlanCuotasServiceTests
{
    private const int EmisorId = 7;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"PlanCuotas_{Guid.NewGuid()}").Options);

    private static CrearVentaCreditoDto VentaDosCuotas()
    {
        var venta = new CreateFacturaElectronicaDto
        {
            SucursalId = 1,
            CuerpoDocumento = new List<ItemDocumentoDto>
            {
                new() { NumItem = 1, TipoItem = 1, Cantidad = 1, UniMedida = 59,
                        Descripcion = "Producto X", PrecioUni = 100m, VentaGravada = 100m, IvaItem = 13m }
            },
            Resumen = new ResumenDto { CondicionOperacion = 2, TotalGravada = 100m, TotalIva = 13m,
                                       SubTotal = 100m, TotalPagar = 113m, Pagos = new() }
        };
        return new CrearVentaCreditoDto
        {
            Venta = venta,
            Cuotas = new List<DefinicionCuotaDto>
            {
                new() { Numero = 1, Monto = 56.50m, FechaPactada = new DateTime(2026, 6, 9) },
                new() { Numero = 2, Monto = 56.50m, FechaPactada = new DateTime(2026, 7, 9) }
            }
        };
    }

    // IFacturaService falso: devuelve un response con Id incremental.
    private static Mock<IFacturaService> FacturaServiceFake(ApplicationDbContext ctx)
    {
        var mock = new Mock<IFacturaService>();
        int next = 1000;
        mock.Setup(s => s.CreateAsync(It.IsAny<CreateFacturaElectronicaDto>(), EmisorId))
            .ReturnsAsync(() => new FacturaElectronicaResponseDto
            {
                Id = ++next,
                CodigoGeneracion = $"COD-{next}"
            });
        return mock;
    }

    private static PlanCuotasService BuildSut(ApplicationDbContext ctx, IFacturaService facturaSvc) =>
        new(ctx, facturaSvc, new FacturacionCuotaStrategy(), new MoraService(), NullLogger<PlanCuotasService>.Instance);

    [Fact]
    public async Task CrearVentaCredito_EmiteCuota1_YDejaCuota2Pendiente()
    {
        var ctx = BuildContext();
        var facturaSvc = FacturaServiceFake(ctx);
        var sut = BuildSut(ctx, facturaSvc.Object);

        var plan = await sut.CrearVentaCreditoAsync(VentaDosCuotas(), EmisorId);

        plan.MontoTotal.Should().Be(113m);
        plan.MontoPagado.Should().Be(56.50m);
        plan.SaldoAdeudado.Should().Be(56.50m);
        plan.EstadoCobro.Should().Be(nameof(EstadoCobroPlan.Parcial));
        plan.Cuotas.Should().HaveCount(2);
        plan.Cuotas[0].Estado.Should().Be(nameof(EstadoCuota.Pagada));
        plan.Cuotas[0].FacturaId.Should().NotBeNull();
        plan.Cuotas[1].Estado.Should().Be(nameof(EstadoCuota.Pendiente));
        facturaSvc.Verify(s => s.CreateAsync(It.IsAny<CreateFacturaElectronicaDto>(), EmisorId), Times.Once);
    }

    [Fact]
    public async Task GetById_ConReceptor_ExponeReceptorNombre()
    {
        var ctx = BuildContext();
        var sut = BuildSut(ctx, FacturaServiceFake(ctx).Object);

        // Receptor existente en la BD, referenciado por la venta.
        var receptor = new Receptor
        {
            EmisorId = EmisorId,
            NombreRazonSocial = "Cliente Ejemplo S.A. de C.V.",
            NumeroDocumento = "0614-010190-101-0"
        };
        ctx.Set<Receptor>().Add(receptor);
        await ctx.SaveChangesAsync();

        var dto = VentaDosCuotas();
        dto.Venta.ReceptorId = receptor.Id;
        var creado = await sut.CrearVentaCreditoAsync(dto, EmisorId);

        var plan = await sut.GetByIdAsync(creado.Id, EmisorId);

        plan.Should().NotBeNull();
        plan!.ReceptorId.Should().Be(receptor.Id);
        plan.ReceptorNombre.Should().Be("Cliente Ejemplo S.A. de C.V.");
    }

    [Fact]
    public async Task CrearVentaCredito_CuotasNoSuman_LanzaError()
    {
        var ctx = BuildContext();
        var sut = BuildSut(ctx, FacturaServiceFake(ctx).Object);
        var dto = VentaDosCuotas();
        dto.Cuotas[1].Monto = 10m; // 56.50 + 10 != 113

        var act = async () => await sut.CrearVentaCreditoAsync(dto, EmisorId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*suma de las cuotas*");
    }

    [Fact]
    public async Task PagarCuotaFinal_MarcaPlanPagado()
    {
        var ctx = BuildContext();
        var facturaSvc = FacturaServiceFake(ctx);
        var sut = BuildSut(ctx, facturaSvc.Object);
        var creado = await sut.CrearVentaCreditoAsync(VentaDosCuotas(), EmisorId);

        var plan = await sut.PagarCuotaAsync(creado.Id, 2,
            new RegistrarPagoCuotaDto { CatFormaPagoId = 1 }, EmisorId);

        plan.MontoPagado.Should().Be(113m);
        plan.SaldoAdeudado.Should().Be(0m);
        plan.EstadoCobro.Should().Be(nameof(EstadoCobroPlan.Pagada));
        plan.Cuotas[1].Estado.Should().Be(nameof(EstadoCuota.Pagada));
        facturaSvc.Verify(s => s.CreateAsync(It.IsAny<CreateFacturaElectronicaDto>(), EmisorId), Times.Exactly(2));
    }

    [Fact]
    public async Task PagarCuota_FueraDeOrden_LanzaError()
    {
        var ctx = BuildContext();
        var sut = BuildSut(ctx, FacturaServiceFake(ctx).Object);
        var creado = await sut.CrearVentaCreditoAsync(VentaDosCuotas(), EmisorId);

        // La cuota 1 ya está pagada; intentar pagar la 1 de nuevo debe fallar
        var act = async () => await sut.PagarCuotaAsync(creado.Id, 1,
            new RegistrarPagoCuotaDto { CatFormaPagoId = 1 }, EmisorId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya fue pagada*");
    }

    [Fact]
    public async Task PagarCuotaVencida_RegistraInteresMora()
    {
        var ctx = BuildContext();
        var facturaSvc = FacturaServiceFake(ctx);
        var sut = BuildSut(ctx, facturaSvc.Object);

        // Plan con cuota 1 ya vencida (fecha pactada en el pasado)
        var dto = VentaDosCuotas();
        dto.Cuotas[0].FechaPactada = new DateTime(2020, 1, 1); // muy vencida
        // mora habilitada por defecto (no hay config → defaults)
        var creado = await sut.CrearVentaCreditoAsync(dto, EmisorId);

        var cuota1 = creado.Cuotas.First(c => c.Numero == 1);
        cuota1.InteresMora.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task RefinanciarPlan_DesactivaPendientesYCreaNuevas()
    {
        var ctx = BuildContext();
        var sut = BuildSut(ctx, FacturaServiceFake(ctx).Object);
        var creado = await sut.CrearVentaCreditoAsync(VentaDosCuotas(), EmisorId);
        // saldo pendiente = 56.50 (cuota 2)

        var dto = new RefinanciarPlanDto
        {
            Motivo = "Acuerdo con el cliente",
            NuevasCuotas = new List<DefinicionCuotaDto>
            {
                new() { Numero = 1, Monto = 28.25m, FechaPactada = new DateTime(2026, 8, 9) },
                new() { Numero = 2, Monto = 28.25m, FechaPactada = new DateTime(2026, 9, 9) }
            }
        };

        var plan = await sut.RefinanciarPlanAsync(creado.Id, dto, EmisorId, usuarioId: 5);

        // Quedan: cuota 1 pagada + 2 nuevas pendientes (la vieja cuota 2 queda inactiva, no aparece)
        plan.Cuotas.Count(c => c.Estado == nameof(EstadoCuota.Pendiente)).Should().Be(2);
        plan.SaldoAdeudado.Should().Be(56.50m);
        // historial registrado
        (await ctx.Set<HistorialRefinanciamiento>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RefinanciarPlan_NuevasCuotasNoSumanSaldo_LanzaError()
    {
        var ctx = BuildContext();
        var sut = BuildSut(ctx, FacturaServiceFake(ctx).Object);
        var creado = await sut.CrearVentaCreditoAsync(VentaDosCuotas(), EmisorId);
        var dto = new RefinanciarPlanDto
        {
            Motivo = "x",
            NuevasCuotas = new List<DefinicionCuotaDto>
            {
                new() { Numero = 1, Monto = 10m, FechaPactada = new DateTime(2026, 8, 9) },
                new() { Numero = 2, Monto = 10m, FechaPactada = new DateTime(2026, 9, 9) }
            }
        };
        var act = async () => await sut.RefinanciarPlanAsync(creado.Id, dto, EmisorId, 5);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*saldo*");
    }

    /// <summary>
    /// Regresión del bug: tras refinanciamiento con numeración continua (ej. cuota pagada=1,
    /// nuevas=3,4), la estrategia usaba numeroCuota==totalCuotas para determinar si era final,
    /// lo que hacía que la cuota 3 (total activas=3) se emitiera como FINAL y la 4 como INTERMEDIA.
    /// Con el fix, se usa cuota.EsCuotaFinal explícito y el comportamiento es correcto.
    /// </summary>
    [Fact]
    public async Task RefinanciarYPagarCuotas_CuotaFinalRefinanciada_SeEmiteComoFinalNoComoIntermedia()
    {
        // Arrange — plan 2 cuotas, pagar cuota 1, refinanciar saldo en 2 nuevas.
        var ctx = BuildContext();
        var dteCapturados = new List<CreateFacturaElectronicaDto>();
        var facturaMock = new Mock<IFacturaService>();
        int next = 2000;
        facturaMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateFacturaElectronicaDto>(), EmisorId))
            .Callback<CreateFacturaElectronicaDto, int>((dto, _) => dteCapturados.Add(dto))
            .ReturnsAsync(() => new FacturaElectronicaResponseDto { Id = ++next, CodigoGeneracion = $"COD-{next}" });

        var sut = BuildSut(ctx, facturaMock.Object);

        // Crear plan de 2 cuotas (cuota 1 se emite automáticamente al crear).
        var creado = await sut.CrearVentaCreditoAsync(VentaDosCuotas(), EmisorId);
        // DTE #1 ya capturado (cuota 1, intermedia según el plan original).

        // Pagar cuota 1 explícitamente ya se hizo en CrearVentaCredito; solo queda cuota 2.
        // Refinanciar el saldo de cuota 2 (56.50) en 2 nuevas cuotas (Num=3, Num=4).
        var refinDto = new RefinanciarPlanDto
        {
            Motivo = "Acuerdo cliente",
            NuevasCuotas = new List<DefinicionCuotaDto>
            {
                new() { Numero = 1, Monto = 28.25m, FechaPactada = new DateTime(2026, 8, 9) },
                new() { Numero = 2, Monto = 28.25m, FechaPactada = new DateTime(2026, 9, 9) }
            }
        };
        await sut.RefinanciarPlanAsync(creado.Id, refinDto, EmisorId, usuarioId: 5);
        // Ahora las cuotas activas son: Num=1 (Pagada), Num=3 (Pendiente, intermedia), Num=4 (Pendiente, final).

        // Act — pagar la cuota intermedia nueva (Num=3).
        await sut.PagarCuotaAsync(creado.Id, 3, new RegistrarPagoCuotaDto { CatFormaPagoId = 1 }, EmisorId);
        var dteIntermediaRefinanciada = dteCapturados[1]; // segundo DTE emitido

        // Act — pagar la cuota final nueva (Num=4).
        var planFinal = await sut.PagarCuotaAsync(creado.Id, 4, new RegistrarPagoCuotaDto { CatFormaPagoId = 1 }, EmisorId);
        var dteFinalRefinanciada = dteCapturados[2]; // tercer DTE emitido

        // Assert — DTE de la cuota INTERMEDIA refinanciada: una sola línea (pago a cuenta).
        dteIntermediaRefinanciada.CuerpoDocumento.Should().HaveCount(1,
            because: "la cuota 3 es intermedia y debe emitirse como una sola línea de cuota");
        dteIntermediaRefinanciada.Resumen.DescuGravada.Should().Be(0m,
            because: "las cuotas intermedias no llevan descuento global");

        // Assert — DTE de la cuota FINAL refinanciada: detalle completo con ítems originales + descuento.
        dteFinalRefinanciada.CuerpoDocumento.Should().HaveCount(1,
            because: "la venta original tiene 1 ítem y la cuota final lo replica completo");
        dteFinalRefinanciada.CuerpoDocumento[0].VentaGravada.Should().Be(100m,
            because: "el ítem original tiene VentaGravada=100 (se copia completo en cuota final)");
        dteFinalRefinanciada.Resumen.DescuGravada.Should().BeGreaterThan(0m,
            because: "la cuota final debe descontar lo ya facturado en cuotas previas");

        // Assert — plan queda Pagado al final.
        planFinal.EstadoCobro.Should().Be(nameof(EstadoCobroPlan.Pagada));
        planFinal.SaldoAdeudado.Should().Be(0m);

        // Verificación crítica: si el bug existiera, la cuota 4 se emitiría como intermedia
        // (una sola línea, DescuGravada == 0). El test fallaría en las aserciones anteriores.
        facturaMock.Verify(s => s.CreateAsync(It.IsAny<CreateFacturaElectronicaDto>(), EmisorId), Times.Exactly(3));
    }

    [Fact]
    public async Task EstimarMora_CuotaVencida_DevuelveInteresYDias()
    {
        var ctx = BuildContext();
        var sut = BuildSut(ctx, FacturaServiceFake(ctx).Object);
        var dto = VentaDosCuotas();
        dto.Cuotas[1].FechaPactada = new DateTime(2020, 1, 1); // cuota 2 muy vencida
        var creado = await sut.CrearVentaCreditoAsync(dto, EmisorId);

        var est = await sut.EstimarMoraCuotaAsync(creado.Id, 2, EmisorId);

        est.Numero.Should().Be(2);
        est.InteresMora.Should().BeGreaterThan(0m);
        est.DiasAtraso.Should().BeGreaterThan(0);
        est.MoraHabilitada.Should().BeTrue();
    }

    [Fact]
    public async Task EstimarMora_CuotaNoVencida_DevuelveCero()
    {
        var ctx = BuildContext();
        var sut = BuildSut(ctx, FacturaServiceFake(ctx).Object);
        var dto = VentaDosCuotas();
        dto.Cuotas[1].FechaPactada = DateTime.UtcNow.Date.AddMonths(1); // futura
        var creado = await sut.CrearVentaCreditoAsync(dto, EmisorId);

        var est = await sut.EstimarMoraCuotaAsync(creado.Id, 2, EmisorId);

        est.InteresMora.Should().Be(0m);
        est.DiasAtraso.Should().Be(0);
    }

    [Fact]
    public async Task EstimarMora_CuotaInexistente_LanzaError()
    {
        var ctx = BuildContext();
        var sut = BuildSut(ctx, FacturaServiceFake(ctx).Object);
        var creado = await sut.CrearVentaCreditoAsync(VentaDosCuotas(), EmisorId);

        var act = async () => await sut.EstimarMoraCuotaAsync(creado.Id, 99, EmisorId);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cuota*");
    }
}

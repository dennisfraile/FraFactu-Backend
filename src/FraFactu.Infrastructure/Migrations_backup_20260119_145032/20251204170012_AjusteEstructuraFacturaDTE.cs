using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FraFactu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjusteEstructuraFacturaDTE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_actividades_economicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EsSeleccionable = table.Column<bool>(type: "boolean", nullable: false),
                    PadreId = table.Column<int>(type: "integer", nullable: true),
                    Codigo = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Valor = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_actividades_economicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_actividades_economicas_cat_actividades_economicas_Padre~",
                        column: x => x.PadreId,
                        principalTable: "cat_actividades_economicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_ambiente_destino",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_ambiente_destino", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_condicion_operacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_condicion_operacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_departamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_departamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_forma_pago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_forma_pago", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_modelo_fac",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_modelo_fac", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_municipio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoDepartamento = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_municipio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_otros_documentos_asociados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_otros_documentos_asociados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_plazo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_plazo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_contingencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_contingencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_doc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_doc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_documento_contingencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_documento_contingencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_documento_identificacion_receptor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_documento_identificacion_receptor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_establecimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_establecimiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_generacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_generacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_invalidacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_invalidacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_item",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_item", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_servicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_servicio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tipo_transmision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_transmision", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_tributos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DescripcionCorta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EsRetencion = table.Column<bool>(type: "boolean", nullable: false),
                    EsValorPorcentual = table.Column<bool>(type: "boolean", nullable: false),
                    Seccion = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Valor = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tributos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_uni_medida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_uni_medida", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "emisores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreRazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NombreComercial = table.Column<string>(type: "text", nullable: true),
                    CorreoElectronico = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emisores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Receptor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    TipoDocumento = table.Column<string>(type: "text", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "text", nullable: false),
                    NombreRazonSocial = table.Column<string>(type: "text", nullable: false),
                    Direccion = table.Column<string>(type: "text", nullable: false),
                    CorreoElectronico = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receptor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receptor_emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles_permisos",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    PermisoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles_permisos", x => new { x.RolId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_roles_permisos_permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_roles_permisos_roles_RolId",
                        column: x => x.RolId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    RequiereCambioPwd = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiracionPwdTemporal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PermiteCambioPwd = table.Column<bool>(type: "boolean", nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usuarios_emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuarios_roles_RolId",
                        column: x => x.RolId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    ReceptorId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Ambiente = table.Column<string>(type: "text", nullable: false),
                    CatTipoDocumentoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroControl = table.Column<string>(type: "text", nullable: false),
                    CodigoGeneracion = table.Column<string>(type: "text", nullable: false),
                    CatModeloFacturacionId = table.Column<int>(type: "integer", nullable: false),
                    CatTipoTransmisionId = table.Column<int>(type: "integer", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraEmision = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CatMonedaId = table.Column<int>(type: "integer", nullable: false),
                    CatTipoContingenciaId = table.Column<int>(type: "integer", nullable: true),
                    MotivoContingencia = table.Column<string>(type: "text", nullable: true),
                    TotalNoSujeto = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalExento = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalGravado = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotalVentas = table.Column<decimal>(type: "numeric", nullable: false),
                    DescuentoNoSujeto = table.Column<decimal>(type: "numeric", nullable: false),
                    DescuentoExento = table.Column<decimal>(type: "numeric", nullable: false),
                    DescuentoGravado = table.Column<decimal>(type: "numeric", nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalDescuento = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    IvaPercibido = table.Column<decimal>(type: "numeric", nullable: false),
                    IvaRetenido = table.Column<decimal>(type: "numeric", nullable: false),
                    RetencionRenta = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoTotalOperacion = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalNoGravado = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPagar = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalLetras = table.Column<string>(type: "text", nullable: false),
                    SaldoFavor = table.Column<decimal>(type: "numeric", nullable: false),
                    CatCondicionOperacionId = table.Column<int>(type: "integer", nullable: false),
                    EstadoHacienda = table.Column<string>(type: "text", nullable: false),
                    SelloRecibido = table.Column<string>(type: "text", nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facturas_Receptor_ReceptorId",
                        column: x => x.ReceptorId,
                        principalTable: "Receptor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Facturas_cat_tipo_doc_CatTipoDocumentoId",
                        column: x => x.CatTipoDocumentoId,
                        principalTable: "cat_tipo_doc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Facturas_emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacturaDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    NumeroItem = table.Column<int>(type: "integer", nullable: false),
                    CatTipoItemId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric", nullable: false),
                    CatUnidadMedidaId = table.Column<int>(type: "integer", nullable: false),
                    CodigoProducto = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoDescuento = table.Column<decimal>(type: "numeric", nullable: false),
                    VentaNoSujeta = table.Column<decimal>(type: "numeric", nullable: false),
                    VentaExenta = table.Column<decimal>(type: "numeric", nullable: false),
                    VentaGravada = table.Column<decimal>(type: "numeric", nullable: false),
                    TributosAplicados = table.Column<string>(type: "text", nullable: true),
                    NoGravado = table.Column<decimal>(type: "numeric", nullable: false),
                    IvaItem = table.Column<decimal>(type: "numeric", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_cat_tipo_item_CatTipoItemId",
                        column: x => x.CatTipoItemId,
                        principalTable: "cat_tipo_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_cat_uni_medida_CatUnidadMedidaId",
                        column: x => x.CatUnidadMedidaId,
                        principalTable: "cat_uni_medida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacturaPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    CatFormaPagoId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: true),
                    CatPlazoId = table.Column<int>(type: "integer", nullable: true),
                    Periodo = table.Column<decimal>(type: "numeric", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaPagos_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaPagos_cat_forma_pago_CatFormaPagoId",
                        column: x => x.CatFormaPagoId,
                        principalTable: "cat_forma_pago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacturaTributos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    CatTributoId = table.Column<int>(type: "integer", nullable: false),
                    CodigoAttribute = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaTributos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaTributos_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaTributos_cat_tributos_CatTributoId",
                        column: x => x.CatTributoId,
                        principalTable: "cat_tributos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "cat_actividades_economicas",
                columns: new[] { "Id", "Codigo", "EsSeleccionable", "PadreId", "Valor" },
                values: new object[,]
                {
                    { 1, "SEC-0001", false, null, "AGRICULTURA, GANADERIA, SILVICULTURA Y PESCA" },
                    { 2, "SEC-0002", false, null, "PRODUCCION AGRÍCOLA, PECUARIA, CAZA Y ACTIVIDADES DE SERVICIOS CONEXAS" },
                    { 59, "SEC-0059", false, null, "SILVICULTURA Y EXTRACCIÓN DE MADERA" },
                    { 64, "SEC-0064", false, null, "PESCA Y ACUICULTURA" },
                    { 70, "SEC-0070", false, null, "EXPLOTACION DE MINAS Y CANTERAS" },
                    { 71, "SEC-0071", false, null, "EXTRACCION CARBON DE PIEDRA Y LIGNITO" },
                    { 74, "SEC-0074", false, null, "EXTRACCION DE PETROLEO CRUDO Y GAS NATURAL" },
                    { 77, "SEC-0077", false, null, "EXTRACCION DE MINERALES METALFEROS" },
                    { 81, "SEC-0081", false, null, "EXPLOTACION DE OTRAS MINAS Y CANTERAS" },
                    { 87, "SEC-0087", false, null, "ACTIVIDADES DE SERVICIOS DE APOYO A LA EXPLOTACION DE MINAS Y CANTERAS" },
                    { 90, "SEC-0090", false, null, "INDUSTRIAS MANUFACTURERAS" },
                    { 91, "SEC-0091", false, null, "ELABORACION DE PRODUCTOS ALIMENTICIOS" },
                    { 132, "SEC-0132", false, null, "ELABORACIÓN DE BEBIDAS" },
                    { 141, "SEC-0141", false, null, "ELABORACIÓN DE PRODUCTOS DE TABACO" },
                    { 143, "SEC-0143", false, null, "FABRICACIÓN DE PRODUCTOS TEXTILES" },
                    { 159, "SEC-0159", false, null, "FABRICACIÓN DE PRENDAS DE VESTIR" },
                    { 172, "SEC-0172", false, null, "FABRICACIÓN DE CUEROS Y PRODUCTOS CONEXOS" },
                    { 181, "SEC-0181", false, null, "PRODUCCIÓN DE MADERA Y FABRICACIÓN DE PRODUCTOS DE MADERA Y CORCHO EXCEPTO MUEBLES; FABRICACIÓN DE ARTÍCULOS DE PAJA Y DE MATERIALES TRENZABLES" },
                    { 188, "SEC-0188", false, null, "FABRICACIÓN DE PAPEL Y DE PRODUCTOS DE PAPEL" },
                    { 193, "SEC-0193", false, null, "IMPRESIÓN Y REPRODUCCIÓN DE GRABACIONES" },
                    { 197, "SEC-0197", false, null, "FABRICACIÓN DE COQUE Y DE PRODUCTOS DE LA REFINACIÓN DE PETRÓLEO" },
                    { 201, "SEC-0201", false, null, "FABRICACIÓN DE SUSTANCIAS Y PRODUCTOS QUÍMICOS" },
                    { 217, "SEC-0217", false, null, "FABRICACIÓN DE PRODUCTOS FARMACÉUTICOS, SUSTANCIAS QUÍMICAS MEDICINALES Y PRODUCTOS BOTÁNICOS DE USO FARMACÉUTICO" },
                    { 220, "SEC-0220", false, null, "FABRICACIÓN DE PRODUCTOS DE CAUCHO Y PLÁSTICO" },
                    { 227, "SEC-0227", false, null, "FABRICACIÓN DE PRODUCTOS MINERALES NO METÁLICOS" },
                    { 240, "SEC-0240", false, null, "FABRICACIÓN DE METALES COMUNES" },
                    { 245, "SEC-0245", false, null, "FABRICACIÓN DE PRODUCTOS DERIVADOS DE METAL, EXCEPTO MAQUINARIA Y EQUIPO" },
                    { 257, "SEC-0257", false, null, "FABRICACIÓN DE PRODUCTOS DE INFORMÁTICA, ELECTRÓNICA Y ÓPTICA" },
                    { 267, "SEC-0267", false, null, "FABRICACIÓN DE EQUIPO ELÉCTRICO" },
                    { 276, "SEC-0276", false, null, "FABRICACIÓN DE MAQUINARIA Y EQUIPO NCP" },
                    { 294, "SEC-0294", false, null, "FABRICACIÓN DE VEHÍCULOS AUTOMOTORES, REMOLQUES Y SEMIRREMOLQUES" },
                    { 298, "SEC-0298", false, null, "FABRICACIÓN DE OTROS TIPOS DE EQUIPO DE TRANSPORTE" },
                    { 307, "SEC-0307", false, null, "FABRICACIÓN DE MUEBLES" },
                    { 312, "SEC-0312", false, null, "OTRAS INDUSTRIAS MANUFACTURERAS" },
                    { 329, "SEC-0329", false, null, "REPARACIÓN E INSTALACIÓN DE MAQUINARIA Y EQUIPO" },
                    { 337, "SEC-0337", false, null, "SUMINISTROS DE ELECTRICIDAD, GAS, VAPOR Y AIRE ACONDICIONADO" },
                    { 338, "SEC-0338", false, null, "SUMINISTROS DE ELECTRICIDAD, GAS, VAPOR Y AIRE ACONDICIONADO" },
                    { 344, "SEC-0344", false, null, "SUMINISTRO DE AGUA, EVACUACIÓN DE AGUAS RESIDUALES (ALCANTARILLADO); GESTIÓN DE DESECHOS Y ACTIVIDADES DE SANEAMIENTO" },
                    { 345, "SEC-0345", false, null, "CAPTACIÓN, TRATAMIENTO Y SUMINISTRO DE AGUA" },
                    { 347, "SEC-0347", false, null, "EVACUACIÓN DE AGUAS RESIDUALES (ALCANTARILLADO)" },
                    { 349, "SEC-0349", false, null, "RECOLECCIÓN, TRATAMIENTO Y ELIMINACIÓN DE DESECHOS; RECICLAJE" },
                    { 360, "SEC-0360", false, null, "ACTIVIDADES DE SANEAMIENTO Y OTROS SERVICIOS DE GESTIÓN DE DESECHOS" },
                    { 362, "SEC-0362", false, null, "CONSTRUCCIÓN" },
                    { 363, "SEC-0363", false, null, "CONSTRUCCIÓN DE EDIFICIOS" },
                    { 366, "SEC-0366", false, null, "OBRAS DE INGENIER═A CIVIL" },
                    { 370, "SEC-0370", false, null, "ACTIVIDADES ESPECIALIZADAS DE CONSTRUCCION" },
                    { 379, "SEC-0379", false, null, "COMERCIO AL POR MAYOR Y AL POR MENOR; REPARACION DE VEHICULOS AUTOMOTORES Y MOTOCICLETAS" },
                    { 380, "SEC-0380", false, null, "COMERCIO AL POR MAYOR Y AL POR MENOR Y REPARACION DE VEHICULOS AUTOMOTORES Y MOTOCICLETAS" },
                    { 397, "SEC-0397", false, null, "COMERCIO AL POR MAYOR, EXCEPTO EL COMERCIO DE VEHICULOS AUTOMOTORES Y MOTOCICLETAS (Parte 1)" },
                    { 485, "SEC-0485", false, null, "COMERCIO AL POR MAYOR, EXCEPTO EL COMERCIO DE VEHÍCULOS AUTOMOTORES Y MOTOCICLETAS (Parte 2)" },
                    { 499, "SEC-0499", false, null, "COMERCIO AL POR MENOR, EXCEPTO DE VEHÍCULOS AUTOMOTORES Y MOTOCICLETAS" },
                    { 581, "SEC-0581", false, null, "TRANSPORTE Y ALMACENAMIENTO" },
                    { 582, "SEC-0582", false, null, "TRANSPORTE POR VÍA TERRESTRE Y TRANSPORTE POR TUBERÍAS" },
                    { 601, "SEC-0601", false, null, "TRANSPORTE POR VÍA ACUÁTICA" },
                    { 607, "SEC-0607", false, null, "TRANSPORTE POR VÍA AÉREA" },
                    { 611, "SEC-0611", false, null, "ALMACENAMIENTO Y ACTIVIDADES DE APOYO AL TRANSPORTE" },
                    { 624, "SEC-0624", false, null, "ACTIVIDADES POSTALES Y DE MENSAJERIA" },
                    { 628, "SEC-0628", false, null, "ACTIVIDADES DE ALOJAMIENTO Y DE SERVICIO DE COMIDAS" },
                    { 629, "SEC-0629", false, null, "ACTIVIDADES DE ALOJAMIENTO" },
                    { 634, "SEC-0634", false, null, "ACTIVIDADES DE SERVICIO DE COMIDAS Y BEBIDAS" },
                    { 646, "SEC-0646", false, null, "INFORMACIÓN Y COMUNICACIONES" },
                    { 647, "SEC-0647", false, null, "ACTIVIDADES DE EDICIÓN" },
                    { 653, "SEC-0653", false, null, "ACTIVIDADES DE PRODUCCIÓN DE PELÍCULAS CINEMATOGRÁFICAS, VIDEOS Y PROGRAMAS DE TELEVISIÓN, GRABACIÓN DE SONIDO Y EDICIÓN DE MÚSICA" },
                    { 659, "SEC-0659", false, null, "ACTIVIDADES DE PROGRAMACION Y TRANSMISION" },
                    { 665, "SEC-0665", false, null, "TELECOMUNICACIONES" },
                    { 676, "SEC-0676", false, null, "PROGRAMACIÓN INFORMÁTICA, CONSULTORÍA INFORMÁTICA Y ACTIVIDADES CONEXAS" },
                    { 680, "SEC-0680", false, null, "ACTIVIDADES DE SERVICIOS DE INFORMACIÓN" },
                    { 685, "SEC-0685", false, null, "ACTIVIDADES FINANCIERAS Y DE SEGUROS" },
                    { 686, "SEC-0686", false, null, "ACTIVIDADES DE SERVICIOS FINANCIEROS EXCEPTO LAS DE SEGUROS Y FONDOS DE PENSIONES" },
                    { 699, "SEC-0699", false, null, "SEGUROS, REASEGUROS Y FONDOS DE PENSIONES, EXCEPTO PLANES DE SEGURIDAD SOCIAL DE AFILIACIÓN OBLIGATORIA." },
                    { 705, "SEC-0705", false, null, "ACTIVIDADES AUXILIARES DE LAS ACTIVIDADES DE SERVICIOS FINANCIEROS" },
                    { 713, "SEC-0713", false, null, "ACTIVIDADES INMOBILIARIAS" },
                    { 714, "SEC-0714", false, null, "ACTIVIDADES INMOBILIARIAS" },
                    { 718, "SEC-0718", false, null, "ACTIVIDADES PROFESIONALES, CIENTÍFICAS Y TÉCNICAS" },
                    { 719, "SEC-0719", false, null, "ACTIVIDADES JURÍDICAS Y CONTABLES" },
                    { 722, "SEC-0722", false, null, "ACTIVIDADES DE OFICINAS CENTRALES; ACTIVIDADES DE CONSULTORIA EN GESTIÓN EMPRESARIAL" },
                    { 725, "SEC-0725", false, null, "ACTIVIDADES DE ARQUITECTURA E INGENIERÍA; ENSAYOS Y ANÁLISIS TÉCNICOS" },
                    { 730, "SEC-0730", false, null, "INVESTIGACIÓN CIENTÍFICA Y DESARROLLO" },
                    { 734, "SEC-0734", false, null, "PUBLICIDAD Y ESTUDIOS DE MERCADO" },
                    { 737, "SEC-0737", false, null, "OTRAS ACTIVIDADES PROFESIONALES, CIENTÍFICAS Y TÉCNICAS" },
                    { 741, "SEC-0741", false, null, "ACTIVIDADES VETERINARIAS" },
                    { 743, "SEC-0743", false, null, "ACTIVIDADES DE SERVICIOS ADMINISTRATIVOS Y DE APOYO" },
                    { 744, "SEC-0744", false, null, "ACTIVIDADES DE ALQUILER Y ARRENDAMIENTO" },
                    { 753, "SEC-0753", false, null, "ACTIVIDADES DE EMPLEO" },
                    { 757, "SEC-0757", false, null, "ACTIVIDADES DE AGENCIAS DE VIAJES, OPERADORES TURÍSTICOS Y OTROS SERVICIOS DE RESERVA Y ACTIVIDADES CONEXAS" },
                    { 761, "SEC-0761", false, null, "ACTIVIDADES DE INVESTIGACIÓN Y SEGURIDAD" },
                    { 766, "SEC-0766", false, null, "ACTIVIDADES DE SERVICIOS A EDIFICIOS Y PAISAJISMO" },
                    { 771, "SEC-0771", false, null, "ACTIVIDADES ADMINISTRATIVAS Y DE APOYO DE OFICINAS Y OTRAS ACTIVIDADES DE APOYO A LAS EMPRESAS" },
                    { 781, "SEC-0781", false, null, "ADMINISTRACIÓN PÚBLICA Y DEFENSA; PLANES DE SEGURIDAD SOCIAL DE AFILIACIÓN OBLIGATORIA" },
                    { 782, "SEC-0782", false, null, "ADMINISTRACIÓN PÚBLICA Y DEFENSA; PLANES DE SEGURIDAD SOCIAL DE AFILIACIÓN OBLIGATORIA" },
                    { 791, "SEC-0791", false, null, "ENSEÑANZA" },
                    { 808, "SEC-0808", false, null, "ACTIVIDADES DE ATENCIÓN A LA SALUD HUMANA Y DE ASISTENCIA SOCIAL" },
                    { 809, "SEC-0809", false, null, "ACTIVIDADES DE ATENCIÓN DE LA SALUD HUMANA" },
                    { 817, "SEC-0817", false, null, "ACTIVIDADES DE ATENCIÓN DE ENFERMERÍA EN INSTITUCIONES" },
                    { 823, "SEC-0823", false, null, "ACTIVIDADES DE ASISTENCIA SOCIAL SIN ALOJAMIENTO" },
                    { 826, "SEC-0826", false, null, "ACTIVIDADES ARTÍSTICAS, DE ENTRETENIMIENTO Y RECREATIVAS" },
                    { 827, "SEC-0827", false, null, "ACTIVIDADES CREATIVAS ARTÍSTICAS Y DE ESPARCIMIENTO" },
                    { 829, "SEC-0829", false, null, "ACTIVIDADES BIBLIOTECAS, ARCHIVOS, MUSEOS Y OTRAS ACTIVIDADES CULTURALES" },
                    { 833, "SEC-0833", false, null, "ACTIVIDADES DE JUEGOS DE AZAR Y APUESTAS" },
                    { 835, "SEC-0835", false, null, "ACTIVIDADES DEPORTIVAS, DE ESPARCIMIENTO Y RECREATIVAS" },
                    { 843, "SEC-0843", false, null, "OTRAS ACTIVIDADES DE SERVICIOS" },
                    { 844, "SEC-0844", false, null, "ACTIVIDADES DE ASOCIACIONES" },
                    { 851, "SEC-0851", false, null, "REPARACION DE COMPUTADORAS Y DE EFECTOS PERSONALES Y ENSERES DOMESTICOS" },
                    { 863, "SEC-0863", false, null, "OTRAS ACTIVIDADES DE SERVICIOS PERSONALES" },
                    { 869, "SEC-0869", false, null, "ACTIVIDADES DE LOS HOGARES COMO EMPLEADORES, ACTIVIDADES INDIFERENCIADAS DE PRODUCCION DE BIENES Y SERVICIOS DE LOS HOGARES PARA USO PROPIO" },
                    { 870, "SEC-0870", false, null, "ACTIVIDAD DE LOS HOGARES EN CALIDAD DE EMPLEADORES DE PERSONAL DOMESTICO" },
                    { 872, "SEC-0872", false, null, "ACTIVIDADES INDIFERENCIADAS DE PRODUCCION DE BIENES Y SERVICIOS DE LOS HOGARES PARA USO PROPIO" },
                    { 875, "SEC-0875", false, null, "ACTIVIDADES DE ORGANIZACIONES Y ORGANOS EXTRATERRITORIALES" },
                    { 876, "SEC-0876", false, null, "ACTIVIDADES DE ORGANIZACIONES Y ORGANOS EXTRATERRITORIALES" },
                    { 878, "SEC-0878", false, null, "EMPLEADOS Y OTRAS PERSONAS NATURALES" },
                    { 879, "SEC-0879", false, null, "EMPLEADOS Y OTRAS PERSONAS NATURALES" }
                });

            migrationBuilder.InsertData(
                table: "cat_ambiente_destino",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "00", "Modo prueba" },
                    { 2, "01", "Modo producción" }
                });

            migrationBuilder.InsertData(
                table: "cat_condicion_operacion",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "1", "Contado" },
                    { 2, "2", "A crédito" },
                    { 3, "3", "Otro" }
                });

            migrationBuilder.InsertData(
                table: "cat_departamento",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "00", "Otro (Para extranjeros)" },
                    { 2, "01", "Ahuachapán" },
                    { 3, "02", "Santa Ana" },
                    { 4, "03", "Sonsonate" },
                    { 5, "04", "Chalatenango" },
                    { 6, "05", "La Libertad" },
                    { 7, "06", "San Salvador" },
                    { 8, "07", "Cuscatlán" },
                    { 9, "08", "La Paz" },
                    { 10, "09", "Cabañas" },
                    { 11, "10", "San Vicente" },
                    { 12, "11", "Usulután" },
                    { 13, "12", "San Miguel" },
                    { 14, "13", "Morazán" },
                    { 15, "14", "La Unión" }
                });

            migrationBuilder.InsertData(
                table: "cat_forma_pago",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Billetes y monedas" },
                    { 2, "02", "Tarjeta Débito" },
                    { 3, "03", "Tarjeta Crédito" },
                    { 4, "04", "Cheque" },
                    { 5, "05", "Transferencia-Depósito Bancario" },
                    { 6, "08", "Dinero electrónico" },
                    { 7, "09", "Monedero electrónico" },
                    { 8, "11", "Bitcoin" },
                    { 9, "12", "Otras Criptomonedas" },
                    { 10, "13", "Cuentas por pagar del receptor" },
                    { 11, "14", "Giro bancario" },
                    { 12, "99", "Otros" }
                });

            migrationBuilder.InsertData(
                table: "cat_modelo_fac",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Modelo Facturación previo" },
                    { 2, "02", "Modelo Facturación diferido" }
                });

            migrationBuilder.InsertData(
                table: "cat_municipio",
                columns: new[] { "Id", "Codigo", "CodigoDepartamento", "Valor" },
                values: new object[,]
                {
                    { 1, "13", "01", "AHUACHAPAN NORTE" },
                    { 2, "14", "01", "AHUACHAPAN CENTRO" },
                    { 3, "15", "01", "AHUACHAPAN SUR" },
                    { 4, "14", "02", "SANTA ANA NORTE" },
                    { 5, "15", "02", "SANTA ANA CENTRO" },
                    { 6, "16", "02", "SANTA ANA ESTE" },
                    { 7, "17", "02", "SANTA ANA OESTE" },
                    { 8, "17", "03", "SONSONATE NORTE" },
                    { 9, "18", "03", "SONSONATE CENTRO" },
                    { 10, "19", "03", "SONSONATE ESTE" },
                    { 11, "20", "03", "SONSONATE OESTE" },
                    { 12, "34", "04", "CHALATENANGO NORTE" },
                    { 13, "35", "04", "CHALATENANGO CENTRO" },
                    { 14, "36", "04", "CHALATENANGO SUR" },
                    { 15, "23", "05", "LA LIBERTAD NORTE" },
                    { 16, "24", "05", "LA LIBERTAD CENTRO" },
                    { 17, "25", "05", "LA LIBERTAD OESTE" },
                    { 18, "26", "05", "LA LIBERTAD ESTE" },
                    { 19, "27", "05", "LA LIBERTAD COSTA" },
                    { 20, "28", "05", "LA LIBERTAD SUR" },
                    { 21, "20", "06", "SAN SALVADOR NORTE" },
                    { 22, "21", "06", "SAN SALVADOR OESTE" },
                    { 23, "22", "06", "SAN SALVADOR ESTE" },
                    { 24, "23", "06", "SAN SALVADOR CENTRO" },
                    { 25, "24", "06", "SAN SALVADOR SUR" },
                    { 26, "17", "07", "CUSCATLAN NORTE" },
                    { 27, "18", "07", "CUSCATLAN SUR" },
                    { 28, "23", "08", "LA PAZ OESTE" },
                    { 29, "24", "08", "LA PAZ CENTRO" },
                    { 30, "25", "08", "LA PAZ ESTE" },
                    { 31, "10", "09", "CABAÑAS OESTE" },
                    { 32, "11", "09", "CABAÑAS ESTE" },
                    { 33, "14", "10", "SAN VICENTE NORTE" },
                    { 34, "15", "10", "SAN VICENTE SUR" },
                    { 35, "24", "11", "USULUTAN NORTE" },
                    { 36, "25", "11", "USULUTAN ESTE" },
                    { 37, "26", "11", "USULUTAN OESTE" },
                    { 38, "21", "12", "SAN MIGUEL NORTE" },
                    { 39, "22", "12", "SAN MIGUEL CENTRO" },
                    { 40, "23", "12", "SAN MIGUEL OESTE" },
                    { 41, "27", "13", "MORAZAN NORTE" },
                    { 42, "28", "13", "MORAZAN SUR" },
                    { 43, "19", "14", "LA UNION NORTE" },
                    { 44, "20", "14", "LA UNION SUR" },
                    { 45, "00", "00", "Otro (Para extranjeros)" }
                });

            migrationBuilder.InsertData(
                table: "cat_otros_documentos_asociados",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "1", "Emisor" },
                    { 2, "2", "Receptor" },
                    { 3, "3", "Médico (solo aplica para contribuyentes obligados a la presentación de F-958)" },
                    { 4, "4", "Transporte (solo aplica para Factura de exportación)" }
                });

            migrationBuilder.InsertData(
                table: "cat_plazo",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Días" },
                    { 2, "02", "Meses" },
                    { 3, "03", "Años" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_contingencia",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "1", "No disponibilidad de sistema del MH" },
                    { 2, "2", "No disponibilidad de sistema del emisor" },
                    { 3, "3", "Falla en el suministro de servicio de Internet del Emisor" },
                    { 4, "4", "Falla en el suministro de servicio de energía eléctrica del emisor que impida la transmisión de los DTE" },
                    { 5, "5", "Otro" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_doc",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Factura" },
                    { 2, "03", "Comprobante de crédito fiscal" },
                    { 3, "04", "Nota de remisión" },
                    { 4, "05", "Nota de crédito" },
                    { 5, "06", "Nota de débito" },
                    { 6, "07", "Comprobante de retención" },
                    { 7, "08", "Comprobante de liquidación" },
                    { 8, "09", "Documento contable de liquidación" },
                    { 9, "11", "Facturas de exportación" },
                    { 10, "14", "Factura de sujeto excluido" },
                    { 11, "15", "Comprobante de donación" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_documento_contingencia",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Factura Electrónica" },
                    { 2, "03", "Comprobante de Crédito Fiscal Electrónico" },
                    { 3, "04", "Nota de Remisión Electrónica" },
                    { 4, "05", "Nota de Crédito Electrónica" },
                    { 5, "06", "Nota de Débito Electrónica" },
                    { 6, "11", "Factura de Exportación Electrónica" },
                    { 7, "14", "Factura de Sujeto Excluido Electrónica" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_documento_identificacion_receptor",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "36", "NIT" },
                    { 2, "13", "DUI" },
                    { 3, "37", "Otro" },
                    { 4, "03", "Pasaporte" },
                    { 5, "02", "Carnet de Residente" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_establecimiento",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Sucursal" },
                    { 2, "02", "Casa Matriz" },
                    { 3, "04", "Bodega" },
                    { 4, "07", "Patio" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_generacion",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "1", "Físico" },
                    { 2, "2", "Electrónico" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_invalidacion",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Error en la Información del Documento Tributario Electrónico a invalidar" },
                    { 2, "02", "Rescindir de la operación realizada" },
                    { 3, "03", "Otro" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_item",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "1", "Bienes" },
                    { 2, "2", "Servicios" },
                    { 3, "3", "Ambos (Bienes y Servicios, incluye los dos inherente a los Productos o servicios)" },
                    { 4, "4", "Otros tributos por ítem" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_servicio",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Cirugía" },
                    { 2, "02", "Operación" },
                    { 3, "03", "Tratamiento médico" },
                    { 4, "04", "Cirugía instituto salvadoreño de Bienestar Magisterial" },
                    { 5, "05", "Operación Instituto Salvadoreño de Bienestar Magisterial" },
                    { 6, "06", "Tratamiento médico Instituto Salvadoreño de Bienestar Magisterial" }
                });

            migrationBuilder.InsertData(
                table: "cat_tipo_transmision",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Transmisión normal" },
                    { 2, "02", "Transmisión por contingencia" }
                });

            migrationBuilder.InsertData(
                table: "cat_tributos",
                columns: new[] { "Id", "Codigo", "DescripcionCorta", "EsRetencion", "EsValorPorcentual", "Seccion", "Valor" },
                values: new object[,]
                {
                    { 1, "20", "IVA", false, true, 1, "Impuesto al Valor Agregado 13%" },
                    { 2, "C3", "IVA Exp.", false, true, 1, "Impuesto al Valor Agregado (exportaciones) 0%" },
                    { 3, "59", "Turismo", false, true, 1, "Turismo: por alojamiento (5%)" },
                    { 4, "71", "Turismo Salida", false, false, 1, "Turismo: salida del país por vía aérea $7.00" },
                    { 5, "D1", "FOVIAL", false, false, 1, "FOVIAL ($0.20 Ctvs. por galón)" },
                    { 6, "C8", "COTRANS", false, false, 1, "COTRANS ($0.10 Ctvs. por galón)" },
                    { 7, "D5", "Otras Tasas", false, false, 1, "Otras tasas casos especiales" },
                    { 8, "D4", "Otros Impuestos", false, false, 1, "Otros impuestos casos especiales" },
                    { 9, "A8", "IEC", false, true, 2, "Impuesto Especial al Combustible (0%, 0.5%, 1%)" },
                    { 10, "57", "Imp. Cemento", false, false, 2, "Impuesto industria de Cemento" },
                    { 11, "90", "1ra Matrícula", false, false, 2, "Impuesto especial a la primera matrícula" },
                    { 12, "A6", "Ad-Valorem Armas", false, true, 2, "Impuesto ad-valorem, armas de fuego, municiones explosivas" },
                    { 13, "C5", "Ad-Valorem Alcohol", false, true, 3, "Impuesto ad-valorem por diferencial de precios de bebidas alcohólicas (8%)" },
                    { 14, "C6", "Ad-Valorem Cigarros", false, true, 3, "Impuesto ad-valorem por diferencial de precios al tabaco cigarrillos (39%)" },
                    { 15, "C7", "Ad-Valorem Tabaco", false, true, 3, "Impuesto ad-valorem por diferencial de precios al tabaco cigarros (100%)" },
                    { 16, "19", "Fab. Bebidas", false, false, 3, "Fabricante de Bebidas Gaseosas..." },
                    { 17, "28", "Imp. Bebidas", false, false, 3, "Importador de Bebidas Gaseosas..." },
                    { 18, "31", "Exp. Alcohol", false, false, 3, "Detallistas o Expendedores de Bebidas Alcohólicas" },
                    { 19, "32", "Fab. Cerveza", false, false, 3, "Fabricante de Cerveza" },
                    { 20, "33", "Imp. Cerveza", false, false, 3, "Importador de Cerveza" },
                    { 21, "34", "Fab. Tabaco", false, false, 3, "Fabricante de Productos de Tabaco" },
                    { 22, "35", "Imp. Tabaco", false, false, 3, "Importador de Productos de Tabaco" },
                    { 23, "36", "Fab. Armas", false, false, 3, "Fabricante de Armas de Fuego y Municiones" },
                    { 24, "37", "Imp. Armas", false, false, 3, "Importador de Arma de Fuego y Municiones" },
                    { 25, "38", "Fab. Explosivos", false, false, 3, "Fabricante de Explosivos" },
                    { 26, "39", "Imp. Explosivos", false, false, 3, "Importador de Explosivos" },
                    { 27, "42", "Fab. Pirotecnia", false, false, 3, "Fabricante de Productos Pirotécnicos" },
                    { 28, "43", "Imp. Pirotecnia", false, false, 3, "Importador de Productos Pirotécnicos" },
                    { 29, "44", "Prod. Tabaco", false, false, 3, "Productor de Tabaco" },
                    { 30, "50", "Dist. Bebidas", false, false, 3, "Distribuidor de Bebidas Gaseosas..." },
                    { 31, "51", "Bebidas Alc.", false, false, 3, "Bebidas Alcohólicas" },
                    { 32, "52", "Cerveza", false, false, 3, "Cerveza" },
                    { 33, "53", "Tabaco", false, false, 3, "Productos del Tabaco" },
                    { 34, "54", "Gaseosas", false, false, 3, "Bebidas Carbonatadas o Gaseosas" },
                    { 35, "55", "Otros Específicos", false, false, 3, "Otros Específicos" },
                    { 36, "58", "Alcohol", false, false, 3, "Alcohol" },
                    { 37, "77", "Imp. Jugos", false, false, 3, "Importador de Jugos y Refrescos" },
                    { 38, "78", "Dist. Jugos", false, false, 3, "Distribuidor de Jugos y Refrescos" },
                    { 39, "79", "Llamadas Ext.", false, false, 3, "Sobre Llamadas Telefónicas Provenientes del Ext." },
                    { 40, "85", "Det. Jugos", false, false, 3, "Detallista de Jugos y Refrescos" },
                    { 41, "86", "Fab. Prep. Bebidas", false, false, 3, "Fabricante de Preparaciones para Bebidas" },
                    { 42, "91", "Fab. Jugos", false, false, 3, "Fabricante de Jugos y Refrescos" },
                    { 43, "92", "Imp. Prep. Bebidas", false, false, 3, "Importador de Preparaciones para Bebidas" },
                    { 44, "A1", "Esp. y Ad-Valorem", false, false, 3, "Específicos y Ad-Valorem" },
                    { 45, "A5", "Bebidas Energ.", false, false, 3, "Bebidas Gaseosas y Energizantes" },
                    { 46, "A7", "Alcohol Etílico", false, false, 3, "Alcohol Etílico" },
                    { 47, "A9", "Sacos Sintéticos", false, false, 3, "Sacos Sintéticos" },
                    { 48, "D4", "Otros Impuestos", false, false, 2, "Otros impuestos casos especiales" },
                    { 49, "D5", "Otras Tasas", false, false, 2, "Otras tasas casos especiales" }
                });

            migrationBuilder.InsertData(
                table: "cat_uni_medida",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 1, "01", "Metro" },
                    { 2, "02", "Yarda" },
                    { 3, "06", "Milímetro" },
                    { 4, "09", "Kilómetro cuadrado" },
                    { 5, "10", "Hectárea" },
                    { 6, "13", "Metro cuadrado" },
                    { 7, "15", "Vara cuadrada" },
                    { 8, "18", "Metro cúbico" },
                    { 9, "20", "Barril" },
                    { 10, "22", "Galón" },
                    { 11, "23", "Litro" },
                    { 12, "24", "Botella" },
                    { 13, "26", "Mililitro" },
                    { 14, "30", "Tonelada" },
                    { 15, "32", "Quintal" },
                    { 16, "33", "Arroba" },
                    { 17, "34", "Kilogramo" },
                    { 18, "36", "Libra" },
                    { 19, "37", "Onza troy" },
                    { 20, "38", "Onza" },
                    { 21, "39", "Gramo" },
                    { 22, "40", "Miligramo" },
                    { 23, "42", "Megawatt" },
                    { 24, "43", "Kilowatt" },
                    { 25, "44", "Watt" },
                    { 26, "45", "Megavoltio-amperio" },
                    { 27, "46", "Kilovoltio-amperio" },
                    { 28, "47", "Voltio-amperio" },
                    { 29, "49", "Gigawatt-hora" },
                    { 30, "50", "Megawatt-hora" },
                    { 31, "51", "Kilowatt-hora" },
                    { 32, "52", "Watt-hora" },
                    { 33, "53", "Kilovoltio" },
                    { 34, "54", "Voltio" },
                    { 35, "55", "Millar" },
                    { 36, "56", "Medio millar" },
                    { 37, "57", "Ciento" },
                    { 38, "58", "Docena" },
                    { 39, "59", "Unidad" },
                    { 40, "99", "Otra" }
                });

            migrationBuilder.InsertData(
                table: "cat_actividades_economicas",
                columns: new[] { "Id", "Codigo", "EsSeleccionable", "PadreId", "Valor" },
                values: new object[,]
                {
                    { 3, "1111", true, 2, "Cultivo de cereales excepto arroz y para forrajes" },
                    { 4, "1112", true, 2, "Cultivo de legumbres" },
                    { 5, "1113", true, 2, "Cultivo de semillas oleaginosas" },
                    { 6, "1114", true, 2, "Cultivo de plantas para la preparación de semillas" },
                    { 7, "1119", true, 2, "Cultivo de otros cereales excepto arroz y forrajeros n.c.p." },
                    { 8, "1120", true, 2, "Cultivo de arroz" },
                    { 9, "1131", true, 2, "Cultivo de raíces y tubérculos" },
                    { 10, "1132", true, 2, "Cultivo de brotes, bulbos, vegetales tubérculos y cultivos similares" },
                    { 11, "1133", true, 2, "Cultivo hortícola de fruto" },
                    { 12, "1134", true, 2, "Cultivo de hortalizas de hoja y otras hortalizas ncp" },
                    { 13, "1140", true, 2, "Cultivo de caña de azúcar" },
                    { 14, "1150", true, 2, "Cultivo de tabaco" },
                    { 15, "1161", true, 2, "Cultivo de algodón" },
                    { 16, "1162", true, 2, "Cultivo de fibras vegetales excepto algodón" },
                    { 17, "1191", true, 2, "Cultivo de plantas no perennes para la producción de semillas y flores" },
                    { 18, "1192", true, 2, "Cultivo de cereales y pastos para la alimentación animal" },
                    { 19, "1199", true, 2, "Producción de cultivos no estacionales ncp" },
                    { 20, "1220", true, 2, "Cultivo de frutas tropicales" },
                    { 21, "1230", true, 2, "Cultivo de cítricos" },
                    { 22, "1240", true, 2, "Cultivo de frutas de pepita y hueso" },
                    { 23, "1251", true, 2, "Cultivo de frutas ncp" },
                    { 24, "1252", true, 2, "Cultivo de otros frutos y nueces de árboles y arbustos" },
                    { 25, "1260", true, 2, "Cultivo de frutos oleaginosos" },
                    { 26, "1271", true, 2, "Cultivo de cafe" },
                    { 27, "1272", true, 2, "Cultivo de plantas para la elaboración de bebidas excepto cafe" },
                    { 28, "1281", true, 2, "Cultivo de especias y aromáticas" },
                    { 29, "1282", true, 2, "Cultivo de plantas para la obtención de productos medicinales y farmacéuticos" },
                    { 30, "1291", true, 2, "Cultivo de árboles de hule (caucho) para la obtención de látex" },
                    { 31, "1292", true, 2, "Cultivo de plantas para la obtención de productos químicos y colorantes" },
                    { 32, "1299", true, 2, "Producción de cultivos perennes ncp" },
                    { 33, "1300", true, 2, "Propagación de plantas" },
                    { 34, "1301", true, 2, "Cultivo de plantas y flores ornamentales" },
                    { 35, "1410", true, 2, "Cría y engorde de ganado bovino" },
                    { 36, "1420", true, 2, "Cría de caballos y otros equinos" },
                    { 37, "1440", true, 2, "Cría de ovejas y cabras" },
                    { 38, "1450", true, 2, "Cría de cerdos" },
                    { 39, "1460", true, 2, "Cría de aves de corral y producción de huevos" },
                    { 40, "1491", true, 2, "Cría de abejas apicultura para la obtención de miel y otros productos apícolas" },
                    { 41, "1492", true, 2, "Cría de conejos" },
                    { 42, "1493", true, 2, "Cría de iguanas y garrobos" },
                    { 43, "1494", true, 2, "Cría de mariposas y otros insectos" },
                    { 44, "1499", true, 2, "Cría y obtención de productos animales n.c.p." },
                    { 45, "1500", true, 2, "Cultivo de productos agrícolas en combinación con la cría de animales" },
                    { 46, "1611", true, 2, "Servicios de maquinaria agrícola" },
                    { 47, "1612", true, 2, "Control de plagas" },
                    { 48, "1613", true, 2, "Servicios de riego" },
                    { 49, "1614", true, 2, "Servicios de contratación de mano de obra para la agricultura" },
                    { 50, "1619", true, 2, "Servicios agrícolas ncp" },
                    { 51, "1621", true, 2, "Actividades para mejorar la reproducción, el crecimiento y el rendimiento de los animales y sus productos" },
                    { 52, "1622", true, 2, "Servicios de mano de obra pecuaria" },
                    { 53, "1629", true, 2, "Servicios pecuarios ncp" },
                    { 54, "1631", true, 2, "Labores post cosecha de preparación de los productos agrícolas para su comercialización o para la industria" },
                    { 55, "1632", true, 2, "Servicio de beneficio de café" },
                    { 56, "1633", true, 2, "Servicio de beneficiado de plantas textiles (incluye el beneficiado cuando este es realizado en la misma explotación agropecuaria)" },
                    { 57, "1640", true, 2, "Tratamiento de semillas para la propagación" },
                    { 58, "1700", true, 2, "Caza ordinaria y mediante trampas, repoblación de animales de caza y servicios conexos" },
                    { 60, "2100", true, 59, "Silvicultura y otras actividades forestales" },
                    { 61, "2200", true, 59, "Extracción de madera" },
                    { 62, "2300", true, 59, "Recolección de productos diferentes a la madera" },
                    { 63, "2400", true, 59, "Servicios de apoyo a la silvicultura" },
                    { 65, "3110", true, 64, "Pesca marítima de altura y costera" },
                    { 66, "3120", true, 64, "Pesca de agua dulce" },
                    { 67, "3210", true, 64, "Acuicultura marítima" },
                    { 68, "3220", true, 64, "Acuicultura de agua dulce" },
                    { 69, "3300", true, 64, "Servicios de apoyo a la pesca y acuicultura" },
                    { 72, "5100", true, 71, "Extracción de hulla" },
                    { 73, "5200", true, 71, "Extracción y aglomeración de lignito" },
                    { 75, "6100", true, 74, "Extracción de petróleo crudo" },
                    { 76, "6200", true, 74, "Extracción de gas natural" },
                    { 78, "7100", true, 77, "Extracción de minerales de hierro" },
                    { 79, "7210", true, 77, "Extracción de minerales de uranio y torio" },
                    { 80, "7290", true, 77, "Extracción de minerales metalíferos no ferrosos" },
                    { 82, "8100", true, 81, "Extracción de piedra, arena y arcilla" },
                    { 83, "8910", true, 81, "Extracción de minerales para la fabricación de abonos y productos químicos" },
                    { 84, "8920", true, 81, "Extracción y aglomeración de turba" },
                    { 85, "8930", true, 81, "Extracción de sal" },
                    { 86, "8990", true, 81, "Explotación de otras minas y canteras ncp" },
                    { 88, "9100", true, 87, "Actividades de apoyo a la extracción de petróleo y gas natural" },
                    { 89, "9900", true, 87, "Actividades de apoyo a la explotación de minas y canteras" },
                    { 92, "10101", true, 91, "Servicio de rastros y mataderos de bovinos y porcinos" },
                    { 93, "10102", true, 91, "Matanza y procesamiento de bovinos y porcinos" },
                    { 94, "10103", true, 91, "Matanza y procesamientos de aves de corral" },
                    { 95, "10104", true, 91, "Elaboracion y conservación de embutidos y tripas naturales" },
                    { 96, "10105", true, 91, "Servicios de conservación y empaque de carnes" },
                    { 97, "10106", true, 91, "Elaboración y conservación de grasas y aceites animales" },
                    { 98, "10107", true, 91, "Servicios de molienda de carne" },
                    { 99, "10108", true, 91, "Elaboración de productos de carne ncp" },
                    { 100, "10201", true, 91, "Procesamiento y conservación de pescado, crustáceos y moluscos" },
                    { 101, "10209", true, 91, "Fabricación de productos de pescado ncp" },
                    { 102, "10301", true, 91, "Elaboración de jugos de frutas y hortalizas" },
                    { 103, "10302", true, 91, "Elaboración y envase de jaleas, mermeladas y frutas deshidratadas" },
                    { 104, "10309", true, 91, "Elaboración de productos de frutas y hortalizas n.c.p." },
                    { 105, "10401", true, 91, "Fabricación de aceites y grasas vegetales y animales comestibles" },
                    { 106, "10402", true, 91, "Fabricación de aceites y grasas vegetales y animales no comestibles" },
                    { 107, "10409", true, 91, "Servicio de maquilado de aceites" },
                    { 108, "10501", true, 91, "Fabricación de productos lácteos excepto sorbetes y quesos sustitutos" },
                    { 109, "10502", true, 91, "Fabricación de sorbetes y helados" },
                    { 110, "10503", true, 91, "Fabricación de quesos" },
                    { 111, "10611", true, 91, "Molienda de cereales" },
                    { 112, "10612", true, 91, "Elaboración de cereales para el desayuno y similares" },
                    { 113, "10613", true, 91, "Servicios de beneficiado de productos agrícolas ncp (excluye Beneficio de azúcar rama 1072 y beneficio de café rama 0163)" },
                    { 114, "10621", true, 91, "Fabricación de almidón" },
                    { 115, "10628", true, 91, "Servicio de molienda de maíz húmedo molino para nixtamal" },
                    { 116, "10711", true, 91, "Elaboración de tortillas" },
                    { 117, "10712", true, 91, "Fabricación de pan, galletas y barquillos" },
                    { 118, "10713", true, 91, "Fabricación de repostería" },
                    { 119, "10721", true, 91, "Ingenios azucareros" },
                    { 120, "10722", true, 91, "Molienda de caña de azúcar para la elaboración de dulces" },
                    { 121, "10723", true, 91, "Elaboración de jarabes de azúcar y otros similares" },
                    { 122, "10724", true, 91, "Maquilado de azúcar de caña" },
                    { 123, "10730", true, 91, "Fabricación de cacao, chocolates y productos de confitería" },
                    { 124, "10740", true, 91, "Elaboración de macarrones, fideos, y productos farináceos similares" },
                    { 125, "10750", true, 91, "Elaboración de comidas y platos preparados para la reventa en locales y/o para exportación" },
                    { 126, "10791", true, 91, "Elaboración de productos de café" },
                    { 127, "10792", true, 91, "Elaboración de especies, sazonadores y condimentos" },
                    { 128, "10793", true, 91, "Elaboración de sopas, cremas y consomé" },
                    { 129, "10794", true, 91, "Fabricación de bocadillos tostados y/o fritos" },
                    { 130, "10799", true, 91, "Elaboración de productos alimenticios ncp" },
                    { 131, "10800", true, 91, "Elaboración de alimentos preparados para animales" },
                    { 133, "11012", true, 132, "Fabricación de aguardiente y licores" },
                    { 134, "11020", true, 132, "Elaboración de vinos" },
                    { 135, "11030", true, 132, "Fabricación de cerveza" },
                    { 136, "11041", true, 132, "Fabricación de aguas gaseosas" },
                    { 137, "11042", true, 132, "Fabricación y envasado de agua" },
                    { 138, "11043", true, 132, "Elaboración de refrescos" },
                    { 139, "11048", true, 132, "Maquilado de aguas gaseosas" },
                    { 140, "11049", true, 132, "Elaboración de bebidas no alcohólicas" },
                    { 142, "12000", true, 141, "Elaboración de productos de tabaco" },
                    { 144, "13111", true, 143, "Preparación de fibras textiles" },
                    { 145, "13112", true, 143, "Fabricación de hilados" },
                    { 146, "13120", true, 143, "Fabricación de telas" },
                    { 147, "13130", true, 143, "Acabado de productos textiles" },
                    { 148, "13910", true, 143, "Fabricación de tejidos de punto y ganchillo" },
                    { 149, "13921", true, 143, "Fabricación de productos textiles para el hogar" },
                    { 150, "13922", true, 143, "Sacos, bolsas y otros artículos textiles" },
                    { 151, "13929", true, 143, "Fabricación de artículos confeccionados con materiales textiles, excepto prendas de vestir n.c.p" },
                    { 152, "13930", true, 143, "Fabricación de tapices y alfombras" },
                    { 153, "13941", true, 143, "Fabricación de cuerdas de henequén y otras fibras naturales (lazos, pitas)" },
                    { 154, "13942", true, 143, "Fabricación de redes de diversos materiales" },
                    { 155, "13948", true, 143, "Maquilado de productos trenzables de cualquier material (petates, sillas, etc.)" },
                    { 156, "13991", true, 143, "Fabricación de adornos, etiquetas y otros artículos para prendas de vestir" },
                    { 157, "13992", true, 143, "Servicio de bordados en artículos y prendas de tela" },
                    { 158, "13999", true, 143, "Fabricación de productos textiles ncp" },
                    { 160, "14101", true, 159, "Fabricación de ropa interior, para dormir y similares" },
                    { 161, "14102", true, 159, "Fabricación de ropa para niños" },
                    { 162, "14103", true, 159, "Fabricación de prendas de vestir para ambos sexos" },
                    { 163, "14104", true, 159, "Confección de prendas a medida" },
                    { 164, "14105", true, 159, "Fabricación de prendas de vestir para deportes" },
                    { 165, "14106", true, 159, "Elaboración de artesanías de uso personal confeccionadas especialmente de materiales textiles" },
                    { 166, "14108", true, 159, "Maquilado de prendas de vestir, accesorios y otros" },
                    { 167, "14109", true, 159, "Fabricación de prendas y accesorios de vestir n.c.p." },
                    { 168, "14200", true, 159, "Fabricación de artículos de piel" },
                    { 169, "14301", true, 159, "Fabricación de calcetines, calcetas, medias (panty house) y otros similares" },
                    { 170, "14302", true, 159, "Fabricación de ropa interior de tejido de punto" },
                    { 171, "14309", true, 159, "Fabricación de prendas de vestir de tejido de punto ncp" },
                    { 173, "15110", true, 172, "Curtido y adobo de cueros; adobo y teñido de pieles" },
                    { 174, "15121", true, 172, "Fabricación de maletas, bolsos de mano y otros artículos de marroquinería" },
                    { 175, "15122", true, 172, "Fabricación de monturas, accesorios y vainas talabartería" },
                    { 176, "15123", true, 172, "Fabricación de artesanías principalmente de cuero natural y sintético" },
                    { 177, "15128", true, 172, "Maquilado de artículos de cuero natural, sintético y de otros materiales" },
                    { 178, "15201", true, 172, "Fabricación de calzado" },
                    { 179, "15202", true, 172, "Fabricación de partes y accesorios de calzado" },
                    { 180, "15208", true, 172, "Maquilado de partes y accesorios de calzado" },
                    { 182, "16100", true, 181, "Aserradero y acepilladura de madera" },
                    { 183, "16210", true, 181, "Fabricación de madera laminada, terciada, enchapada y contrachapada, paneles para la construcción" },
                    { 184, "16220", true, 181, "Fabricación de partes y piezas de carpintería para edificios y construcciones" },
                    { 185, "16230", true, 181, "Fabricación de envases y recipientes de madera" },
                    { 186, "16292", true, 181, "Fabricación de artesanías de madera, semillas, materiales trenzables" },
                    { 187, "16299", true, 181, "Fabricación de productos de madera, corcho, paja y materiales trenzables ncp" },
                    { 189, "17010", true, 188, "Fabricación de pasta de madera, papel y cartón" },
                    { 190, "17020", true, 188, "Fabricación de papel y cartón ondulado y envases de papel y cartón" },
                    { 191, "17091", true, 188, "Fabricación de artículos de papel y cartón de uso personal y doméstico" },
                    { 192, "17092", true, 188, "Fabricación de productos de papel ncp" },
                    { 194, "18110", true, 193, "Impresión" },
                    { 195, "18120", true, 193, "Servicios relacionados con la impresión" },
                    { 196, "18200", true, 193, "Reproducción de grabaciones" },
                    { 198, "19100", true, 197, "Fabricación de productos de hornos de coque" },
                    { 199, "19201", true, 197, "Fabricación de combustible" },
                    { 200, "19202", true, 197, "Fabricación de aceites y lubricantes" },
                    { 202, "20111", true, 201, "Fabricación de materias primas para la fabricación de colorantes" },
                    { 203, "20112", true, 201, "Fabricación de materiales curtientes" },
                    { 204, "20113", true, 201, "Fabricación de gases industriales" },
                    { 205, "20114", true, 201, "Fabricación de alcohol etílico" },
                    { 206, "20119", true, 201, "Fabricación de sustancias químicas básicas" },
                    { 207, "20120", true, 201, "Fabricación de abonos y fertilizantes" },
                    { 208, "20130", true, 201, "Fabricación de plástico y caucho en formas primarias" },
                    { 209, "20210", true, 201, "Fabricación de plaguicidas y otros productos químicos de uso agropecuario" },
                    { 210, "20220", true, 201, "Fabricación de pinturas, barnices y productos de revestimiento similares; tintas de imprenta y masillas" },
                    { 211, "20231", true, 201, "Fabricación de jabones, detergentes y similares para limpieza" },
                    { 212, "20232", true, 201, "Fabricación de perfumes, cosméticos y productos de higiene y cuidado personal, incluyendo tintes, champú, etc." },
                    { 213, "20291", true, 201, "Fabricación de tintas y colores para escribir y pintar; fabricación de cintas para impresoras" },
                    { 214, "20292", true, 201, "Fabricación de productos pirotécnicos, explosivos y municiones" },
                    { 215, "20299", true, 201, "Fabricación de productos químicos n.c.p." },
                    { 216, "20300", true, 201, "Fabricación de fibras artificiales" },
                    { 218, "21001", true, 217, "Manufactura de productos farmacéuticos, sustancias químicas y productos botánicos" },
                    { 219, "21008", true, 217, "Maquilado de medicamentos" },
                    { 221, "22110", true, 220, "Fabricación de cubiertas y cámaras; renovación y recauchutado de cubiertas" },
                    { 222, "22190", true, 220, "Fabricación de otros productos de caucho" },
                    { 223, "22201", true, 220, "Fabricación de envases plásticos" },
                    { 224, "22202", true, 220, "Fabricación de productos plásticos para uso personal o doméstico" },
                    { 225, "22208", true, 220, "Maquila de plásticos" },
                    { 226, "22209", true, 220, "Fabricación de productos plásticos n.c.p." },
                    { 228, "23101", true, 227, "Fabricación de vidrio" },
                    { 229, "23102", true, 227, "Fabricación de recipientes y envases de vidrio" },
                    { 230, "23108", true, 227, "Servicio de maquilado" },
                    { 231, "23109", true, 227, "Fabricación de productos de vidrio ncp" },
                    { 232, "23910", true, 227, "Fabricación de productos refractarios" },
                    { 233, "23920", true, 227, "Fabricación de productos de arcilla para la construcción" },
                    { 234, "23931", true, 227, "Fabricación de productos de cerámica y porcelana no refractaria" },
                    { 235, "23932", true, 227, "Fabricación de productos de cerámica y porcelana ncp" },
                    { 236, "23940", true, 227, "Fabricación de cemento, cal y yeso" },
                    { 237, "23950", true, 227, "Fabricación de artículos de hormigón, cemento y yeso" },
                    { 238, "23960", true, 227, "Corte, tallado y acabado de la piedra" },
                    { 239, "23990", true, 227, "Fabricación de productos minerales no metálicos ncp" },
                    { 241, "24100", true, 240, "Industrias básicas de hierro y acero" },
                    { 242, "24200", true, 240, "Fabricación de productos primarios de metales preciosos y metales no ferrosos" },
                    { 243, "24310", true, 240, "Fundición de hierro y acero" },
                    { 244, "24320", true, 240, "Fundición de metales no ferrosos" },
                    { 246, "25111", true, 245, "Fabricación de productos metálicos para uso estructural" },
                    { 247, "25118", true, 245, "Servicio de maquila para la fabricación de estructuras metálicas" },
                    { 248, "25120", true, 245, "Fabricación de tanques, depósitos y recipientes de metal" },
                    { 249, "25130", true, 245, "Fabricación de generadores de vapor, excepto calderas de agua caliente para calefacción central" },
                    { 250, "25200", true, 245, "Fabricación de armas y municiones" },
                    { 251, "25910", true, 245, "Forjado, prensado, estampado y laminado de metales; pulvimetalurgia" },
                    { 252, "25920", true, 245, "Tratamiento y revestimiento de metales" },
                    { 253, "25930", true, 245, "Fabricación de artículos de cuchillería, herramientas de mano y artículos de ferretería" },
                    { 254, "25991", true, 245, "Fabricación de envases y artículos conexos de metal" },
                    { 255, "25992", true, 245, "Fabricación de artículos metálicos de uso personal y/o doméstico" },
                    { 256, "25999", true, 245, "Fabricación de productos elaborados de metal ncp" },
                    { 258, "26100", true, 257, "Fabricación de componentes electrónicos" },
                    { 259, "26200", true, 257, "Fabricación de computadoras y equipo conexo" },
                    { 260, "26300", true, 257, "Fabricación de equipo de comunicaciones" },
                    { 261, "26400", true, 257, "Fabricación de aparatos electrónicos de consumo para audio, video radio y televisión" },
                    { 262, "26510", true, 257, "Fabricación de instrumentos y aparatos para medir, verificar, ensayar, navegar y de control de procesos industriales" },
                    { 263, "26520", true, 257, "Fabricación de relojes y piezas de relojes" },
                    { 264, "26600", true, 257, "Fabricación de equipo médico de irradiación y equipo electrónico de uso médico y terapéutico" },
                    { 265, "26700", true, 257, "Fabricación de instrumentos de óptica y equipo fotográfico" },
                    { 266, "26800", true, 257, "Fabricación de medios magnéticos y ópticos" },
                    { 268, "27100", true, 267, "Fabricación de motores, generadores, transformadores eléctricos, aparatos de distribución y control de electricidad" },
                    { 269, "27200", true, 267, "Fabricación de pilas, baterías y acumuladores" },
                    { 270, "27310", true, 267, "Fabricación de cables de fibra óptica" },
                    { 271, "27320", true, 267, "Fabricación de otros hilos y cables eléctricos" },
                    { 272, "27330", true, 267, "Fabricación de dispositivos de cableados" },
                    { 273, "27400", true, 267, "Fabricación de equipo eléctrico de iluminación" },
                    { 274, "27500", true, 267, "Fabricación de aparatos de uso doméstico" },
                    { 275, "27900", true, 267, "Fabricación de otros tipos de equipo eléctrico" },
                    { 277, "28110", true, 276, "Fabricación de motores y turbinas, excepto motores para aeronaves, vehículos automotores y motocicletas" },
                    { 278, "28120", true, 276, "Fabricación de equipo hidráulico" },
                    { 279, "28130", true, 276, "Fabricación de otras bombas, compresores, grifos y válvulas" },
                    { 280, "28140", true, 276, "Fabricación de cojinetes, engranajes, trenes de engranajes y piezas de transmisión" },
                    { 281, "28150", true, 276, "Fabricación de hornos y quemadores" },
                    { 282, "28160", true, 276, "Fabricación de equipo de elevación y manipulación" },
                    { 283, "28170", true, 276, "Fabricación de maquinaria y equipo de oficina" },
                    { 284, "28180", true, 276, "Fabricación de herramientas manuales" },
                    { 285, "28190", true, 276, "Fabricación de otros tipos de maquinaria de uso general" },
                    { 286, "28210", true, 276, "Fabricación de maquinaria agropecuaria y forestal" },
                    { 287, "28220", true, 276, "Fabricación de máquinas para conformar metales y maquinaria herramienta" },
                    { 288, "28230", true, 276, "Fabricación de maquinaria metalúrgica" },
                    { 289, "28240", true, 276, "Fabricación de maquinaria para la explotación de minas y canteras y para obras de construcción" },
                    { 290, "28250", true, 276, "Fabricación de maquinaria para la elaboración de alimentos, bebidas y tabaco" },
                    { 291, "28260", true, 276, "Fabricación de maquinaria para la elaboración de productos textiles, prendas de vestir y cueros" },
                    { 292, "28291", true, 276, "Fabricación de máquinas para imprenta" },
                    { 293, "28299", true, 276, "Fabricación de maquinaria de uso especial ncp" },
                    { 295, "29100", true, 294, "Fabricación vehículos automotores" },
                    { 296, "29200", true, 294, "Fabricación de carrocerías para vehículos automotores; fabricación de remolques y semirremolques" },
                    { 297, "29300", true, 294, "Fabricación de partes, piezas y accesorios para vehículos automotores" },
                    { 299, "30110", true, 298, "Fabricación de buques" },
                    { 300, "30120", true, 298, "Construcción y reparación de embarcaciones de recreo" },
                    { 301, "30200", true, 298, "Fabricación de locomotoras y de material rodante" },
                    { 302, "30300", true, 298, "Fabricación de aeronaves y naves espaciales" },
                    { 303, "30400", true, 298, "Fabricación de vehículos militares de combate" },
                    { 304, "30910", true, 298, "Fabricación de motocicletas" },
                    { 305, "30920", true, 298, "Fabricación de bicicletas y sillones de ruedas para inválidos" },
                    { 306, "30990", true, 298, "Fabricación de equipo de transporte ncp" },
                    { 308, "31001", true, 307, "Fabricación de colchones y somier" },
                    { 309, "31002", true, 307, "Fabricación de muebles y otros productos de madera a medida" },
                    { 310, "31008", true, 307, "Servicios de maquilado de muebles" },
                    { 311, "31009", true, 307, "Fabricación de muebles ncp" },
                    { 313, "32110", true, 312, "Fabricación de joyas platerías y joyerías" },
                    { 314, "32120", true, 312, "Fabricación de joyas de imitación (fantasía) y artículos conexos" },
                    { 315, "32200", true, 312, "Fabricación de instrumentos musicales" },
                    { 316, "32301", true, 312, "Fabricación de artículos de deporte" },
                    { 317, "32308", true, 312, "Servicio de maquila de productos deportivos" },
                    { 318, "32401", true, 312, "Fabricación de juegos de mesa y de salón" },
                    { 319, "32402", true, 312, "Servicio de maquilado de juguetes y juegos" },
                    { 320, "32409", true, 312, "Fabricación de juegos y juguetes n.c.p." },
                    { 321, "32500", true, 312, "Fabricación de instrumentos y materiales médicos y odontológicos" },
                    { 322, "32901", true, 312, "Fabricación de lápices, bolígrafos, sellos y artículos de librería en general" },
                    { 323, "32902", true, 312, "Fabricación de escobas, cepillos, pinceles y similares" },
                    { 324, "32903", true, 312, "Fabricación de artesanías de materiales diversos" },
                    { 325, "32904", true, 312, "Fabricación de artículos de uso personal y domésticos n.c.p." },
                    { 326, "32905", true, 312, "Fabricación de accesorios para las confecciones y la marroquinería n.c.p." },
                    { 327, "32908", true, 312, "Servicios de maquila ncp" },
                    { 328, "32909", true, 312, "Fabricación de productos manufacturados n.c.p." },
                    { 330, "33110", true, 329, "Reparación y mantenimiento de productos elaborados de metal" },
                    { 331, "33120", true, 329, "Reparación y mantenimiento de maquinaria" },
                    { 332, "33130", true, 329, "Reparación y mantenimiento de equipo electrónico y óptico" },
                    { 333, "33140", true, 329, "Reparación y mantenimiento de equipo eléctrico" },
                    { 334, "33150", true, 329, "Reparación y mantenimiento de equipo de transporte, excepto vehículos automotores" },
                    { 335, "33190", true, 329, "Reparación y mantenimiento de equipos n.c.p." },
                    { 336, "33200", true, 329, "Instalación de maquinaria y equipo industrial" },
                    { 339, "35101", true, 338, "Generación de energía eléctrica" },
                    { 340, "35102", true, 338, "Transmisión de energía eléctrica" },
                    { 341, "35103", true, 338, "Distribución de energía eléctrica" },
                    { 342, "35200", true, 338, "Fabricación de gas, distribución de combustibles gaseosos por tuberías" },
                    { 343, "35300", true, 338, "Suministro de vapor y agua caliente" },
                    { 346, "36000", true, 345, "Captación, tratamiento y suministro de agua" },
                    { 348, "37000", true, 347, "Evacuación de aguas residuales (alcantarillado)" },
                    { 350, "38110", true, 349, "Recolección y transporte de desechos sólidos proveniente de hogares y sector urbano" },
                    { 351, "38120", true, 349, "Recolección de desechos peligrosos" },
                    { 352, "38210", true, 349, "Tratamiento y eliminación de desechos inicuos" },
                    { 353, "38220", true, 349, "Tratamiento y eliminación de desechos peligrosos" },
                    { 354, "38301", true, 349, "Reciclaje de desperdicios y desechos textiles" },
                    { 355, "38302", true, 349, "Reciclaje de desperdicios y desechos de plastico y caucho" },
                    { 356, "38303", true, 349, "Reciclaje de desperdicios y desechos de vidrio" },
                    { 357, "38304", true, 349, "Reciclaje de desperdicios y desechos de papel y cart≤n" },
                    { 358, "38305", true, 349, "Reciclaje de desperdicios y desechos metálicos" },
                    { 359, "38309", true, 349, "Reciclaje de desperdicios y desechos no metálicos n.c.p." },
                    { 361, "39000", true, 360, "Actividades de Saneamiento y otros Servicios de Gestión de Desechos" },
                    { 364, "41001", true, 363, "Construcción de edificios residenciales" },
                    { 365, "41002", true, 363, "Construcción de edificios no residenciales" },
                    { 367, "42100", true, 366, "Construccion de carreteras, calles y caminos" },
                    { 368, "42200", true, 366, "Construccion de proyectos de servicio publico" },
                    { 369, "42900", true, 366, "Construccion de obras de ingenieria civil n.c.p." },
                    { 371, "43110", true, 370, "Demolicion" },
                    { 372, "43120", true, 370, "Preparacion de terreno" },
                    { 373, "43210", true, 370, "Instalaciones electricas" },
                    { 374, "43220", true, 370, "Instalacion de fontaneria, calefaccion y aire acondicionado" },
                    { 375, "43290", true, 370, "Otras instalaciones para obras de construccion" },
                    { 376, "43300", true, 370, "Terminacion y acabado de edificios" },
                    { 377, "43900", true, 370, "Otras actividades especializadas de construccion" },
                    { 378, "43901", true, 370, "Fabricacion de techos y materiales diversos" },
                    { 381, "45100", true, 380, "Venta de vehiculos automotores" },
                    { 382, "45201", true, 380, "Reparacion mecanica de vehiculos automotores" },
                    { 383, "45202", true, 380, "Reparaciones electricas del automotor y recarga de baterias" },
                    { 384, "45203", true, 380, "Enderezado y pintura de vehiculos automotores" },
                    { 385, "45204", true, 380, "Reparaciones de radiadores, escapes y silenciadores" },
                    { 386, "45205", true, 380, "Reparacion y reconstruccion de vias, stop y otros articulos de fibra de vidrio" },
                    { 387, "45206", true, 380, "Reparacion de llantas de vehiculos automotores" },
                    { 388, "45207", true, 380, "Polarizado de vehiculos (mediante la adhesion de papel especial a los vidrios)" },
                    { 389, "45208", true, 380, "Lavado y pasteado de vehiculos (carwash)" },
                    { 390, "45209", true, 380, "Reparaciones de vehiculos n.c.p." },
                    { 391, "45211", true, 380, "Remolque de vehiculos automotores" },
                    { 392, "45301", true, 380, "Venta de partes, piezas y accesorios nuevos para vehiculos automotores" },
                    { 393, "45302", true, 380, "Venta de partes, piezas y accesorios usados para vehiculos automotores" },
                    { 394, "45401", true, 380, "Venta de motocicletas" },
                    { 395, "45402", true, 380, "Venta de repuestos, piezas y accesorios de motocicletas" },
                    { 396, "45403", true, 380, "Mantenimiento y reparacion de motocicletas" },
                    { 398, "46100", true, 397, "Venta al por mayor a cambio de retribucion o por contrata" },
                    { 399, "46201", true, 397, "Venta al por mayor de materias primas agrícolas" },
                    { 400, "46202", true, 397, "Venta al por mayor de productos de la silvicultura" },
                    { 401, "46203", true, 397, "Venta al por mayor de productos pecuarios y de granja" },
                    { 402, "46211", true, 397, "Venta de productos para uso agropecuario" },
                    { 403, "46291", true, 397, "Venta al por mayor de granos básicos (cereales, leguminosas)" },
                    { 404, "46292", true, 397, "Venta al por mayor de semillas mejoradas para cultivo" },
                    { 405, "46293", true, 397, "Venta al por mayor de café, oro y uva" },
                    { 406, "46294", true, 397, "Venta al por mayor de caña de azúcar" },
                    { 407, "46295", true, 397, "Venta al por mayor de flores, plantas y otros productos naturales" },
                    { 408, "46296", true, 397, "Venta al por mayor de productos agrícolas" },
                    { 409, "46297", true, 397, "Venta al por mayor de ganado bovino (vivo)" },
                    { 410, "46298", true, 397, "Venta al por mayor de animales porcinos, ovinos, caprino, canículas, apícolas, avícolas vivos" },
                    { 411, "46299", true, 397, "Venta de otras especies vivas del reino animal" },
                    { 412, "46301", true, 397, "Venta al por mayor de alimentos" },
                    { 413, "46302", true, 397, "Venta al por mayor de bebidas" },
                    { 414, "46303", true, 397, "Venta al por mayor de tabaco" },
                    { 415, "46371", true, 397, "Venta al por mayor de frutas, hortalizas (verduras), legumbres y tubérculos" },
                    { 416, "46372", true, 397, "Venta al por mayor de pollos, gallinas destazadas, pavos y otras aves" },
                    { 417, "46373", true, 397, "Venta al por mayor de carne bovina y porcina, productos de carne y embutidos" },
                    { 418, "46374", true, 397, "Venta al por mayor de huevos" },
                    { 419, "46375", true, 397, "Venta al por mayor de productos lácteos" },
                    { 420, "46376", true, 397, "Venta al por mayor de productos farináceos de panadería (pan dulce, cakes, repostería, etc.)" },
                    { 421, "46377", true, 397, "Venta al por mayor de pastas alimenticias, aceites y grasas comestibles vegetal y animal" },
                    { 422, "46378", true, 397, "Venta al por mayor de sal comestible" },
                    { 423, "46379", true, 397, "Venta al por mayor de azúcar" },
                    { 424, "46391", true, 397, "Venta al por mayor de abarrotes (vinos, licores, productos alimenticios envasados, etc.)" },
                    { 425, "46392", true, 397, "Venta al por mayor de aguas gaseosas" },
                    { 426, "46393", true, 397, "Venta al por mayor de agua purificada" },
                    { 427, "46394", true, 397, "Venta al por mayor de refrescos y otras bebidas, líquidas o en polvo" },
                    { 428, "46395", true, 397, "Venta al por mayor de cerveza y licores" },
                    { 429, "46396", true, 397, "Venta al por mayor de hielo" },
                    { 430, "46411", true, 397, "Venta al por mayor de hilados, tejidos y productos textiles de mercería" },
                    { 431, "46412", true, 397, "Venta al por mayor de artículos textiles excepto confecciones para el hogar" },
                    { 432, "46413", true, 397, "Venta al por mayor de confecciones textiles para el hogar" },
                    { 433, "46414", true, 397, "Venta al por mayor de prendas de vestir y accesorios de vestir" },
                    { 434, "46415", true, 397, "Venta al por mayor de ropa usada" },
                    { 435, "46416", true, 397, "Venta al por mayor de calzado" },
                    { 436, "46417", true, 397, "Venta al por mayor de artículos de marroquinería y talabartería" },
                    { 437, "46418", true, 397, "Venta al por mayor de artículos de peletería" },
                    { 438, "46419", true, 397, "Venta al por mayor de otros artículos textiles n.c.p." },
                    { 439, "46471", true, 397, "Venta al por mayor de instrumentos musicales" },
                    { 440, "46472", true, 397, "Venta al por mayor de colchones, almohadas, cojines, etc." },
                    { 441, "46473", true, 397, "Venta al por mayor de artículos de aluminio para el hogar y para otros usos" },
                    { 442, "46474", true, 397, "Venta al por mayor de depósitos y otros artículos plásticos para el hogar y otros usos, incluyendo los desechables de durapax y no desechables" },
                    { 443, "46475", true, 397, "Venta al por mayor de cámaras fotográficas, accesorios y materiales" },
                    { 444, "46482", true, 397, "Venta al por mayor de medicamentos, artículos y otros productos de uso veterinario" },
                    { 445, "46483", true, 397, "Venta al por mayor de productos y artículos de belleza y de uso personal" },
                    { 446, "46484", true, 397, "Venta de productos farmacéuticos y medicinales" },
                    { 447, "46491", true, 397, "Venta al por mayor de productos medicinales, cosméticos, perfumería y productos de limpieza" },
                    { 448, "46492", true, 397, "Venta al por mayor de relojes y artículos de joyería" },
                    { 449, "46493", true, 397, "Venta al por mayor de electrodomésticos y artículos del hogar excepto bazar; artículos de iluminación" },
                    { 450, "46494", true, 397, "Venta al por mayor de artículos de bazar y similares" },
                    { 451, "46495", true, 397, "Venta al por mayor de artículos de óptica" },
                    { 452, "46496", true, 397, "Venta al por mayor de revistas, periódicos, libros, artículos de librería y artículos de papel y cartón en general" },
                    { 453, "46497", true, 397, "Venta de artículos deportivos, juguetes y rodados" },
                    { 454, "46498", true, 397, "Venta al por mayor de productos usados para el hogar o el uso personal" },
                    { 455, "46499", true, 397, "Venta al por mayor de enseres domésticos y de uso personal n.c.p." },
                    { 456, "46500", true, 397, "Venta al por mayor de bicicletas, partes, accesorios y otros" },
                    { 457, "46510", true, 397, "Venta al por mayor de computadoras, equipo periférico y programas informáticos" },
                    { 458, "46520", true, 397, "Venta al por mayor de equipos de comunicación" },
                    { 459, "46530", true, 397, "Venta al por mayor de maquinaria y equipo agropecuario, accesorios, partes y suministros" },
                    { 460, "46590", true, 397, "Venta de equipos e instrumentos de uso profesional y científico y aparatos de medida y control" },
                    { 461, "46591", true, 397, "Venta al por mayor de maquinaria equipo, accesorios y materiales para la industria de la madera y sus productos" },
                    { 462, "46592", true, 397, "Venta al por mayor de maquinaria, equipo, accesorios y materiales para la industria gráfica y del papel, cartón y productos de papel y cartón" },
                    { 463, "46593", true, 397, "Venta al por mayor de maquinaria, equipo, accesorios y materiales para la industria de productos químicos, plástico y caucho" },
                    { 464, "46594", true, 397, "Venta al por mayor de maquinaria, equipo, accesorios y materiales para la industria metálica y de sus productos" },
                    { 465, "46595", true, 397, "Venta al por mayor de equipamiento para uso médico, odontológico, veterinario y servicios conexos" },
                    { 466, "46596", true, 397, "Venta al por mayor de maquinaria, equipo, accesorios y partes para la industria de la alimentación" },
                    { 467, "46597", true, 397, "Venta al por mayor de maquinaria, equipo, accesorios y partes para la industria textil, confecciones y cuero" },
                    { 468, "46598", true, 397, "Venta al por mayor de maquinaria, equipo y accesorios para la construcción y explotación de minas y canteras" },
                    { 469, "46599", true, 397, "Venta al por mayor de otro tipo de maquinaria y equipo con sus accesorios y partes" },
                    { 470, "46610", true, 397, "Venta al por mayor de otros combustibles sólidos, líquidos, gaseosos y de productos conexos" },
                    { 471, "46612", true, 397, "Venta al por mayor de combustibles para automotores, aviones, barcos, maquinaria y otros" },
                    { 472, "46613", true, 397, "Venta al por mayor de lubricantes, grasas y otros aceites para automotores, maquinaria industrial, etc." },
                    { 473, "46614", true, 397, "Venta al por mayor de gas propano" },
                    { 474, "46615", true, 397, "Venta al por mayor de leña y carbón" },
                    { 475, "46620", true, 397, "Venta al por mayor de metales y minerales metálicos" },
                    { 476, "46631", true, 397, "Venta al por mayor de puertas, ventanas, vitrinas y similares" },
                    { 477, "46632", true, 397, "Venta al por mayor de artículos de ferretería y pintureras" },
                    { 478, "46633", true, 397, "Vidrieras" },
                    { 479, "46634", true, 397, "Venta al por mayor de maderas" },
                    { 480, "46639", true, 397, "Venta al por mayor de materiales para la construcción n.c.p." },
                    { 481, "46691", true, 397, "Venta al por mayor de sal industrial sin yodar" },
                    { 482, "46692", true, 397, "Venta al por mayor de productos intermedios y desechos de origen textil" },
                    { 483, "46693", true, 397, "Venta al por mayor de productos intermedios y desechos de origen metálico" },
                    { 484, "46694", true, 397, "Venta al por mayor de productos intermedios y desechos de papel y cartón" },
                    { 486, "46695", true, 485, "Venta al por mayor fertilizantes, abonos, agroquímicos y productos similares" },
                    { 487, "46696", true, 485, "Venta al por mayor de productos intermedios y desechos de origen plástico" },
                    { 488, "46697", true, 485, "Venta al por mayor de tintas para imprenta, productos curtientes y materias y productos colorantes" },
                    { 489, "46698", true, 485, "Venta de productos intermedios y desechos de origen químico y de caucho" },
                    { 490, "46699", true, 485, "Venta al por mayor de productos intermedios y desechos ncp" },
                    { 491, "46701", true, 485, "Venta de algodón en oro" },
                    { 492, "46900", true, 485, "Venta al por mayor de otros productos" },
                    { 493, "46901", true, 485, "Venta al por mayor de cohetes y otros productos pirotécnicos" },
                    { 494, "46902", true, 485, "Venta al por mayor de artículos diversos para consumo humano" },
                    { 495, "46903", true, 485, "Venta al por mayor de armas de fuego, municiones y accesorios" },
                    { 496, "46904", true, 485, "Venta al por mayor de toldos y tiendas de campaña de cualquier material" },
                    { 497, "46905", true, 485, "Venta al por mayor de exhibidores publicitarios y rótulos" },
                    { 498, "46906", true, 485, "Venta al por mayor de artículos promocionales diversos" },
                    { 500, "47111", true, 499, "Venta en supermercados" },
                    { 501, "47112", true, 499, "Venta en tiendas de artículos de primera necesidad" },
                    { 502, "47119", true, 499, "Almacenes (venta de diversos artículos)" },
                    { 503, "47120", true, 499, "Almacenes (venta de diversos artículos), y venta de vehículos automotores y motocicletas" },
                    { 504, "47190", true, 499, "Venta al por menor de otros productos en comercios no especializados" },
                    { 505, "47199", true, 499, "Venta de establecimientos no especializados con surtido compuesto principalmente de alimentos, bebidas y tabaco" },
                    { 506, "47211", true, 499, "Venta al por menor de frutas y hortalizas" },
                    { 507, "47212", true, 499, "Venta al por menor de carnes, embutidos y productos de granja" },
                    { 508, "47213", true, 499, "Venta al por menor de pescado y mariscos" },
                    { 509, "47214", true, 499, "Venta al por menor de productos lacteos" },
                    { 510, "47215", true, 499, "Venta al por menor de productos de panaderia, repostería y galletas" },
                    { 511, "47216", true, 499, "Venta al por menor de huevos" },
                    { 512, "47217", true, 499, "Venta al por menor de carnes y productos cárnicos" },
                    { 513, "47218", true, 499, "Venta al por menor de granos básicos y otros" },
                    { 514, "47219", true, 499, "Venta al por menor de alimentos n.c.p." },
                    { 515, "47221", true, 499, "Venta al por menor de hielo" },
                    { 516, "47223", true, 499, "Venta de bebidas no alcohólicas, para su consumo fuera del establecimiento" },
                    { 517, "47224", true, 499, "Venta de bebidas alcohólicas, para su consumo fuera del establecimiento" },
                    { 518, "47225", true, 499, "Venta de bebidas alcohólicas para su consumo dentro del establecimiento" },
                    { 519, "47230", true, 499, "Venta al por menor de tabaco" },
                    { 520, "47300", true, 499, "Venta de combustibles, lubricantes y otros (gasolineras)" },
                    { 521, "47411", true, 499, "Venta al por menor de computadoras y equipo periférico" },
                    { 522, "47412", true, 499, "Venta de equipo y accesorios de telecomunicación" },
                    { 523, "47420", true, 499, "Venta al por menor de equipo de audio y video" },
                    { 524, "47510", true, 499, "Venta al por menor de hilados, tejidos y productos textiles de mercería; confecciones para el hogar y textiles n.c.p." },
                    { 525, "47521", true, 499, "Venta al por menor de productos de madera" },
                    { 526, "47522", true, 499, "Venta al por menor de artículos de ferretería" },
                    { 527, "47523", true, 499, "Venta al por menor de productos de pinturerías" },
                    { 528, "47524", true, 499, "Venta al por menor en vidrierías" },
                    { 529, "47529", true, 499, "Venta al por menor de materiales de construcción y artículos conexos" },
                    { 530, "47530", true, 499, "Venta al por menor de tapices, alfombras y revestimientos de paredes y pisos en comercios especializados" },
                    { 531, "47591", true, 499, "Venta al por menor de muebles" },
                    { 532, "47592", true, 499, "Venta al por menor de artículos de bazar" },
                    { 533, "47593", true, 499, "Venta al por menor de aparatos electrodomésticos, repuestos y accesorios" },
                    { 534, "47594", true, 499, "Venta al por menor de artículos eléctricos y de iluminación" },
                    { 535, "47598", true, 499, "Venta al por menor de instrumentos musicales" },
                    { 536, "47610", true, 499, "Venta al por menor de libros, periódicos y artículos de papelería en comercios especializados" },
                    { 537, "47620", true, 499, "Venta al por menor de discos láser, cassettes, cintas de video y otros" },
                    { 538, "47630", true, 499, "Venta al por menor de productos y equipos de deporte" },
                    { 539, "47631", true, 499, "Venta al por menor de bicicletas, accesorios y repuestos" },
                    { 540, "47640", true, 499, "Venta al por menor de juegos y juguetes en comercios especializados" },
                    { 541, "47711", true, 499, "Venta al por menor de prendas de vestir y accesorios de vestir" },
                    { 542, "47712", true, 499, "Venta al por menor de calzado" },
                    { 543, "47713", true, 499, "Venta al por menor de artículos de peletería, marroquinería y talabartería" },
                    { 544, "47721", true, 499, "Venta al por menor de medicamentos farmacéuticos y otros materiales y artículos de uso médico, odontológico y veterinario" },
                    { 545, "47722", true, 499, "Venta al por menor de productos cosméticos y de tocador" },
                    { 546, "47731", true, 499, "Venta al por menor de productos de joyería, bisutería, óptica, relojería" },
                    { 547, "47732", true, 499, "Venta al por menor de plantas, semillas, animales y artículos conexos" },
                    { 548, "47733", true, 499, "Venta al por menor de combustibles de uso doméstico (gas propano y gas licuado)" },
                    { 549, "47734", true, 499, "Venta al por menor de artesanías, artículos cerámicos y recuerdos en general" },
                    { 550, "47735", true, 499, "Venta al por menor de ataúdes, lápidas y cruces, trofeos, artículos religiosos en general" },
                    { 551, "47736", true, 499, "Venta al por menor de armas de fuego, municiones y accesorios" },
                    { 552, "47737", true, 499, "Venta al por menor de artículos de cohetería y pirotécnicos" },
                    { 553, "47738", true, 499, "Venta al por menor de artículos desechables de uso personal y doméstico (servilletas, papel higiénico, pañales, toallas sanitarias, etc.)" },
                    { 554, "47739", true, 499, "Venta al por menor de otros productos n.c.p." },
                    { 555, "47741", true, 499, "Venta al por menor de artículos usados" },
                    { 556, "47742", true, 499, "Venta al por menor de textiles y confecciones usados" },
                    { 557, "47743", true, 499, "Venta al por menor de libros, revistas, papel y cartón usados" },
                    { 558, "47749", true, 499, "Venta al por menor de productos usados n.c.p." },
                    { 559, "47811", true, 499, "Venta al por menor de frutas, verduras y hortalizas" },
                    { 560, "47814", true, 499, "Venta al por menor de productos lácteos" },
                    { 561, "47815", true, 499, "Venta al por menor de productos de panadería, galletas y similares" },
                    { 562, "47816", true, 499, "Venta al por menor de bebidas" },
                    { 563, "47818", true, 499, "Venta al por menor en tiendas de mercado y puestos" },
                    { 564, "47821", true, 499, "Venta al por menor de hilados, tejidos y productos textiles de mercería en puestos de mercados y ferias" },
                    { 565, "47822", true, 499, "Venta al por menor de artículos textiles excepto confecciones para el hogar en puestos de mercados y ferias" },
                    { 566, "47823", true, 499, "Venta al por menor de confecciones textiles para el hogar en puestos de mercados y ferias" },
                    { 567, "47824", true, 499, "Venta al por menor de prendas de vestir, accesorios de vestir y similares en puestos de mercados y ferias" },
                    { 568, "47825", true, 499, "Venta al por menor de ropa usada" },
                    { 569, "47826", true, 499, "Venta al por menor de calzado, artículos de marroquinería y talabartería en puestos de mercados y ferias" },
                    { 570, "47827", true, 499, "Venta al por menor de artículos de marroquinería y talabartería en puestos de mercados y ferias" },
                    { 571, "47829", true, 499, "Venta al por menor de artículos textiles ncp en puestos de mercados y ferias" },
                    { 572, "47891", true, 499, "Venta al por menor de animales, flores y productos conexos en puestos de feria y mercados" },
                    { 573, "47892", true, 499, "Venta al por menor de productos medicinales, cosméticos, de tocador y de limpieza en puestos de ferias y mercados" },
                    { 574, "47893", true, 499, "Venta al por menor de artículos de bazar en puestos de ferias y mercados" },
                    { 575, "47894", true, 499, "Venta al por menor de artículos de papel, envases, libros, revistas y conexos en puestos de feria y mercados" },
                    { 576, "47895", true, 499, "Venta al por menor de materiales de construcción, electrodomésticos, accesorios para autos y similares en puestos de feria y mercados" },
                    { 577, "47896", true, 499, "Venta al por menor de equipos accesorios para las comunicaciones en puestos de feria y mercados" },
                    { 578, "47899", true, 499, "Venta al por menor en puestos de ferias y mercados n.c.p." },
                    { 579, "47910", true, 499, "Venta al por menor por correo o Internet" },
                    { 580, "47990", true, 499, "Otros tipos de venta al por menor no realizada, en almacenes, puestos de venta o mercado" },
                    { 583, "49110", true, 582, "Transporte interurbano de pasajeros por ferrocarril" },
                    { 584, "49120", true, 582, "Transporte de carga por ferrocarril" },
                    { 585, "49211", true, 582, "Transporte de pasajeros urbanos e interurbano mediante buses" },
                    { 586, "49212", true, 582, "Transporte de pasajeros interdepartamental mediante microbuses" },
                    { 587, "49213", true, 582, "Transporte de pasajeros urbanos e interurbano mediante microbuses" },
                    { 588, "49214", true, 582, "Transporte de pasajeros interdepartamental mediante buses" },
                    { 589, "49221", true, 582, "Transporte internacional de pasajeros" },
                    { 590, "49222", true, 582, "Transporte de pasajeros mediante taxis y autos con chofer" },
                    { 591, "49223", true, 582, "Transporte escolar" },
                    { 592, "49225", true, 582, "Transporte de pasajeros para excursiones" },
                    { 593, "49226", true, 582, "Servicios de transporte de personal" },
                    { 594, "49229", true, 582, "Transporte de pasajeros por vía terrestre ncp" },
                    { 595, "49231", true, 582, "Transporte de carga urbano" },
                    { 596, "49232", true, 582, "Transporte nacional de carga" },
                    { 597, "49233", true, 582, "Transporte de carga internacional" },
                    { 598, "49234", true, 582, "Servicios de mudanza" },
                    { 599, "49235", true, 582, "Alquiler de vehículos de carga con conductor" },
                    { 600, "49300", true, 582, "Transporte por oleoducto o gasoducto" },
                    { 602, "50110", true, 601, "Transporte de pasajeros marítimo y de cabotaje" },
                    { 603, "50120", true, 601, "Transporte de carga marítimo y de cabotaje" },
                    { 604, "50211", true, 601, "Transporte de pasajeros por vías de navegación interiores" },
                    { 605, "50212", true, 601, "Alquiler de equipo de transporte de pasajeros por vías de navegación interior con conductor" },
                    { 606, "50220", true, 601, "Transporte de carga por vías de navegación interiores" },
                    { 608, "51100", true, 607, "Transporte aéreo de pasajeros" },
                    { 609, "51201", true, 607, "Transporte de carga por vía aérea" },
                    { 610, "51202", true, 607, "Alquiler de equipo de aerotransporte con operadores para el propósito de transportar carga" },
                    { 612, "52101", true, 611, "Alquiler de instalaciones de almacenamiento en zonas francas" },
                    { 613, "52102", true, 611, "Alquiler de silos para conservación y almacenamiento de granos" },
                    { 614, "52103", true, 611, "Alquiler de instalaciones con refrigeración para almacenamiento y conservación de alimentos y otros productos" },
                    { 615, "52109", true, 611, "Alquiler de bodegas para almacenamiento y depósito n.c.p." },
                    { 616, "52211", true, 611, "Servicio de garaje y estacionamiento" },
                    { 617, "52212", true, 611, "Servicios de terminales para el transporte por vía terrestre" },
                    { 618, "52219", true, 611, "Servicios para el transporte por vía terrestre n.c.p." },
                    { 619, "52220", true, 611, "Servicios para el transporte acuático" },
                    { 620, "52230", true, 611, "Servicios para el transporte aéreo" },
                    { 621, "52240", true, 611, "Manipulación de carga" },
                    { 622, "52290", true, 611, "Servicios para el transporte ncp" },
                    { 623, "52291", true, 611, "Agencias de tramitaciones aduanales" },
                    { 625, "53100", true, 624, "Servicios de correo nacional" },
                    { 626, "53200", true, 624, "Actividades de correo distintas a las actividades postales nacionales" },
                    { 627, "53201", true, 624, "Agencia privada de correo y encomiendas" },
                    { 630, "55101", true, 629, "Actividades de alojamiento para estancias cortas" },
                    { 631, "55102", true, 629, "Hoteles" },
                    { 632, "55200", true, 629, "Actividades de campamentos, parques de vehículos de recreo y parques de caravanas" },
                    { 633, "55900", true, 629, "Alojamiento n.c.p." },
                    { 635, "56101", true, 634, "Restaurantes" },
                    { 636, "56106", true, 634, "Pupusería" },
                    { 637, "56107", true, 634, "Actividades varias de restaurantes" },
                    { 638, "56108", true, 634, "Comedores" },
                    { 639, "56109", true, 634, "Merenderos ambulantes" },
                    { 640, "56210", true, 634, "Preparación de comida para eventos especiales" },
                    { 641, "56291", true, 634, "Servicios de provisión de comidas por contrato" },
                    { 642, "56292", true, 634, "Servicios de concesión de cafetines y chalet en empresas e instituciones" },
                    { 643, "56299", true, 634, "Servicios de preparación de comidas ncp" },
                    { 644, "56301", true, 634, "Servicio de expendio de bebidas en salones y bares" },
                    { 645, "56302", true, 634, "Servicio de expendio de bebidas en puestos callejeros, mercados y ferias" },
                    { 648, "58110", true, 647, "Edición de libros, folletos, partituras y otras ediciones distintas a estas" },
                    { 649, "58120", true, 647, "Edición de directorios y listas de correos" },
                    { 650, "58130", true, 647, "Edición de periódicos, revistas y otras publicaciones periódicas" },
                    { 651, "58190", true, 647, "Otras actividades de edición" },
                    { 652, "58200", true, 647, "Edición de programas informáticos (software)" },
                    { 654, "59110", true, 653, "Actividades de producción cinematográfica" },
                    { 655, "59120", true, 653, "Actividades de post producción de películas, videos y programas de televisión" },
                    { 656, "59130", true, 653, "Actividades de distribución de películas cinematográficas, videos y programas de televisión" },
                    { 657, "59140", true, 653, "Actividades de exhibición de películas cinematográficas y cintas de vídeo" },
                    { 658, "59200", true, 653, "Actividades de edición y grabación de música" },
                    { 660, "60100", true, 659, "Servicios de difusiones de radio" },
                    { 661, "60201", true, 659, "Actividades de programación y difusión de televisión abierta" },
                    { 662, "60202", true, 659, "Actividades de suscripción y difusión de televisión por cable y/o suscripción" },
                    { 663, "60299", true, 659, "Servicios de televisión, incluye televisión por cable" },
                    { 664, "60900", true, 659, "Programación y transmisión de radio y televisión" },
                    { 666, "61101", true, 665, "Servicio de telefonía" },
                    { 667, "61102", true, 665, "Servicio de Internet" },
                    { 668, "61103", true, 665, "Servicio de telefonía fija" },
                    { 669, "61109", true, 665, "Servicio de Internet n.c.p." },
                    { 670, "61201", true, 665, "Servicios de telefonía celular" },
                    { 671, "61202", true, 665, "Servicios de Internet inalámbrico" },
                    { 672, "61209", true, 665, "Servicios de telecomunicaciones inalámbrico n.c.p." },
                    { 673, "61301", true, 665, "Telecomunicaciones satelitales" },
                    { 674, "61309", true, 665, "Comunicación vía satélite n.c.p." },
                    { 675, "61900", true, 665, "Actividades de telecomunicación n.c.p." },
                    { 677, "62010", true, 676, "Programación Informática" },
                    { 678, "62020", true, 676, "Consultorías y gestión de servicios informáticos" },
                    { 679, "62090", true, 676, "Otras actividades de tecnología de información y servicios de computadora" },
                    { 681, "63110", true, 680, "Procesamiento de datos y Actividades relacionadas" },
                    { 682, "63120", true, 680, "Portales WEB" },
                    { 683, "63910", true, 680, "Servicios de Agencias de Noticias" },
                    { 684, "63990", true, 680, "Otros servicios de información n.c.p." },
                    { 687, "64110", true, 686, "Servicios provistos por el Banco Central de El Salvador" },
                    { 688, "64190", true, 686, "Bancos" },
                    { 689, "64192", true, 686, "Entidades dedicadas al envío de remesas" },
                    { 690, "64199", true, 686, "Otras entidades financieras" },
                    { 691, "64200", true, 686, "Actividades de sociedades de cartera" },
                    { 692, "64300", true, 686, "Fideicomisos, fondos y otras fuentes de financiamiento" },
                    { 693, "64910", true, 686, "Arrendamientos financieros" },
                    { 694, "64920", true, 686, "Asociaciones cooperativas de ahorro y crédito dedicadas a la intermediación financiera" },
                    { 695, "64921", true, 686, "Instituciones emisoras de tarjetas de crédito y otros" },
                    { 696, "64922", true, 686, "Tipos de crédito ncp" },
                    { 697, "64928", true, 686, "Prestamistas y casas de empeño" },
                    { 698, "64990", true, 686, "Actividades de servicios financieros, excepto la financiación de planes de seguros y de pensiones n.c.p." },
                    { 700, "65110", true, 699, "Planes de seguros de vida" },
                    { 701, "65120", true, 699, "Planes de seguro excepto de vida" },
                    { 702, "65199", true, 699, "Seguros generales de todo tipo" },
                    { 703, "65200", true, 699, "Planes se seguro" },
                    { 704, "65300", true, 699, "Planes de pensiones" },
                    { 706, "66110", true, 705, "Administración de mercados financieros (Bolsa de Valores)" },
                    { 707, "66120", true, 705, "Actividades bursátiles (Corredores de Bolsa)" },
                    { 708, "66190", true, 705, "Actividades auxiliares de la intermediación financiera ncp" },
                    { 709, "66210", true, 705, "Evaluación de riesgos y daños" },
                    { 710, "66220", true, 705, "Actividades de agentes y corredores de seguros" },
                    { 711, "66290", true, 705, "Otras actividades auxiliares de seguros y fondos de pensiones" },
                    { 712, "66300", true, 705, "Actividades de administración de fondos" },
                    { 715, "68101", true, 714, "Servicio de alquiler y venta de lotes en cementerios" },
                    { 716, "68109", true, 714, "Actividades inmobiliarias realizadas con bienes propios o arrendados n.c.p." },
                    { 717, "68200", true, 714, "Actividades Inmobiliarias Realizadas a Cambio de una Retribución o por Contrata" },
                    { 720, "69100", true, 719, "Actividades jurídicas" },
                    { 721, "69200", true, 719, "Actividades de contabilidad, teneduría de libros y auditoría; asesoramiento en materia de impuestos" },
                    { 723, "70100", true, 722, "Actividades de oficinas centrales de sociedades de cartera" },
                    { 724, "70200", true, 722, "Actividades de consultoría en gestión empresarial" },
                    { 726, "71101", true, 725, "Servicios de arquitectura y planificación urbana y servicios conexos" },
                    { 727, "71102", true, 725, "Servicios de ingeniería" },
                    { 728, "71103", true, 725, "Servicios de agrimensura, topografía, cartografía, prospección y geofísica y servicios conexos" },
                    { 729, "71200", true, 725, "Ensayos y análisis técnicos" },
                    { 731, "72100", true, 730, "Investigaciones y desarrollo experimental en el campo de las ciencias naturales y la ingeniería" },
                    { 732, "72199", true, 730, "Investigaciones científicas" },
                    { 733, "72200", true, 730, "Investigaciones y desarrollo experimental en el campo de las ciencias sociales y las humanidades científica y desarrollo" },
                    { 735, "73100", true, 734, "Publicidad" },
                    { 736, "73200", true, 734, "Investigación de mercados y realización de encuestas de opinión pública" },
                    { 738, "74100", true, 737, "Actividades de diseño especializado" },
                    { 739, "74200", true, 737, "Actividades de fotografía" },
                    { 740, "74900", true, 737, "Servicios profesionales y científicos ncp" },
                    { 742, "75000", true, 741, "Actividades veterinarias" },
                    { 745, "77101", true, 744, "Alquiler de equipo de transporte terrestre" },
                    { 746, "77102", true, 744, "Alquiler de equipo de transporte acuático" },
                    { 747, "77103", true, 744, "Alquiler de equipo de transporte por vía aérea" },
                    { 748, "77210", true, 744, "Alquiler y arrendamiento de equipo de recreo y deportivo" },
                    { 749, "77220", true, 744, "Alquiler de cintas de video y discos" },
                    { 750, "77290", true, 744, "Alquiler de otros efectos personales y enseres domésticos" },
                    { 751, "77300", true, 744, "Alquiler de maquinaria y equipo" },
                    { 752, "77400", true, 744, "Arrendamiento de productos de propiedad intelectual" },
                    { 754, "78100", true, 753, "Obtención y dotación de personal" },
                    { 755, "78200", true, 753, "Actividades de las agencias de trabajo temporal" },
                    { 756, "78300", true, 753, "Dotación de recursos humanos y gestión; gestión de las funciones de recursos humanos" },
                    { 758, "79110", true, 757, "Actividades de agencias de viajes y organizadores de viajes; actividades de asistencia a turistas" },
                    { 759, "79120", true, 757, "Actividades de los operadores turísticos" },
                    { 760, "79900", true, 757, "Otros servicios de reservas y actividades relacionadas" },
                    { 762, "80100", true, 761, "Servicios de seguridad privados" },
                    { 763, "80201", true, 761, "Actividades de servicios de sistemas de seguridad" },
                    { 764, "80202", true, 761, "Actividades para la prestación de sistemas de seguridad" },
                    { 765, "80300", true, 761, "Actividades de investigación" },
                    { 767, "81100", true, 766, "Actividades combinadas de mantenimiento de edificios e instalaciones" },
                    { 768, "81210", true, 766, "Limpieza general de edificios" },
                    { 769, "81290", true, 766, "Otras actividades combinadas de mantenimiento de edificios e instalaciones ncp" },
                    { 770, "81300", true, 766, "Servicio de jardinería" },
                    { 772, "82110", true, 771, "Servicios administrativos de oficinas" },
                    { 773, "82190", true, 771, "Servicio de fotocopiado y similares, excepto en imprentas" },
                    { 774, "82200", true, 771, "Actividades de las centrales de llamadas (call center)" },
                    { 775, "82300", true, 771, "Organización de convenciones y ferias de negocios" },
                    { 776, "82910", true, 771, "Actividades de agencias de cobro y oficinas de crédito" },
                    { 777, "82921", true, 771, "Servicios de envase y empaque de productos alimenticios" },
                    { 778, "82922", true, 771, "Servicios de envase y empaque de productos medicinales" },
                    { 779, "82929", true, 771, "Servicio de envase y empaque ncp" },
                    { 780, "82990", true, 771, "Actividades de apoyo empresariales ncp" },
                    { 783, "84110", true, 782, "Actividades de la Administración Pública en general" },
                    { 784, "84111", true, 782, "Alcaldías Municipales" },
                    { 785, "84120", true, 782, "Regulación de las actividades de prestación de servicios sanitarios, educativos, culturales y otros servicios sociales, excepto seguridad social" },
                    { 786, "84130", true, 782, "Regulación y facilitación de la actividad económica" },
                    { 787, "84210", true, 782, "Actividades de administración y funcionamiento del Ministerio de Relaciones Exteriores" },
                    { 788, "84220", true, 782, "Actividades de defensa" },
                    { 789, "84230", true, 782, "Actividades de mantenimiento del orden público y de seguridad" },
                    { 790, "84300", true, 782, "Actividades de planes de seguridad social de afiliación obligatoria" },
                    { 792, "85101", true, 791, "Guardería educativa" },
                    { 793, "85102", true, 791, "Enseñanza preescolar o parvularia" },
                    { 794, "85103", true, 791, "Enseñanza primaria" },
                    { 795, "85104", true, 791, "Servicio de educación preescolar y primaria integrada" },
                    { 796, "85211", true, 791, "Enseñanza secundaria tercer ciclo (7°, 8° y 9°)" },
                    { 797, "85212", true, 791, "Enseñanza secundaria de formación general bachillerato" },
                    { 798, "85221", true, 791, "Enseñanza secundaria de formación técnica y profesional" },
                    { 799, "85222", true, 791, "Enseñanza secundaria de formación técnica y profesional integrada con enseñanza primaria" },
                    { 800, "85301", true, 791, "Enseñanza superior universitaria" },
                    { 801, "85302", true, 791, "Enseñanza superior no universitaria" },
                    { 802, "85303", true, 791, "Enseñanza superior integrada a educación secundaria y/o primaria" },
                    { 803, "85410", true, 791, "Educación deportiva y recreativa" },
                    { 804, "85420", true, 791, "Educación cultural" },
                    { 805, "85490", true, 791, "Otros tipos de enseñanza n.c.p." },
                    { 806, "85499", true, 791, "Enseñanza formal" },
                    { 807, "85500", true, 791, "Servicios de apoyo a la enseñanza" },
                    { 810, "86100", true, 809, "Actividades de hospitales" },
                    { 811, "86201", true, 809, "Clínicas médicas" },
                    { 812, "86202", true, 809, "Servicios de Odontología" },
                    { 813, "86203", true, 809, "Servicios médicos" },
                    { 814, "86901", true, 809, "Servicios de análisis y estudios de diagnóstico" },
                    { 815, "86902", true, 809, "Actividades de atención de la salud humana" },
                    { 816, "86909", true, 809, "Otros Servicio relacionados con la salud ncp" },
                    { 818, "87100", true, 817, "Residencias de ancianos con atención de enfermería" },
                    { 819, "87200", true, 817, "Instituciones dedicadas al tratamiento del retraso mental, problemas de salud mental y el uso indebido de sustancias nocivas" },
                    { 820, "87300", true, 817, "Instituciones dedicadas al cuidado de ancianos y discapacitados" },
                    { 821, "87900", true, 817, "Actividades de asistencia a niños y jóvenes" },
                    { 822, "87901", true, 817, "Otras actividades de atención en instituciones" },
                    { 824, "88100", true, 823, "Actividades de asistencia sociales sin alojamiento para ancianos y discapacitados" },
                    { 825, "88900", true, 823, "servicios sociales sin alojamiento ncp" },
                    { 828, "90000", true, 827, "Actividades creativas artísticas y de esparcimiento" },
                    { 830, "91010", true, 829, "Actividades de bibliotecas y archivos" },
                    { 831, "91020", true, 829, "Actividades de museos y preservación de lugares y edificios históricos" },
                    { 832, "91030", true, 829, "Actividades de jardines botánicos, zoológicos y de reservas naturales" },
                    { 834, "92000", true, 833, "Actividades de juegos y apuestas" },
                    { 836, "93110", true, 835, "Gestión de instalaciones deportivas" },
                    { 837, "93120", true, 835, "Actividades de clubes deportivos" },
                    { 838, "93190", true, 835, "Otras actividades deportivas" },
                    { 839, "93210", true, 835, "Actividades de parques de atracciones y parques temáticos" },
                    { 840, "93291", true, 835, "Discotecas y salas de baile" },
                    { 841, "93298", true, 835, "Centros vacacionales" },
                    { 842, "93299", true, 835, "Actividades de esparcimiento ncp" },
                    { 845, "94110", true, 844, "Actividades de organizaciones empresariales y de empleadores" },
                    { 846, "94120", true, 844, "Actividades de organizaciones profesionales" },
                    { 847, "94200", true, 844, "Actividades de sindicatos" },
                    { 848, "94910", true, 844, "Actividades de organizaciones religiosas" },
                    { 849, "94920", true, 844, "Actividades de organizaciones politicas" },
                    { 850, "94990", true, 844, "Actividades de asociaciones n.c.p." },
                    { 852, "95110", true, 851, "Reparacion de computadoras y equipo periferico" },
                    { 853, "95120", true, 851, "Reparacion de equipo de comunicacion" },
                    { 854, "95210", true, 851, "Reparacion de aparatos electronicos de consumo" },
                    { 855, "95220", true, 851, "Reparacion de aparatos domestico y equipo de hogar y jardin" },
                    { 856, "95230", true, 851, "Reparacion de calzado y articulos de cuero" },
                    { 857, "95240", true, 851, "Reparacion de muebles y accesorios para el hogar" },
                    { 858, "95291", true, 851, "Reparacion de Instrumentos musicales" },
                    { 859, "95292", true, 851, "Servicios de cerrajeria y copiado de llaves" },
                    { 860, "95293", true, 851, "Reparacion de joyas y relojes" },
                    { 861, "95294", true, 851, "Reparacion de bicicletas, sillas de ruedas y rodados n.c.p." },
                    { 862, "95299", true, 851, "Reparaciones de enseres personales n.c.p." },
                    { 864, "96010", true, 863, "Lavado y limpieza de prendas de tela y de piel, incluso la limpieza en seco" },
                    { 865, "96020", true, 863, "Peluqueria y otros tratamientos de belleza" },
                    { 866, "96030", true, 863, "Pompas funebres y actividades conexas" },
                    { 867, "96091", true, 863, "Servicios de sauna y otros servicios para la estetica corporal n.c.p." },
                    { 868, "96092", true, 863, "Servicios n.c.p." },
                    { 871, "97000", true, 870, "Actividad de los hogares en calidad de empleadores de personal domestico" },
                    { 873, "98100", true, 872, "Actividades indiferenciadas de produccion de bienes de los hogares privados para uso propio" },
                    { 874, "98200", true, 872, "Actividades indiferenciadas de produccion de servicios de los hogares privados para uso propio" },
                    { 877, "99000", true, 876, "Actividades de organizaciones y organos extraterritoriales" },
                    { 880, "10001", true, 879, "Empleados" },
                    { 881, "10002", true, 879, "Pensionado" },
                    { 882, "10003", true, 879, "Estudiante" },
                    { 883, "10004", true, 879, "Desempleado" },
                    { 884, "10005", true, 879, "Otros" },
                    { 885, "10006", true, 879, "Comerciante" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_actividades_economicas_PadreId",
                table: "cat_actividades_economicas",
                column: "PadreId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_CatTipoItemId",
                table: "FacturaDetalles",
                column: "CatTipoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_CatUnidadMedidaId",
                table: "FacturaDetalles",
                column: "CatUnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_FacturaId",
                table: "FacturaDetalles",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPagos_CatFormaPagoId",
                table: "FacturaPagos",
                column: "CatFormaPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPagos_FacturaId",
                table: "FacturaPagos",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CatTipoDocumentoId",
                table: "Facturas",
                column: "CatTipoDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_EmisorId",
                table: "Facturas",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_ReceptorId",
                table: "Facturas",
                column: "ReceptorId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaTributos_CatTributoId",
                table: "FacturaTributos",
                column: "CatTributoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaTributos_FacturaId",
                table: "FacturaTributos",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_permisos_Slug",
                table: "permisos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receptor_EmisorId",
                table: "Receptor",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_permisos_PermisoId",
                table: "roles_permisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_EmisorId",
                table: "usuarios",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_RolId",
                table: "usuarios",
                column: "RolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_actividades_economicas");

            migrationBuilder.DropTable(
                name: "cat_ambiente_destino");

            migrationBuilder.DropTable(
                name: "cat_condicion_operacion");

            migrationBuilder.DropTable(
                name: "cat_departamento");

            migrationBuilder.DropTable(
                name: "cat_modelo_fac");

            migrationBuilder.DropTable(
                name: "cat_municipio");

            migrationBuilder.DropTable(
                name: "cat_otros_documentos_asociados");

            migrationBuilder.DropTable(
                name: "cat_plazo");

            migrationBuilder.DropTable(
                name: "cat_tipo_contingencia");

            migrationBuilder.DropTable(
                name: "cat_tipo_documento_contingencia");

            migrationBuilder.DropTable(
                name: "cat_tipo_documento_identificacion_receptor");

            migrationBuilder.DropTable(
                name: "cat_tipo_establecimiento");

            migrationBuilder.DropTable(
                name: "cat_tipo_generacion");

            migrationBuilder.DropTable(
                name: "cat_tipo_invalidacion");

            migrationBuilder.DropTable(
                name: "cat_tipo_servicio");

            migrationBuilder.DropTable(
                name: "cat_tipo_transmision");

            migrationBuilder.DropTable(
                name: "FacturaDetalles");

            migrationBuilder.DropTable(
                name: "FacturaPagos");

            migrationBuilder.DropTable(
                name: "FacturaTributos");

            migrationBuilder.DropTable(
                name: "roles_permisos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "cat_tipo_item");

            migrationBuilder.DropTable(
                name: "cat_uni_medida");

            migrationBuilder.DropTable(
                name: "cat_forma_pago");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DropTable(
                name: "cat_tributos");

            migrationBuilder.DropTable(
                name: "permisos");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "Receptor");

            migrationBuilder.DropTable(
                name: "cat_tipo_doc");

            migrationBuilder.DropTable(
                name: "emisores");
        }
    }
}

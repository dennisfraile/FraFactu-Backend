using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatDistrito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "cat_distrito",
                columns: new[] { "Id", "Codigo", "CodigoDepartamento", "CodigoMunicipio", "Valor" },
                values: new object[,]
                {
                    { 1, "00", "00", "00", "Otro (Para extranjeros)" },
                    { 2, "01", "01", "14", "Ahuachapán" },
                    { 3, "02", "01", "14", "Apaneca" },
                    { 4, "03", "01", "13", "Atiquizaya" },
                    { 5, "04", "01", "14", "Concepción de Ataco" },
                    { 6, "05", "01", "13", "El Refugio" },
                    { 7, "06", "01", "15", "Guaymango" },
                    { 8, "07", "01", "15", "Jujutla" },
                    { 9, "08", "01", "15", "San Francisco Menéndez" },
                    { 10, "09", "01", "13", "San Lorenzo" },
                    { 11, "10", "01", "15", "San Pedro Puxtla" },
                    { 12, "11", "01", "14", "Tacuba" },
                    { 13, "12", "01", "13", "Turín" },
                    { 14, "01", "02", "17", "Candelaria de la Frontera" },
                    { 15, "02", "02", "16", "Coatepeque" },
                    { 16, "03", "02", "17", "Chalchuapa" },
                    { 17, "04", "02", "16", "El Congo" },
                    { 18, "05", "02", "17", "El Porvenir" },
                    { 19, "06", "02", "14", "Masahuat" },
                    { 20, "07", "02", "14", "Metapán" },
                    { 21, "08", "02", "17", "San Antonio Pajonal" },
                    { 22, "09", "02", "17", "San Sebastián Salitrillo" },
                    { 23, "10", "02", "15", "Santa Ana" },
                    { 24, "11", "02", "14", "Santa Rosa Guachipilín" },
                    { 25, "12", "02", "17", "Santiago de la Frontera" },
                    { 26, "13", "02", "14", "Texistepeque" },
                    { 27, "01", "03", "20", "Acajutla" },
                    { 28, "02", "03", "19", "Armenia" },
                    { 29, "03", "03", "19", "Caluco" },
                    { 30, "04", "03", "19", "Cuisnahuat" },
                    { 31, "05", "03", "19", "Santa Isabel Ishuatán" },
                    { 32, "06", "03", "19", "Izalco" },
                    { 33, "07", "03", "17", "Juayúa" },
                    { 34, "08", "03", "17", "Nahuizalco" },
                    { 35, "09", "03", "18", "Nahulingo" },
                    { 36, "10", "03", "17", "Salcoatitán" },
                    { 37, "11", "03", "18", "San Antonio del Monte" },
                    { 38, "12", "03", "19", "San Julián" },
                    { 39, "13", "03", "17", "Santa Catarina Masahuat" },
                    { 40, "14", "03", "18", "Santo Domingo de Guzmán" },
                    { 41, "15", "03", "18", "Sonsonate" },
                    { 42, "16", "03", "18", "Sonzacate" },
                    { 43, "01", "04", "35", "Agua Caliente" },
                    { 44, "02", "04", "36", "Arcatao" },
                    { 45, "03", "04", "36", "Azacualpa" },
                    { 46, "04", "04", "34", "Citalá" },
                    { 47, "05", "04", "36", "Comalapa" },
                    { 48, "06", "04", "36", "Concepción Quezaltepeque" },
                    { 49, "07", "04", "36", "Chalatenango" },
                    { 50, "08", "04", "35", "Dulce Nombre de María" },
                    { 51, "09", "04", "36", "El Carrizal" },
                    { 52, "10", "04", "35", "El Paraíso" },
                    { 53, "11", "04", "36", "La Laguna" },
                    { 54, "12", "04", "34", "La Palma" },
                    { 55, "13", "04", "35", "La Reina" },
                    { 56, "14", "04", "36", "Las Vueltas" },
                    { 57, "15", "04", "36", "Nombre de Jesús" },
                    { 58, "16", "04", "35", "Nueva Concepción" },
                    { 59, "17", "04", "36", "Nueva Trinidad" },
                    { 60, "18", "04", "36", "Ojos de Agua" },
                    { 61, "19", "04", "36", "Potonico" },
                    { 62, "20", "04", "36", "San Antonio de la Cruz" },
                    { 63, "21", "04", "36", "San Antonio Los Ranchos" },
                    { 64, "22", "04", "35", "San Fernando" },
                    { 65, "23", "04", "36", "San Francisco Lempa" },
                    { 66, "24", "04", "35", "San Francisco Morazán" },
                    { 67, "25", "04", "34", "San Ignacio" },
                    { 68, "26", "04", "36", "San Isidro Labrador" },
                    { 69, "27", "04", "36", "San José Cancasque" },
                    { 70, "28", "04", "36", "San José Las Flores" },
                    { 71, "29", "04", "36", "San Luis del Carmen" },
                    { 72, "30", "04", "36", "San Miguel de Mercedes" },
                    { 73, "31", "04", "35", "San Rafael" },
                    { 74, "32", "04", "35", "Santa Rita" },
                    { 75, "33", "04", "35", "Tejutla" },
                    { 76, "01", "05", "26", "Antiguo Cuscatlán" },
                    { 77, "02", "05", "24", "Ciudad Arce" },
                    { 78, "03", "05", "25", "Colón" },
                    { 79, "04", "05", "28", "Comasagua" },
                    { 80, "05", "05", "27", "Chiltiupán" },
                    { 81, "06", "05", "26", "Huizúcar" },
                    { 82, "07", "05", "25", "Jayaque" },
                    { 83, "08", "05", "27", "Jicalapa" },
                    { 84, "09", "05", "27", "La Libertad" },
                    { 85, "10", "05", "26", "Nuevo Cuscatlán" },
                    { 86, "11", "05", "28", "Santa Tecla" },
                    { 87, "12", "05", "23", "Quezaltepeque" },
                    { 88, "13", "05", "25", "Sacacoyo" },
                    { 89, "14", "05", "26", "San José Villanueva" },
                    { 90, "15", "05", "24", "San Juan Opico" },
                    { 91, "16", "05", "23", "San Matías" },
                    { 92, "17", "05", "23", "San Pablo Tacachico" },
                    { 93, "18", "05", "27", "Tamanique" },
                    { 94, "19", "05", "25", "Talnique" },
                    { 95, "20", "05", "27", "Teotepeque" },
                    { 96, "21", "05", "25", "Tepecoyo" },
                    { 97, "22", "05", "26", "Zaragoza" },
                    { 98, "01", "06", "20", "Aguilares" },
                    { 99, "02", "06", "21", "Apopa" },
                    { 100, "03", "06", "23", "Ayutuxtepeque" },
                    { 101, "04", "06", "23", "Cuscatancingo" },
                    { 102, "05", "06", "20", "El Paisnal" },
                    { 103, "06", "06", "20", "Guazapa" },
                    { 104, "07", "06", "22", "Ilopango" },
                    { 105, "08", "06", "23", "Mejicanos" },
                    { 106, "09", "06", "21", "Nejapa" },
                    { 107, "10", "06", "24", "Panchimalco" },
                    { 108, "11", "06", "24", "Rosario de Mora" },
                    { 109, "12", "06", "24", "San Marcos" },
                    { 110, "13", "06", "22", "San Martín" },
                    { 111, "14", "06", "23", "San Salvador" },
                    { 112, "15", "06", "24", "Santiago Texacuangos" },
                    { 113, "16", "06", "24", "Santo Tomás" },
                    { 114, "17", "06", "22", "Soyapango" },
                    { 115, "18", "06", "22", "Tonacatepeque" },
                    { 116, "19", "06", "23", "Ciudad Delgado" },
                    { 117, "01", "07", "18", "Candelaria" },
                    { 118, "02", "07", "18", "Cojutepeque" },
                    { 119, "03", "07", "18", "El Carmen" },
                    { 120, "04", "07", "18", "El Rosario" },
                    { 121, "05", "07", "18", "Monte San Juan" },
                    { 122, "06", "07", "17", "Oratorio de Concepción" },
                    { 123, "07", "07", "17", "San Bartolomé Perulapía" },
                    { 124, "08", "07", "18", "San Cristóbal" },
                    { 125, "09", "07", "17", "San José Guayabal" },
                    { 126, "10", "07", "17", "San Pedro Perulapán" },
                    { 127, "11", "07", "18", "San Rafael Cedros" },
                    { 128, "12", "07", "18", "San Ramón" },
                    { 129, "13", "07", "18", "Santa Cruz Analquito" },
                    { 130, "14", "07", "18", "Santa Cruz Michapa" },
                    { 131, "15", "07", "17", "Suchitoto" },
                    { 132, "16", "07", "18", "Tenancingo" },
                    { 133, "01", "08", "23", "Cuyultitán" },
                    { 134, "02", "08", "24", "El Rosario" },
                    { 135, "03", "08", "24", "Jerusalén" },
                    { 136, "04", "08", "24", "Mercedes La Ceiba" },
                    { 137, "05", "08", "23", "Olocuilta" },
                    { 138, "06", "08", "24", "Paraíso de Osorio" },
                    { 139, "07", "08", "24", "San Antonio Masahuat" },
                    { 140, "08", "08", "24", "San Emigdio" },
                    { 141, "09", "08", "23", "San Francisco Chinameca" },
                    { 142, "10", "08", "25", "San Juan Nonualco" },
                    { 143, "11", "08", "23", "San Juan Talpa" },
                    { 144, "12", "08", "24", "San Juan Tepezontes" },
                    { 145, "13", "08", "23", "San Luis Talpa" },
                    { 146, "14", "08", "24", "San Miguel Tepezontes" },
                    { 147, "15", "08", "23", "San Pedro Masahuat" },
                    { 148, "16", "08", "24", "San Pedro Nonualco" },
                    { 149, "17", "08", "25", "San Rafael Obrajuelo" },
                    { 150, "18", "08", "24", "Santa María Ostuma" },
                    { 151, "19", "08", "24", "Santiago Nonualco" },
                    { 152, "20", "08", "23", "Tapalhuaca" },
                    { 153, "21", "08", "25", "Zacatecoluca" },
                    { 154, "22", "08", "24", "San Luis La Herradura" },
                    { 155, "01", "09", "10", "Cinquera" },
                    { 156, "02", "09", "11", "Guacotecti" },
                    { 157, "03", "09", "10", "Ilobasco" },
                    { 158, "04", "09", "10", "Jutiapa" },
                    { 159, "05", "09", "11", "San Isidro" },
                    { 160, "06", "09", "11", "Sensuntepeque" },
                    { 161, "07", "09", "10", "Tejutepeque" },
                    { 162, "08", "09", "11", "Victoria" },
                    { 163, "09", "09", "11", "Villa Dolores" },
                    { 164, "01", "10", "14", "Apastepeque" },
                    { 165, "02", "10", "15", "Guadalupe" },
                    { 166, "03", "10", "15", "San Cayetano Istepeque" },
                    { 167, "04", "10", "14", "Santa Clara" },
                    { 168, "05", "10", "14", "Santo Domingo" },
                    { 169, "06", "10", "14", "San Esteban Catarina" },
                    { 170, "07", "10", "14", "San Ildefonso" },
                    { 171, "08", "10", "14", "San Lorenzo" },
                    { 172, "09", "10", "14", "San Sebastián" },
                    { 173, "10", "10", "15", "San Vicente" },
                    { 174, "11", "10", "15", "Tecoluca" },
                    { 175, "12", "10", "15", "Tepetitán" },
                    { 176, "13", "10", "15", "Verapaz" },
                    { 177, "01", "11", "24", "Alegría" },
                    { 178, "02", "11", "24", "Berlín" },
                    { 179, "03", "11", "25", "California" },
                    { 180, "04", "11", "25", "Concepción Batres" },
                    { 181, "05", "11", "24", "El Triunfo" },
                    { 182, "06", "11", "25", "Ereguayquín" },
                    { 183, "07", "11", "24", "Estanzuelas" },
                    { 184, "08", "11", "26", "Jiquilisco" },
                    { 185, "09", "11", "24", "Jucuapa" },
                    { 186, "10", "11", "25", "Jucuarán" },
                    { 187, "11", "11", "24", "Mercedes Umaña" },
                    { 188, "12", "11", "24", "Nueva Granada" },
                    { 189, "13", "11", "25", "Ozatlán" },
                    { 190, "14", "11", "26", "Puerto El Triunfo" },
                    { 191, "15", "11", "26", "San Agustín" },
                    { 192, "16", "11", "24", "San Buenaventura" },
                    { 193, "17", "11", "25", "San Dionisio" },
                    { 194, "18", "11", "25", "Santa Elena" },
                    { 195, "19", "11", "26", "San Francisco Javier" },
                    { 196, "20", "11", "25", "Santa María" },
                    { 197, "21", "11", "24", "Santiago de María" },
                    { 198, "22", "11", "25", "Tecapán" },
                    { 199, "23", "11", "25", "Usulután" },
                    { 200, "01", "12", "21", "Carolina" },
                    { 201, "02", "12", "21", "Ciudad Barrios" },
                    { 202, "03", "12", "22", "Comacarán" },
                    { 203, "04", "12", "21", "Chapeltique" },
                    { 204, "05", "12", "23", "Chinameca" },
                    { 205, "06", "12", "22", "Chirilagua" },
                    { 206, "07", "12", "23", "El Tránsito" },
                    { 207, "08", "12", "23", "Lolotique" },
                    { 208, "09", "12", "22", "Moncagua" },
                    { 209, "10", "12", "23", "Nueva Guadalupe" },
                    { 210, "11", "12", "21", "Nuevo Edén de San Juan" },
                    { 211, "12", "12", "22", "Quelepa" },
                    { 212, "13", "12", "21", "San Antonio del Mosco" },
                    { 213, "14", "12", "21", "San Gerardo" },
                    { 214, "15", "12", "23", "San Jorge" },
                    { 215, "16", "12", "21", "San Luis de la Reina" },
                    { 216, "17", "12", "22", "San Miguel" },
                    { 217, "18", "12", "23", "San Rafael Oriente" },
                    { 218, "19", "12", "21", "Sesori" },
                    { 219, "20", "12", "22", "Uluazapa" },
                    { 220, "01", "13", "27", "Arambala" },
                    { 221, "02", "13", "27", "Cacaopera" },
                    { 222, "03", "13", "27", "Corinto" },
                    { 223, "04", "13", "28", "Chilanga" },
                    { 224, "05", "13", "28", "Delicias de Concepción" },
                    { 225, "06", "13", "28", "El Divisadero" },
                    { 226, "07", "13", "27", "El Rosario" },
                    { 227, "08", "13", "28", "Gualococti" },
                    { 228, "09", "13", "28", "Guatajiagua" },
                    { 229, "10", "13", "27", "Joateca" },
                    { 230, "11", "13", "27", "Jocoaitique" },
                    { 231, "12", "13", "28", "Jocoro" },
                    { 232, "13", "13", "28", "Lolotiquillo" },
                    { 233, "14", "13", "27", "Meanguera" },
                    { 234, "15", "13", "28", "Osicala" },
                    { 235, "16", "13", "27", "Perquín" },
                    { 236, "17", "13", "28", "San Carlos" },
                    { 237, "18", "13", "27", "San Fernando" },
                    { 238, "19", "13", "28", "San Francisco Gotera" },
                    { 239, "20", "13", "27", "San Isidro" },
                    { 240, "21", "13", "28", "San Simón" },
                    { 241, "22", "13", "28", "Sensembra" },
                    { 242, "23", "13", "28", "Sociedad" },
                    { 243, "24", "13", "27", "Torola" },
                    { 244, "25", "13", "28", "Yamabal" },
                    { 245, "26", "13", "28", "Yoloaiquín" },
                    { 246, "01", "14", "19", "Anamorós" },
                    { 247, "02", "14", "19", "Bolívar" },
                    { 248, "03", "14", "19", "Concepción de Oriente" },
                    { 249, "04", "14", "20", "Conchagua" },
                    { 250, "05", "14", "20", "El Carmen" },
                    { 251, "06", "14", "19", "El Sauce" },
                    { 252, "07", "14", "20", "Intipucá" },
                    { 253, "08", "14", "20", "La Unión" },
                    { 254, "09", "14", "19", "Lislique" },
                    { 255, "10", "14", "20", "Meanguera del Golfo" },
                    { 256, "11", "14", "19", "Nueva Esparta" },
                    { 257, "12", "14", "19", "Pasaquina" },
                    { 258, "13", "14", "19", "Polorós" },
                    { 259, "14", "14", "20", "San Alejo" },
                    { 260, "15", "14", "19", "San José La Fuente" },
                    { 261, "16", "14", "19", "Santa Rosa de Lima" },
                    { 262, "17", "14", "20", "Yayantique" },
                    { 263, "18", "14", "20", "Yucuaiquín" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 142);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 143);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 144);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 145);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 146);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 147);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 148);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 149);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 150);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 151);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 152);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 153);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 154);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 155);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 156);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 157);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 158);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 159);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 160);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 161);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 162);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 163);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 164);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 165);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 166);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 167);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 168);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 169);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 170);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 171);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 172);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 173);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 174);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 175);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 176);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 177);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 178);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 179);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 180);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 181);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 182);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 183);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 184);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 185);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 186);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 187);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 188);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 189);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 190);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 191);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 192);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 193);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 194);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 195);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 196);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 197);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 198);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 199);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 201);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 202);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 203);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 204);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 205);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 207);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 208);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 209);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 210);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 211);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 212);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 213);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 214);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 215);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 216);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 217);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 218);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 219);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 220);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 221);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 222);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 223);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 224);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 225);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 226);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 227);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 228);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 229);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 230);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 231);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 232);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 233);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 234);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 235);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 236);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 237);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 238);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 239);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 240);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 241);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 242);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 243);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 244);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 245);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 246);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 247);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 248);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 249);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 250);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 251);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 252);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 253);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 254);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 255);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 256);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 257);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 258);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 259);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 260);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 261);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 262);

            migrationBuilder.DeleteData(
                table: "cat_distrito",
                keyColumn: "Id",
                keyValue: 263);
        }
    }
}

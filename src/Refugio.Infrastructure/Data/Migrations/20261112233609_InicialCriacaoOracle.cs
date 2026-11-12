using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Refugio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InicialCriacaoOracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_RFG_REFUGIO",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    TIPO = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    CEP = table.Column<string>(type: "NVARCHAR2(9)", maxLength: 9, nullable: false),
                    ENDERECO = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    BAIRRO = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    CAPACIDADE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ABRE = table.Column<string>(type: "VARCHAR2(5)", nullable: false),
                    FECHA = table.Column<string>(type: "VARCHAR2(5)", nullable: false),
                    ACESSIVEL = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ATIVO = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_RFG_REFUGIO", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "T_RFG_USUARIO",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    SENHA_HASH = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    PAPEL = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    BAIRRO = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    TELEFONE = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    CONTATO_EMERGENCIA_NOME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    CONTATO_EMERGENCIA_TELEFONE = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    CRIADO_EM = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_RFG_USUARIO", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "T_RFG_RESERVA",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    IDOSO_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    REFUGIO_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    DATA_RESERVA = table.Column<DateTime>(type: "DATE", nullable: false),
                    STATUS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CRIADO_EM = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    CHAVE_IDEMPOTENCIA = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_RFG_RESERVA", x => x.ID);
                    table.ForeignKey(
                        name: "FK_T_RFG_RESERVA_REFUGIO",
                        column: x => x.REFUGIO_ID,
                        principalTable: "T_RFG_REFUGIO",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_T_RFG_RESERVA_USUARIO",
                        column: x => x.IDOSO_ID,
                        principalTable: "T_RFG_USUARIO",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_RFG_REFUGIO_BAIRRO",
                table: "T_RFG_REFUGIO",
                column: "BAIRRO");

            migrationBuilder.CreateIndex(
                name: "IX_T_RFG_RESERVA_IDOSO_ID_DATA_RESERVA",
                table: "T_RFG_RESERVA",
                columns: new[] { "IDOSO_ID", "DATA_RESERVA" });

            migrationBuilder.CreateIndex(
                name: "IX_T_RFG_RESERVA_REFUGIO_ID_DATA_RESERVA_STATUS",
                table: "T_RFG_RESERVA",
                columns: new[] { "REFUGIO_ID", "DATA_RESERVA", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "UX_T_RFG_RESERVA_IDOSO_ID_CHAVE_IDEMPOTENCIA",
                table: "T_RFG_RESERVA",
                columns: new[] { "IDOSO_ID", "CHAVE_IDEMPOTENCIA" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_T_RFG_USUARIO_EMAIL",
                table: "T_RFG_USUARIO",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_RFG_RESERVA");

            migrationBuilder.DropTable(
                name: "T_RFG_REFUGIO");

            migrationBuilder.DropTable(
                name: "T_RFG_USUARIO");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KUKULCAN.SharedKernel.i18n.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    NativeName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DefaultLanguageMarker = table.Column<int>(type: "int", nullable: true, computedColumnSql: "CASE WHEN `IsDefault` = 1 THEN 1 ELSE NULL END"),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(9)", maxLength: 9, nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Text = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    Context = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    IsReviewed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrencyFormats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    CurrencyName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    SymbolPosition = table.Column<int>(type: "int", nullable: false),
                    SpaceBetweenSymbolAndAmount = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DecimalSeparator = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false),
                    ThousandsSeparator = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    NegativePattern = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    LanguageId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyFormats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyFormats_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LocaleConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    DateFormat = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ShortDateFormat = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TimeFormat = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DateTimeFormat = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FirstDayOfWeek = table.Column<int>(type: "int", nullable: false),
                    DecimalSeparator = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false),
                    ThousandsSeparator = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    CurrencyDecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocaleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocaleConfigurations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyFormats_LanguageId",
                table: "CurrencyFormats",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "UX_CurrencyFormats_Language_Currency",
                table: "CurrencyFormats",
                columns: new[] { "LanguageCode", "CurrencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Languages_Code",
                table: "Languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Languages_Default",
                table: "Languages",
                column: "DefaultLanguageMarker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocaleConfigurations_LanguageId",
                table: "LocaleConfigurations",
                column: "LanguageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LocaleConfigurations_LanguageCode",
                table: "LocaleConfigurations",
                column: "LanguageCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Translations_LanguageCode",
                table: "Translations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "UX_Translations_Code_Language",
                table: "Translations",
                columns: new[] { "Code", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyFormats");

            migrationBuilder.DropTable(
                name: "LocaleConfigurations");

            migrationBuilder.DropTable(
                name: "Translations");

            migrationBuilder.DropTable(
                name: "Languages");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KUKULCAN.SharedKernel.i18n.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "i18n");

            migrationBuilder.CreateTable(
                name: "Languages",
                schema: "i18n",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NativeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Translations",
                schema: "i18n",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyFormats",
                schema: "i18n",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CurrencyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    SymbolPosition = table.Column<int>(type: "int", nullable: false),
                    SpaceBetweenSymbolAndAmount = table.Column<bool>(type: "bit", nullable: false),
                    DecimalSeparator = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    ThousandsSeparator = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    NegativePattern = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyFormats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyFormats_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "i18n",
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocaleConfigurations",
                schema: "i18n",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DateFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShortDateFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TimeFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateTimeFormat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstDayOfWeek = table.Column<int>(type: "int", nullable: false),
                    DecimalSeparator = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    ThousandsSeparator = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    CurrencyDecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocaleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocaleConfigurations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "i18n",
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyFormats_LanguageId",
                schema: "i18n",
                table: "CurrencyFormats",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "UX_CurrencyFormats_Language_Currency",
                schema: "i18n",
                table: "CurrencyFormats",
                columns: new[] { "LanguageCode", "CurrencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Languages_Code",
                schema: "i18n",
                table: "Languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Languages_Default",
                schema: "i18n",
                table: "Languages",
                column: "IsDefault",
                unique: true,
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_LocaleConfigurations_LanguageId",
                schema: "i18n",
                table: "LocaleConfigurations",
                column: "LanguageId",
                unique: true,
                filter: "[LanguageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_LocaleConfigurations_LanguageCode",
                schema: "i18n",
                table: "LocaleConfigurations",
                column: "LanguageCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Translations_LanguageCode",
                schema: "i18n",
                table: "Translations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "UX_Translations_Code_Language",
                schema: "i18n",
                table: "Translations",
                columns: new[] { "Code", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyFormats",
                schema: "i18n");

            migrationBuilder.DropTable(
                name: "LocaleConfigurations",
                schema: "i18n");

            migrationBuilder.DropTable(
                name: "Translations",
                schema: "i18n");

            migrationBuilder.DropTable(
                name: "Languages",
                schema: "i18n");
        }
    }
}

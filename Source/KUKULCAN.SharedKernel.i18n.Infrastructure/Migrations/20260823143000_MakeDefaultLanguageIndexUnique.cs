using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Migrations;

/// <inheritdoc />
public partial class MakeDefaultLanguageIndexUnique : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Languages_Default",
            schema: "i18n",
            table: "Languages");

        migrationBuilder.CreateIndex(
            name: "UX_Languages_Default",
            schema: "i18n",
            table: "Languages",
            column: "IsDefault",
            unique: true,
            filter: "\"IsDefault\" = true");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_Languages_Default",
            schema: "i18n",
            table: "Languages");

        migrationBuilder.CreateIndex(
            name: "IX_Languages_Default",
            schema: "i18n",
            table: "Languages",
            column: "IsDefault",
            filter: "\"IsDefault\" = true");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoodDeedsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedOrgId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "users",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "users");
        }
    }
}

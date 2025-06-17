using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledServiceDateTimeToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledServiceDateTime",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledServiceDateTime",
                table: "Orders");
        }
    }
}

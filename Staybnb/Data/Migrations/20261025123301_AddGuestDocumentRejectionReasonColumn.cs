using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staybnb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestDocumentRejectionReasonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "GuestDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "GuestDocuments");
        }
    }
}

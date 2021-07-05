using Microsoft.EntityFrameworkCore.Migrations;

namespace BackUpCollectionDAL.Migrations
{
    public partial class MoveMailSend : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isMailSend",
                table: "ServiceSettings");

            migrationBuilder.AddColumn<bool>(
                name: "isMailSend",
                table: "ADOConnectionStrings",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isMailSend",
                table: "ADOConnectionStrings");

            migrationBuilder.AddColumn<bool>(
                name: "isMailSend",
                table: "ServiceSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

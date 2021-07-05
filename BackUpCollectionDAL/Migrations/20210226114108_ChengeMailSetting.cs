using Microsoft.EntityFrameworkCore.Migrations;

namespace BackUpCollectionDAL.Migrations
{
    public partial class ChengeMailSetting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enable",
                table: "MailSettings");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "MailSettings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "MailSettings");

            migrationBuilder.AddColumn<bool>(
                name: "Enable",
                table: "MailSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

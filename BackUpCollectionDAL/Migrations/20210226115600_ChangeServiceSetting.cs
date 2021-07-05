using Microsoft.EntityFrameworkCore.Migrations;

namespace BackUpCollectionDAL.Migrations
{
    public partial class ChangeServiceSetting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isMailSend",
                table: "ServiceSettings",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isMailSend",
                table: "ServiceSettings");
        }
    }
}

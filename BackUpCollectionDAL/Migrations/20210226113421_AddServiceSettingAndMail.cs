using Microsoft.EntityFrameworkCore.Migrations;

namespace BackUpCollectionDAL.Migrations
{
    public partial class AddServiceSettingAndMail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADOConnectionStrings",
                columns: table => new
                {
                    ADOConnectionStringId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    ConnectionString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADOConnectionStrings", x => x.ADOConnectionStringId);
                });

            migrationBuilder.CreateTable(
                name: "MailSettings",
                columns: table => new
                {
                    MailSettingId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enable = table.Column<bool>(nullable: false),
                    Server = table.Column<string>(nullable: true),
                    ToAddress = table.Column<string>(nullable: true),
                    FromAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailSettings", x => x.MailSettingId);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSettings",
                columns: table => new
                {
                    ServiceSettingId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(nullable: true),
                    ADOConnectionStringId = table.Column<int>(nullable: false),
                    DelayMs = table.Column<int>(nullable: false),
                    UpdateDelayAndFrequency = table.Column<bool>(nullable: false),
                    Mode = table.Column<string>(nullable: true),
                    Type = table.Column<byte>(nullable: false),
                    HalfPolicies = table.Column<byte>(nullable: false),
                    MailSettingId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSettings", x => x.ServiceSettingId);
                    table.ForeignKey(
                        name: "FK_ServiceSettings_ADOConnectionStrings_ADOConnectionStringId",
                        column: x => x.ADOConnectionStringId,
                        principalTable: "ADOConnectionStrings",
                        principalColumn: "ADOConnectionStringId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceSettings_MailSettings_MailSettingId",
                        column: x => x.MailSettingId,
                        principalTable: "MailSettings",
                        principalColumn: "MailSettingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSettings_ADOConnectionStringId",
                table: "ServiceSettings",
                column: "ADOConnectionStringId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSettings_MailSettingId",
                table: "ServiceSettings",
                column: "MailSettingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceSettings");

            migrationBuilder.DropTable(
                name: "ADOConnectionStrings");

            migrationBuilder.DropTable(
                name: "MailSettings");
        }
    }
}
